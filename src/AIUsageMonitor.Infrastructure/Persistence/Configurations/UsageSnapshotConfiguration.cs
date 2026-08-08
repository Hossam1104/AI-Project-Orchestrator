using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="UsageSnapshot"/> and its owned <see cref="QuotaWindow"/> onto a single flat
/// <c>UsageSnapshots</c> table (BRD §30), per the mandatory Session 03 materialization
/// addendum: <see cref="QuotaWindow"/> has no independent identity/table of its own — it is
/// always table-split with its owning snapshot via <c>OwnsOne</c>.
/// </summary>
internal sealed class UsageSnapshotConfiguration : IEntityTypeConfiguration<UsageSnapshot>
{
    public void Configure(EntityTypeBuilder<UsageSnapshot> builder)
    {
        builder.ToTable("UsageSnapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ProviderId).IsRequired();
        builder.Property(s => s.QuotaDefinitionId).IsRequired();

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(s => s.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuotaDefinition>()
            .WithMany()
            .HasForeignKey(s => s.QuotaDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports both the duplicate-snapshot material-change check and history queries
        // (BRD §16/§26), both of which filter by provider + quota definition first.
        builder.HasIndex(s => new { s.ProviderId, s.QuotaDefinitionId });

        builder.OwnsOne(s => s.Quota, quota =>
        {
            quota.Property(q => q.ExternalKey).HasColumnName("ExternalKey").HasMaxLength(128).IsRequired();
            quota.Property(q => q.Type).HasColumnName("QuotaType").HasConversion<string>().HasMaxLength(32).IsRequired();
            quota.Property(q => q.Unit).HasColumnName("QuotaUnit").HasConversion<string>().HasMaxLength(32).IsRequired();
            quota.Property(q => q.UsedValue).HasColumnName("UsedValue");
            quota.Property(q => q.RemainingValue).HasColumnName("RemainingValue");
            quota.Property(q => q.LimitValue).HasColumnName("LimitValue");
            quota.Property(q => q.UsedPercentage).HasColumnName("UsedPercentage");
            quota.Property(q => q.RemainingPercentage).HasColumnName("RemainingPercentage");
            quota.Property(q => q.WindowStart).HasColumnName("WindowStart");
            quota.Property(q => q.ResetAt).HasColumnName("ResetAt");
            quota.Property(q => q.Source).HasColumnName("Source").HasConversion<string>().HasMaxLength(32).IsRequired();
            quota.Property(q => q.Confidence).HasColumnName("Confidence").HasConversion<string>().HasMaxLength(32).IsRequired();
            quota.Property(q => q.CapturedAt).HasColumnName("CapturedAt").IsRequired();

            quota.WithOwner();
        });

        builder.Navigation(s => s.Quota).IsRequired();
    }
}
