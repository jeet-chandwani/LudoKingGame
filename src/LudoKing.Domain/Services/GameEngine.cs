using System.Security.Cryptography;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Services;
using LudoKing.Shared.Constants;

namespace LudoKing.Domain.Services;

public sealed class GameEngine : IGameEngine
{
    // Total steps from start square to home center:
    // 52 outer track steps + 5 home stretch steps = 57
    private const int TotalStepsToHome = 57;

    // -------------------------------------------------------------------------
    // InitializeGame
    // -------------------------------------------------------------------------
    public GameStateDto InitializeGame(Guid gameId, List<PlayerStateDto> players, RuleSetDto rules)
    {
        var state = new GameStateDto
        {
            GameId = gameId.ToString(),
            Status = "Waiting",
            CurrentTurnUserId = players[0].UserId,
            TurnNumber = 1,
            ConsecutiveSixCount = 0,
            WaitingForMove = false,
            LastDiceValue = null,
            LastValidMoves = new List<ValidMoveDto>(),
            Rules = rules,
            Players = new List<PlayerStateDto>()
        };

        foreach (var p in players)
        {
            var tokens = new List<TokenStateDto>();
            for (int i = 0; i < rules.TokensPerPlayer; i++)
            {
                tokens.Add(new TokenStateDto
                {
                    Index = i,
                    Square = BoardConstants.YardSquare,
                    IsHome = false
                });
            }

            state.Players.Add(new PlayerStateDto
            {
                UserId = p.UserId,
                Color = p.Color,
                SeatPosition = p.SeatPosition,
                IsConnected = p.IsConnected,
                IsBot = p.IsBot,
                HasFinished = false,
                FinishPosition = 0,
                Tokens = tokens
            });
        }

        return state;
    }

    // -------------------------------------------------------------------------
    // RollDice
    // -------------------------------------------------------------------------
    public int RollDice(int diceCount)
    {
        if (diceCount == 2)
            return RollSingleDie() + RollSingleDie();
        return RollSingleDie();
    }

    public int[] RollDiceAll(int diceCount)
    {
        if (diceCount == 2)
            return new[] { RollSingleDie(), RollSingleDie() };
        return new[] { RollSingleDie() };
    }

    private static int RollSingleDie()
        => RandomNumberGenerator.GetInt32(1, 7); // [1, 6]

