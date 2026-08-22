using EstateHub.Application.Common;
using EstateHub.Application.ViewingBookings;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.ViewingBookings;

public sealed class PublicViewingSlotQueryService(EstateHubDbContext dbContext, TimeProvider timeProvider) : IPublicViewingSlotQueryService
{
    public async Task<PagedResult<PublicViewingSlotItem>?> GetSlotsAsync(string listingSlug, PublicViewingSlotQuery query, CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();
        var listingId = await dbContext.Set<Listing>().AsNoTracking().WherePublic(utcNow)
            .Where(listing => listing.Slug == listingSlug)
            .Select(listing => (Guid?)listing.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (listingId is null) return null;

        var slots = dbContext.Set<ViewingSlot>().AsNoTracking()
            .Where(slot => slot.ListingId == listingId.Value
                && slot.Status == ViewingSlotStatus.Open
                && slot.StartsAt > utcNow
                && slot.Capacity - (slot.Bookings
                    .Where(booking => booking.Status == ViewingBookingStatus.Pending || booking.Status == ViewingBookingStatus.Confirmed || booking.Status == ViewingBookingStatus.CheckedIn)
                    .Sum(booking => (int?)booking.VisitorCount) ?? 0) > 0);
        if (query.From is not null) slots = slots.Where(slot => slot.StartsAt >= query.From);
        if (query.To is not null) slots = slots.Where(slot => slot.StartsAt < query.To);

        var totalCount = await slots.CountAsync(cancellationToken);
        var items = await slots.OrderBy(slot => slot.StartsAt).ThenBy(slot => slot.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(slot => new PublicViewingSlotItem(
                slot.Id, slot.StartsAt, slot.EndsAt, slot.Capacity,
                slot.Bookings.Where(booking => booking.Status == ViewingBookingStatus.Pending || booking.Status == ViewingBookingStatus.Confirmed || booking.Status == ViewingBookingStatus.CheckedIn).Sum(booking => (int?)booking.VisitorCount) ?? 0,
                slot.Capacity - (slot.Bookings.Where(booking => booking.Status == ViewingBookingStatus.Pending || booking.Status == ViewingBookingStatus.Confirmed || booking.Status == ViewingBookingStatus.CheckedIn).Sum(booking => (int?)booking.VisitorCount) ?? 0),
                slot.MeetingPoint))
            .ToListAsync(cancellationToken);
        return new PagedResult<PublicViewingSlotItem>(items, query.PageNumber, query.PageSize, totalCount);
    }
}
