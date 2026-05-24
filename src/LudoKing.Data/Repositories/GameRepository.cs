using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class GameRepository : IGameRepository
{
    private readonly LudoKingDbContext _ctx;

    public GameRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Games.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<Game?> GetByRoomIdAsync(Guid roomId, CancellationToken ct = default)
        => _ctx.Games
            .Where(g => g.RoomId == roomId)
            .OrderByDescending(g => g.StartedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Game game, CancellationToken ct = default)
        => await _ctx.Games.AddAsync(game, ct);

    public void Update(Game game)
        => _ctx.Games.Update(game);

    public Task<List<Game>> GetRecentByUserAsync(Guid userId, int limit, CancellationToken ct = default)
        => _ctx.Games
            .Where(g => _ctx.GamePlayers
                .Any(p => p.GameId == g.Id && p.UserId == userId))
            .OrderByDescending(g => g.StartedAt)
            .Take(limit)
            .ToListAsync(ct);
}
