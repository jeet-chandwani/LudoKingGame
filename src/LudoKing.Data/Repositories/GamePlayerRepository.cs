using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class GamePlayerRepository : IGamePlayerRepository
{
    private readonly LudoKingDbContext _ctx;

    public GamePlayerRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<List<GamePlayer>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
        => _ctx.GamePlayers
            .Where(p => p.GameId == gameId)
            .OrderBy(p => p.SeatPosition)
            .ToListAsync(ct);

    public Task<GamePlayer?> GetByGameAndUserAsync(Guid gameId, Guid userId, CancellationToken ct = default)
        => _ctx.GamePlayers
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId, ct);

    public async Task AddAsync(GamePlayer player, CancellationToken ct = default)
        => await _ctx.GamePlayers.AddAsync(player, ct);

    public async Task AddRangeAsync(IEnumerable<GamePlayer> players, CancellationToken ct = default)
        => await _ctx.GamePlayers.AddRangeAsync(players, ct);

    public void Update(GamePlayer player)
        => _ctx.GamePlayers.Update(player);
}
