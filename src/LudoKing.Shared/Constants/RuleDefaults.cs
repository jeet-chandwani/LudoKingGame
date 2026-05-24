namespace LudoKing.Shared.Constants;

public static class RuleDefaults
{
    public const int MaxPlayers = 4;
    public const int TokensPerPlayer = 4;
    public const int DiceCount = 1;
    public const bool RequireSixToStart = true;
    public const bool ExtraTurnOnSix = true;
    public const bool ExtraTurnOnCut = false;
    public const int MaxConsecutiveSixes = 3;
    public const string HomeEntryRule = "Exact";
    public const bool StackProtectionEnabled = true;
    public const string SafeSquaresMode = "Standard";
    public const int TurnTimerSeconds = 30;
    public const int AutoKickOnDisconnectSeconds = 60;
    public const bool StopOnFirstWinner = false;
    public const bool AllowSpectators = true;
    public const int AutomatedPlayerCount = 0;
    public const bool BotRequireSixToStart = false;
}