    // -------------------------------------------------------------------------
    // ComputeValidMoves
    // -------------------------------------------------------------------------
    public List<ValidMoveDto> ComputeValidMoves(GameStateDto state, string playerId, int diceValue)
    {
        var player = state.Players.FirstOrDefault(p => p.UserId == playerId);
        if (player is null)
            return new List<ValidMoveDto>();

        var moves = new List<ValidMoveDto>();
        int startSquare = BoardConstants.GetStartSquare(player.Color);
        int homeEntrySquare = BoardConstants.GetHomeEntrySquare(player.Color);
        int homeStretchStart = BoardConstants.GetHomeStretchStart(player.Color);

        foreach (var token in player.Tokens)
        {
            if (token.IsHome)
                continue;

            if (token.Square == BoardConstants.YardSquare)
            {
                // Token in yard — can enter if RequireSixToStart is satisfied
                bool canEnter = !state.Rules.RequireSixToStart || diceValue == 6;
                if (!canEnter)
                    continue;

                bool wouldCut = CheckWouldCut(state, player.Color, startSquare, state.Rules);
                moves.Add(new ValidMoveDto
                {
                    TokenIndex = token.Index,
                    TargetSquare = startSquare,
                    WouldCut = wouldCut
                });
                continue;
            }

            // Token on outer track or home stretch
            int progress = GetProgress(token.Square, player.Color);
            int stepsRemaining = TotalStepsToHome - progress;

            if (diceValue == stepsRemaining)
            {
                // Lands exactly on home center
                moves.Add(new ValidMoveDto
                {
                    TokenIndex = token.Index,
                    TargetSquare = BoardConstants.HomeCenterSquare,
                    WouldCut = false
                });
            }
            else if (diceValue < stepsRemaining)
            {
                int newProgress = progress + diceValue;
                int newSquare = ProgressToSquare(newProgress, player.Color, startSquare, homeStretchStart);

                // Only cuts possible on outer track (not home stretch, not yard, not center)
                bool wouldCut = false;
                if (newSquare >= 0 && newSquare <= 51)
                    wouldCut = CheckWouldCut(state, player.Color, newSquare, state.Rules);

                moves.Add(new ValidMoveDto
                {
                    TokenIndex = token.Index,
                    TargetSquare = newSquare,
                    WouldCut = wouldCut
                });
            }
            else
            {
                // diceValue > stepsRemaining — overshoot
                if (string.Equals(state.Rules.HomeEntryRule, "BounceBack", StringComparison.OrdinalIgnoreCase))
                {
                    // Bounce back from home center
                    int overshoot = diceValue - stepsRemaining;
                    // bounced progress = TotalStepsToHome - overshoot (must stay within home stretch)
                    int bouncedProgress = TotalStepsToHome - overshoot;
                    if (bouncedProgress >= 52 && bouncedProgress < TotalStepsToHome)
                    {
                        int newSquare = ProgressToSquare(bouncedProgress, player.Color, startSquare, homeStretchStart);
                        moves.Add(new ValidMoveDto
                        {
                            TokenIndex = token.Index,
                            TargetSquare = newSquare,
                            WouldCut = false
                        });
                    }
                    // If overshoot > HomeStretchLength (5), the bounce would go back to outer track
                    // which is not a standard rule — skip in that case
                }
                // HomeEntryRule == "Exact": no valid move — just skip (don't add)
            }
        }

        return moves;
    }

