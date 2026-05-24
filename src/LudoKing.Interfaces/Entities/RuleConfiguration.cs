namespace LudoKing.Interfaces.Entities;

public class RuleConfiguration
{
    public Guid Id { get; set; }
    public string? PresetName { get; set; }
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
    public bool AllowSpectators { get; set; }
    public int AutomatedPlayerCount { get; set; }
    public bool BotRequireSixToStart { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
