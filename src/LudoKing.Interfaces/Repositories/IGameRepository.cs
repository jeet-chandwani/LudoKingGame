using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Game?> GetByRoomIdAsync(Guid roomId, CancellationToken ct = default);
    Task AddAsync(Game game, CancellationToken ct = default);
    void Update(Game game);
    Task<List<Game>> GetRecentByUserAsync(Guid userId, int limit, CancellationToken ct = default);
}
