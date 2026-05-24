using System.Security.Claims;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Infrastructure;
using LudoKing.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LudoKing.API.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameStateStore _gameStateStore;
    private readonly IGameEngine _gameEngine;
    private readonly IBotPlayerService _botPlayerService;
    private readonly ITurnTimerService _turnTimerService;
    private readonly IDisconnectWatchdog _disconnectWatchdog;

    public GameHub(
        IGameStateStore gameStateStore,
        IGameEngine gameEngine,
        IBotPlayerService botPlayerService,
        ITurnTimerService turnTimerService,
        IDisconnectWatchdog disconnectWatchdog)
    {
        _gameStateStore = gameStateStore;
        _gameEngine = gameEngine;
        _botPlayerService = botPlayerService;
        _turnTimerService = turnTimerService;
        _disconnectWatchdog = disconnectWatchdog;
    }

    // -------------------------------------------------------------------------
    // Connection lifecycle
    // -------------------------------------------------------------------------

    public override async Task OnConnectedAsync()
    {
        // Best-effort: cancel any pending auto-kick for a reconnecting player.
        // We don't know which game the player belongs to at this point,
        // so we pass empty string — implementations should handle that gracefully.
        _disconnectWatchdog.PlayerReconnected(string.Empty, GetUserId());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // -------------------------------------------------------------------------
    // Hub methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Add the caller to the game group and immediately send them a full state snapshot.
    /// </summary>
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");

        var state = await _gameStateStore.GetAsync(gameId);
        if (state is null) return;

        await Clients.Caller.SendAsync("GameStateSnapshot", state);
    }

    /// <summary>Remove the caller from the game group.</summary>
    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{gameId}");
    }

    /// <summary>
    /// Roll the dice for the current player's turn.
    /// Broadcasts <c>DiceRolled</c> to everyone and sends <c>ValidMovesForYou</c>
    /// only to the rolling player. If there are no valid moves the turn advances automatically.
    /// </summary>
    public async Task RollDice(string gameId)
    {
        string userId = GetUserId();

        var state = await _gameStateStore.GetAsync(gameId);
        if (state is null || state.CurrentTurnUserId != userId) return;
        if (state.WaitingForMove) return; // already rolled — waiting for move selection

        int[] diceValues = _gameEngine.RollDiceAll(state.Rules.DiceCount);
        int diceValue = diceValues.Sum();

        var validMoves = _gameEngine.ComputeValidMoves(state, userId, diceValue);
        bool hasValidMoves = validMoves.Count > 0;

        state.LastDiceValue = diceValue;
        state.LastValidMoves = validMoves;
        state.WaitingForMove = hasValidMoves;

        // Player acted — cancel the turn timer.
        _turnTimerService.CancelTurn(gameId);

        await _gameStateStore.SaveAsync(gameId, state);

        await Clients.Group($"game:{gameId}").SendAsync("DiceRolled", new
        {
            gameId,
            rollingUserId = userId,
            diceValues,
            hasValidMoves
        });

        if (hasValidMoves)
        {
            await Clients.Caller.SendAsync("ValidMovesForYou", new { gameId, moves = validMoves });
        }
        else
        {
            // No valid moves — auto-advance to the next player's turn.
            state = _gameEngine.AdvanceTurn(state, false);
            await _gameStateStore.SaveAsync(gameId, state);
            await BroadcastTurnChanged(gameId, state);
            await ProcessBotTurnIfNeeded(gameId, state);
        }
    }

    /// <summary>
    /// Apply a token move chosen by the current player.
    /// Validates the move against the pre-computed valid-moves list before applying.
    /// Broadcasts move, cut, finish, and game-over events as appropriate.
    /// </summary>
    public async Task MoveToken(string gameId, int tokenIndex, int targetSquare)
    {
        string userId = GetUserId();

        var state = await _gameStateStore.GetAsync(gameId);
        if (state is null || state.CurrentTurnUserId != userId) return;
        if (!state.WaitingForMove || state.LastDiceValue is null) return;

        // Validate that this move is in the pre-computed valid moves list.
        bool isValid = state.LastValidMoves.Any(m =>
            m.TokenIndex == tokenIndex && m.TargetSquare == targetSquare);
        if (!isValid) return;

        var result = _gameEngine.ApplyMove(state, userId, tokenIndex, targetSquare, state.LastDiceValue.Value);
        if (!result.Success) return;

        state = result.UpdatedState;
        await _gameStateStore.SaveAsync(gameId, state);

        // Broadcast: token moved.
        await Clients.Group($"game:{gameId}").SendAsync("TokenMoved", new
        {
            gameId,
            userId,
            tokenIndex,
            fromSquare = result.FromSquare,
            toSquare = result.ToSquare
        });

        // Broadcast: token cut (if applicable).
        if (result.WasCut)
        {
            await Clients.Group($"game:{gameId}").SendAsync("TokenCut", new
            {
                gameId,
                attackerUserId = userId,
                victimUserId = result.CutVictimUserId,
                victimTokenIndex = result.CutVictimTokenIndex
            });
        }

        // Broadcast: player finished all tokens.
        if (result.PlayerWon)
        {
            var finishedPlayer = state.Players.First(p => p.UserId == userId);

            await Clients.Group($"game:{gameId}").SendAsync("PlayerFinished", new
            {
                gameId,
                userId,
                finishPosition = finishedPlayer.FinishPosition
            });

            // End the game when all human players have finished.
            bool allHumansDone = state.Players
                .Where(p => !p.IsBot)
                .All(p => p.HasFinished);

            if (allHumansDone)
            {
                var finalRankings = state.Players
                    .Where(p => !p.IsBot && p.HasFinished)
                    .OrderBy(p => p.FinishPosition)
                    .Select(p => new { position = p.FinishPosition, userId = p.UserId })
                    .ToList();

                await Clients.Group($"game:{gameId}").SendAsync("GameOver", new
                {
                    gameId,
                    finalRankings
                });

                _turnTimerService.CancelTurn(gameId);
                return;
            }
        }

        // Advance turn.
        state = _gameEngine.AdvanceTurn(state, result.GrantExtraTurn);
        await _gameStateStore.SaveAsync(gameId, state);
        await BroadcastTurnChanged(gameId, state);
        await ProcessBotTurnIfNeeded(gameId, state);
    }

    /// <summary>Broadcast a quick emoji reaction to all players in the game.</summary>
    public async Task SendReaction(string gameId, string reactionEmoji)
    {
        string userId = GetUserId();

        await Clients.Group($"game:{gameId}").SendAsync("ReactionReceived", new
        {
            gameId,
            userId,
            reactionEmoji
        });
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task BroadcastTurnChanged(string gameId, GameStateDto state)
    {
        await Clients.Group($"game:{gameId}").SendAsync("TurnChanged", new
        {
            gameId,
            nextTurnUserId = state.CurrentTurnUserId,
            turnNumber = state.TurnNumber,
            timerEndsAt = state.TimerEndsAt
        });

        if (state.Rules.TurnTimerSeconds > 0)
        {
            _turnTimerService.StartTurn(gameId, state.CurrentTurnUserId, state.Rules.TurnTimerSeconds);
        }
    }

    private async Task ProcessBotTurnIfNeeded(string gameId, GameStateDto state)
    {
        var currentPlayer = state.Players.FirstOrDefault(p => p.UserId == state.CurrentTurnUserId);
        if (currentPlayer is null || !currentPlayer.IsBot) return;

        // Brief delay so clients can animate the previous move.
        await Task.Delay(800);

        var result = await _botPlayerService.ExecuteBotTurnAsync(state, state.CurrentTurnUserId);
        if (!result.Success) return;

        state = result.UpdatedState;
        await _gameStateStore.SaveAsync(gameId, state);

        // Broadcast bot dice roll.
        await Clients.Group($"game:{gameId}").SendAsync("DiceRolled", new
        {
            gameId,
            rollingUserId = state.CurrentTurnUserId,
            diceValues = new[] { result.FromSquare }, // bots reveal their move directly
            hasValidMoves = true
        });

        // Broadcast bot token move.
        await Clients.Group($"game:{gameId}").SendAsync("TokenMoved", new
        {
            gameId,
            userId = state.CurrentTurnUserId,
            tokenIndex = result.CutVictimTokenIndex ?? 0,
            fromSquare = result.FromSquare,
            toSquare = result.ToSquare
        });

        if (result.WasCut)
        {
            await Clients.Group($"game:{gameId}").SendAsync("TokenCut", new
            {
                gameId,
                attackerUserId = state.CurrentTurnUserId,
                victimUserId = result.CutVictimUserId,
                victimTokenIndex = result.CutVictimTokenIndex
            });
        }

        // Advance turn after bot move, then handle consecutive bot turns recursively.
        state = _gameEngine.AdvanceTurn(state, result.GrantExtraTurn);
        await _gameStateStore.SaveAsync(gameId, state);
        await BroadcastTurnChanged(gameId, state);
        await ProcessBotTurnIfNeeded(gameId, state);
    }

    private string GetUserId() =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("Not authenticated");
}
