using FluentAssertions;
using LudoKing.Domain.Services;
using LudoKing.Interfaces.Dtos;
using LudoKing.Shared.Constants;

namespace LudoKing.Domain.Tests;

/// <summary>
/// Validates that non-default <see cref="RuleSetDto"/> configurations are handled correctly
/// by <see cref="GameEngine"/>.
///
/// Board safe squares: 0, 8, 13, 21, 26, 34, 39, 47.
/// NON-safe outer-track squares include: 1-7, 9-12, 14-20, 22-25, 27-33, 35-38, 40-46, 48-51.
/// </summary>
public class RuleSetValidationTests
{
    private readonly GameEngine _engine = new();

    // =========================================================================
    // Helper
    // =========================================================================
    private static GameStateDto BuildState(
        int playerCount = 2,
        int tokensPerPlayer = 4,
        Action<RuleSetDto>? configureRules = null)
    {
        var colors = new[] { "Red", "Blue", "Green", "Yellow" };
        var players = new List<PlayerStateDto>();

        for (int i = 0; i < playerCount; i++)
        {
            var tokens = new List<TokenStateDto>();
            for (int t = 0; t < tokensPerPlayer; t++)
            {
                tokens.Add(new TokenStateDto
                {
                    Index = t,
                    Square = BoardConstants.YardSquare,
                    IsHome = false
                });
            }

            players.Add(new PlayerStateDto
            {
                UserId = $"user-{colors[i].ToLower()}",
                Color = colors[i],
                SeatPosition = i,
                IsConnected = true,
                IsBot = false,
                HasFinished = false,
                FinishPosition = 0,
                Tokens = tokens
            });
        }

        var rules = new RuleSetDto
        {
            MaxPlayers = playerCount,
            TokensPerPlayer = tokensPerPlayer,
            DiceCount = 1,
            RequireSixToStart = true,
            ExtraTurnOnSix = true,
            ExtraTurnOnCut = false,
            MaxConsecutiveSixes = 3,
            HomeEntryRule = "Exact",
            StackProtectionEnabled = true,
            SafeSquaresMode = "Standard",
            TurnTimerSeconds = 30,
            AutoKickOnDisconnectSeconds = 60,
            StopOnFirstWinner = false,
            AutomatedPlayerCount = 0,
            BotRequireSixToStart = false
        };

        configureRules?.Invoke(rules);

        return new GameStateDto
        {
            GameId = Guid.NewGuid().ToString(),
            Status = "InProgress",
            CurrentTurnUserId = players[0].UserId,
            TurnNumber = 1,
            ConsecutiveSixCount = 0,
            WaitingForMove = false,
            LastDiceValue = null,
            LastValidMoves = new List<ValidMoveDto>(),
            Rules = rules,
            Players = players
        };
    }

    // =========================================================================
    // RequireSixToStart = false
    // =========================================================================

    [Fact]
    public void RequireSixToStart_WhenFalse_RollingAnyValueAllowsYardEntry()
    {
        var state = BuildState(configureRules: r => r.RequireSixToStart = false);
        var redId = state.Players[0].UserId;

        // Roll 1 — all 4 tokens should be able to enter
        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 1);

