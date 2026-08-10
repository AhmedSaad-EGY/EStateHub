using EstateHub.Domain.Entities.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Discovery;

public sealed class LeadInterestConfiguration : IEntityTypeConfiguration<LeadInterest>
{
    public void Configure(EntityTypeBuilder<LeadInterest> builder)
    {
        builder.HasKey(interest => interest.Id);

        builder.Property(interest => interest.InterestType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(interest => new
            {
                interest.LeadId,
                interest.ListingId,
                interest.InterestType
            })
            .HasDatabaseName("UX_LeadInterests_LeadId_ListingId_InterestType")
            .IsUnique();

        builder.HasOne(interest => interest.Lead)
            .WithMany(lead => lead.Interests)
            .HasForeignKey(interest => interest.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(interest => interest.Listing)
            .WithMany(listing => listing.LeadInterests)
            .HasForeignKey(interest => interest.ListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
