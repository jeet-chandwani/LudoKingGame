using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByDisplayNameAsync(string displayName, CancellationToken ct = default);
    Task<Dictionary<Guid, string>> GetDisplayNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
}
