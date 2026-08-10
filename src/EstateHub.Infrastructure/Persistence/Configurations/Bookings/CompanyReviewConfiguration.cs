using EstateHub.Domain.Entities.Bookings;
using EstateHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstateHub.Infrastructure.Persistence.Configurations.Bookings;

public sealed class CompanyReviewConfiguration : IEntityTypeConfiguration<CompanyReview>
{
    public void Configure(EntityTypeBuilder<CompanyReview> builder)
    {
        builder.HasKey(review => review.Id);

        builder.Property(review => review.Comment)
            .HasMaxLength(4000);

        builder.Property(review => review.ModerationReason)
            .HasMaxLength(1000);

        builder.HasIndex(review => review.ViewingBookingId)
            .HasDatabaseName("UX_CompanyReviews_ViewingBookingId")
            .IsUnique();

        builder.HasOne(review => review.ViewingBooking)
            .WithOne(booking => booking.Review)
            .HasForeignKey<CompanyReview>(review => review.ViewingBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(review => review.ModeratedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CompanyReviews_Rating_Range",
                "[Rating] >= 1 AND [Rating] <= 5");
        });
    }
}
