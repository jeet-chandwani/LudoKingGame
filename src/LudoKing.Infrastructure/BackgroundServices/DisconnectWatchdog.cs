using System.Collections.Concurrent;
using LudoKing.Interfaces.Infrastructure;

namespace LudoKing.Infrastructure.BackgroundServices;

/// <summary>
/// Singleton service that tracks disconnected players and fires PlayerAutoKicked
/// when they do not reconnect within the configured window.
/// The composite key is "{gameId}:{userId}" to support multiple concurrent games.
/// </summary>
public sealed class DisconnectWatchdog : IDisconnectWatchdog
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

    public event Func<string, string, Task>? PlayerAutoKicked;

    public void PlayerDisconnected(string gameId, string userId, int autoKickSeconds)
    {
        if (autoKickSeconds <= 0) return; // auto-kick disabled

        var key = $"{gameId}:{userId}";

        // If there is already a watchdog running (double-disconnect), replace it.
        if (_timers.TryRemove(key, out var existing))
        {
            existing.Cancel();
            existing.Dispose();
        }

        var cts = new CancellationTokenSource();
        _timers[key] = cts;

        _ = Task.Delay(TimeSpan.FromSeconds(autoKickSeconds), cts.Token)
            .ContinueWith(
                async t =>
                {
                    if (t.IsCanceled) return;
                    _timers.TryRemove(key, out _);
                    if (PlayerAutoKicked is not null)
                        await PlayerAutoKicked.Invoke(gameId, userId);
                },
                TaskScheduler.Default);
    }

    public void PlayerReconnected(string gameId, string userId)
    {
        var key = $"{gameId}:{userId}";
        if (_timers.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
