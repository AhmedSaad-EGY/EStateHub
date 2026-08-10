using EstateHub.Domain.Entities.Users;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Users;

public sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.HasKey(consent => consent.Id);

        builder.Property(consent => consent.PolicyType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(consent => consent.PolicyVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(consent => consent.ConsentSource)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(consent => new
            {
                consent.ApplicationUserId,
                consent.PolicyType,
                consent.PolicyVersion
            })
            .HasDatabaseName("UX_UserConsents_User_Policy_Version")
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(consent => consent.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
