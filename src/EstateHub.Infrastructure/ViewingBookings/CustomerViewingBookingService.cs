using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.ViewingBookings;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.ViewingBookings;

public sealed class CustomerViewingBookingService(EstateHubDbContext dbContext, TimeProvider timeProvider) : ICustomerViewingBookingService
{
    public async Task<PagedResult<CustomerViewingBookingSummary>?> GetBookingsAsync(Guid applicationUserId, CustomerViewingBookingDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        if (customerProfileId is null) return null;
        var bookings = SummaryQuery(customerProfileId.Value);
        if (query.Status is not null) bookings = bookings.Where(booking => booking.Status == query.Status);
        var totalCount = await bookings.CountAsync(cancellationToken);
        var items = await bookings.OrderByDescending(booking => booking.CreatedAt).ThenByDescending(booking => booking.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<CustomerViewingBookingSummary>(items, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<CustomerViewingBookingDetails?> GetBookingAsync(Guid applicationUserId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        return customerProfileId is null ? null : await DetailsAsync(customerProfileId.Value, bookingId, cancellationToken);
    }

    public async Task<CustomerViewingBookingMutationResult> CreateBookingAsync(Guid applicationUserId, CreateCustomerViewingBookingCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await BookingLeadIntegration.CustomerIdentityQuery(dbContext, applicationUserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null) return new(CustomerViewingBookingOperationStatus.NotFound);
        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var lockedSlotCount = await dbContext.Set<ViewingSlot>()
                .Where(slot => slot.Id == command.ViewingSlotId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(slot => slot.Status, slot => slot.Status), cancellationToken);
            if (lockedSlotCount != 1) return new(CustomerViewingBookingOperationStatus.NotFound);

            var slot = await dbContext.Set<ViewingSlot>().AsNoTracking()
                .Where(candidate => candidate.Id == command.ViewingSlotId)
                .Select(candidate => new { candidate.Id, candidate.ListingId, candidate.Listing.CompanyId, candidate.Status, candidate.StartsAt, candidate.Capacity })
                .SingleAsync(cancellationToken);
            if (slot.Status != ViewingSlotStatus.Open || slot.StartsAt <= utcNow) return new(CustomerViewingBookingOperationStatus.Conflict);
            var listingIsPublic = await dbContext.Set<Listing>().AsNoTracking().WherePublic(utcNow)
                .AnyAsync(listing => listing.Id == slot.ListingId, cancellationToken);
            if (!listingIsPublic) return new(CustomerViewingBookingOperationStatus.Conflict);

            var activeBookings = dbContext.Set<ViewingBooking>().AsNoTracking().Where(booking =>
                booking.ViewingSlotId == slot.Id
                && (booking.Status == ViewingBookingStatus.Pending || booking.Status == ViewingBookingStatus.Confirmed || booking.Status == ViewingBookingStatus.CheckedIn));
            var reservedVisitors = await activeBookings.SumAsync(booking => (int?)booking.VisitorCount, cancellationToken) ?? 0;
            if (reservedVisitors + command.VisitorCount > slot.Capacity) return new(CustomerViewingBookingOperationStatus.Conflict);
            if (await activeBookings.AnyAsync(booking => booking.CustomerProfileId == customer.CustomerProfileId, cancellationToken)) return new(CustomerViewingBookingOperationStatus.Conflict);

            var booking = new ViewingBooking
            {
                Id = Guid.NewGuid(), BookingCode = $"EHB-{Guid.NewGuid():N}".ToUpperInvariant(),
                ViewingSlotId = slot.Id, CustomerProfileId = customer.CustomerProfileId,
                VisitorCount = command.VisitorCount, ContactPhone = command.ContactPhone,
                SpecialRequests = command.SpecialRequests, Status = ViewingBookingStatus.Pending,
                AssignedEmployeeId = null, CancellationSource = null, CreatedAt = utcNow,
                ConfirmedAt = null, CheckedInAt = null, CancelledAt = null, CompletedAt = null
            };
            dbContext.Add(booking);
            dbContext.Add(new BookingStatusHistory
            {
                Id = Guid.NewGuid(), ViewingBookingId = booking.Id, FromStatus = null,
                ToStatus = ViewingBookingStatus.Pending, ChangedByApplicationUserId = applicationUserId,
                ActorType = BookingActorType.Customer, ChangedAt = utcNow, Reason = null
            });
            dbContext.Add(BookingLeadIntegration.CreateRequestedLead(
                slot.CompanyId,
                customer,
                booking.Id,
                slot.ListingId,
                command.ContactPhone,
                utcNow));
            await dbContext.SaveChangesAsync(cancellationToken);
            var details = await DetailsAsync(customer.CustomerProfileId, booking.Id, cancellationToken);
            if (details is null) return new(CustomerViewingBookingOperationStatus.ServiceUnavailable);
            await transaction.CommitAsync(cancellationToken);
            return new(CustomerViewingBookingOperationStatus.Succeeded, details);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return new(CustomerViewingBookingOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CustomerViewingBookingOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CustomerViewingBookingOperationStatus.ServiceUnavailable); }
    }

    public async Task<CustomerViewingBookingOperationStatus> CancelBookingAsync(Guid applicationUserId, Guid bookingId, byte[] rowVersion, string? reason, CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        if (customerProfileId is null) return CustomerViewingBookingOperationStatus.NotFound;
        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var booking = await dbContext.Set<ViewingBooking>()
                .SingleOrDefaultAsync(candidate => candidate.Id == bookingId && candidate.CustomerProfileId == customerProfileId.Value, cancellationToken);
            if (booking is null) return CustomerViewingBookingOperationStatus.NotFound;
            if (booking.Status == ViewingBookingStatus.Cancelled) return CustomerViewingBookingOperationStatus.Succeeded;
            if (booking.Status is not (ViewingBookingStatus.Pending or ViewingBookingStatus.Confirmed)) return CustomerViewingBookingOperationStatus.Conflict;
            var slotStartsAt = await dbContext.Set<ViewingSlot>().AsNoTracking().Where(slot => slot.Id == booking.ViewingSlotId).Select(slot => slot.StartsAt).SingleAsync(cancellationToken);
            if (slotStartsAt <= utcNow) return CustomerViewingBookingOperationStatus.Conflict;

            var previousStatus = booking.Status;
            dbContext.Entry(booking).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
            booking.Status = ViewingBookingStatus.Cancelled;
            booking.CancellationSource = BookingCancellationSource.Customer;
            booking.CancelledAt = utcNow;
            dbContext.Add(new BookingStatusHistory
            {
                Id = Guid.NewGuid(), ViewingBookingId = booking.Id, FromStatus = previousStatus,
                ToStatus = ViewingBookingStatus.Cancelled, ChangedByApplicationUserId = applicationUserId,
                ActorType = BookingActorType.Customer, ChangedAt = utcNow, Reason = reason
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CustomerViewingBookingOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CustomerViewingBookingOperationStatus.Conflict; }
        catch (DbUpdateException) { return CustomerViewingBookingOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CustomerViewingBookingOperationStatus.ServiceUnavailable; }
    }

    private async Task<CustomerViewingBookingDetails?> DetailsAsync(Guid customerProfileId, Guid bookingId, CancellationToken cancellationToken)
    {
        var header = await SummaryQuery(customerProfileId).SingleOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);
        if (header is null) return null;
        var statusHistory = await dbContext.Set<BookingStatusHistory>().AsNoTracking().Where(history => history.ViewingBookingId == bookingId)
            .OrderBy(history => history.ChangedAt).ThenBy(history => history.Id)
            .Select(history => new CustomerBookingStatusHistoryItem(history.Id, history.FromStatus, history.ToStatus, history.ActorType, history.ChangedAt, history.Reason))
            .ToListAsync(cancellationToken);
        var rescheduleHistory = await dbContext.Set<BookingRescheduleHistory>().AsNoTracking().Where(history => history.ViewingBookingId == bookingId)
            .OrderBy(history => history.ChangedAt).ThenBy(history => history.Id)
            .Select(history => new CustomerBookingRescheduleHistoryItem(history.Id, history.FromViewingSlotId, history.FromViewingSlot.StartsAt, history.FromViewingSlot.EndsAt, history.ToViewingSlotId, history.ToViewingSlot.StartsAt, history.ToViewingSlot.EndsAt, history.ChangedAt, history.Reason))
            .ToListAsync(cancellationToken);
        return new CustomerViewingBookingDetails(header, header.SpecialRequests, header.CancellationSource, statusHistory, rescheduleHistory);
    }

    private IQueryable<CustomerViewingBookingSummary> SummaryQuery(Guid customerProfileId) => dbContext.Set<ViewingBooking>().AsNoTracking()
        .Where(booking => booking.CustomerProfileId == customerProfileId)
        .Select(booking => new CustomerViewingBookingSummary(
            booking.Id, booking.BookingCode, booking.Status, booking.VisitorCount, booking.ContactPhone, booking.SpecialRequests, booking.CancellationSource,
            booking.CreatedAt, booking.ConfirmedAt, booking.CheckedInAt, booking.CancelledAt, booking.CompletedAt, booking.RowVersion,
            new CustomerBookingSlotSummary(booking.ViewingSlot.Id, booking.ViewingSlot.StartsAt, booking.ViewingSlot.EndsAt, booking.ViewingSlot.MeetingPoint, booking.ViewingSlot.Status),
            new CustomerBookingListingSummary(booking.ViewingSlot.Listing.Id, booking.ViewingSlot.Listing.Slug, booking.ViewingSlot.Listing.Title, booking.ViewingSlot.Listing.ListingType),
            new CustomerBookingCompanySummary(booking.ViewingSlot.Listing.Company.Id, booking.ViewingSlot.Listing.Company.Slug, booking.ViewingSlot.Listing.Company.DisplayName, booking.ViewingSlot.Listing.Company.LogoFileAssetId)));

    private Task<Guid?> CustomerProfileIdAsync(Guid applicationUserId, CancellationToken cancellationToken) => dbContext.Set<CustomerProfile>().AsNoTracking()
        .Where(profile => profile.ApplicationUserId == applicationUserId).Select(profile => (Guid?)profile.Id).SingleOrDefaultAsync(cancellationToken);
    private static bool IsUniqueViolation(DbUpdateException exception) => exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
