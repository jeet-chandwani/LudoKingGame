using System.Security.Claims;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LudoKing.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public UsersController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/users/me
    // -------------------------------------------------------------------------

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user is null)
            return NotFound(new { error = "User not found." });

        return Ok(MapToProfile(user));
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/users/me
    // -------------------------------------------------------------------------

    /// <summary>Updates the authenticated user's display name and country code.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        Guid userId = GetCurrentUserId();
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user is null)
            return NotFound(new { error = "User not found." });

        if (!string.IsNullOrWhiteSpace(req.DisplayName))
            user.DisplayName = req.DisplayName.Trim();

        if (req.CountryCode is not null)
            user.CountryCode = string.IsNullOrWhiteSpace(req.CountryCode) ? null : req.CountryCode.Trim().ToUpperInvariant();

        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Ok(MapToProfile(user));
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/users/{id}/stats
    // -------------------------------------------------------------------------

    /// <summary>Returns game statistics for the specified user.</summary>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStats(Guid id, CancellationToken ct)
    {
        var stats = await _uow.UserStats.GetByUserIdAsync(id, ct);
        if (stats is null)
            return NotFound(new { error = "Stats not found for this user." });

        return Ok(stats);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/users/{id}/history
    // -------------------------------------------------------------------------

    /// <summary>Returns the recent game history for the specified user.</summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var games = await _uow.Games.GetRecentByUserAsync(id, limit, ct);
        return Ok(games);
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

    private static UserProfileResponse MapToProfile(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.CountryCode, user.Role, user.CreatedAt);
}

/// <summary>Slimmed-down view of a user's public profile.</summary>
public record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? CountryCode,
    string Role,
    DateTime CreatedAt);

/// <summary>Request body for updating the current user's profile.</summary>
public record UpdateProfileRequest(string? DisplayName, string? CountryCode);
