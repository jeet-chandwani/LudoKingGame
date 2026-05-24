namespace LudoKing.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IUserStatsRepository UserStats { get; }
    IGameRepository Games { get; }
    IRoomRepository Rooms { get; }
    IGameMoveRepository GameMoves { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IRuleConfigurationRepository RuleConfigurations { get; }
    IGamePlayerRepository GamePlayers { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
