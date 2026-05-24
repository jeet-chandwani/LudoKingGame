using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(g => g.Status).HasMaxLength(20);

        builder.Property(g => g.StartedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<GameRoom>()
            .WithMany()
            .HasForeignKey(g => g.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RuleConfiguration>()
            .WithMany()
            .HasForeignKey(g => g.RuleConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(g => g.WinnerUserId).IsRequired(false);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.WinnerUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
