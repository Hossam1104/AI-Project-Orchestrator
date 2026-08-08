using AIUsageMonitor.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.Enabled).IsRequired();
        builder.Property(p => p.SortOrder).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
