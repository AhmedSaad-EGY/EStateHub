using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.Notifications;
using EstateHub.Application.PlatformReviews;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.PlatformReviews;

public sealed class PlatformReviewModerationService(
    EstateHubDbContext dbContext,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider) : IPlatformReviewModerationService
{
    public async Task<PlatformReviewModerationResult<PagedResult<PlatformReviewSummary>>> GetReviewsAsync(
        PlatformReviewDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reviews = FilteredReviews(query);
            var totalCount = await reviews.CountAsync(cancellationToken);
            var items = await reviews
                .OrderByDescending(review => review.CreatedAt)
                .ThenByDescending(review => review.Id)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(review => new PlatformReviewSummary(
                    review.Id,
                    review.ViewingBookingId,
                    review.Rating,
                    review.Comment,
                    review.Status,
                    review.CreatedAt,
                    review.ModeratedByApplicationUserId,
                    review.ModeratedAt,
                    review.ModerationReason,
                    new PlatformReviewCompanySummary(
                        review.ViewingBooking.ViewingSlot.Listing.Company.Id,
                        review.ViewingBooking.ViewingSlot.Listing.Company.Slug,
                        review.ViewingBooking.ViewingSlot.Listing.Company.DisplayName),
                    new PlatformReviewListingSummary(
                        review.ViewingBooking.ViewingSlot.Listing.Id,
                        review.ViewingBooking.ViewingSlot.Listing.Slug,
                        review.ViewingBooking.ViewingSlot.Listing.Title),
                    new PlatformReviewReviewerSummary(
                        review.ViewingBooking.CustomerProfile.Id,
                        review.ViewingBooking.CustomerProfile.FullName)))
                .ToListAsync(cancellationToken);

            return new(
                PlatformReviewModerationOperationStatus.Succeeded,
                new PagedResult<PlatformReviewSummary>(
                    items,
                    query.PageNumber,
                    query.PageSize,
                    totalCount));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbException)
        {
            return new(PlatformReviewModerationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<PlatformReviewModerationResult<PlatformReviewDetails>> GetReviewAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await dbContext.Set<CompanyReview>()
                .AsNoTracking()
                .Where(candidate => candidate.Id == reviewId)
                .Select(candidate => new PlatformReviewDetails(
                    new PlatformReviewSummary(
                        candidate.Id,
                        candidate.ViewingBookingId,
                        candidate.Rating,
                        candidate.Comment,
                        candidate.Status,
                        candidate.CreatedAt,
                        candidate.ModeratedByApplicationUserId,
                        candidate.ModeratedAt,
                        candidate.ModerationReason,
                        new PlatformReviewCompanySummary(
                            candidate.ViewingBooking.ViewingSlot.Listing.Company.Id,
                            candidate.ViewingBooking.ViewingSlot.Listing.Company.Slug,
                            candidate.ViewingBooking.ViewingSlot.Listing.Company.DisplayName),
                        new PlatformReviewListingSummary(
                            candidate.ViewingBooking.ViewingSlot.Listing.Id,
                            candidate.ViewingBooking.ViewingSlot.Listing.Slug,
                            candidate.ViewingBooking.ViewingSlot.Listing.Title),
                        new PlatformReviewReviewerSummary(
                            candidate.ViewingBooking.CustomerProfile.Id,
                            candidate.ViewingBooking.CustomerProfile.FullName)),
                    candidate.ViewingBooking.BookingCode,
                    candidate.ViewingBooking.Status,
                    candidate.ViewingBooking.CompletedAt,
                    candidate.ViewingBooking.ViewingSlot.StartsAt))
                .SingleOrDefaultAsync(cancellationToken);

            return review is null
                ? new(PlatformReviewModerationOperationStatus.NotFound)
                : new(PlatformReviewModerationOperationStatus.Succeeded, review);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbException)
        {
            return new(PlatformReviewModerationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<PlatformReviewModerationOperationStatus> ModerateAsync(
        Guid actingPlatformAdminId,
        Guid reviewId,
        PlatformReviewModerationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var current = await dbContext.Set<CompanyReview>()
                .AsNoTracking()
                .Where(review => review.Id == reviewId)
                .Select(review => new ReviewModerationState(
                    review.ViewingBookingId,
                    review.ViewingBooking.CustomerProfile.ApplicationUserId,
                    review.Status,
                    review.ModerationReason))
                .SingleOrDefaultAsync(cancellationToken);
            if (current is null)
            {
                return PlatformReviewModerationOperationStatus.NotFound;
            }

            if (current.Status == command.Status
                && string.Equals(
                    current.ModerationReason,
                    command.Reason,
                    StringComparison.Ordinal))
            {
                await transaction.CommitAsync(cancellationToken);
                return PlatformReviewModerationOperationStatus.Succeeded;
            }

            if (current.Status != command.ExpectedStatus)
            {
                return PlatformReviewModerationOperationStatus.Conflict;
            }

            var moderatedAt = timeProvider.GetUtcNow();
            var affected = await dbContext.Set<CompanyReview>()
                .Where(review =>
                    review.Id == reviewId
                    && review.Status == command.ExpectedStatus)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(review => review.Status, command.Status)
                        .SetProperty(
                            review => review.ModeratedByApplicationUserId,
                            actingPlatformAdminId)
                        .SetProperty(review => review.ModeratedAt, moderatedAt)
                        .SetProperty(review => review.ModerationReason, command.Reason),
                    cancellationToken);
            if (affected != 1)
            {
                return PlatformReviewModerationOperationStatus.Conflict;
            }

            if (current.RecipientApplicationUserId == Guid.Empty)
            {
                return PlatformReviewModerationOperationStatus.ServiceUnavailable;
            }

            var title = command.Status == CompanyReviewStatus.Hidden
                ? "Company review hidden"
                : "Company review visible";
            var body = command.Status == CompanyReviewStatus.Hidden
                ? $"Your company review was hidden. Reason: {command.Reason}"
                : "Your company review is visible.";
            notificationWriter.Add(new NotificationWriteCommand(
                current.RecipientApplicationUserId,
                title,
                body,
                new CompanyReviewModeratedNotificationPayload(
                    reviewId,
                    current.ViewingBookingId,
                    command.Status)));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return PlatformReviewModerationOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return PlatformReviewModerationOperationStatus.ServiceUnavailable;
        }
        catch (DbException)
        {
            return PlatformReviewModerationOperationStatus.ServiceUnavailable;
        }
    }

    private IQueryable<CompanyReview> FilteredReviews(PlatformReviewDirectoryQuery query)
    {
        var reviews = dbContext.Set<CompanyReview>().AsNoTracking();
        if (query.Status is not null)
        {
            reviews = reviews.Where(review => review.Status == query.Status.Value);
        }

        if (query.Rating is not null)
        {
            reviews = reviews.Where(review => review.Rating == query.Rating.Value);
        }

        if (query.CompanyId is not null)
        {
            reviews = reviews.Where(review =>
                review.ViewingBooking.ViewingSlot.Listing.CompanyId == query.CompanyId.Value);
        }

        if (query.CreatedFrom is not null)
        {
            reviews = reviews.Where(review => review.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo is not null)
        {
            reviews = reviews.Where(review => review.CreatedAt <= query.CreatedTo.Value);
        }

        return reviews;
    }

    private sealed record ReviewModerationState(
        Guid ViewingBookingId,
        Guid RecipientApplicationUserId,
        CompanyReviewStatus Status,
        string? ModerationReason);
}
