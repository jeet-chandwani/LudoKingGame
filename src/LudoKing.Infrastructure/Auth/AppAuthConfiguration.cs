using LudoKing.Interfaces.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace LudoKing.Infrastructure.Auth;

public sealed class AppAuthConfiguration : IAuthConfiguration
{
    public AppAuthConfiguration(IConfiguration config)
    {
        TokenSecret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret not configured");
        AccessTokenMinutes = int.Parse(config["Jwt:AccessTokenMinutes"] ?? "15");
        RefreshTokenDays = int.Parse(config["Jwt:RefreshTokenDays"] ?? "7");
    }

    public string TokenSecret { get; }
    public int AccessTokenMinutes { get; }
    public int RefreshTokenDays { get; }
}
