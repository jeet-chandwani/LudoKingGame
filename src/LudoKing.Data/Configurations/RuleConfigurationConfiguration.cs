using LudoKing.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudoKing.Data.Configurations;

public class RuleConfigurationConfiguration : IEntityTypeConfiguration<RuleConfiguration>
{
    public void Configure(EntityTypeBuilder<RuleConfiguration> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.PresetName).HasMaxLength(100);

        builder.Property(r => r.HomeEntryRule).HasMaxLength(20).IsRequired();
        builder.Property(r => r.SafeSquaresMode).HasMaxLength(20).IsRequired();

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
