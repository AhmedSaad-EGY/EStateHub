using EstateHub.Domain.Entities.Users;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Users;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(profile => profile.Persona)
            .IsRequired();

        builder.HasIndex(profile => profile.ApplicationUserId)
            .HasDatabaseName("UX_CustomerProfiles_ApplicationUserId")
            .IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(profile => profile.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
