using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class RoomRepository : IRoomRepository
{
    private readonly LudoKingDbContext _ctx;

    public RoomRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<GameRoom?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.GameRooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<GameRoom?> GetByJoinCodeAsync(string code, CancellationToken ct = default)
        => _ctx.GameRooms.FirstOrDefaultAsync(r => r.JoinCode == code, ct);

    public Task<List<GameRoom>> GetOpenPublicRoomsAsync(int page, int pageSize, CancellationToken ct = default)
        => _ctx.GameRooms
            .Where(r => r.Status == "Waiting" && !r.IsPrivate)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task AddAsync(GameRoom room, CancellationToken ct = default)
        => await _ctx.GameRooms.AddAsync(room, ct);

    public void Update(GameRoom room)
        => _ctx.GameRooms.Update(room);

    /// <summary>
    /// Returns the user IDs currently in the room's active game.
    /// For Phase 1, lobby membership is tracked via SignalR groups;
    /// this method returns an empty list when there is no active game.
    /// </summary>
    public async Task<List<Guid>> GetRoomPlayerIdsAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _ctx.GameRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

        if (room?.ActiveGameId is null)
            return new List<Guid>();

        return await _ctx.GamePlayers
            .Where(p => p.GameId == room.ActiveGameId.Value && !p.IsBot && p.UserId != null)
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);
    }
}
