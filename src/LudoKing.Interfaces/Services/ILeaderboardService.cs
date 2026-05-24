using LudoKing.Interfaces.Dtos;

namespace LudoKing.Interfaces.Services;

public interface ILeaderboardService
{
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string period, int page, int pageSize, CancellationToken ct = default);
    Task<LeaderboardEntryDto?> GetPlayerRankAsync(Guid userId, string period, CancellationToken ct = default);
    Task RecordGameResultAsync(Guid gameId, CancellationToken ct = default);
}
