using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LudoKing.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;
    private const string RefreshTokenCookie = "ludoking_refresh";

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(req, ct);
        return StatusCode(StatusCodes.Status201Created, new
        {
            result.UserId,
            result.DisplayName,
            result.Role,
            message = "Registration successful. Please check your email to confirm your account."
        });
    }

    /// <summary>
    /// Login with email and password.
    /// Returns access token in body; refresh token is set in an HTTP-only cookie.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(req, ct);

        // Place raw refresh token in HTTP-only cookie — never exposed in the response body.
        if (!string.IsNullOrEmpty(result.RawRefreshToken))
            SetRefreshCookie(result.RawRefreshToken);

        return Ok(new
        {
            result.AccessToken,
            result.ExpiresAt,
            result.UserId,
            result.DisplayName,
            result.Role
        });
    }

    /// <summary>Refresh the access token using the HTTP-only refresh cookie.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        string? refreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { error = "Refresh token missing." });

        var result = await _authService.RefreshTokenAsync(refreshToken, ct);

        // Rotate cookie with new raw refresh token.
        if (!string.IsNullOrEmpty(result.RawRefreshToken))
            SetRefreshCookie(result.RawRefreshToken);

        return Ok(new
        {
            result.AccessToken,
            result.ExpiresAt,
            result.UserId,
            result.DisplayName,
            result.Role
        });
    }

    /// <summary>Logout: revoke the refresh token and clear the cookie.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        string? refreshToken = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _authService.LogoutAsync(refreshToken, ct);

        ClearRefreshCookie();
        return NoContent();
    }

    /// <summary>Request a password reset email. Always returns 204 to prevent user enumeration.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        await _authService.RequestPasswordResetAsync(req.Email, ct);
        return NoContent();
    }

    /// <summary>Reset password using a valid reset token.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        bool success = await _authService.ResetPasswordAsync(req, ct);
        if (!success)
            return BadRequest(new { error = "Invalid or expired reset token." });

        return NoContent();
    }

    /// <summary>Confirm a user's email address via a token query string.</summary>
    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token, CancellationToken ct)
    {
        bool success = await _authService.ConfirmEmailAsync(token, ct);
        if (!success)
            return BadRequest(new { error = "Invalid or expired confirmation token." });

        return Ok(new { message = "Email confirmed successfully." });
    }

    // -------------------------------------------------------------------------
    // Cookie helpers
    // -------------------------------------------------------------------------

    private void SetRefreshCookie(string value)
    {
        bool isSecure = !_env.IsDevelopment();
        Response.Cookies.Append(RefreshTokenCookie, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void ClearRefreshCookie()
    {
        bool isSecure = !_env.IsDevelopment();
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.Strict : SameSiteMode.Lax
        });
    }
}

/// <summary>Request body for the forgot-password endpoint.</summary>
public record ForgotPasswordRequest(string Email);