    // -------------------------------------------------------------------------
    // ApplyMove
    // -------------------------------------------------------------------------
    public MoveResultDto ApplyMove(
        GameStateDto state,
        string playerId,
        int tokenIndex,
        int targetSquare,
        int diceValue)
    {
        if (state.CurrentTurnUserId != playerId)
        {
            return new MoveResultDto
            {
                Success = false,
                Error = "It is not your turn.",
                UpdatedState = DeepClone(state)
            };
        }

        var player = state.Players.FirstOrDefault(p => p.UserId == playerId);
        if (player is null)
        {
            return new MoveResultDto
            {
                Success = false,
                Error = "Player not found.",
                UpdatedState = DeepClone(state)
            };
        }

        var token = player.Tokens.FirstOrDefault(t => t.Index == tokenIndex);
        if (token is null || token.IsHome)
        {
            return new MoveResultDto
            {
                Success = false,
                Error = "Invalid token.",
                UpdatedState = DeepClone(state)
            };
        }

        // Validate move exists in valid moves
        var validMoves = ComputeValidMoves(state, playerId, diceValue);
        bool isValid = validMoves.Any(m => m.TokenIndex == tokenIndex && m.TargetSquare == targetSquare);
        if (!isValid)
        {
            return new MoveResultDto
            {
                Success = false,
                Error = "Invalid move.",
                UpdatedState = DeepClone(state)
            };
        }

        // Deep clone state to avoid mutation
        var newState = DeepClone(state);
        var newPlayer = newState.Players.First(p => p.UserId == playerId);
        var newToken = newPlayer.Tokens.First(t => t.Index == tokenIndex);

        int fromSquare = newToken.Square;
        newToken.Square = targetSquare;

        bool isHomeEntry = targetSquare == BoardConstants.HomeCenterSquare;
        if (isHomeEntry)
            newToken.IsHome = true;

        // Check for cut
        bool wasCut = false;
        string? cutVictimUserId = null;
        int? cutVictimTokenIndex = null;

        if (!isHomeEntry && targetSquare >= 0 && targetSquare <= 51)
        {
            bool isEffectivelySafe = IsEffectivelySafe(targetSquare, state.Rules.SafeSquaresMode);

            if (!isEffectivelySafe)
            {
                foreach (var opponent in newState.Players)
                {
                    if (opponent.UserId == playerId)
                        continue;

                    var tokensOnSquare = opponent.Tokens
                        .Where(t => !t.IsHome && t.Square == targetSquare)
                        .ToList();

                    if (tokensOnSquare.Count == 0)
                        continue;

                    // Stack protection: if opponent has 2+ tokens here and protection is on, no cut
                    if (state.Rules.StackProtectionEnabled && tokensOnSquare.Count >= 2)
                        continue;

                    // Cut the first token (if multiple, cut one — typically only 1 unprotected)
                    var victim = tokensOnSquare[0];
                    victim.Square = BoardConstants.YardSquare;
                    victim.IsHome = false;
                    wasCut = true;
                    cutVictimUserId = opponent.UserId;
                    cutVictimTokenIndex = victim.Index;
                    break; // only one opponent can be cut per move
                }
            }
        }

        // Check win
        bool playerWon = CheckWin(newState, playerId);
        if (playerWon)
        {
            var winner = newState.Players.First(p => p.UserId == playerId);
            winner.HasFinished = true;
            int finishPos = newState.Players.Count(p => p.HasFinished);
            winner.FinishPosition = finishPos;

            // Check if game should end
            bool allHumanFinished = newState.Players
                .Where(p => !p.IsBot)
                .All(p => p.HasFinished);
            bool allFinished = newState.Players.All(p => p.HasFinished);

            if (state.Rules.StopOnFirstWinner || allFinished || allHumanFinished)
                newState.Status = "Completed";
        }

        // Determine extra turn
        bool grantExtraTurn = false;
        bool rolledSix = (state.Rules.DiceCount == 1 && diceValue == 6)
                      || (state.Rules.DiceCount == 2 && (diceValue % 6 == 0)); // rough heuristic for 2d6

        // For DiceCount == 2, track via the individual dice — but we only have sum here.
        // The caller passes the sum; for 2d6 "six" isn't well-defined the same way.
        // Use the single-die rule for both cases (matches typical Ludo variants):
        if (state.Rules.DiceCount == 1)
            rolledSix = diceValue == 6;

        int newConsecutiveSixCount = state.ConsecutiveSixCount;

        if (rolledSix)
        {
            newConsecutiveSixCount++;
            if (state.Rules.MaxConsecutiveSixes > 0
                && newConsecutiveSixCount >= state.Rules.MaxConsecutiveSixes)
            {
                // Penalty: token sent back to yard
                newToken.Square = BoardConstants.YardSquare;
                newToken.IsHome = false;
                newConsecutiveSixCount = 0;
                grantExtraTurn = false;
            }
            else if (state.Rules.ExtraTurnOnSix)
            {
                grantExtraTurn = true;
            }
        }
        else
        {
            newConsecutiveSixCount = 0;
            if (state.Rules.ExtraTurnOnCut && wasCut)
                grantExtraTurn = true;
        }

        newState.ConsecutiveSixCount = newConsecutiveSixCount;
        newState.WaitingForMove = false;

        return new MoveResultDto
        {
            Success = true,
            Error = null,
            FromSquare = fromSquare,
            ToSquare = targetSquare,
            WasCut = wasCut,
            CutVictimUserId = cutVictimUserId,
            CutVictimTokenIndex = cutVictimTokenIndex,
            IsHomeEntry = isHomeEntry,
            PlayerWon = playerWon,
            GrantExtraTurn = grantExtraTurn,
            UpdatedState = newState
        };
    }

    // -------------------------------------------------------------------------
    // CheckWin
    // -------------------------------------------------------------------------
    public bool CheckWin(GameStateDto state, string playerId)
    {
        var player = state.Players.FirstOrDefault(p => p.UserId == playerId);
        if (player is null)
            return false;
        return player.Tokens.All(t => t.IsHome);
    }

