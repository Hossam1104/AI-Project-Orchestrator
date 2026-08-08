using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ProviderId).IsRequired();
        builder.Property(s => s.PlanName).HasMaxLength(128);
        builder.Property(s => s.OriginalStartDate);
        builder.Property(s => s.BillingPeriodStart);
        builder.Property(s => s.BillingPeriodEnd);
        builder.Property(s => s.RenewalDate);
        builder.Property(s => s.CancelledDate);
        builder.Property(s => s.AutoRenew);

        // Explicit precision/scale — EF Core's implicit decimal(18,2) default emits a
        // configuration warning if left unset (Session 02R EF-readiness note).
        builder.Property(s => s.Price).HasPrecision(18, 2);
        builder.Property(s => s.Currency).HasMaxLength(3);

        builder.Property(s => s.Cadence).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.Confidence).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(s => s.LastVerifiedAt);

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(s => s.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ProviderId);
    }
}
