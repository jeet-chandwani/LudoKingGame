using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(f => f.Status).HasMaxLength(20);

        builder.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(f => new { f.RequesterId, f.TargetId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.TargetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
