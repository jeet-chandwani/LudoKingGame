using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class UserStatsConfiguration : IEntityTypeConfiguration<UserStats>
{
    public void Configure(EntityTypeBuilder<UserStats> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.HasIndex(s => s.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(new UserStats
        {
            Id = new Guid("b0000000-0000-0000-0000-000000000001"),
            UserId = new Guid("a0000000-0000-0000-0000-000000000001"),
            EloRating = 1000,
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
