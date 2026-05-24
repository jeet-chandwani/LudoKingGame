using System.Text;
using LudoKing.API.Hubs;
using LudoKing.API.Middleware;
using LudoKing.Data;
using LudoKing.Data.Context;
using LudoKing.Domain.Services;
using LudoKing.Infrastructure.Extensions;
using LudoKing.Interfaces.Infrastructure;
using LudoKing.Interfaces.Repositories;
using LudoKing.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<LudoKingDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly(typeof(LudoKingDbContext).Assembly.FullName)));

// UnitOfWork is scoped so it shares the DbContext per request.
builder.Services.AddScoped<IUnitOfWork, CUnitOfWork>();

// ── Infrastructure (Redis, JWT helpers, Password, Email, Timers) ──────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Domain Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddSingleton<IGameEngine, GameEngine>();
builder.Services.AddSingleton<IBotPlayerService, BotPlayerService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
string jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "LudoKing",
            ValidateAudience = true,
            ValidAudience = "LudoKing",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Allow token in query string for SignalR WebSocket connections.
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ── CORS ──────────────────────────────────────────────────────────────────────
string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(opts =>
    opts.AddPolicy("LudoPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LudoPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LobbyHub>("/hubs/lobby");
app.MapHub<GameHub>("/hubs/game");

// ── Auto-migrate on startup in development ────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LudoKingDbContext>();
    await db.Database.MigrateAsync();
}

// ── Wire TurnTimerService timeout event → GameHub ─────────────────────────────
var turnTimerService = app.Services.GetRequiredService<ITurnTimerService>();
var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();
var gameStateStore = app.Services.GetRequiredService<IGameStateStore>();
var gameEngine = app.Services.GetRequiredService<IGameEngine>();

turnTimerService.TurnTimedOut += async (gameId, userId) =>
{
    // Auto-skip the timed-out player's turn.
    var state = await gameStateStore.GetAsync(gameId);
    if (state is null || state.CurrentTurnUserId != userId) return;

    await hubContext.Clients.Group($"game:{gameId}").SendAsync("TurnTimedOut", new { gameId, userId });

    state = gameEngine.AdvanceTurn(state, false);
    await gameStateStore.SaveAsync(gameId, state);

    await hubContext.Clients.Group($"game:{gameId}").SendAsync("TurnChanged", new
    {
        gameId,
        nextTurnUserId = state.CurrentTurnUserId,
        turnNumber = state.TurnNumber,
        timerEndsAt = state.TimerEndsAt
    });

    // Restart the timer for the next player if configured.
    if (state.Rules.TurnTimerSeconds > 0)
        turnTimerService.StartTurn(gameId, state.CurrentTurnUserId, state.Rules.TurnTimerSeconds);
};

app.Run();
