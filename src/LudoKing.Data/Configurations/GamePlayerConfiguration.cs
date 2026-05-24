using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.ColorAssigned).HasMaxLength(10).IsRequired();

        // UserId is Guid on the POCO but nullable in DB to support bot players
        // that have no real User row. IsRequired(false) makes the column nullable.
        builder.Property(p => p.UserId).IsRequired(false);

        builder.Property(p => p.JoinedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(p => p.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
