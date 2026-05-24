using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IGamePlayerRepository
{
    Task<List<GamePlayer>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<GamePlayer?> GetByGameAndUserAsync(Guid gameId, Guid userId, CancellationToken ct = default);
    Task AddAsync(GamePlayer player, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<GamePlayer> players, CancellationToken ct = default);
    void Update(GamePlayer player);
}
