using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.TokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.CreatedByIp).HasMaxLength(45);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
