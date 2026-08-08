using AIUsageMonitor.Domain.Alerts;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ProviderId).IsRequired();
        builder.Property(r => r.QuotaDefinitionId);
        builder.Property(r => r.WarningThreshold).IsRequired();
        builder.Property(r => r.CriticalThreshold).IsRequired();
        builder.Property(r => r.Enabled).IsRequired();

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(r => r.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional — an alert rule may apply to a provider as a whole rather than one quota
        // (Session 02R EF-readiness note).
        builder.HasOne<QuotaDefinition>()
            .WithMany()
            .HasForeignKey(r => r.QuotaDefinitionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ProviderId);
    }
}
