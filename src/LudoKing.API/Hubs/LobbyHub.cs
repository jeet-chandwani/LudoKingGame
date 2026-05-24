using System.Security.Claims;
using LudoKing.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LudoKing.API.Hubs;

[Authorize]
public class LobbyHub : Hub
{
    private readonly ILobbyService _lobbyService;

    public LobbyHub(ILobbyService lobbyService) => _lobbyService = lobbyService;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Add the caller's connection to the SignalR group for the given room lobby
    /// and notify other members that a new player has arrived.
    /// </summary>
    public async Task JoinRoomLobby(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"lobby:{roomId}");

        string userId = GetUserId();
        string displayName = GetDisplayName();

        await Clients.OthersInGroup($"lobby:{roomId}").SendAsync("PlayerJoinedLobby", new
        {
            roomId,
            userId,
            displayName
        });
    }

    /// <summary>
    /// Remove the caller's connection from the room lobby group
    /// and notify remaining members.
    /// </summary>
    public async Task LeaveRoomLobby(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby:{roomId}");

        await Clients.Group($"lobby:{roomId}").SendAsync("PlayerLeftLobby", new
        {
            roomId,
            userId = GetUserId()
        });
    }

    /// <summary>Toggle the ready state for the caller and broadcast the change.</summary>
    public async Task SetReady(string roomId, bool isReady)
    {
        string userId = GetUserId();
        await _lobbyService.SetPlayerReadyAsync(Guid.Parse(userId), Guid.Parse(roomId), isReady);

        await Clients.Group($"lobby:{roomId}").SendAsync("PlayerReadyChanged", new
        {
            roomId,
            userId,
            isReady
        });
    }

    /// <summary>
    /// Host requests the game to start.
    /// If all preconditions are met the service creates the game
    /// and all lobby members receive a <c>GameStarting</c> event.
    /// </summary>
    public async Task RequestStart(string roomId)
    {
        var game = await _lobbyService.StartGameAsync(Guid.Parse(roomId));
        if (game is null) return;

        await Clients.Group($"lobby:{roomId}").SendAsync("GameStarting", new
        {
            roomId,
            gameId = game.Id.ToString(),
            countdownSeconds = 3
        });
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private string GetUserId() =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("Not authenticated");

    private string GetDisplayName() =>
        Context.User?.FindFirst("displayName")?.Value ?? "Unknown";
}
