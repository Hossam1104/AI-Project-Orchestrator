using AIUsageMonitor.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageMonitor.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConnectionConfiguration : IEntityTypeConfiguration<ProviderConnection>
{
    public void Configure(EntityTypeBuilder<ProviderConnection> builder)
    {
        builder.ToTable("ProviderConnections");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ProviderId).IsRequired();
        builder.Property(c => c.ConnectionType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.AccountDisplayName).HasMaxLength(256);
        builder.Property(c => c.LastSuccessfulSync);
        builder.Property(c => c.LastAttempt);
        builder.Property(c => c.LastErrorCode).HasMaxLength(64);
        builder.Property(c => c.LastErrorMessage).HasMaxLength(1024);

        // Opaque lookup key into ISecureCredentialStore only — never the secret itself (BRD §31).
        builder.Property(c => c.CredentialReference).HasMaxLength(256);

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ProviderId);
    }
}
