using System.Security.Claims;
using LudoKing.API.Hubs;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LudoKing.API.Controllers;

[ApiController]
[Route("api/v1/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly ILobbyService _lobbyService;
    private readonly IHubContext<LobbyHub> _lobbyHub;

    public RoomsController(ILobbyService lobbyService, IHubContext<LobbyHub> lobbyHub)
    {
        _lobbyService = lobbyService;
        _lobbyHub = lobbyHub;
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/rooms
    // -------------------------------------------------------------------------

    /// <summary>Returns a paged list of public rooms available to join.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicRooms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var rooms = await _lobbyService.GetPublicRoomsAsync(page, pageSize, ct);
        return Ok(rooms);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/rooms
    // -------------------------------------------------------------------------

    /// <summary>Creates a new game room. The caller becomes the host.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest req, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        var room = await _lobbyService.CreateRoomAsync(userId, req, ct);
        return StatusCode(StatusCodes.Status201Created, room);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/rooms/{id}
    // -------------------------------------------------------------------------

    /// <summary>Returns the details of a specific room.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoom(Guid id, CancellationToken ct)
    {
        var room = await _lobbyService.GetRoomAsync(id, ct);
        return Ok(room);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/rooms/{id}/join
    // -------------------------------------------------------------------------

    /// <summary>Join an existing room. For private rooms, supply the joinCode.</summary>
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinRoom(Guid id, [FromBody] JoinRoomRequest? req, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        var room = await _lobbyService.JoinRoomAsync(userId, id, req?.JoinCode, ct);

        // Notify other lobby members via SignalR.
        await _lobbyHub.Clients
            .Group($"lobby:{id}")
            .SendAsync("PlayerJoinedLobby", new
            {
                roomId = id.ToString(),
                userId = userId.ToString()
            }, ct);

        return Ok(room);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/rooms/{id}/leave
    // -------------------------------------------------------------------------

    /// <summary>Leave a room.</summary>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveRoom(Guid id, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        await _lobbyService.LeaveRoomAsync(userId, id, ct);

        await _lobbyHub.Clients
            .Group($"lobby:{id}")
            .SendAsync("PlayerLeftLobby", new
            {
                roomId = id.ToString(),
                userId = userId.ToString()
            }, ct);

        return NoContent();
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/rooms/{id}/kick/{targetUserId}
    // -------------------------------------------------------------------------

    /// <summary>Kick a player from the room. Only the host may do this.</summary>
    [HttpPost("{id:guid}/kick/{targetUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KickPlayer(Guid id, Guid targetUserId, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        await _lobbyService.KickPlayerAsync(userId, id, targetUserId, ct);

        await _lobbyHub.Clients
            .Group($"lobby:{id}")
            .SendAsync("PlayerKicked", new
            {
                roomId = id.ToString(),
                kickedUserId = targetUserId.ToString(),
                byUserId = userId.ToString()
            }, ct);

        return NoContent();
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/rooms/{id}/rules
    // -------------------------------------------------------------------------

    /// <summary>Update the room's rule set. Only the host may do this.</summary>
    [HttpPut("{id:guid}/rules")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRules(Guid id, [FromBody] RuleSetDto rules, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        await _lobbyService.UpdateRulesAsync(userId, id, rules, ct);

        var room = await _lobbyService.GetRoomAsync(id, ct);

        await _lobbyHub.Clients
            .Group($"lobby:{id}")
            .SendAsync("RulesUpdated", new
            {
                roomId = id.ToString(),
                rules
            }, ct);

        return Ok(room);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/rooms/{id}/transfer-host
    // -------------------------------------------------------------------------

    /// <summary>Transfer the host role to another player in the room.</summary>
    [HttpPost("{id:guid}/transfer-host")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TransferHost(Guid id, [FromBody] TransferHostRequest req, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        await _lobbyService.TransferHostAsync(userId, id, req.NewHostUserId, ct);

        await _lobbyHub.Clients
            .Group($"lobby:{id}")
            .SendAsync("HostTransferred", new
            {
                roomId = id.ToString(),
                newHostUserId = req.NewHostUserId.ToString(),
                previousHostUserId = userId.ToString()
            }, ct);

        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Guid GetCurrentUserId()
    {
        string? value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(value!);
    }
}

/// <summary>Request body for joining a room (joinCode is optional for public rooms).</summary>
public record JoinRoomRequest(string? JoinCode);

/// <summary>Request body for transferring host rights.</summary>
public record TransferHostRequest(Guid NewHostUserId);
