using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class SyncEventConfiguration : IEntityTypeConfiguration<SyncEvent>
{
    public void Configure(EntityTypeBuilder<SyncEvent> builder)
    {
        builder.ToTable("SyncEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ProviderId).IsRequired();
        builder.Property(e => e.StartedAt).IsRequired();
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.Success).IsRequired();
        builder.Property(e => e.DataChanged).IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(64);
        builder.Property(e => e.ErrorSummary).HasMaxLength(1024);

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(e => e.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ProviderId, e.StartedAt });
    }
}
