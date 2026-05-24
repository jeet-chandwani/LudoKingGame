using System.Security.Claims;
using LudoKing.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LudoKing.API.Controllers;

[ApiController]
[Route("api/v1/leaderboard")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/leaderboard
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the global leaderboard for the specified period.
    /// </summary>
    /// <param name="period">Scoring window: "AllTime", "Monthly", or "Weekly".</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of entries per page (max 100).</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] string period = "AllTime",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var entries = await _leaderboardService.GetLeaderboardAsync(period, page, pageSize, ct);
        return Ok(entries);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/leaderboard/me
    // -------------------------------------------------------------------------

    /// <summary>Returns the authenticated player's rank and stats for the specified period.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyRank(
        [FromQuery] string period = "AllTime",
        CancellationToken ct = default)
    {
        Guid userId = GetCurrentUserId();
        var entry = await _leaderboardService.GetPlayerRankAsync(userId, period, ct);

        if (entry is null)
            return NotFound(new { error = "No ranking data found for this period." });

        return Ok(entry);
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
