using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyViewingBookings;
using EstateHub.Application.Notifications;
using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using EstateHub.Infrastructure.ViewingBookings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyViewingBookings;

public sealed class CompanyViewingBookingManagementService(
    EstateHubDbContext dbContext,
    ICompanyAccessService companyAccessService,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider) : ICompanyViewingBookingManagementService
{
    public async Task<PagedResult<CompanyViewingBookingSummary>?> GetBookingsAsync(
        Guid applicationUserId,
        CompanyViewingBookingDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsRead, cancellationToken);
        if (scope is null)
        {
            return null;
        }

        var bookings = BookingQuery(scope.CompanyId);
        if (query.Search is not null)
        {
            bookings = bookings.Where(booking =>
                booking.BookingCode.Contains(query.Search)
                || booking.ContactPhone.Contains(query.Search)
                || booking.CustomerProfile.FullName.Contains(query.Search));
        }

        if (query.Status is not null)
        {
            bookings = bookings.Where(booking => booking.Status == query.Status);
        }

        if (query.ListingId is not null)
        {
            bookings = bookings.Where(booking => booking.ViewingSlot.ListingId == query.ListingId);
        }

        if (query.ViewingSlotId is not null)
        {
            bookings = bookings.Where(booking => booking.ViewingSlotId == query.ViewingSlotId);
        }

        if (query.AssignedEmployeeId is not null)
        {
            bookings = bookings.Where(booking => booking.AssignedEmployeeId == query.AssignedEmployeeId);
        }

        if (query.From is not null)
        {
            bookings = bookings.Where(booking => booking.ViewingSlot.StartsAt >= query.From);
        }

        if (query.To is not null)
        {
            bookings = bookings.Where(booking => booking.ViewingSlot.StartsAt < query.To);
        }

        var totalCount = await bookings.CountAsync(cancellationToken);
        var page = bookings
            .OrderByDescending(booking => booking.ViewingSlot.StartsAt)
            .ThenByDescending(booking => booking.CreatedAt)
            .ThenByDescending(booking => booking.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize);
        var items = await ProjectSummaries(page)
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyViewingBookingSummary>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<CompanyViewingBookingDetails?> GetBookingAsync(
        Guid applicationUserId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsRead, cancellationToken);
        return scope is null
            ? null
            : await DetailsAsync(scope.CompanyId, bookingId, cancellationToken);
    }

    public async Task<CompanyViewingBookingMutationResult> AssignEmployeeAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null)
        {
            return new(CompanyViewingBookingOperationStatus.NotFound);
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var booking = await OwnedBookingAsync(scope.CompanyId, bookingId, cancellationToken);
            if (booking is null)
            {
                return new(CompanyViewingBookingOperationStatus.NotFound);
            }

            if (booking.Status is not (
                ViewingBookingStatus.Pending
                or ViewingBookingStatus.Confirmed
                or ViewingBookingStatus.CheckedIn))
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            if (!RowVersionMatches(booking, command.RowVersion))
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            if (command.EmployeeId is not null)
            {
                var employeeIsEligible = await dbContext.Set<CompanyEmployee>()
                    .AsNoTracking()
                    .AnyAsync(employee =>
                        employee.Id == command.EmployeeId
                        && employee.CompanyId == scope.CompanyId
                        && employee.Status == CompanyEmployeeStatus.Active
                        && employee.EndedAt == null,
                        cancellationToken);
                if (!employeeIsEligible)
                {
                    return new(CompanyViewingBookingOperationStatus.NotFound);
                }
            }

            dbContext.Entry(booking).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion;
            booking.AssignedEmployeeId = command.EmployeeId;
            dbContext.Entry(booking).Property(candidate => candidate.AssignedEmployeeId).IsModified = true;

            await dbContext.SaveChangesAsync(cancellationToken);
            var details = await DetailsAsync(scope.CompanyId, booking.Id, cancellationToken);
            if (details is null)
            {
                return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
            }

            await transaction.CommitAsync(cancellationToken);
            return new(CompanyViewingBookingOperationStatus.Succeeded, details);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CompanyViewingBookingOperationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<CompanyViewingBookingOperationStatus> ConfirmAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null)
        {
            return CompanyViewingBookingOperationStatus.NotFound;
        }

        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var booking = await OwnedBookingAsync(scope.CompanyId, bookingId, cancellationToken);
            if (booking is null)
            {
                return CompanyViewingBookingOperationStatus.NotFound;
            }

            if (booking.Status == ViewingBookingStatus.Confirmed)
            {
                var existingCharge = await ChargeAccountingStateAsync(booking.Id, cancellationToken);
                return IsValidConfirmedCharge(existingCharge, scope.CompanyId)
                    ? CompanyViewingBookingOperationStatus.Succeeded
                    : CompanyViewingBookingOperationStatus.ServiceUnavailable;
            }

            if (booking.Status != ViewingBookingStatus.Pending)
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            if (!RowVersionMatches(booking, command.RowVersion))
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            var slot = await dbContext.Set<ViewingSlot>()
                .AsNoTracking()
                .Where(candidate => candidate.Id == booking.ViewingSlotId)
                .Select(candidate => new SlotTiming(
                    candidate.ListingId,
                    candidate.Status,
                    candidate.StartsAt,
                    candidate.EndsAt))
                .SingleOrDefaultAsync(cancellationToken);
            if (slot is null)
            {
                return CompanyViewingBookingOperationStatus.ServiceUnavailable;
            }

            if (slot.Status == ViewingSlotStatus.Cancelled || slot.StartsAt <= utcNow)
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            var subscriptions = await CurrentSubscriptionQuery(scope.CompanyId, utcNow)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (subscriptions.Count != 1)
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            var subscription = subscriptions[0];
            if (subscription.BookingFeeSnapshot < 0m)
            {
                return CompanyViewingBookingOperationStatus.ServiceUnavailable;
            }

            var chargeExists = await dbContext.Set<BookingCharge>()
                .AsNoTracking()
                .AnyAsync(charge => charge.ViewingBookingId == booking.Id, cancellationToken);
            if (chargeExists)
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            var sourceLeads = await dbContext.Set<Lead>()
                .Where(lead => lead.SourceBookingId == booking.Id)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (sourceLeads.Count > 1)
            {
                return CompanyViewingBookingOperationStatus.ServiceUnavailable;
            }

            Lead lead;
            if (sourceLeads.Count == 0)
            {
                var customer = await BookingLeadIntegration
                    .CustomerIdentityByProfileQuery(dbContext, booking.CustomerProfileId)
                    .SingleOrDefaultAsync(cancellationToken);
                if (customer is null)
                {
                    return CompanyViewingBookingOperationStatus.ServiceUnavailable;
                }

                lead = BookingLeadIntegration.CreateRequestedLead(
                    scope.CompanyId,
                    customer,
                    booking.Id,
                    slot.ListingId,
                    booking.ContactPhone,
                    utcNow);
                dbContext.Add(lead);
            }
            else
            {
                lead = sourceLeads[0];
                if (lead.CompanyId != scope.CompanyId
                    || lead.CustomerProfileId != booking.CustomerProfileId)
                {
                    return CompanyViewingBookingOperationStatus.ServiceUnavailable;
                }

                var interestExists = await dbContext.Set<LeadInterest>()
                    .AsNoTracking()
                    .AnyAsync(interest =>
                        interest.LeadId == lead.Id
                        && interest.ListingId == slot.ListingId
                        && interest.InterestType == BookingLeadIntegration.InterestType,
                        cancellationToken);
                if (!interestExists)
                {
                    dbContext.Add(BookingLeadIntegration.CreateInterest(
                        lead.Id,
                        slot.ListingId,
                        utcNow));
                }
            }

            if (lead.OwnerEmployeeId is null)
            {
                var assignedEmployeeIsEligible = booking.AssignedEmployeeId is not null
                    && await dbContext.Set<CompanyEmployee>()
                        .AsNoTracking()
                        .AnyAsync(employee =>
                            employee.Id == booking.AssignedEmployeeId
                            && employee.CompanyId == scope.CompanyId
                            && employee.Status == CompanyEmployeeStatus.Active
                            && employee.EndedAt == null,
                            cancellationToken);
                lead.OwnerEmployeeId = assignedEmployeeIsEligible
                    ? booking.AssignedEmployeeId
                    : scope.CompanyEmployeeId;
            }

            lead.Stage = BookingLeadIntegration.AdvanceStage(lead.Stage);
            lead.LastActivityAt = utcNow;
            dbContext.Add(BookingLeadIntegration.CreateConfirmedActivity(
                lead.Id,
                scope.CompanyEmployeeId,
                utcNow,
                command.Reason));

            dbContext.Entry(booking).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion;
            var previousStatus = booking.Status;
            booking.Status = ViewingBookingStatus.Confirmed;
            booking.ConfirmedAt = utcNow;

            var recipientApplicationUserId = await RecipientApplicationUserIdAsync(
                booking.CustomerProfileId,
                cancellationToken);
            if (recipientApplicationUserId is null)
            {
                return CompanyViewingBookingOperationStatus.ServiceUnavailable;
            }

            dbContext.Add(CreateStatusHistory(
                booking.Id,
                applicationUserId,
                previousStatus,
                ViewingBookingStatus.Confirmed,
                utcNow,
                command.Reason));
            dbContext.Add(new BookingCharge
            {
                Id = Guid.NewGuid(),
                ViewingBookingId = booking.Id,
                CompanyId = scope.CompanyId,
                CompanySubscriptionId = subscription.Id,
                AmountSnapshot = subscription.BookingFeeSnapshot,
                CurrencyCode = subscription.CurrencyCode,
                Status = BookingChargeStatus.Billable,
                CreatedAt = utcNow,
                VoidedAt = null,
                VoidReason = null
            });
            AddBookingStatusNotification(
                recipientApplicationUserId.Value,
                booking.Id,
                ViewingBookingStatus.Confirmed);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyViewingBookingOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompanyViewingBookingOperationStatus.Conflict;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return CompanyViewingBookingOperationStatus.Conflict;
        }
        catch (DbUpdateException)
        {
            return CompanyViewingBookingOperationStatus.ServiceUnavailable;
        }
        catch (DbException exception) when (IsUniqueViolation(exception))
        {
            return CompanyViewingBookingOperationStatus.Conflict;
        }
        catch (DbException)
        {
            return CompanyViewingBookingOperationStatus.ServiceUnavailable;
        }
    }

    public Task<CompanyViewingBookingOperationStatus> RejectAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(applicationUserId, bookingId, command, LifecycleOperation.Reject, cancellationToken);

    public Task<CompanyViewingBookingOperationStatus> CheckInAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(applicationUserId, bookingId, command, LifecycleOperation.CheckIn, cancellationToken);

    public Task<CompanyViewingBookingOperationStatus> CompleteAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(applicationUserId, bookingId, command, LifecycleOperation.Complete, cancellationToken);

    public Task<CompanyViewingBookingOperationStatus> MarkNoShowAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(applicationUserId, bookingId, command, LifecycleOperation.NoShow, cancellationToken);

    public Task<CompanyViewingBookingOperationStatus> CancelAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(applicationUserId, bookingId, command, LifecycleOperation.Cancel, cancellationToken);

    public async Task<CompanyViewingBookingMutationResult> RescheduleAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingRescheduleCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null)
        {
            return new(CompanyViewingBookingOperationStatus.NotFound);
        }

        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var lockedTargetCount = await dbContext.Set<ViewingSlot>()
                .Where(slot =>
                    slot.Id == command.ToViewingSlotId
                    && slot.Listing.CompanyId == scope.CompanyId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(slot => slot.Status, slot => slot.Status),
                    cancellationToken);
            if (lockedTargetCount != 1)
            {
                return new(CompanyViewingBookingOperationStatus.NotFound);
            }

            var booking = await OwnedBookingAsync(scope.CompanyId, bookingId, cancellationToken);
            if (booking is null)
            {
                return new(CompanyViewingBookingOperationStatus.NotFound);
            }

            var targetSlot = await dbContext.Set<ViewingSlot>()
                .AsNoTracking()
                .Where(slot => slot.Id == command.ToViewingSlotId)
                .Select(slot => new TargetSlot(
                    slot.Id,
                    slot.ListingId,
                    slot.Status,
                    slot.StartsAt,
                    slot.Capacity))
                .SingleAsync(cancellationToken);

            if (!RowVersionMatches(booking, command.RowVersion))
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            if (booking.Status is not (ViewingBookingStatus.Pending or ViewingBookingStatus.Confirmed))
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            if (booking.ViewingSlotId == targetSlot.Id)
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            var sourceSlot = await dbContext.Set<ViewingSlot>()
                .AsNoTracking()
                .Where(slot => slot.Id == booking.ViewingSlotId)
                .Select(slot => new SourceSlot(slot.Id, slot.ListingId, slot.StartsAt))
                .SingleOrDefaultAsync(cancellationToken);
            if (sourceSlot is null)
            {
                return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
            }

            if (targetSlot.ListingId != sourceSlot.ListingId)
            {
                return new(CompanyViewingBookingOperationStatus.NotFound);
            }

            if (targetSlot.Status != ViewingSlotStatus.Open || targetSlot.StartsAt <= utcNow)
            {
                return new(CompanyViewingBookingOperationStatus.NotFound);
            }

            if (sourceSlot.StartsAt <= utcNow)
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            var activeTargetBookings = dbContext.Set<ViewingBooking>()
                .AsNoTracking()
                .Where(candidate =>
                    candidate.ViewingSlotId == targetSlot.Id
                    && (candidate.Status == ViewingBookingStatus.Pending
                        || candidate.Status == ViewingBookingStatus.Confirmed
                        || candidate.Status == ViewingBookingStatus.CheckedIn));

            var reservedVisitors = await activeTargetBookings
                .SumAsync(candidate => (int?)candidate.VisitorCount, cancellationToken) ?? 0;
            if (reservedVisitors + booking.VisitorCount > targetSlot.Capacity)
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            var duplicateCustomerBooking = await activeTargetBookings
                .AnyAsync(candidate => candidate.CustomerProfileId == booking.CustomerProfileId, cancellationToken);
            if (duplicateCustomerBooking)
            {
                return new(CompanyViewingBookingOperationStatus.Conflict);
            }

            dbContext.Entry(booking).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion;
            var originalSlotId = booking.ViewingSlotId;
            booking.ViewingSlotId = targetSlot.Id;
            var recipientApplicationUserId = await RecipientApplicationUserIdAsync(
                booking.CustomerProfileId,
                cancellationToken);
            if (recipientApplicationUserId is null)
            {
                return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
            }

            dbContext.Add(new BookingRescheduleHistory
            {
                Id = Guid.NewGuid(),
                ViewingBookingId = booking.Id,
                FromViewingSlotId = originalSlotId,
                ToViewingSlotId = targetSlot.Id,
                ChangedByApplicationUserId = applicationUserId,
                ChangedAt = utcNow,
                Reason = command.Reason
            });
            notificationWriter.Add(new NotificationWriteCommand(
                recipientApplicationUserId.Value,
                "Viewing booking rescheduled",
                "Your viewing booking was rescheduled.",
                new ViewingBookingRescheduledNotificationPayload(
                    booking.Id,
                    booking.Status,
                    targetSlot.Id)));

            await dbContext.SaveChangesAsync(cancellationToken);
            var details = await DetailsAsync(scope.CompanyId, booking.Id, cancellationToken);
            if (details is null)
            {
                return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
            }

            await transaction.CommitAsync(cancellationToken);
            return new(CompanyViewingBookingOperationStatus.Succeeded, details);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CompanyViewingBookingOperationStatus.Conflict);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return new(CompanyViewingBookingOperationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
        }
        catch (DbException exception) when (IsUniqueViolation(exception))
        {
            return new(CompanyViewingBookingOperationStatus.Conflict);
        }
        catch (DbException)
        {
            return new(CompanyViewingBookingOperationStatus.ServiceUnavailable);
        }
    }

    private async Task<CompanyViewingBookingOperationStatus> TransitionAsync(
        Guid applicationUserId,
        Guid bookingId,
        CompanyViewingBookingLifecycleCommand command,
        LifecycleOperation operation,
        CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(applicationUserId, CompanyPermissionCodes.BookingsManage, cancellationToken);
        if (scope is null)
        {
            return CompanyViewingBookingOperationStatus.NotFound;
        }

        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var booking = await OwnedBookingAsync(scope.CompanyId, bookingId, cancellationToken);
            if (booking is null)
            {
                return CompanyViewingBookingOperationStatus.NotFound;
            }

            var targetStatus = TargetStatus(operation);
            if (booking.Status == targetStatus)
            {
                return CompanyViewingBookingOperationStatus.Succeeded;
            }

            if (!TransitionIsAllowed(booking.Status, operation))
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            if (!RowVersionMatches(booking, command.RowVersion))
            {
                return CompanyViewingBookingOperationStatus.Conflict;
            }

            if (operation is LifecycleOperation.CheckIn or LifecycleOperation.NoShow or LifecycleOperation.Cancel)
            {
                var slot = await dbContext.Set<ViewingSlot>()
                    .AsNoTracking()
                    .Where(candidate => candidate.Id == booking.ViewingSlotId)
                    .Select(candidate => new SlotTiming(
                        candidate.ListingId,
                        candidate.Status,
                        candidate.StartsAt,
                        candidate.EndsAt))
                    .SingleOrDefaultAsync(cancellationToken);
                if (slot is null)
                {
                    return CompanyViewingBookingOperationStatus.ServiceUnavailable;
                }

                var timingIsValid = operation switch
                {
                    LifecycleOperation.CheckIn => slot.StartsAt <= utcNow && utcNow < slot.EndsAt,
                    LifecycleOperation.NoShow => utcNow >= slot.EndsAt,
                    LifecycleOperation.Cancel => slot.StartsAt > utcNow,
                    _ => true
                };
                if (!timingIsValid)
                {
                    return CompanyViewingBookingOperationStatus.Conflict;
                }
            }

            dbContext.Entry(booking).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion;
            var previousStatus = booking.Status;
            booking.Status = targetStatus;

            var recipientApplicationUserId = await RecipientApplicationUserIdAsync(
                booking.CustomerProfileId,
                cancellationToken);
            if (recipientApplicationUserId is null)
            {
                return CompanyViewingBookingOperationStatus.ServiceUnavailable;
            }

            switch (operation)
            {
                case LifecycleOperation.CheckIn:
                    booking.CheckedInAt = utcNow;
                    break;
                case LifecycleOperation.Complete:
                    booking.CompletedAt = utcNow;
                    break;
                case LifecycleOperation.Cancel:
                    booking.CancellationSource = BookingCancellationSource.Company;
                    booking.CancelledAt = utcNow;
                    break;
            }

            dbContext.Add(CreateStatusHistory(
                booking.Id,
                applicationUserId,
                previousStatus,
                targetStatus,
                utcNow,
                command.Reason));
            AddBookingStatusNotification(
                recipientApplicationUserId.Value,
                booking.Id,
                targetStatus);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyViewingBookingOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompanyViewingBookingOperationStatus.Conflict;
        }
        catch (DbUpdateException)
        {
            return CompanyViewingBookingOperationStatus.ServiceUnavailable;
        }
        catch (DbException)
        {
            return CompanyViewingBookingOperationStatus.ServiceUnavailable;
        }
    }

    private async Task<CompanyViewingBookingDetails?> DetailsAsync(
        Guid companyId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var header = await HeaderQuery(companyId, bookingId)
            .SingleOrDefaultAsync(cancellationToken);
        if (header is null)
        {
            return null;
        }

        var statusHistory = await dbContext.Set<BookingStatusHistory>()
            .AsNoTracking()
            .Where(history => history.ViewingBookingId == bookingId)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .Select(history => new CompanyViewingBookingStatusHistoryItem(
                history.Id,
                history.FromStatus,
                history.ToStatus,
                history.ActorType,
                history.ChangedAt,
                history.Reason))
            .ToListAsync(cancellationToken);

        var rescheduleHistory = await dbContext.Set<BookingRescheduleHistory>()
            .AsNoTracking()
            .Where(history => history.ViewingBookingId == bookingId)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .Select(history => new CompanyViewingBookingRescheduleHistoryItem(
                history.Id,
                history.FromViewingSlotId,
                history.FromViewingSlot.StartsAt,
                history.FromViewingSlot.EndsAt,
                history.ToViewingSlotId,
                history.ToViewingSlot.StartsAt,
                history.ToViewingSlot.EndsAt,
                history.ChangedAt,
                history.Reason))
            .ToListAsync(cancellationToken);

        return new CompanyViewingBookingDetails(header, statusHistory, rescheduleHistory);
    }

    private IQueryable<ViewingBooking> BookingQuery(Guid companyId) =>
        dbContext.Set<ViewingBooking>()
            .AsNoTracking()
            .Where(booking => booking.ViewingSlot.Listing.CompanyId == companyId);

    private IQueryable<CompanyViewingBookingSummary> ProjectSummaries(
        IQueryable<ViewingBooking> bookings) =>
        ProjectHeaders(bookings).Select(header => header.Summary);

    private IQueryable<CompanyViewingBookingDetailsHeader> HeaderQuery(
        Guid companyId,
        Guid bookingId) =>
        ProjectHeaders(BookingQuery(companyId).Where(booking => booking.Id == bookingId));

    private static IQueryable<CompanyViewingBookingDetailsHeader> ProjectHeaders(
        IQueryable<ViewingBooking> bookings) =>
        bookings
            .Select(booking => new CompanyViewingBookingDetailsHeader(
                new CompanyViewingBookingSummary(
                    booking.Id,
                    booking.BookingCode,
                    booking.Status,
                    booking.VisitorCount,
                    booking.ContactPhone,
                    booking.CreatedAt,
                    booking.ConfirmedAt,
                    booking.CheckedInAt,
                    booking.CancelledAt,
                    booking.CompletedAt,
                    booking.RowVersion,
                    new CompanyViewingBookingCustomerSummary(
                        booking.CustomerProfile.Id,
                        booking.CustomerProfile.FullName,
                        booking.CustomerProfile.Persona),
                    new CompanyViewingBookingSlotSummary(
                        booking.ViewingSlot.Id,
                        booking.ViewingSlot.StartsAt,
                        booking.ViewingSlot.EndsAt,
                        booking.ViewingSlot.MeetingPoint,
                        booking.ViewingSlot.Status),
                    new CompanyViewingBookingListingSummary(
                        booking.ViewingSlot.Listing.Id,
                        booking.ViewingSlot.Listing.ListingCode,
                        booking.ViewingSlot.Listing.Slug,
                        booking.ViewingSlot.Listing.Title,
                        booking.ViewingSlot.Listing.ListingType),
                    booking.AssignedEmployee == null
                        ? null
                        : new CompanyViewingBookingEmployeeSummary(
                            booking.AssignedEmployee.Id,
                            booking.AssignedEmployee.FullName,
                            booking.AssignedEmployee.JobTitle),
                    booking.Charge == null
                        ? null
                        : new CompanyViewingBookingChargeSummary(
                            booking.Charge.Id,
                            booking.Charge.AmountSnapshot,
                            booking.Charge.CurrencyCode,
                            booking.Charge.Status,
                            booking.Charge.CreatedAt,
                            booking.Charge.VoidedAt,
                            booking.Charge.VoidReason)),
                booking.SpecialRequests,
                booking.CancellationSource));

    private Task<ViewingBooking?> OwnedBookingAsync(
        Guid companyId,
        Guid bookingId,
        CancellationToken cancellationToken) =>
        dbContext.Set<ViewingBooking>()
            .SingleOrDefaultAsync(booking =>
                booking.Id == bookingId
                && booking.ViewingSlot.Listing.CompanyId == companyId,
                cancellationToken);

    private IQueryable<EffectiveSubscription> CurrentSubscriptionQuery(
        Guid companyId,
        DateTimeOffset utcNow) =>
        dbContext.Set<CompanySubscription>()
            .AsNoTracking()
            .Where(subscription =>
                subscription.CompanyId == companyId
                && subscription.Status == CompanySubscriptionStatus.Active
                && subscription.EndedAt == null
                && subscription.CurrentPeriodStart <= utcNow
                && subscription.CurrentPeriodEnd > utcNow)
            .Select(subscription => new EffectiveSubscription(
                subscription.Id,
                subscription.BookingFeeSnapshot,
                subscription.CurrencyCode));

    private Task<ChargeAccountingState?> ChargeAccountingStateAsync(
        Guid bookingId,
        CancellationToken cancellationToken) =>
        dbContext.Set<BookingCharge>()
            .AsNoTracking()
            .Where(charge => charge.ViewingBookingId == bookingId)
            .Select(charge => new ChargeAccountingState(
                charge.CompanyId,
                charge.CompanySubscriptionId,
                charge.CompanySubscription == null
                    ? null
                    : charge.CompanySubscription.CompanyId,
                charge.AmountSnapshot,
                charge.CurrencyCode))
            .SingleOrDefaultAsync(cancellationToken);

    private Task<CompanyAccessScope?> ScopeAsync(
        Guid applicationUserId,
        string permissionCode,
        CancellationToken cancellationToken) =>
        companyAccessService.GetAuthorizedScopeAsync(
            applicationUserId,
            permissionCode,
            cancellationToken);

    private async Task<Guid?> RecipientApplicationUserIdAsync(
        Guid customerProfileId,
        CancellationToken cancellationToken)
    {
        var applicationUserId = await dbContext.Set<CustomerProfile>()
            .AsNoTracking()
            .Where(profile => profile.Id == customerProfileId)
            .Select(profile => (Guid?)profile.ApplicationUserId)
            .SingleOrDefaultAsync(cancellationToken);
        return applicationUserId == Guid.Empty ? null : applicationUserId;
    }

    private void AddBookingStatusNotification(
        Guid recipientApplicationUserId,
        Guid bookingId,
        ViewingBookingStatus status)
    {
        var statusText = status switch
        {
            ViewingBookingStatus.Confirmed => "Confirmed",
            ViewingBookingStatus.Rejected => "Rejected",
            ViewingBookingStatus.CheckedIn => "CheckedIn",
            ViewingBookingStatus.Completed => "Completed",
            ViewingBookingStatus.NoShow => "NoShow",
            ViewingBookingStatus.Cancelled => "Cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported notification status.")
        };
        notificationWriter.Add(new NotificationWriteCommand(
            recipientApplicationUserId,
            "Viewing booking status updated",
            $"Your viewing booking status changed to {statusText}.",
            new ViewingBookingStatusChangedNotificationPayload(
                bookingId,
                status)));
    }

    private static BookingStatusHistory CreateStatusHistory(
        Guid bookingId,
        Guid applicationUserId,
        ViewingBookingStatus fromStatus,
        ViewingBookingStatus toStatus,
        DateTimeOffset utcNow,
        string? reason) => new()
        {
            Id = Guid.NewGuid(),
            ViewingBookingId = bookingId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByApplicationUserId = applicationUserId,
            ActorType = BookingActorType.Employee,
            ChangedAt = utcNow,
            Reason = reason
        };

    private static ViewingBookingStatus TargetStatus(LifecycleOperation operation) => operation switch
    {
        LifecycleOperation.Reject => ViewingBookingStatus.Rejected,
        LifecycleOperation.CheckIn => ViewingBookingStatus.CheckedIn,
        LifecycleOperation.Complete => ViewingBookingStatus.Completed,
        LifecycleOperation.NoShow => ViewingBookingStatus.NoShow,
        LifecycleOperation.Cancel => ViewingBookingStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };

    private static bool TransitionIsAllowed(
        ViewingBookingStatus currentStatus,
        LifecycleOperation operation) => operation switch
    {
        LifecycleOperation.Reject => currentStatus == ViewingBookingStatus.Pending,
        LifecycleOperation.CheckIn => currentStatus == ViewingBookingStatus.Confirmed,
        LifecycleOperation.Complete => currentStatus == ViewingBookingStatus.CheckedIn,
        LifecycleOperation.NoShow => currentStatus == ViewingBookingStatus.Confirmed,
        LifecycleOperation.Cancel => currentStatus is ViewingBookingStatus.Pending or ViewingBookingStatus.Confirmed,
        _ => false
    };

    private static bool RowVersionMatches(ViewingBooking booking, byte[] rowVersion) =>
        booking.RowVersion.AsSpan().SequenceEqual(rowVersion);

    private static bool IsValidConfirmedCharge(
        ChargeAccountingState? charge,
        Guid companyId) =>
        charge is not null
        && charge.CompanyId == companyId
        && charge.CompanySubscriptionId is not null
        && charge.SubscriptionCompanyId == companyId
        && charge.AmountSnapshot >= 0m
        && !string.IsNullOrWhiteSpace(charge.CurrencyCode);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private static bool IsUniqueViolation(DbException exception) =>
        exception is SqlException { Number: 2601 or 2627 };

    private sealed record SlotTiming(
        Guid ListingId,
        ViewingSlotStatus Status,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);

    private sealed record SourceSlot(
        Guid Id,
        Guid ListingId,
        DateTimeOffset StartsAt);

    private sealed record TargetSlot(
        Guid Id,
        Guid ListingId,
        ViewingSlotStatus Status,
        DateTimeOffset StartsAt,
        int Capacity);

    private sealed record EffectiveSubscription(
        Guid Id,
        decimal BookingFeeSnapshot,
        string CurrencyCode);

    private sealed record ChargeAccountingState(
        Guid CompanyId,
        Guid? CompanySubscriptionId,
        Guid? SubscriptionCompanyId,
        decimal AmountSnapshot,
        string CurrencyCode);

    private enum LifecycleOperation
    {
        Reject,
        CheckIn,
        Complete,
        NoShow,
        Cancel
    }
}
