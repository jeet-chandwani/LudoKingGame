using LudoKing.Interfaces.Repositories;

namespace LudoKing.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoomRepository Rooms { get; }
    IGameRepository Games { get; }
    IGameMoveRepository GameMoves { get; }
    IUserStatsRepository UserStats { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
