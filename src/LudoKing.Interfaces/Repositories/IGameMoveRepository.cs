using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IGameMoveRepository
{
    Task<List<GameMove>> GetByGameIdAsync(Guid gameId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(GameMove move, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<GameMove> moves, CancellationToken ct = default);
    Task<int> GetLastSequenceNumberAsync(Guid gameId, CancellationToken ct = default);
}
