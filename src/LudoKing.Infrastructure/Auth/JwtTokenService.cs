using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LudoKing.Interfaces.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace LudoKing.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IAuthConfiguration _config;

    public JwtTokenService(IAuthConfiguration config) => _config = config;

    public string GenerateAccessToken(Guid userId, string email, string displayName, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.TokenSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("displayName", displayName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "LudoKing",
            audience: "LudoKing",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
