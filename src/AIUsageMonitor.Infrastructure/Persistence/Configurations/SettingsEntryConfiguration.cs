using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class SettingsEntryConfiguration : IEntityTypeConfiguration<SettingsEntry>
{
    public void Configure(EntityTypeBuilder<SettingsEntry> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key).HasMaxLength(128);
        builder.Property(s => s.Value).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
