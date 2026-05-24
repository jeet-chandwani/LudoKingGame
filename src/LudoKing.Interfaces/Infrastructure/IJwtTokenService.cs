namespace LudoKing.Interfaces.Infrastructure;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string displayName, string role);
    string GenerateRefreshToken();
}
