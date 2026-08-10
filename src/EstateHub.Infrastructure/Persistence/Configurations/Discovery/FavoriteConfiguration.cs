using EstateHub.Domain.Entities.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Discovery;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(favorite => favorite.Id);

        builder.HasIndex(favorite => new { favorite.CustomerProfileId, favorite.ListingId })
            .HasDatabaseName("UX_Favorites_CustomerProfileId_ListingId")
            .IsUnique();

        builder.HasOne(favorite => favorite.CustomerProfile)
            .WithMany(profile => profile.Favorites)
            .HasForeignKey(favorite => favorite.CustomerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(favorite => favorite.Listing)
            .WithMany(listing => listing.Favorites)
            .HasForeignKey(favorite => favorite.ListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
