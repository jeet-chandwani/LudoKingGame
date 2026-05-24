using LudoKing.Interfaces.Infrastructure;
using LudoKing.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LudoKing.API.Controllers;

[ApiController]
[Route("api/v1/games")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IGameStateStore _gameStateStore;
    private readonly IUnitOfWork _uow;

    public GamesController(IGameStateStore gameStateStore, IUnitOfWork uow)
    {
        _gameStateStore = gameStateStore;
        _uow = uow;
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/games/{id}
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the current game state.
    /// Checks the in-memory/Redis store first; falls back to the database if not found.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame(Guid id, CancellationToken ct)
    {
        // 1. Try the live state store (Redis).
        var liveState = await _gameStateStore.GetAsync(id.ToString());
        if (liveState is not null)
            return Ok(liveState);

        // 2. Fall back to the database.
        var game = await _uow.Games.GetByIdAsync(id, ct);
        if (game is null)
            return NotFound(new { error = "Game not found." });

        return Ok(game);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/games/{id}/moves
    // -------------------------------------------------------------------------

    /// <summary>Returns a paged list of moves recorded for the specified game.</summary>
    [HttpGet("{id:guid}/moves")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMoves(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        // Verify the game exists in the DB.
        bool exists = await _gameStateStore.ExistsAsync(id.ToString());
        if (!exists)
        {
            var game = await _uow.Games.GetByIdAsync(id, ct);
            if (game is null)
                return NotFound(new { error = "Game not found." });
        }

        var moves = await _uow.GameMoves.GetByGameIdAsync(id, page, pageSize, ct);
        return Ok(moves);
    }
}
