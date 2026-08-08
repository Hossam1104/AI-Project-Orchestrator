using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class QuotaDefinitionConfiguration : IEntityTypeConfiguration<QuotaDefinition>
{
    public void Configure(EntityTypeBuilder<QuotaDefinition> builder)
    {
        builder.ToTable("QuotaDefinitions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();

        builder.Property(q => q.ProviderId).IsRequired();
        builder.Property(q => q.ExternalKey).HasMaxLength(128).IsRequired();
        builder.Property(q => q.Name).HasMaxLength(128).IsRequired();
        builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(q => q.Unit).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(q => q.SortOrder).IsRequired();

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(q => q.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // A provider never exposes the same quota identity twice.
        builder.HasIndex(q => new { q.ProviderId, q.ExternalKey }).IsUnique();
    }
}
