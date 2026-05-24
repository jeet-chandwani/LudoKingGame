using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Repositories;
using LudoKing.Interfaces.Services;
using LudoKing.Shared.Helpers;

namespace LudoKing.Domain.Services;

public sealed class LeaderboardService : ILeaderboardService
{
    private readonly IUnitOfWork _uow;

    public LeaderboardService(IUnitOfWork unitOfWork)
    {
        _uow = unitOfWork;
    }

    // -------------------------------------------------------------------------
    // GetLeaderboardAsync
    // -------------------------------------------------------------------------
    public Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(
        string period,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return _uow.UserStats.GetLeaderboardAsync(period, page, pageSize, ct);
    }

    // -------------------------------------------------------------------------
    // GetPlayerRankAsync
    // -------------------------------------------------------------------------
    public async Task<LeaderboardEntryDto?> GetPlayerRankAsync(
        Guid userId,
        string period,
        CancellationToken ct = default)
    {
        var stats = await _uow.UserStats.GetByUserIdAsync(userId, ct);
        if (stats is null)
            return null;

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user is null)
            return null;

        // Get the full leaderboard to determine rank (small-scale; production would use a DB rank query)
        var board = await _uow.UserStats.GetLeaderboardAsync(period, 1, int.MaxValue, ct);
        int rank = board.FindIndex(e => e.UserId == userId) + 1;

        return new LeaderboardEntryDto
        {
            UserId = userId,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            EloRating = stats.EloRating,
            WinCount = stats.GamesWon,
            GamesPlayed = stats.GamesPlayed,
            WinRate = stats.GamesPlayed > 0
                ? Math.Round((decimal)stats.GamesWon / stats.GamesPlayed, 4)
                : 0m,
            Rank = rank > 0 ? rank : int.MaxValue
        };
    }

    // -------------------------------------------------------------------------
    // RecordGameResultAsync
    // -------------------------------------------------------------------------
    public async Task RecordGameResultAsync(Guid gameId, CancellationToken ct = default)
    {
        var game = await _uow.Games.GetByIdAsync(gameId, ct);
        if (game is null)
            return;

        var gamePlayers = await _uow.GamePlayers.GetByGameIdAsync(gameId, ct);
        var humanPlayers = gamePlayers.Where(p => !p.IsBot && p.UserId.HasValue).ToList();

        if (humanPlayers.Count == 0)
            return;

        // Load all stats in parallel
        var statsMap = new Dictionary<Guid, LudoKing.Interfaces.Entities.UserStats>();
        foreach (var gp in humanPlayers)
        {
            var s = await _uow.UserStats.GetByUserIdAsync(gp.UserId!.Value, ct);
            if (s is not null)
                statsMap[gp.UserId.Value] = s;
        }

        // Determine finish order
        var ordered = humanPlayers
            .OrderBy(p => p.FinishPosition ?? int.MaxValue)
            .ToList();

        bool gameAbandoned = string.Equals(game.Status, "Abandoned", StringComparison.OrdinalIgnoreCase);

        for (int i = 0; i < ordered.Count; i++)
        {
            var gp = ordered[i];
            if (!statsMap.TryGetValue(gp.UserId!.Value, out var stats))
                continue;

            stats.GamesPlayed++;
            stats.TotalTokensCut += gp.TokensCut;
            stats.TotalTokensLost += gp.TokensLost;

            if (game.DurationSeconds.HasValue)
                stats.TotalPlayTimeSeconds += game.DurationSeconds.Value;

            if (gameAbandoned)
            {
                stats.GamesAbandoned++;
                stats.CurrentWinStreak = 0;
            }
            else if (i == 0 && gp.FinishPosition == 1)
            {
                // Winner
                stats.GamesWon++;
                stats.CurrentWinStreak++;
                if (stats.CurrentWinStreak > stats.BestWinStreak)
                    stats.BestWinStreak = stats.CurrentWinStreak;
            }
            else
            {
                stats.GamesLost++;
                stats.CurrentWinStreak = 0;
            }

            stats.UpdatedAt = DateTime.UtcNow;
        }

        // Apply ELO changes between winner and losers
        if (!gameAbandoned && ordered.Count >= 2)
        {
            var winner = ordered[0];
            if (statsMap.TryGetValue(winner.UserId!.Value, out var winnerStats))
            {
                for (int j = 1; j < ordered.Count; j++)
                {
                    var loser = ordered[j];
                    if (!statsMap.TryGetValue(loser.UserId!.Value, out var loserStats))
                        continue;

                    var (newWinnerElo, newLoserElo) = EloCalculator.Calculate(
                        winnerStats.EloRating, loserStats.EloRating);

                    winnerStats.EloRating = newWinnerElo;
                    loserStats.EloRating = newLoserElo;
                }
            }
        }

        // Persist all stat changes
        foreach (var stats in statsMap.Values)
            _uow.UserStats.Update(stats);

        await _uow.SaveChangesAsync(ct);
    }
}
