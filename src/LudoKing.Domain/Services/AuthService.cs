using System.Security.Cryptography;
using System.Text;
using LudoKing.Domain.Utilities;
using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Infrastructure;
using LudoKing.Interfaces.Repositories;
using LudoKing.Interfaces.Services;

namespace LudoKing.Domain.Services;

public sealed class AuthService : IAuthService
{
    private const string EmailConfirmPurpose = "email-confirm";
    private const string PasswordResetPurpose = "password-reset";

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthConfiguration _authConfig;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IJwtTokenService jwtTokenService,
        IAuthConfiguration authConfig)
    {
        _uow = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _jwtTokenService = jwtTokenService;
        _authConfig = authConfig;
    }

    // -------------------------------------------------------------------------
    // RegisterAsync
    // -------------------------------------------------------------------------
    public async Task<AuthResultDto> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        if (await _uow.Users.GetByEmailAsync(req.Email, ct) is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        if (await _uow.Users.GetByDisplayNameAsync(req.DisplayName, ct) is not null)
            throw new InvalidOperationException("A user with this display name already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = req.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(req.Password),
            DisplayName = req.DisplayName,
            EmailConfirmed = false,
            IsActive = true,
            IsBanned = false,
            Role = "Player",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, ct);

        var stats = new UserStats
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EloRating = 1000,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.UserStats.AddAsync(stats, ct);
        await _uow.SaveChangesAsync(ct);

        string confirmToken = TokenHelper.GenerateToken(
            user.Id,
            EmailConfirmPurpose,
            _authConfig.TokenSecret,
            TimeSpan.FromDays(2));

        await _emailService.SendEmailConfirmationAsync(user.Email, user.DisplayName, confirmToken);

        return new AuthResultDto
        {
            AccessToken = string.Empty,
            ExpiresAt = DateTime.UtcNow,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    // -------------------------------------------------------------------------
    // LoginAsync
    // -------------------------------------------------------------------------
    public async Task<AuthResultDto> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByEmailAsync(req.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, req.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        if (user.IsBanned)
            throw new UnauthorizedAccessException("Account is banned.");

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Email address has not been confirmed.");

        string accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.DisplayName, user.Role);

        string rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        string refreshTokenHash = HashRefreshToken(rawRefreshToken);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_authConfig.RefreshTokenDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.RefreshTokens.AddAsync(refreshToken, ct);

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        await _uow.SaveChangesAsync(ct);

        return new AuthResultDto
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_authConfig.AccessTokenMinutes),
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Role = user.Role,
            RawRefreshToken = rawRefreshToken
        };
    }

    // -------------------------------------------------------------------------
    // RefreshTokenAsync
    // -------------------------------------------------------------------------
    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        string tokenHash = HashRefreshToken(refreshToken);
        var stored = await _uow.RefreshTokens.GetByTokenHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedAccessException("Refresh token not found.");

        if (stored.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token has been revoked.");

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        // Revoke old token
        stored.IsRevoked = true;
        _uow.RefreshTokens.Update(stored);

        var user = await _uow.Users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive || user.IsBanned)
            throw new UnauthorizedAccessException("Account is no longer active.");

        string newAccessToken = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email, user.DisplayName, user.Role);

        string newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        string newRefreshTokenHash = HashRefreshToken(newRawRefreshToken);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_authConfig.RefreshTokenDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.RefreshTokens.AddAsync(newRefreshToken, ct);
        await _uow.SaveChangesAsync(ct);

        return new AuthResultDto
        {
            AccessToken = newAccessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_authConfig.AccessTokenMinutes),
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Role = user.Role,
            RawRefreshToken = newRawRefreshToken
        };
    }

    // -------------------------------------------------------------------------
    // LogoutAsync
    // -------------------------------------------------------------------------
    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        string tokenHash = HashRefreshToken(refreshToken);
        var stored = await _uow.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);
        if (stored is null || stored.IsRevoked)
            return;

        stored.IsRevoked = true;
        _uow.RefreshTokens.Update(stored);
        await _uow.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // ConfirmEmailAsync
    // -------------------------------------------------------------------------
    public async Task<bool> ConfirmEmailAsync(string token, CancellationToken ct = default)
    {
        Guid? userId = TokenHelper.ValidateToken(token, EmailConfirmPurpose, _authConfig.TokenSecret);
        if (userId is null)
            return false;

        var user = await _uow.Users.GetByIdAsync(userId.Value, ct);
        if (user is null)
            return false;

        if (user.EmailConfirmed)
            return true;

        user.EmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    // -------------------------------------------------------------------------
    // RequestPasswordResetAsync
    // -------------------------------------------------------------------------
    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByEmailAsync(email.ToLowerInvariant(), ct);
        if (user is null)
            return; // don't reveal whether user exists

        string resetToken = TokenHelper.GenerateToken(
            user.Id,
            PasswordResetPurpose,
            _authConfig.TokenSecret,
            TimeSpan.FromHours(2));

        await _emailService.SendPasswordResetAsync(user.Email, user.DisplayName, resetToken);
    }

    // -------------------------------------------------------------------------
    // ResetPasswordAsync
    // -------------------------------------------------------------------------
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default)
    {
        Guid? userId = TokenHelper.ValidateToken(req.Token, PasswordResetPurpose, _authConfig.TokenSecret);
        if (userId is null)
            return false;

        var user = await _uow.Users.GetByIdAsync(userId.Value, ct);
        if (user is null)
            return false;

        user.PasswordHash = _passwordHasher.HashPassword(req.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        // Revoke all refresh tokens for this user
        await _uow.RefreshTokens.RevokeAllForUserAsync(user.Id, ct);

        await _uow.SaveChangesAsync(ct);
        return true;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------
    private static string HashRefreshToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
