using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly LudoKingDbContext _ctx;

    public UserRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => _ctx.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _ctx.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<User?> GetByDisplayNameAsync(string displayName, CancellationToken ct = default)
        => _ctx.Users.FirstOrDefaultAsync(u => u.DisplayName == displayName, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _ctx.Users.AddAsync(user, ct);

    public void Update(User user)
        => _ctx.Users.Update(user);
}
