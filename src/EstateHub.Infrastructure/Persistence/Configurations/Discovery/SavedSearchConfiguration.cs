using EstateHub.Domain.Entities.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Discovery;

public sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.HasKey(search => search.Id);

        builder.Property(search => search.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(search => search.FilterJson)
            .IsRequired();

        builder.HasOne(search => search.CustomerProfile)
            .WithMany(profile => profile.SavedSearches)
            .HasForeignKey(search => search.CustomerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
