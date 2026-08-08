using AIUsageMonitor.Domain.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class AlertEventConfiguration : IEntityTypeConfiguration<AlertEvent>
{
    public void Configure(EntityTypeBuilder<AlertEvent> builder)
    {
        builder.ToTable("AlertEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.AlertRuleId).IsRequired();
        builder.Property(e => e.TriggeredAt).IsRequired();
        builder.Property(e => e.ResolvedAt);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(48).IsRequired();
        builder.Property(e => e.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.Value);
        builder.Property(e => e.Message).HasMaxLength(1024);

        builder.HasOne<AlertRule>()
            .WithMany()
            .HasForeignKey(e => e.AlertRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AlertRuleId, e.TriggeredAt });
    }
}
