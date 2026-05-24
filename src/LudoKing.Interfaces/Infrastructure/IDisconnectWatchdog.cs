namespace LudoKing.Interfaces.Infrastructure;

public interface IDisconnectWatchdog
{
    /// <summary>Starts a countdown for the disconnected player. Fires PlayerAutoKicked when it expires.</summary>
    void PlayerDisconnected(string gameId, string userId, int autoKickSeconds);

    /// <summary>Cancels the auto-kick countdown because the player reconnected in time.</summary>
    void PlayerReconnected(string gameId, string userId);

    /// <summary>Raised when an auto-kick timer expires. Args: (gameId, userId).</summary>
    event Func<string, string, Task>? PlayerAutoKicked;
}
