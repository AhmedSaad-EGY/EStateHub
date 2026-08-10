using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Identity.Configurations;

public sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.HasKey(securityEvent => securityEvent.Id);

        builder.Property(securityEvent => securityEvent.EventType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(securityEvent => securityEvent.FailureReason)
            .HasMaxLength(1024);

        builder.Property(securityEvent => securityEvent.SourceIpHash)
            .HasMaxLength(128);

        builder.HasIndex(securityEvent => securityEvent.OccurredAt);

        builder.HasOne(securityEvent => securityEvent.ApplicationUser)
            .WithMany()
            .HasForeignKey(securityEvent => securityEvent.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
