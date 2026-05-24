using FluentAssertions;
using LudoKing.Domain.Services;
using LudoKing.Interfaces.Dtos;
using LudoKing.Shared.Constants;

namespace LudoKing.Domain.Tests;

/// <summary>
/// Unit tests for <see cref="GameEngine"/>.
/// Board layout (Red perspective):
///   Start squares   : Red=0,  Blue=13, Green=26, Yellow=39
///   Home entry sqrs : Red=50, Blue=11, Green=24, Yellow=37  (last outer-track square before home stretch)
///   Home stretch    : Red=100-104, Blue=110-114, Green=120-124, Yellow=130-134
///   TotalStepsToHome: 57  (52 outer + 5 home stretch)
///   Safe squares    : 0,8,13,21,26,34,39,47
/// </summary>
public class GameEngineTests
{
    private readonly GameEngine _engine = new();

    // =========================================================================
    // Helper: build a minimal GameStateDto
    // =========================================================================
    private static GameStateDto BuildState(
        int playerCount = 2,
        int tokensPerPlayer = 4,
        bool requireSixToStart = true)
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
            RequireSixToStart = requireSixToStart,
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
    // InitializeGame Tests
    // =========================================================================

    [Fact]
    public void InitializeGame_WithTwoPlayers_ShouldCreateStateWithTwoPlayers()
    {
        var players = new List<PlayerStateDto>
        {
            new() { UserId = "u1", Color = "Red",  SeatPosition = 0, IsConnected = true },
            new() { UserId = "u2", Color = "Blue", SeatPosition = 1, IsConnected = true }
        };
        var rules = new RuleSetDto { TokensPerPlayer = 4, MaxPlayers = 2, DiceCount = 1, HomeEntryRule = "Exact", SafeSquaresMode = "Standard" };
        var gameId = Guid.NewGuid();

        var state = _engine.InitializeGame(gameId, players, rules);

        state.Players.Should().HaveCount(2);
        state.GameId.Should().Be(gameId.ToString());
    }

    [Fact]
    public void InitializeGame_AllTokensShouldStartInYard()
    {
        var players = new List<PlayerStateDto>
        {
            new() { UserId = "u1", Color = "Red",  SeatPosition = 0 },
            new() { UserId = "u2", Color = "Blue", SeatPosition = 1 }
        };
        var rules = new RuleSetDto { TokensPerPlayer = 4, MaxPlayers = 2, DiceCount = 1, HomeEntryRule = "Exact", SafeSquaresMode = "Standard" };

        var state = _engine.InitializeGame(Guid.NewGuid(), players, rules);

        foreach (var player in state.Players)
        {
            player.Tokens.Should().AllSatisfy(t =>
            {
                t.Square.Should().Be(BoardConstants.YardSquare);
                t.IsHome.Should().BeFalse();
            });
        }
    }

    [Fact]
    public void InitializeGame_CurrentTurnUserIdShouldBeFirstPlayer()
    {
        var players = new List<PlayerStateDto>
        {
            new() { UserId = "u1", Color = "Red",  SeatPosition = 0 },
            new() { UserId = "u2", Color = "Blue", SeatPosition = 1 }
        };
        var rules = new RuleSetDto { TokensPerPlayer = 4, MaxPlayers = 2, DiceCount = 1, HomeEntryRule = "Exact", SafeSquaresMode = "Standard" };

        var state = _engine.InitializeGame(Guid.NewGuid(), players, rules);

        state.CurrentTurnUserId.Should().Be("u1");
    }

    [Fact]
    public void InitializeGame_TurnNumberShouldBeOne()
    {
        var players = new List<PlayerStateDto>
        {
            new() { UserId = "u1", Color = "Red",  SeatPosition = 0 },
            new() { UserId = "u2", Color = "Blue", SeatPosition = 1 }
        };
        var rules = new RuleSetDto { TokensPerPlayer = 4, MaxPlayers = 2, DiceCount = 1, HomeEntryRule = "Exact", SafeSquaresMode = "Standard" };

        var state = _engine.InitializeGame(Guid.NewGuid(), players, rules);

        state.TurnNumber.Should().Be(1);
    }

    // =========================================================================
    // ComputeValidMoves — RequireSixToStart = true
    // =========================================================================

