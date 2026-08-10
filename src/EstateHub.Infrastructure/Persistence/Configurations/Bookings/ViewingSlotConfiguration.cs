using EstateHub.Domain.Entities.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Bookings;

public sealed class ViewingSlotConfiguration : IEntityTypeConfiguration<ViewingSlot>
{
    public void Configure(EntityTypeBuilder<ViewingSlot> builder)
    {
        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.MeetingPoint)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(slot => slot.RowVersion)
            .IsRowVersion();

        builder.HasIndex(slot => new { slot.ListingId, slot.StartsAt })
            .HasDatabaseName("UX_ViewingSlots_ListingId_StartsAt")
            .IsUnique();

        builder.HasOne(slot => slot.Listing)
            .WithMany(listing => listing.ViewingSlots)
            .HasForeignKey(slot => slot.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ViewingSlots_TimeRange",
                "[StartsAt] < [EndsAt]");
            table.HasCheckConstraint(
                "CK_ViewingSlots_Capacity_Positive",
                "[Capacity] > 0");
        });
    }
}
