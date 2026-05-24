using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IUserStatsRepository
{
    Task<UserStats?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserStats stats, CancellationToken ct = default);
    void Update(UserStats stats);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string period, int page, int pageSize, CancellationToken ct = default);
}