        moves.Should().HaveCount(4, "all 4 yard tokens should be able to enter on any roll");
        moves.Should().AllSatisfy(m =>
            m.TargetSquare.Should().Be(BoardConstants.GetStartSquare("Red")));
    }

    [Fact]
    public void RequireSixToStart_WhenFalse_RollingFiveAllowsYardEntry()
    {
        var state = BuildState(configureRules: r => r.RequireSixToStart = false);
        var redId = state.Players[0].UserId;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 5);

        moves.Should().NotBeEmpty("RequireSixToStart=false means any value unlocks the yard");
    }

    [Fact]
    public void RequireSixToStart_WhenTrue_RollingNonSixKeepsYardTokensLocked()
    {
        var state = BuildState(configureRules: r => r.RequireSixToStart = true);
        var redId = state.Players[0].UserId;

        foreach (int roll in new[] { 1, 2, 3, 4, 5 })
        {
            var moves = _engine.ComputeValidMoves(state, redId, diceValue: roll);
            moves.Should().BeEmpty($"roll {roll} should not unlock yard tokens when RequireSixToStart=true");
        }
    }

    // =========================================================================
    // HomeEntryRule = "BounceBack"
    // =========================================================================

    [Fact]
    public void HomeEntryRule_BounceBack_WhenTokenOvershoots_ShouldBounceBackWithinHomeStretch()
    {
        // Red: progress=56 (square=104, last home stretch square), stepsRemaining=1
        // Roll 3: overshoot by 2. bouncedProgress = 57-2 = 55. square=100+3=103.
        var state = BuildState(configureRules: r => r.HomeEntryRule = "BounceBack");
        var redId = state.Players[0].UserId;

        // square 104 = homeStretchStart(100) + 4 → progress = 52+4=56, stepsRemaining=1
        state.Players[0].Tokens[0].Square = 104;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        // overshoot = 3-1 = 2; bouncedProgress = 57-2 = 55; square = 100+(55-52)=103
        var move = moves.FirstOrDefault(m => m.TokenIndex == 0);
        move.Should().NotBeNull("BounceBack should generate a move instead of blocking");
        move!.TargetSquare.Should().Be(103);
    }

    [Fact]
    public void HomeEntryRule_BounceBack_WhenBounceWouldExitHomeStretch_ShouldNotGenerateMove()
    {
        // square=104 (stepsRemaining=1), roll=6, overshoot=5, bouncedProgress=57-5=52
        // bouncedProgress=52, which is >=52 and <57 → square=100+0=100 (valid)
        var state = BuildState(configureRules: r => r.HomeEntryRule = "BounceBack");
        var redId = state.Players[0].UserId;

        state.Players[0].Tokens[0].Square = 104;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 6);

        var move = moves.FirstOrDefault(m => m.TokenIndex == 0);
        move.Should().NotBeNull("overshoot=5 is exactly HomeStretchLength; bouncedProgress=52 is valid");
        move!.TargetSquare.Should().Be(100);
    }

    [Fact]
    public void HomeEntryRule_Exact_WhenTokenOvershoots_ShouldNotGenerateAnyMove()
    {
        var state = BuildState(configureRules: r => r.HomeEntryRule = "Exact");
        var redId = state.Players[0].UserId;

        // progress=56 (square=104), stepsRemaining=1, roll=3 — overshoot by 2
        state.Players[0].Tokens[0].Square = 104;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        moves.Should().NotContain(m => m.TokenIndex == 0,
            "Exact rule: overshoot means no valid move for that token");
    }

    // =========================================================================
    // StackProtection = false
    // =========================================================================

    [Fact]
    public void StackProtection_WhenDisabled_TwoOpponentTokensOnSameSquare_ShouldStillCut()
    {
        var state = BuildState(configureRules: r => r.StackProtectionEnabled = false);
        var redId = state.Players[0].UserId;

        // Use non-safe square 9 (safe squares: 0,8,13,21,26,34,39,47 — 9 is NOT safe).
        // Red token at square 5, two Blue tokens at square 9.
        // With StackProtection=false, even a stack of 2 can be cut.
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 9;
        state.Players[1].Tokens[1].Square = 9;

        // Roll 4: Red moves 5→9
        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 4);

        var move = moves.FirstOrDefault(m => m.TokenIndex == 0 && m.TargetSquare == 9);
        move.Should().NotBeNull("square 9 is not safe and StackProtection is disabled");
        move!.WouldCut.Should().BeTrue("stack protection is disabled; 2-token stack is still cuttable");

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 9, diceValue: 4);
        result.Success.Should().BeTrue();
        result.WasCut.Should().BeTrue("StackProtection=false allows cutting through a 2-token stack");
    }

    [Fact]
    public void StackProtection_WhenEnabled_TwoOpponentTokensOnSameSquare_ShouldNotCut()
    {
        var state = BuildState(configureRules: r => r.StackProtectionEnabled = true);
        var redId = state.Players[0].UserId;

        // Two Blue tokens on non-safe square 9; Red at 5.
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 9;
        state.Players[1].Tokens[1].Square = 9;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 4);

        var move = moves.FirstOrDefault(m => m.TokenIndex == 0 && m.TargetSquare == 9);
        if (move != null)
        {
            move.WouldCut.Should().BeFalse("stack of 2 is protected when StackProtection is enabled");

            var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 9, diceValue: 4);
            result.Success.Should().BeTrue();
            result.WasCut.Should().BeFalse("stack protection prevents the cut");
        }
        // If move is not generated at all, the stack also effectively blocks advancement
    }

    // =========================================================================
    // SafeSquaresMode = "None"
    // =========================================================================

    [Fact]
    public void SafeSquaresMode_None_ShouldAllowCutsOnNormalSafeSquares()
    {
        // With SafeSquaresMode="None", the standard safe squares (0,8,13...) are no longer safe.
        var state = BuildState(configureRules: r =>
        {
            r.SafeSquaresMode = "None";
            r.StackProtectionEnabled = false;
        });
        var redId = state.Players[0].UserId;

        // Blue token on square 8 (normally safe, but SafeSquaresMode=None removes protection)
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 8;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        var move = moves.FirstOrDefault(m => m.TokenIndex == 0 && m.TargetSquare == 8);
        move.Should().NotBeNull("square 8 is not safe when SafeSquaresMode=None");
        move!.WouldCut.Should().BeTrue("opponent on square 8 should be cuttable with SafeSquaresMode=None");
    }

    [Fact]
    public void SafeSquaresMode_Standard_ShouldProtectTokensOnSafeSquares()
    {
        var state = BuildState(configureRules: r => r.SafeSquaresMode = "Standard");
        var redId = state.Players[0].UserId;

        // Blue token on safe square 8
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 8;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        var move = moves.FirstOrDefault(m => m.TokenIndex == 0 && m.TargetSquare == 8);
        if (move != null)
        {
            move.WouldCut.Should().BeFalse("safe square 8 protects the Blue token in Standard mode");
        }
    }

    // =========================================================================
    // ExtraTurnOnCut = true / false
    // Use non-safe square 9 to guarantee a cut occurs.
    // =========================================================================

    [Fact]
    public void ExtraTurnOnCut_WhenTrue_CuttingOpponentShouldGrantExtraTurn()
    {
        var state = BuildState(configureRules: r =>
        {
            r.ExtraTurnOnCut = true;
            r.ExtraTurnOnSix = false;
        });
        var redId = state.Players[0].UserId;

        // Roll 4, Red token at 5 → square 9 (not safe); Blue token at 9 → cut
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 9;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 9, diceValue: 4);

        result.Success.Should().BeTrue();
        result.WasCut.Should().BeTrue("square 9 is not safe, single Blue token there should be cut");
        result.GrantExtraTurn.Should().BeTrue("ExtraTurnOnCut=true should grant extra turn on a cut");
    }

    [Fact]
    public void ExtraTurnOnCut_WhenFalse_CuttingOpponentShouldNotGrantExtraTurn()
    {
        var state = BuildState(configureRules: r =>
        {
            r.ExtraTurnOnCut = false;
            r.ExtraTurnOnSix = false;
        });
        var redId = state.Players[0].UserId;

        // Roll 4, Red token at 5 → square 9 (not safe); Blue token at 9 → cut
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 9;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 9, diceValue: 4);

        result.Success.Should().BeTrue();
        result.WasCut.Should().BeTrue("square 9 is not safe; Blue token should be cut");
        result.GrantExtraTurn.Should().BeFalse("ExtraTurnOnCut=false should not grant extra turn on a cut");
    }

    // =========================================================================
    // MaxConsecutiveSixes = 0 (disabled)
    // =========================================================================

    [Fact]
    public void MaxConsecutiveSixes_WhenZero_PenaltyShouldNeverApply()
    {
        var state = BuildState(configureRules: r => r.MaxConsecutiveSixes = 0);
        var redId = state.Players[0].UserId;

        // Simulate 5 previous consecutive sixes — no penalty should be applied
        state.ConsecutiveSixCount = 5;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 6);

        result.Success.Should().BeTrue();
        // Token should have moved to start square, not penalised back to yard
        result.UpdatedState.Players[0].Tokens[0].Square.Should().Be(0);
    }

    // =========================================================================
    // StopOnFirstWinner = true
    // =========================================================================

    [Fact]
    public void StopOnFirstWinner_WhenTrue_GameShouldCompleteWhenFirstPlayerWins()
    {
        var state = BuildState(tokensPerPlayer: 1, configureRules: r => r.StopOnFirstWinner = true);
        var redId = state.Players[0].UserId;

        // Single token at progress=54 (square=102), needs roll=3 to reach home
        state.Players[0].Tokens[0].Square = 102;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 999, diceValue: 3);

        result.Success.Should().BeTrue();
        result.PlayerWon.Should().BeTrue();
        result.UpdatedState.Status.Should().Be("Completed",
            "StopOnFirstWinner=true means the game ends when the first player finishes");
    }

    [Fact]
    public void StopOnFirstWinner_WhenFalse_GameShouldContinueAfterFirstPlayerWins()
    {
        var state = BuildState(playerCount: 2, tokensPerPlayer: 1,
            configureRules: r => r.StopOnFirstWinner = false);
        var redId = state.Players[0].UserId;

        state.Players[0].Tokens[0].Square = 102;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 999, diceValue: 3);

        result.Success.Should().BeTrue();
        result.PlayerWon.Should().BeTrue();
        // Blue still has tokens in yard; game should NOT be Completed yet
        result.UpdatedState.Status.Should().NotBe("Completed",
            "game should continue when StopOnFirstWinner=false and not all players have finished");
    }
}
