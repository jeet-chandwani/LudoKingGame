namespace LudoKing.Interfaces.Dtos;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Raw refresh token — only populated by LoginAsync and RefreshTokenAsync.
    /// The API layer reads this to set the HTTP-only cookie, then never sends it to the client.
    /// </summary>
    public string? RawRefreshToken { get; set; }
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
