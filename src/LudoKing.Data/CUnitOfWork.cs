using LudoKing.Data.Context;
using LudoKing.Data.Repositories;
using LudoKing.Interfaces.Repositories;

namespace LudoKing.Data;

/// <summary>
/// Concrete Unit of Work that wraps a single <see cref="LudoKingDbContext"/> instance
/// and exposes lazily-created repository instances. All repositories share the same
/// DbContext so changes tracked across repositories are committed atomically via
/// <see cref="SaveChangesAsync"/>.
/// </summary>
public sealed class CUnitOfWork : IUnitOfWork
{
    private readonly LudoKingDbContext _ctx;

    private IUserRepository?              _users;
    private IUserStatsRepository?         _userStats;
    private IGameRepository?              _games;
    private IRoomRepository?              _rooms;
    private IGameMoveRepository?          _gameMoves;
    private IRefreshTokenRepository?      _refreshTokens;
    private IRuleConfigurationRepository? _ruleConfigurations;
    private IGamePlayerRepository?        _gamePlayers;

    public CUnitOfWork(LudoKingDbContext ctx) => _ctx = ctx;

    public IUserRepository              Users              => _users              ??= new UserRepository(_ctx);
    public IUserStatsRepository         UserStats          => _userStats          ??= new UserStatsRepository(_ctx);
    public IGameRepository              Games              => _games              ??= new GameRepository(_ctx);
    public IRoomRepository              Rooms              => _rooms              ??= new RoomRepository(_ctx);
    public IGameMoveRepository          GameMoves          => _gameMoves          ??= new GameMoveRepository(_ctx);
    public IRefreshTokenRepository      RefreshTokens      => _refreshTokens      ??= new RefreshTokenRepository(_ctx);
    public IRuleConfigurationRepository RuleConfigurations => _ruleConfigurations ??= new RuleConfigurationRepository(_ctx);
    public IGamePlayerRepository        GamePlayers        => _gamePlayers        ??= new GamePlayerRepository(_ctx);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);

    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            _ctx.Dispose();
            _disposed = true;
        }
    }
}
