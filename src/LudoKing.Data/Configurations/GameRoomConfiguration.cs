using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class GameRoomConfiguration : IEntityTypeConfiguration<GameRoom>
{
    public void Configure(EntityTypeBuilder<GameRoom> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();

        builder.Property(r => r.JoinCode).HasMaxLength(6).IsFixedLength();
        builder.HasIndex(r => r.JoinCode)
            .IsUnique()
            .HasFilter("[JoinCode] IS NOT NULL");

        builder.Property(r => r.Status).HasMaxLength(20);

        // Covers GetOpenPublicRoomsAsync: WHERE Status='Waiting' AND IsPrivate=0 ORDER BY CreatedAt DESC
        builder.HasIndex(r => new { r.Status, r.IsPrivate, r.CreatedAt })
            .HasDatabaseName("IX_GameRooms_Status_IsPrivate_CreatedAt");

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RuleConfiguration>()
            .WithMany()
            .HasForeignKey(r => r.RuleConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ActiveGameId is a nullable self-referencing FK to Games — configured via Game navigation
        // We declare it here to avoid cycles: use NoAction to break the cycle
        builder.Property(r => r.ActiveGameId).IsRequired(false);
        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(r => r.ActiveGameId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
