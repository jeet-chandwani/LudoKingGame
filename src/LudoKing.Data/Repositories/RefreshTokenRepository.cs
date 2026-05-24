using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly LudoKingDbContext _ctx;

    public RefreshTokenRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => _ctx.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && !r.IsRevoked, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await _ctx.RefreshTokens.AddAsync(token, ct);

    public void Update(RefreshToken token)
        => _ctx.RefreshTokens.Update(token);

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
        => _ctx.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
}
