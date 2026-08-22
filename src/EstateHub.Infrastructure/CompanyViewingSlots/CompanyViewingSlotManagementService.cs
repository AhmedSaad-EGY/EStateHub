using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyViewingSlots;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyViewingSlots;

public sealed class CompanyViewingSlotManagementService(EstateHubDbContext dbContext, ICompanyAccessService companyAccessService, TimeProvider timeProvider) : ICompanyViewingSlotManagementService
{
    public async Task<PagedResult<CompanyViewingSlotDetails>?> GetSlotsAsync(Guid applicationUserId, CompanyViewingSlotDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsRead, cancellationToken);
        if (scope is null) return null;
        var slots = DetailsQuery(scope.CompanyId);
        if (query.ListingId is not null) slots = slots.Where(x => x.ListingId == query.ListingId);
        if (query.Status is not null) slots = slots.Where(x => x.Status == query.Status);
        if (query.From is not null) slots = slots.Where(x => x.StartsAt >= query.From);
        if (query.To is not null) slots = slots.Where(x => x.StartsAt < query.To);
        var total = await slots.CountAsync(cancellationToken);
        var items = await slots.OrderBy(x => x.StartsAt).ThenBy(x => x.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<CompanyViewingSlotDetails>(items, query.PageNumber, query.PageSize, total);
    }

