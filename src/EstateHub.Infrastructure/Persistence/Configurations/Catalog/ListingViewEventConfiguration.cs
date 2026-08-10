using EstateHub.Domain.Entities.Catalog;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ListingViewEventConfiguration : IEntityTypeConfiguration<ListingViewEvent>
{
    public void Configure(EntityTypeBuilder<ListingViewEvent> builder)
    {
        builder.HasKey(viewEvent => viewEvent.Id);

        builder.Property(viewEvent => viewEvent.AnonymousSessionHash)
            .HasMaxLength(128);

        builder.Property(viewEvent => viewEvent.Source)
            .HasMaxLength(100);

        builder.HasIndex(viewEvent => new { viewEvent.ListingId, viewEvent.ViewedAt })
            .HasDatabaseName("IX_ListingViewEvents_ListingId_ViewedAt");

        builder.HasOne(viewEvent => viewEvent.Listing)
            .WithMany(listing => listing.ViewEvents)
            .HasForeignKey(viewEvent => viewEvent.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(viewEvent => viewEvent.ViewerApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
