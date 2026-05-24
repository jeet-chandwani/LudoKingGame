using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(u => u.Username).HasMaxLength(10).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        builder.HasIndex(u => u.DisplayName).IsUnique();

        builder.Property(u => u.AvatarUrl).HasMaxLength(512);
        builder.Property(u => u.CountryCode).HasMaxLength(2).IsFixedLength();

        builder.Property(u => u.Role).HasMaxLength(20).IsRequired();

        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        var adminId = new Guid("a0000000-0000-0000-0000-000000000001");
        builder.HasData(new User
        {
            Id = adminId,
            Username = "funnyadmin",
            Email = "funnyadmin@ludoking.local",
            PasswordHash = "350000.6ZYBin8T8Y/1QDb/eI1I3Q==./x0zVi2LeqecCdOzQVyF6nRv/dkyrx77IWpPK01SqcE=",
            DisplayName = "funnyAdmin",
            EmailConfirmed = true,
            IsActive = true,
            IsBanned = false,
            Role = "Admin",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
