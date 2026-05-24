using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Services;

public interface ILobbyService
{
    Task<GameRoom> CreateRoomAsync(Guid hostUserId, CreateRoomRequest req, CancellationToken ct = default);
    Task<GameRoom> JoinRoomAsync(Guid userId, Guid roomId, string? joinCode, CancellationToken ct = default);
    Task LeaveRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default);
    Task<GameRoom> GetRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<List<RoomSummaryDto>> GetPublicRoomsAsync(int page, int pageSize, CancellationToken ct = default);
    Task KickPlayerAsync(Guid hostUserId, Guid roomId, Guid targetUserId, CancellationToken ct = default);
    Task UpdateRulesAsync(Guid hostUserId, Guid roomId, RuleSetDto rules, CancellationToken ct = default);
    Task TransferHostAsync(Guid currentHostId, Guid roomId, Guid newHostId, CancellationToken ct = default);
    Task SetPlayerReadyAsync(Guid userId, Guid roomId, bool isReady, CancellationToken ct = default);
    Task<Game?> StartGameAsync(Guid roomId, CancellationToken ct = default);
}