    // -------------------------------------------------------------------------
    // AdvanceTurn
    // -------------------------------------------------------------------------
    public GameStateDto AdvanceTurn(GameStateDto state, bool grantExtraTurn)
    {
        var newState = DeepClone(state);
        newState.TurnNumber++;
        newState.WaitingForMove = false;
        newState.LastDiceValue = null;
        newState.LastValidMoves = new List<ValidMoveDto>();

        if (grantExtraTurn)
        {
            // Same player keeps the turn
            return newState;
        }

        // Find next active player in seat order
        var activePlayers = newState.Players
            .Where(p => !p.HasFinished)
            .OrderBy(p => p.SeatPosition)
            .ToList();

        if (activePlayers.Count == 0)
            return newState;

        var current = newState.Players.FirstOrDefault(p => p.UserId == newState.CurrentTurnUserId);
        int currentSeat = current?.SeatPosition ?? -1;

        // Find the next player with a higher seat position (wrapping around)
        var next = activePlayers.FirstOrDefault(p => p.SeatPosition > currentSeat)
                ?? activePlayers[0];

        newState.CurrentTurnUserId = next.UserId;
        return newState;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    // Returns how many steps a token has moved from its color's start square.
    // Outer track: 0–51 steps. Home stretch: 52–56 steps.
    private static int GetProgress(int square, string color)
    {
        if (square == BoardConstants.YardSquare)
            return 0;

        int homeStretchStart = BoardConstants.GetHomeStretchStart(color);
        if (square >= homeStretchStart && square <= homeStretchStart + BoardConstants.HomeStretchLength - 1)
        {
            // On home stretch: progress is 52 + (square - homeStretchStart)
            return BoardConstants.OuterTrackLength + (square - homeStretchStart);
        }

        // On outer track
        int startSquare = BoardConstants.GetStartSquare(color);
        return (square - startSquare + BoardConstants.OuterTrackLength) % BoardConstants.OuterTrackLength;
    }

    // Converts a progress value (0–56) back to the board square for a given color.
    private static int ProgressToSquare(int progress, string color, int startSquare, int homeStretchStart)
    {
        if (progress < BoardConstants.OuterTrackLength)
            return (startSquare + progress) % BoardConstants.OuterTrackLength;

        // In home stretch
        int stretchOffset = progress - BoardConstants.OuterTrackLength;
        return homeStretchStart + stretchOffset;
    }

    // Checks whether moving to targetSquare would cut an opponent token.
    private static bool CheckWouldCut(GameStateDto state, string movingColor, int targetSquare, RuleSetDto rules)
    {
        if (targetSquare < 0 || targetSquare > 51)
            return false;

        if (IsEffectivelySafe(targetSquare, rules.SafeSquaresMode))
            return false;

        foreach (var opponent in state.Players)
        {
            if (opponent.Color == movingColor)
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

    // Determines if a square is safe given the SafeSquaresMode rule.
    private static bool IsEffectivelySafe(int square, string safeSquaresMode)
    {
        // Home stretch squares are always safe
        if (BoardConstants.IsHomeStretchSquare(square))
            return true;

        if (string.Equals(safeSquaresMode, "None", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(safeSquaresMode, "Standard", StringComparison.OrdinalIgnoreCase)
            || string.Equals(safeSquaresMode, "Extended", StringComparison.OrdinalIgnoreCase))
        {
            if (BoardConstants.SafeSquares.Contains(square))
                return true;
        }

        // Extended mode: color start squares are also safe (they are already in SafeSquares,
        // but explicitly include them for clarity)
        // SafeSquares already contains 0, 13, 26, 39 which are all start squares,
        // so Standard and Extended behave identically for cuts — Extended is a superset
        // but given the current BoardConstants they match.

        return false;
    }

    // Deep clone via JSON to avoid cross-reference mutation
    private static GameStateDto DeepClone(GameStateDto state)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(state);
        return System.Text.Json.JsonSerializer.Deserialize<GameStateDto>(json)!;
    }
}
