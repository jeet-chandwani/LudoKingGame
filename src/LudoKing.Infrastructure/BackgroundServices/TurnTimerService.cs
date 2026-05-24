using System.Collections.Concurrent;
using LudoKing.Interfaces.Infrastructure;

namespace LudoKing.Infrastructure.BackgroundServices;

/// <summary>
/// Singleton service that manages per-game turn countdown timers.
/// When a timer fires without being cancelled the TurnTimedOut event is raised
/// so the game hub can auto-skip the inactive player.
/// </summary>
public sealed class TurnTimerService : ITurnTimerService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

    public event Func<string, string, Task>? TurnTimedOut;

    public void StartTurn(string gameId, string userId, int timerSeconds)
    {
        CancelTurn(gameId); // cancel any existing timer for this game first

        if (timerSeconds <= 0) return; // timer disabled for this ruleset

        var cts = new CancellationTokenSource();
        _timers[gameId] = cts;

        _ = Task.Delay(TimeSpan.FromSeconds(timerSeconds), cts.Token)
            .ContinueWith(
                async t =>
                {
                    if (t.IsCanceled) return;
                    _timers.TryRemove(gameId, out _);
                    if (TurnTimedOut is not null)
                        await TurnTimedOut.Invoke(gameId, userId);
                },
                TaskScheduler.Default);
    }

    public void CancelTurn(string gameId)
    {
        if (_timers.TryRemove(gameId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
