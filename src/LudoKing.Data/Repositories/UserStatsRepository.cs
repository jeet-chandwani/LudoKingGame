using LudoKing.Data.Context;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class UserStatsRepository : IUserStatsRepository
{
    private readonly LudoKingDbContext _ctx;

    public UserStatsRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<UserStats?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _ctx.UserStats.FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task AddAsync(UserStats stats, CancellationToken ct = default)
        => await _ctx.UserStats.AddAsync(stats, ct);

    public void Update(UserStats stats)
        => _ctx.UserStats.Update(stats);

    /// <summary>
    /// Returns a ranked leaderboard page. The <paramref name="period"/> parameter is
    /// accepted for API compatibility but all-time stats are stored on UserStats, so
    /// period filtering (e.g. "weekly") would require a separate aggregation table.
    /// For Phase 1 the same all-time data is returned regardless of period.
    /// </summary>
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(
        string period,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var offset = (page - 1) * pageSize;

        var rows = await _ctx.UserStats
            .AsNoTracking()
            .OrderByDescending(s => s.EloRating)
            .Skip(offset)
            .Take(pageSize)
            .Join(
                _ctx.Users.AsNoTracking(),
                s => s.UserId,
                u => u.Id,
                (s, u) => new
                {
                    s.UserId,
                    u.DisplayName,
                    u.AvatarUrl,
                    s.EloRating,
                    s.GamesWon,
                    s.GamesPlayed
                })
            .ToListAsync(ct);

        return rows
            .Select((row, index) => new LeaderboardEntryDto
            {
                UserId      = row.UserId,
                DisplayName = row.DisplayName,
                AvatarUrl   = row.AvatarUrl,
                EloRating   = row.EloRating,
                WinCount    = row.GamesWon,
                GamesPlayed = row.GamesPlayed,
                WinRate     = row.GamesPlayed > 0
                    ? Math.Round((decimal)row.GamesWon / row.GamesPlayed * 100, 2)
                    : 0m,
                Rank        = offset + index + 1
            })
            .ToList();
    }
}
