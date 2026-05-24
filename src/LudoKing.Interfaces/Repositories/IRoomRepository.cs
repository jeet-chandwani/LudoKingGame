using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IRoomRepository
{
    Task<GameRoom?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GameRoom?> GetByJoinCodeAsync(string code, CancellationToken ct = default);
    Task<List<GameRoom>> GetOpenPublicRoomsAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(GameRoom room, CancellationToken ct = default);
    void Update(GameRoom room);
    Task<List<Guid>> GetRoomPlayerIdsAsync(Guid roomId, CancellationToken ct = default);
}
