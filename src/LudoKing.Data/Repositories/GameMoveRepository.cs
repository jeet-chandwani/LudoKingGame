using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class GameMoveRepository : IGameMoveRepository
{
    private readonly LudoKingDbContext _ctx;

    public GameMoveRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<List<GameMove>> GetByGameIdAsync(Guid gameId, int page, int pageSize, CancellationToken ct = default)
        => _ctx.GameMoves
            .Where(m => m.GameId == gameId)
            .OrderBy(m => m.SequenceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task AddAsync(GameMove move, CancellationToken ct = default)
        => await _ctx.GameMoves.AddAsync(move, ct);

    public async Task AddRangeAsync(IEnumerable<GameMove> moves, CancellationToken ct = default)
        => await _ctx.GameMoves.AddRangeAsync(moves, ct);

    public async Task<int> GetLastSequenceNumberAsync(Guid gameId, CancellationToken ct = default)
    {
        var max = await _ctx.GameMoves
            .Where(m => m.GameId == gameId)
            .MaxAsync(m => (int?)m.SequenceNumber, ct);
        return max ?? 0;
    }
}
