namespace LudoKing.Interfaces.Dtos;

public class GameStateDto
{
    public string GameId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentTurnUserId { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public DateTime? TimerEndsAt { get; set; }
    public List<PlayerStateDto> Players { get; set; } = new();
    public int ConsecutiveSixCount { get; set; }
    public bool WaitingForMove { get; set; }
    public int? LastDiceValue { get; set; }
    public List<ValidMoveDto> LastValidMoves { get; set; } = new();
    public RuleSetDto Rules { get; set; } = new();
}

public class PlayerStateDto
{
    public string UserId { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int SeatPosition { get; set; }
    public bool IsConnected { get; set; }
    public bool IsBot { get; set; }
    public bool HasFinished { get; set; }
    public int FinishPosition { get; set; }
    public List<TokenStateDto> Tokens { get; set; } = new();
}

public class TokenStateDto
{
    public int Index { get; set; }
    public int Square { get; set; }
    public bool IsHome { get; set; }
}

public class ValidMoveDto
{
    public int TokenIndex { get; set; }
    public int TargetSquare { get; set; }
    public bool WouldCut { get; set; }
}

public class RuleSetDto
{
    public int MaxPlayers { get; set; }
    public int TokensPerPlayer { get; set; }
    public int DiceCount { get; set; }
    public bool RequireSixToStart { get; set; }
    public bool ExtraTurnOnSix { get; set; }
    public bool ExtraTurnOnCut { get; set; }
    public int MaxConsecutiveSixes { get; set; }
    public string HomeEntryRule { get; set; } = string.Empty;
    public bool StackProtectionEnabled { get; set; }
    public string SafeSquaresMode { get; set; } = string.Empty;
    public int TurnTimerSeconds { get; set; }
    public int AutoKickOnDisconnectSeconds { get; set; }
    public bool StopOnFirstWinner { get; set; }
    public int AutomatedPlayerCount { get; set; }
    public bool BotRequireSixToStart { get; set; }
}

public class MoveResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int FromSquare { get; set; }
    public int ToSquare { get; set; }
    public bool WasCut { get; set; }
    public string? CutVictimUserId { get; set; }
    public int? CutVictimTokenIndex { get; set; }
    public bool IsHomeEntry { get; set; }
    public bool PlayerWon { get; set; }
    public bool GrantExtraTurn { get; set; }
    public GameStateDto UpdatedState { get; set; } = new();
}