    [Fact]
    public void RollFive_WhenAllTokensInYardAndRequireSixToStart_ShouldReturnZeroValidMoves()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 5);

        moves.Should().BeEmpty();
    }

    [Fact]
    public void RollSix_WhenAllTokensInYardAndRequireSixToStart_ShouldReturnOneValidMovePerYardToken()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Red start square = 0
        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 6);

        moves.Should().HaveCount(4); // one for each of the 4 yard tokens
        moves.Should().AllSatisfy(m => m.TargetSquare.Should().Be(0)); // Red start = 0
    }

    [Fact]
    public void RollSix_WhenAllTokensInYardAndRequireSixToStart_ShouldReturnStartSquareAsTarget()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 6);

        // Each move should target Red's start square = 0
        moves.Select(m => m.TargetSquare).Distinct().Should().ContainSingle()
            .Which.Should().Be(BoardConstants.GetStartSquare("Red"));
    }

    [Fact]
    public void RollThree_WhenTokenOnOuterTrack_ShouldReturnMoveToCorrectSquare()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Place Red token 0 at square 5 (progress = 5)
        state.Players[0].Tokens[0].Square = 5;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        // progress=5, roll=3 → newProgress=8, newSquare=(0+8)%52=8
        var move = moves.Should().ContainSingle(m => m.TokenIndex == 0).Subject;
        move.TargetSquare.Should().Be(8);
    }

    [Fact]
    public void RollExact_WhenTokenCanReachHomeCenter_ShouldReturnHomeCenterAsTarget()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Red: progress 54 means square = homeStretchStart + (54-52) = 100+2 = 102
        // stepsRemaining = 57 - 54 = 3
        state.Players[0].Tokens[0].Square = 102; // Red home stretch, offset 2

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        var move = moves.Should().ContainSingle(m => m.TokenIndex == 0).Subject;
        move.TargetSquare.Should().Be(BoardConstants.HomeCenterSquare);
    }

    [Fact]
    public void Roll_WhenTokenOneStepShortOfHome_ShouldProduceValidMoveWithinHomeStretch()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // progress=52 = homeStretchStart for Red (square=100), stepsRemaining=5
        // Roll 4 → progress=56, square=100+4=104 (last home stretch square), still valid
        state.Players[0].Tokens[0].Square = 100; // Red home stretch index 0

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 4);

        var move = moves.Should().ContainSingle(m => m.TokenIndex == 0).Subject;
        // progress=52+0=52, roll=4 → newProgress=56, homeStretchOffset=4, square=104
        move.TargetSquare.Should().Be(104);
    }

    [Fact]
    public void Roll_WhenTokenWouldOvershoothomeWithExactRule_ShouldHaveNoValidMoveForThatToken()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // progress=55, stepsRemaining=2 → roll 3 overshoots (Exact rule → no move)
        // square: homeStretchStart + (55-52) = 100+3 = 103
        state.Players[0].Tokens[0].Square = 103;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 3);

        moves.Should().NotContain(m => m.TokenIndex == 0);
    }

    [Fact]
    public void RollSix_WhenSomeTokensInYardAndSomeOnTrack_ShouldReturnBothYardEntryAndTrackMoves()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Token 0 on track at square 5, tokens 1-3 in yard
        state.Players[0].Tokens[0].Square = 5;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 6);

        // 3 yard tokens → 3 moves to start square (0)
        // 1 track token → 1 move to (0+5+6)%52 = 11
        moves.Should().HaveCount(4);
        moves.Should().Contain(m => m.TokenIndex == 0 && m.TargetSquare == 11);
        moves.Should().Contain(m => m.TokenIndex != 0 && m.TargetSquare == BoardConstants.GetStartSquare("Red"));
    }

    // =========================================================================
    // ApplyMove Tests
    // =========================================================================

    [Fact]
    public void ApplyMove_MoveFromYardToStartSquare_ShouldSucceedWithRollSix()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;
        // Red start = 0
        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 6);

        result.Success.Should().BeTrue();
        result.ToSquare.Should().Be(0);
        result.FromSquare.Should().Be(BoardConstants.YardSquare);
        result.UpdatedState.Players[0].Tokens[0].Square.Should().Be(0);
    }

    [Fact]
    public void ApplyMove_MoveTokenOnOuterTrack_ShouldMoveCorrectNumberOfSteps()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;
        state.Players[0].Tokens[0].Square = 5; // place token on track first

        // Roll 3: 5+3=8
        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 8, diceValue: 3);

        result.Success.Should().BeTrue();
        result.UpdatedState.Players[0].Tokens[0].Square.Should().Be(8);
    }

    [Fact]
    public void ApplyMove_MoveTokenIntoHomeStretch_ShouldLandOnCorrectHomeStretchSquare()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Red: square 49 → progress=(49-0+52)%52=49, stepsRemaining=57-49=8
        // Roll 4 → newProgress=53, homeStretchOffset=1, square=101
        state.Players[0].Tokens[0].Square = 49;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 101, diceValue: 4);

        result.Success.Should().BeTrue();
        result.UpdatedState.Players[0].Tokens[0].Square.Should().Be(101);
    }

    [Fact]
    public void ApplyMove_MoveTokenToHomeCenter_ShouldMarkTokenAsHome()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Red: square 102 → progress=54, stepsRemaining=3
        state.Players[0].Tokens[0].Square = 102;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 999, diceValue: 3);

        result.Success.Should().BeTrue();
        result.IsHomeEntry.Should().BeTrue();
        result.UpdatedState.Players[0].Tokens[0].IsHome.Should().BeTrue();
        result.UpdatedState.Players[0].Tokens[0].Square.Should().Be(BoardConstants.HomeCenterSquare);
    }

    [Fact]
    public void ApplyMove_WhenMovingToOpponentSquare_ShouldCutOpponentToken()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;
        var blueId = state.Players[1].UserId;

        // Place Red token at square 5, Blue token at square 9 (not a safe square).
        // Safe squares are: 0,8,13,21,26,34,39,47 — square 9 is not safe.
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 9;

        // Roll 4: Red token moves 5→9, cutting Blue token at 9
        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 9, diceValue: 4);

        result.Success.Should().BeTrue();
        result.WasCut.Should().BeTrue();
        result.CutVictimUserId.Should().Be(blueId);
        // Blue token 0 should be sent back to yard
        result.UpdatedState.Players[1].Tokens[0].Square.Should().Be(BoardConstants.YardSquare);
    }

    [Fact]
    public void ApplyMove_WhenStackProtectionEnabled_TwoOpponentTokensOnSquare_ShouldNotCut()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Place Red token at square 5
        // Place TWO Blue tokens at square 9 (not a safe square) → protected stack
        // Safe squares: 0,8,13,21,26,34,39,47 — square 9 is not safe
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 9;
        state.Players[1].Tokens[1].Square = 9;

        // Verify that the move to square 9 is generated but WouldCut=false (stack protected)
        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 4);

        var move = moves.FirstOrDefault(m => m.TokenIndex == 0 && m.TargetSquare == 9);
        if (move != null)
        {
            move.WouldCut.Should().BeFalse("two Blue tokens at square 9 form a protected stack");
            var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 9, diceValue: 4);
            result.Success.Should().BeTrue();
            result.WasCut.Should().BeFalse("stack protection prevents the cut");
        }
        else
        {
            // move not generated at all — also acceptable (no cut possible)
            moves.Should().NotContain(m => m.TokenIndex == 0 && m.TargetSquare == 9 && m.WouldCut);
        }
    }

    [Fact]
    public void ApplyMove_WhenTargetSquareIsSafe_ShouldNotCutOpponentToken()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Safe square = 8. Place Red at square 5, Blue at safe square 8.
        // Red rolls 3: 5+3=8 which is a safe square → no cut
        state.Players[0].Tokens[0].Square = 5;
        state.Players[1].Tokens[0].Square = 8;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 8, diceValue: 3);

        result.Success.Should().BeTrue();
        result.WasCut.Should().BeFalse("square 8 is a safe square");
        result.UpdatedState.Players[1].Tokens[0].Square.Should().Be(8); // blue stays
    }

    [Fact]
    public void ApplyMove_WhenDiceIsSix_AndExtraTurnOnSixIsTrue_ShouldGrantExtraTurn()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Move token from yard to start (requires 6)
        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 6);

        result.Success.Should().BeTrue();
        result.GrantExtraTurn.Should().BeTrue();
    }

    [Fact]
    public void ApplyMove_WhenDiceIsNotSix_AndExtraTurnOnSixIsTrue_ShouldNotGrantExtraTurn()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;
        state.Players[0].Tokens[0].Square = 5;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 8, diceValue: 3);

        result.Success.Should().BeTrue();
        result.GrantExtraTurn.Should().BeFalse();
    }

    [Fact]
    public void ApplyMove_WhenExtraTurnOnSixIsFalse_RollingSixShouldNotGrantExtraTurn()
    {
        var state = BuildState();
        state.Rules.ExtraTurnOnSix = false;
        var redId = state.Players[0].UserId;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 6);

        result.Success.Should().BeTrue();
        result.GrantExtraTurn.Should().BeFalse();
    }

    [Fact]
    public void ApplyMove_WhenNotPlayersTurn_ShouldReturnFailure()
    {
        var state = BuildState();
        var blueId = state.Players[1].UserId;

        // It is Red's turn, but Blue tries to move
        var result = _engine.ApplyMove(state, blueId, tokenIndex: 0, targetSquare: 13, diceValue: 6);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ApplyMove_WhenTokenIsAlreadyHome_ShouldReturnFailure()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Mark token 0 as already home
        state.Players[0].Tokens[0].IsHome = true;
        state.Players[0].Tokens[0].Square = BoardConstants.HomeCenterSquare;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 6);

        result.Success.Should().BeFalse();
    }

    // =========================================================================
    // CheckWin Tests
    // =========================================================================

    [Fact]
    public void CheckWin_WhenAllTokensAreHome_ShouldReturnTrue()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        foreach (var token in state.Players[0].Tokens)
        {
            token.Square = BoardConstants.HomeCenterSquare;
            token.IsHome = true;
        }

        var result = _engine.CheckWin(state, redId);

        result.Should().BeTrue();
    }

    [Fact]
    public void CheckWin_WhenThreeTokensHomeAndOneInYard_ShouldReturnFalse()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        for (int i = 0; i < 3; i++)
        {
            state.Players[0].Tokens[i].Square = BoardConstants.HomeCenterSquare;
            state.Players[0].Tokens[i].IsHome = true;
        }
        // token 3 stays in yard

        var result = _engine.CheckWin(state, redId);

        result.Should().BeFalse();
    }

    [Fact]
    public void CheckWin_WhenNoTokensHome_ShouldReturnFalse()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        var result = _engine.CheckWin(state, redId);

        result.Should().BeFalse();
    }

    // =========================================================================
    // AdvanceTurn Tests
    // =========================================================================

    [Fact]
    public void AdvanceTurn_WhenGrantExtraTurnFalse_ShouldMoveToNextPlayer()
    {
        var state = BuildState(playerCount: 2);
        // CurrentTurn = Red (seat 0)
        var redId = state.Players[0].UserId;
        var blueId = state.Players[1].UserId;

        var newState = _engine.AdvanceTurn(state, grantExtraTurn: false);

        newState.CurrentTurnUserId.Should().Be(blueId);
    }

    [Fact]
    public void AdvanceTurn_WhenGrantExtraTurnTrue_ShouldKeepSamePlayer()
    {
        var state = BuildState(playerCount: 2);
        var redId = state.Players[0].UserId;

        var newState = _engine.AdvanceTurn(state, grantExtraTurn: true);

        newState.CurrentTurnUserId.Should().Be(redId);
    }

    [Fact]
    public void AdvanceTurn_WhenLastPlayerHasTurn_ShouldWrapAroundToFirstPlayer()
    {
        var state = BuildState(playerCount: 2);
        // Set current turn to Blue (seat 1, the last player)
        state.CurrentTurnUserId = state.Players[1].UserId;

        var newState = _engine.AdvanceTurn(state, grantExtraTurn: false);

        // Should wrap back to Red (seat 0)
        newState.CurrentTurnUserId.Should().Be(state.Players[0].UserId);
    }

    [Fact]
    public void AdvanceTurn_ShouldIncrementTurnNumber()
    {
        var state = BuildState(playerCount: 2);
        state.TurnNumber = 5;

        var newState = _engine.AdvanceTurn(state, grantExtraTurn: false);

        newState.TurnNumber.Should().Be(6);
    }

    [Fact]
    public void AdvanceTurn_WhenAPlayerHasFinished_ShouldSkipFinishedPlayer()
    {
        var state = BuildState(playerCount: 3);
        var redId = state.Players[0].UserId;
        var blueId = state.Players[1].UserId;
        var greenId = state.Players[2].UserId;

        // Red has current turn, Blue has finished
        state.CurrentTurnUserId = redId;
        state.Players[1].HasFinished = true;

        var newState = _engine.AdvanceTurn(state, grantExtraTurn: false);

        // Blue is finished so skip to Green
        newState.CurrentTurnUserId.Should().Be(greenId);
    }

    // =========================================================================
    // ConsecutiveSixes Penalty Tests
    // =========================================================================

    [Fact]
    public void ConsecutiveSixes_OnThirdConsecutiveSix_ShouldApplyPenaltyAndSendTokenToYard()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // MaxConsecutiveSixes = 3
        // Simulate 2 previous sixes already counted
        state.ConsecutiveSixCount = 2;

        // Place Red token 0 on track at square 5 (not yard)
        state.Players[0].Tokens[0].Square = 5;

        // Roll 6: progress=5, roll=6, newProgress=11, newSquare=(0+11)%52=11
        // This is the 3rd consecutive six → penalty applied
        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 11, diceValue: 6);

        result.Success.Should().BeTrue();
        result.GrantExtraTurn.Should().BeFalse();

        // The moved token should have been sent back to yard as penalty
        result.UpdatedState.Players[0].Tokens[0].Square.Should().Be(BoardConstants.YardSquare);
        result.UpdatedState.ConsecutiveSixCount.Should().Be(0);
    }

    [Fact]
    public void ConsecutiveSixes_OnFirstSix_ShouldIncrementCountAndGrantExtraTurn()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;
        state.ConsecutiveSixCount = 0;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 6);

        result.Success.Should().BeTrue();
        result.GrantExtraTurn.Should().BeTrue();
        result.UpdatedState.ConsecutiveSixCount.Should().Be(1);
    }

    [Fact]
    public void ConsecutiveSixes_OnNonSixRoll_ShouldResetCountToZero()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;
        state.ConsecutiveSixCount = 2;
        state.Players[0].Tokens[0].Square = 5;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 8, diceValue: 3);

        result.Success.Should().BeTrue();
        result.UpdatedState.ConsecutiveSixCount.Should().Be(0);
    }

    // =========================================================================
    // Edge case / additional tests
    // =========================================================================

    [Fact]
    public void ComputeValidMoves_ForNonExistentPlayer_ShouldReturnEmptyList()
    {
        var state = BuildState();

        var moves = _engine.ComputeValidMoves(state, "nonexistent-user", diceValue: 6);

        moves.Should().BeEmpty();
    }

    [Fact]
    public void ComputeValidMoves_WhenTokenIsAlreadyHome_ShouldNotGenerateMovesForThatToken()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        state.Players[0].Tokens[0].IsHome = true;
        state.Players[0].Tokens[0].Square = BoardConstants.HomeCenterSquare;

        var moves = _engine.ComputeValidMoves(state, redId, diceValue: 6);

        moves.Should().NotContain(m => m.TokenIndex == 0);
    }

    [Fact]
    public void ApplyMove_WhenInvalidMove_ShouldReturnFailure()
    {
        var state = BuildState();
        var redId = state.Players[0].UserId;

        // Attempt to move token to a square not in valid moves (e.g. roll 1 while in yard)
        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 0, diceValue: 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void ApplyMove_WhenPlayerWins_ShouldMarkPlayerAsFinished()
    {
        var state = BuildState(tokensPerPlayer: 1);
        var redId = state.Players[0].UserId;

        // Single token at progress 54 (square 102), needs roll 3 to reach home (999)
        state.Players[0].Tokens[0].Square = 102;

        var result = _engine.ApplyMove(state, redId, tokenIndex: 0, targetSquare: 999, diceValue: 3);

        result.Success.Should().BeTrue();
        result.PlayerWon.Should().BeTrue();
        result.UpdatedState.Players[0].HasFinished.Should().BeTrue();
    }

    [Fact]
    public void ComputeValidMoves_BlueTokenOnTrack_ShouldComputeProgressRelativeToBlueStart()
    {
        var state = BuildState();
        // Switch turn to Blue
        state.CurrentTurnUserId = state.Players[1].UserId;
        var blueId = state.Players[1].UserId;

        // Blue start = 13. Token at square 15 → progress=2
        // Roll 4 → newProgress=6, newSquare=(13+6)%52=19
        state.Players[1].Tokens[0].Square = 15;

        var moves = _engine.ComputeValidMoves(state, blueId, diceValue: 4);

        var move = moves.Should().ContainSingle(m => m.TokenIndex == 0).Subject;
        move.TargetSquare.Should().Be(19);
    }

    [Fact]
    public void RollDice_ShouldReturnValueBetweenOneAndSix()
    {
        for (int i = 0; i < 50; i++)
        {
            int value = _engine.RollDice(1);
            value.Should().BeInRange(1, 6);
        }
    }
}
