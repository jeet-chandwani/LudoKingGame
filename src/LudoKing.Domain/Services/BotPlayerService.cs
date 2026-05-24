using System.Security.Cryptography;
using System.Text.Json;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Services;
using LudoKing.Shared.Constants;

namespace LudoKing.Domain.Services;

public sealed class BotPlayerService : IBotPlayerService
{
    private readonly IGameEngine _engine;

    public BotPlayerService(IGameEngine engine) => _engine = engine;

    public Task<MoveResultDto> ExecuteBotTurnAsync(
        GameStateDto state,
        string botUserId,
        CancellationToken ct = default)
    {
        var player = state.Players.FirstOrDefault(p => p.UserId == botUserId);
        if (player is null)
        {
            return Task.FromResult(new MoveResultDto
            {
                Success = false,
                Error = "Bot player not found.",
                UpdatedState = state
            });
        }

        int diceValue = _engine.RollDice(state.Rules.DiceCount);

        var botMoves = ComputeBotValidMoves(state, player, diceValue);

        if (botMoves.Count == 0)
        {
            // No valid moves — advance turn, no state change to the board
            var skipped = _engine.AdvanceTurn(state, grantExtraTurn: false);
            skipped.LastDiceValue = diceValue;
            return Task.FromResult(new MoveResultDto
            {
                Success = true,
                FromSquare = BoardConstants.YardSquare,
                ToSquare = BoardConstants.YardSquare,
                GrantExtraTurn = false,
                UpdatedState = skipped
            });
        }

        var chosen = ChooseBotMove(botMoves, player, state);
        // Apply move directly — bypasses GameEngine.ApplyMove so bot rules
        // (wrapping outer track, BotRequireSixToStart) are not overridden by
        // the engine's human-player re-validation.
        var result = ApplyBotMove(state, player, chosen, diceValue);
        return Task.FromResult(result);
    }

    // -------------------------------------------------------------------------
    // Direct bot move application (skips engine re-validation)
    // -------------------------------------------------------------------------
    private static MoveResultDto ApplyBotMove(
        GameStateDto state,
        PlayerStateDto botPlayer,
        ValidMoveDto chosen,
        int diceValue)
    {
        var newState = DeepClone(state);
        var newPlayer = newState.Players.First(p => p.UserId == botPlayer.UserId);
        var newToken = newPlayer.Tokens.First(t => t.Index == chosen.TokenIndex);

        int fromSquare = newToken.Square;
        newToken.Square = chosen.TargetSquare;
        // Bots never set IsHome — they circle forever

        bool wasCut = false;
        string? cutVictimUserId = null;
        int? cutVictimTokenIndex = null;

        // Check for cut on outer track only
        if (chosen.TargetSquare >= 0 && chosen.TargetSquare <= 51)
        {
            bool isSafe = IsEffectivelySafe(chosen.TargetSquare, state.Rules.SafeSquaresMode);
            if (!isSafe)
            {
                foreach (var opponent in newState.Players)
                {
                    if (opponent.UserId == botPlayer.UserId) continue;

                    var tokensOnSquare = opponent.Tokens
                        .Where(t => !t.IsHome && t.Square == chosen.TargetSquare)
                        .ToList();

                    if (tokensOnSquare.Count == 0) continue;
                    if (state.Rules.StackProtectionEnabled && tokensOnSquare.Count >= 2) continue;

                    tokensOnSquare[0].Square = BoardConstants.YardSquare;
                    tokensOnSquare[0].IsHome = false;
                    wasCut = true;
                    cutVictimUserId = opponent.UserId;
                    cutVictimTokenIndex = tokensOnSquare[0].Index;
                    break;
                }
            }
        }

        bool grantExtraTurn = state.Rules.ExtraTurnOnSix && diceValue == 6;
        if (state.Rules.ExtraTurnOnCut && wasCut) grantExtraTurn = true;

        newState.WaitingForMove = false;
        newState.LastDiceValue = diceValue;

        return new MoveResultDto
        {
            Success = true,
            FromSquare = fromSquare,
            ToSquare = chosen.TargetSquare,
            WasCut = wasCut,
            CutVictimUserId = cutVictimUserId,
            CutVictimTokenIndex = cutVictimTokenIndex,
            GrantExtraTurn = grantExtraTurn,
            UpdatedState = newState
        };
    }

