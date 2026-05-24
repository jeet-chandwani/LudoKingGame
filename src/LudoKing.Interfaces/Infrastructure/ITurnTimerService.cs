namespace LudoKing.Interfaces.Infrastructure;

public interface ITurnTimerService
{
    /// <summary>Starts a countdown for the given game/user turn. Fires TurnTimedOut when it expires.</summary>
    void StartTurn(string gameId, string userId, int timerSeconds);

    /// <summary>Cancels the active countdown for the given game (e.g. the player moved in time).</summary>
    void CancelTurn(string gameId);

    /// <summary>Raised when a turn timer expires. Args: (gameId, timedOutUserId).</summary>
    event Func<string, string, Task>? TurnTimedOut;
}
