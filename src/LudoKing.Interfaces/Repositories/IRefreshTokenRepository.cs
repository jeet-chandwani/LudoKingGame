using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    void Update(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
