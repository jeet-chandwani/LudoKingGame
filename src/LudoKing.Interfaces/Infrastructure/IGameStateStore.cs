using LudoKing.Interfaces.Dtos;

namespace LudoKing.Interfaces.Infrastructure;

public interface IGameStateStore
{
    Task<GameStateDto?> GetAsync(string gameId);
    Task SaveAsync(string gameId, GameStateDto state, TimeSpan? expiry = null);
    Task DeleteAsync(string gameId);
    Task<bool> ExistsAsync(string gameId);
}