    // -------------------------------------------------------------------------
    // Bot move computation — bots always wrap on the outer track, never going home
    // -------------------------------------------------------------------------
    private static List<ValidMoveDto> ComputeBotValidMoves(
        GameStateDto state,
        PlayerStateDto bot,
        int diceValue)
    {
        var moves = new List<ValidMoveDto>();
        int startSquare = BoardConstants.GetStartSquare(bot.Color);

        foreach (var token in bot.Tokens)
        {
            if (token.IsHome)
                continue;

            if (token.Square == BoardConstants.YardSquare)
            {
                bool canEnter = !state.Rules.BotRequireSixToStart || diceValue == 6;
                if (!canEnter)
                    continue;

                bool wouldCut = CheckBotWouldCut(state, bot.Color, startSquare, state.Rules);
                moves.Add(new ValidMoveDto
                {
                    TokenIndex = token.Index,
                    TargetSquare = startSquare,
                    WouldCut = wouldCut
                });
                continue;
            }

            // If token is already on home stretch (somehow), skip — bots shouldn't be there
            // but handle gracefully by not generating moves for such tokens.
            if (BoardConstants.IsHomeStretchSquare(token.Square))
                continue;

            // Token on outer track — always wrap (never enter home stretch)
            int nextSquare = (token.Square + diceValue) % BoardConstants.OuterTrackLength;
            bool wouldCutTarget = CheckBotWouldCut(state, bot.Color, nextSquare, state.Rules);

            moves.Add(new ValidMoveDto
            {
                TokenIndex = token.Index,
                TargetSquare = nextSquare,
                WouldCut = wouldCutTarget
            });
        }

        return moves;
    }

    // -------------------------------------------------------------------------
    // Bot move selection strategy
    // -------------------------------------------------------------------------
    private static ValidMoveDto ChooseBotMove(
        List<ValidMoveDto> moves,
        PlayerStateDto bot,
        GameStateDto state)
    {
        // Priority 1: cut an opponent
        var cuttingMoves = moves.Where(m => m.WouldCut).ToList();
        if (cuttingMoves.Count > 0)
        {
            // Among cutting moves, prefer lower target square (closer to opponents' territory)
            return cuttingMoves.OrderBy(m => m.TargetSquare).First();
        }

        // Priority 2: enter the track from yard
        var enterMoves = moves
            .Where(m =>
            {
                var token = bot.Tokens.First(t => t.Index == m.TokenIndex);
                return token.Square == BoardConstants.YardSquare;
            })
            .ToList();

        if (enterMoves.Count > 0)
            return enterMoves.First();

        // Priority 3: pick the token on the outer track closest to opponent start squares
        // (harassment: prefer moving forward to threaten opponents)
        // Among remaining moves, pick the token furthest along the outer track
        // (highest progress from its start), so the bot's lead token advances most aggressively.
        var activeMoves = moves
            .Where(m =>
            {
                var token = bot.Tokens.First(t => t.Index == m.TokenIndex);
                return token.Square != BoardConstants.YardSquare;
            })
            .ToList();

        if (activeMoves.Count == 0)
            return moves.First();

        // Pick the move where the current token is furthest ahead (most progress)
        return activeMoves
            .OrderByDescending(m =>
            {
                var token = bot.Tokens.First(t => t.Index == m.TokenIndex);
                return GetOuterProgress(token.Square, bot.Color);
            })
            .First();
    }

    private static int GetOuterProgress(int square, string color)
    {
        if (square == BoardConstants.YardSquare)
            return -1;
        int startSquare = BoardConstants.GetStartSquare(color);
        return (square - startSquare + BoardConstants.OuterTrackLength) % BoardConstants.OuterTrackLength;
    }

    private static bool CheckBotWouldCut(
        GameStateDto state,
        string botColor,
        int targetSquare,
        RuleSetDto rules)
    {
        if (targetSquare < 0 || targetSquare > 51)
            return false;

        // Bots don't cut on safe squares
        if (IsEffectivelySafe(targetSquare, rules.SafeSquaresMode))
            return false;

        foreach (var opponent in state.Players)
        {
            if (opponent.Color == botColor)
                continue;

            var tokensOnSquare = opponent.Tokens
                .Where(t => !t.IsHome && t.Square == targetSquare)
                .ToList();

            if (tokensOnSquare.Count == 0)
                continue;

            if (rules.StackProtectionEnabled && tokensOnSquare.Count >= 2)
                continue;

            return true;
        }

        return false;
    }

    private static bool IsEffectivelySafe(int square, string safeSquaresMode)
    {
        if (BoardConstants.IsHomeStretchSquare(square))
            return true;

        if (string.Equals(safeSquaresMode, "None", StringComparison.OrdinalIgnoreCase))
            return false;

        return BoardConstants.SafeSquares.Contains(square);
    }

    private static GameStateDto DeepClone(GameStateDto state)
    {
        var json = JsonSerializer.Serialize(state);
        return JsonSerializer.Deserialize<GameStateDto>(json)!;
    }
}
