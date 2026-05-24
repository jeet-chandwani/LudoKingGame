using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class GameMoveConfiguration : IEntityTypeConfiguration<GameMove>
{
    public void Configure(EntityTypeBuilder<GameMove> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).UseIdentityColumn();

        builder.HasIndex(m => new { m.GameId, m.SequenceNumber }).IsUnique();

        builder.Property(m => m.MoveType).HasMaxLength(20);

        builder.Property(m => m.BoardStateSnapshot).IsRequired(false);

        builder.Property(m => m.OccurredAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(m => m.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
