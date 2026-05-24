using LudoKing.Infrastructure.Auth;
using LudoKing.Infrastructure.BackgroundServices;
using LudoKing.Infrastructure.Email;
using LudoKing.Infrastructure.Redis;
using LudoKing.Interfaces.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LudoKing.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all infrastructure services: Redis, Auth, Email (stub), TurnTimer, DisconnectWatchdog.
    /// Call this once from Program.cs / Startup.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── Redis ────────────────────────────────────────────────────────────
        string redisConnectionString =
            config.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<IGameStateStore, RedisGameStateStore>();

        // ── Auth ─────────────────────────────────────────────────────────────
        services.AddSingleton<IAuthConfiguration, AppAuthConfiguration>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();

        // ── Email (stub — swap for a real SMTP / SendGrid implementation) ────
        services.AddTransient<IEmailService, StubEmailService>();

        // ── Background / timer services (singletons to preserve in-memory state)
        services.AddSingleton<ITurnTimerService, TurnTimerService>();
        services.AddSingleton<IDisconnectWatchdog, DisconnectWatchdog>();

        return services;
    }
}
