using LudoKing.Interfaces.Dtos;

namespace LudoKing.Interfaces.Services;

public interface IBotPlayerService
{
    Task<MoveResultDto> ExecuteBotTurnAsync(GameStateDto state, string botUserId, CancellationToken ct = default);
}
