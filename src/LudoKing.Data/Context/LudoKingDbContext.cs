using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Context;

public class LudoKingDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<GameRoom> GameRooms { get; set; }
    public DbSet<RuleConfiguration> RuleConfigurations { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GamePlayer> GamePlayers { get; set; }
    public DbSet<GameMove> GameMoves { get; set; }
    public DbSet<UserStats> UserStats { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }

    public LudoKingDbContext(DbContextOptions<LudoKingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LudoKingDbContext).Assembly);
    }
}