    public async Task<CompanyViewingSlotDetails?> GetSlotAsync(Guid applicationUserId, Guid slotId, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsRead, cancellationToken);
        return scope is null ? null : await DetailsQuery(scope.CompanyId).SingleOrDefaultAsync(x => x.Id == slotId, cancellationToken);
    }

    public async Task<CompanyViewingSlotMutationResult> CreateSlotAsync(Guid applicationUserId, CompanyViewingSlotCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null) return new(CompanyViewingSlotOperationStatus.NotFound);
        var utcNow = timeProvider.GetUtcNow();
        if (command.StartsAt <= utcNow || command.EndsAt <= command.StartsAt) return new(CompanyViewingSlotOperationStatus.Conflict);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var eligible = await dbContext.Set<Listing>().AsNoTracking().WherePublic(utcNow)
                .AnyAsync(x => x.Id == command.ListingId && x.CompanyId == scope.CompanyId, cancellationToken);
            if (!eligible) return new(CompanyViewingSlotOperationStatus.Conflict);
            var slot = new ViewingSlot { Id = Guid.NewGuid(), ListingId = command.ListingId, StartsAt = command.StartsAt, EndsAt = command.EndsAt, Capacity = command.Capacity, MeetingPoint = command.MeetingPoint, Status = ViewingSlotStatus.Open };
            dbContext.Add(slot);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CompanyViewingSlotOperationStatus.Succeeded, await DetailsQuery(scope.CompanyId).SingleAsync(x => x.Id == slot.Id, cancellationToken));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return new(CompanyViewingSlotOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyViewingSlotOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyViewingSlotOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyViewingSlotMutationResult> UpdateSlotAsync(Guid applicationUserId, Guid slotId, CompanyViewingSlotCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null) return new(CompanyViewingSlotOperationStatus.NotFound);
        var utcNow = timeProvider.GetUtcNow();
        if (command.StartsAt <= utcNow || command.EndsAt <= command.StartsAt) return new(CompanyViewingSlotOperationStatus.Conflict);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var slot = await SlotAsync(scope.CompanyId, slotId, cancellationToken);
            if (slot is null) return new(CompanyViewingSlotOperationStatus.NotFound);
            if (slot.Status == ViewingSlotStatus.Cancelled) return new(CompanyViewingSlotOperationStatus.Conflict);
            var (activeBookingCount, reservedVisitorCount) = await ActiveBookingTotalsAsync(slotId, cancellationToken);
            if (command.Capacity < reservedVisitorCount || (activeBookingCount > 0 && (command.StartsAt != slot.StartsAt || command.EndsAt != slot.EndsAt))) return new(CompanyViewingSlotOperationStatus.Conflict);
            dbContext.Entry(slot).Property(x => x.RowVersion).OriginalValue = command.RowVersion!;
            slot.StartsAt = command.StartsAt; slot.EndsAt = command.EndsAt; slot.Capacity = command.Capacity; slot.MeetingPoint = command.MeetingPoint;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CompanyViewingSlotOperationStatus.Succeeded, await DetailsQuery(scope.CompanyId).SingleAsync(x => x.Id == slotId, cancellationToken));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyViewingSlotOperationStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return new(CompanyViewingSlotOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyViewingSlotOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyViewingSlotOperationStatus.ServiceUnavailable); }
    }

    public Task<CompanyViewingSlotOperationStatus> CloseSlotAsync(Guid userId, Guid slotId, byte[] rowVersion, CancellationToken cancellationToken = default) => TransitionAsync(userId, slotId, rowVersion, ViewingSlotStatus.Open, ViewingSlotStatus.Closed, cancellationToken);
    public Task<CompanyViewingSlotOperationStatus> ReopenSlotAsync(Guid userId, Guid slotId, byte[] rowVersion, CancellationToken cancellationToken = default) => TransitionAsync(userId, slotId, rowVersion, ViewingSlotStatus.Closed, ViewingSlotStatus.Open, cancellationToken);
    public Task<CompanyViewingSlotOperationStatus> CancelSlotAsync(Guid userId, Guid slotId, byte[] rowVersion, CancellationToken cancellationToken = default) => TransitionAsync(userId, slotId, rowVersion, null, ViewingSlotStatus.Cancelled, cancellationToken);

    private async Task<CompanyViewingSlotOperationStatus> TransitionAsync(Guid applicationUserId, Guid slotId, byte[] rowVersion, ViewingSlotStatus? requiredStatus, ViewingSlotStatus targetStatus, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null) return CompanyViewingSlotOperationStatus.NotFound;
        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var slot = await SlotAsync(scope.CompanyId, slotId, cancellationToken);
            if (slot is null) return CompanyViewingSlotOperationStatus.NotFound;
            if (slot.Status == targetStatus) return CompanyViewingSlotOperationStatus.Succeeded;
            if (slot.Status == ViewingSlotStatus.Cancelled || (requiredStatus is not null && slot.Status != requiredStatus)) return CompanyViewingSlotOperationStatus.Conflict;
            if (targetStatus == ViewingSlotStatus.Open)
            {
                var (_, reservedVisitorCount) = await ActiveBookingTotalsAsync(slotId, cancellationToken);
                var eligible = await dbContext.Set<Listing>().AsNoTracking().WherePublic(utcNow).AnyAsync(x => x.Id == slot.ListingId && x.CompanyId == scope.CompanyId, cancellationToken);
                if (slot.StartsAt <= utcNow || slot.Capacity < reservedVisitorCount || !eligible) return CompanyViewingSlotOperationStatus.Conflict;
            }
            if (targetStatus == ViewingSlotStatus.Cancelled)
            {
                var (activeBookingCount, _) = await ActiveBookingTotalsAsync(slotId, cancellationToken);
                if (slot.StartsAt <= utcNow || activeBookingCount != 0) return CompanyViewingSlotOperationStatus.Conflict;
            }
            dbContext.Entry(slot).Property(x => x.RowVersion).OriginalValue = rowVersion;
            slot.Status = targetStatus;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyViewingSlotOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CompanyViewingSlotOperationStatus.Conflict; }
        catch (DbUpdateException) { return CompanyViewingSlotOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CompanyViewingSlotOperationStatus.ServiceUnavailable; }
    }

    private IQueryable<CompanyViewingSlotDetails> DetailsQuery(Guid companyId) => dbContext.Set<ViewingSlot>().AsNoTracking().Where(x => x.Listing.CompanyId == companyId).Select(x => new CompanyViewingSlotDetails(x.Id, x.ListingId, x.Listing.ListingCode, x.Listing.Slug, x.Listing.Title, x.StartsAt, x.EndsAt, x.Capacity, x.MeetingPoint, x.Status, x.Bookings.Count(booking => booking.Status == ViewingBookingStatus.Pending || booking.Status == ViewingBookingStatus.Confirmed || booking.Status == ViewingBookingStatus.CheckedIn), x.Bookings.Where(booking => booking.Status == ViewingBookingStatus.Pending || booking.Status == ViewingBookingStatus.Confirmed || booking.Status == ViewingBookingStatus.CheckedIn).Sum(booking => (int?)booking.VisitorCount) ?? 0, x.RowVersion));
    private Task<ViewingSlot?> SlotAsync(Guid companyId, Guid slotId, CancellationToken cancellationToken) => dbContext.Set<ViewingSlot>().SingleOrDefaultAsync(x => x.Id == slotId && x.Listing.CompanyId == companyId, cancellationToken);
    private async Task<(int ActiveBookingCount, int ReservedVisitorCount)> ActiveBookingTotalsAsync(Guid slotId, CancellationToken cancellationToken) { var bookings = dbContext.Set<ViewingBooking>().AsNoTracking().Where(x => x.ViewingSlotId == slotId && (x.Status == ViewingBookingStatus.Pending || x.Status == ViewingBookingStatus.Confirmed || x.Status == ViewingBookingStatus.CheckedIn)); return (await bookings.CountAsync(cancellationToken), await bookings.SumAsync(x => (int?)x.VisitorCount, cancellationToken) ?? 0); }
    private async Task<CompanyAccessScope?> ScopeAsync(Guid userId, string permissionCode, CancellationToken cancellationToken) => await companyAccessService.GetAuthorizedScopeAsync(userId, permissionCode, cancellationToken);
    private static bool IsUniqueViolation(DbUpdateException exception) => exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
