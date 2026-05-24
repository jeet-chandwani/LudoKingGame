using LudoKing.Interfaces.Dtos;

namespace LudoKing.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<AuthResultDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> ConfirmEmailAsync(string token, CancellationToken ct = default);
    Task RequestPasswordResetAsync(string email, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct = default);
}
