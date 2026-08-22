using System.Data.Common;
using EstateHub.Application.Reviews;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Reviews;

public sealed class ReviewService(
    EstateHubDbContext dbContext,
    TimeProvider timeProvider) : IReviewService
{
    public async Task<PublicCompanyReviews?> GetPublicCompanyReviewsAsync(
        string companySlug,
        PublicCompanyReviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var company = await PublicCompanies()
            .Where(candidate => candidate.Slug == companySlug)
            .Select(candidate => new PublicCompanyHeader(candidate.Id, candidate.Slug))
            .SingleOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            return null;
        }

        var visibleReviews = VisibleCompanyReviews(company.Id);
        var statistics = await visibleReviews
            .GroupBy(_ => 1)
            .Select(group => new ReviewStatistics(
                group.Count(),
                group.Average(review => (double)review.Rating)))
            .SingleOrDefaultAsync(cancellationToken);

        var filteredReviews = query.Rating is null
            ? visibleReviews
            : visibleReviews.Where(review => review.Rating == query.Rating.Value);
        var filteredCount = await filteredReviews.CountAsync(cancellationToken);
        var items = await filteredReviews
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(review => new PublicCompanyReviewItem(
                review.Id,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.ViewingBooking.CustomerProfile.FullName))
            .ToListAsync(cancellationToken);

        return new PublicCompanyReviews(
            company.Id,
            company.Slug,
            statistics?.AverageRating,
            statistics?.ReviewCount ?? 0,
            filteredCount,
            items,
            query.PageNumber,
            query.PageSize);
    }

    public async Task<CustomerReview?> GetCustomerReviewAsync(
        Guid applicationUserId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        return customerProfileId is null
            ? null
            : await CustomerReviewQuery(customerProfileId.Value, bookingId)
                .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ReviewMutationResult> CreateReviewAsync(
        Guid applicationUserId,
        Guid bookingId,
        ReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        if (customerProfileId is null)
        {
            return new(ReviewOperationStatus.NotFound);
        }

        var utcNow = timeProvider.GetUtcNow();
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var booking = await dbContext.Set<ViewingBooking>()
                .AsNoTracking()
                .Where(candidate =>
                    candidate.Id == bookingId
                    && candidate.CustomerProfileId == customerProfileId.Value)
                .Select(candidate => new CompletedBookingReviewEligibility(
                    candidate.Status,
                    candidate.Review != null))
                .SingleOrDefaultAsync(cancellationToken);
            if (booking is null)
            {
                return new(ReviewOperationStatus.NotFound);
            }

            if (booking.Status != ViewingBookingStatus.Completed)
            {
                return new(ReviewOperationStatus.Conflict);
            }

            if (booking.HasReview)
            {
                return new(ReviewOperationStatus.Conflict);
            }

            var review = new CompanyReview
            {
                Id = Guid.NewGuid(),
                ViewingBookingId = bookingId,
                Rating = command.Rating,
                Comment = command.Comment,
                Status = CompanyReviewStatus.Visible,
                CreatedAt = utcNow,
                ModeratedByApplicationUserId = null,
                ModeratedAt = null,
                ModerationReason = null
            };
            dbContext.Add(review);
            await dbContext.SaveChangesAsync(cancellationToken);

            var response = ProjectCustomerReview(review);
            await transaction.CommitAsync(cancellationToken);
            return new(ReviewOperationStatus.Succeeded, response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return new(ReviewOperationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            return new(ReviewOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(ReviewOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<ReviewMutationResult> UpdateReviewAsync(
        Guid applicationUserId,
        Guid reviewId,
        ReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        if (customerProfileId is null)
        {
            return new(ReviewOperationStatus.NotFound);
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var review = await dbContext.Set<CompanyReview>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == reviewId
                    && candidate.ViewingBooking.CustomerProfileId == customerProfileId.Value,
                    cancellationToken);
            if (review is null)
            {
                return new(ReviewOperationStatus.NotFound);
            }

            if (review.Status != CompanyReviewStatus.Visible)
            {
                return new(ReviewOperationStatus.Conflict);
            }

            var bookingIsCompleted = await dbContext.Set<ViewingBooking>()
                .AsNoTracking()
                .AnyAsync(booking =>
                    booking.Id == review.ViewingBookingId
                    && booking.Status == ViewingBookingStatus.Completed,
                    cancellationToken);
            if (!bookingIsCompleted)
            {
                return new(ReviewOperationStatus.Conflict);
            }

            review.Rating = command.Rating;
            review.Comment = command.Comment;
            await dbContext.SaveChangesAsync(cancellationToken);
            var response = ProjectCustomerReview(review);
            await transaction.CommitAsync(cancellationToken);
            return new(ReviewOperationStatus.Succeeded, response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return new(ReviewOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(ReviewOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<ReviewOperationStatus> DeleteReviewAsync(
        Guid applicationUserId,
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var customerProfileId = await CustomerProfileIdAsync(applicationUserId, cancellationToken);
        if (customerProfileId is null)
        {
            return ReviewOperationStatus.NotFound;
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var review = await dbContext.Set<CompanyReview>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == reviewId
                    && candidate.ViewingBooking.CustomerProfileId == customerProfileId.Value,
                    cancellationToken);
            if (review is null)
            {
                return ReviewOperationStatus.NotFound;
            }

            dbContext.Remove(review);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ReviewOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return ReviewOperationStatus.ServiceUnavailable;
        }
        catch (DbException)
        {
            return ReviewOperationStatus.ServiceUnavailable;
        }
    }

    private IQueryable<Company> PublicCompanies() =>
        dbContext.Set<Company>()
            .AsNoTracking()
            .Where(company =>
                company.Status == CompanyStatus.Active
                && company.VerifiedAt != null);

    private IQueryable<CompanyReview> VisibleCompanyReviews(Guid companyId) =>
        dbContext.Set<CompanyReview>()
            .AsNoTracking()
            .Where(review =>
                review.Status == CompanyReviewStatus.Visible
                && review.ViewingBooking.ViewingSlot.Listing.CompanyId == companyId);

    private IQueryable<CustomerReview> CustomerReviewQuery(
        Guid customerProfileId,
        Guid bookingId) =>
        dbContext.Set<CompanyReview>()
            .AsNoTracking()
            .Where(review =>
                review.ViewingBooking.CustomerProfileId == customerProfileId
                && review.ViewingBookingId == bookingId)
            .Select(review => new CustomerReview(
                review.Id,
                review.ViewingBookingId,
                review.Rating,
                review.Comment,
                review.Status,
                review.CreatedAt));

    private Task<Guid?> CustomerProfileIdAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken) =>
        dbContext.Set<CustomerProfile>()
            .AsNoTracking()
            .Where(profile => profile.ApplicationUserId == applicationUserId)
            .Select(profile => (Guid?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private static CustomerReview ProjectCustomerReview(CompanyReview review) => new(
        review.Id,
        review.ViewingBookingId,
        review.Rating,
        review.Comment,
        review.Status,
        review.CreatedAt);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private sealed record PublicCompanyHeader(Guid Id, string Slug);
    private sealed record ReviewStatistics(int ReviewCount, double AverageRating);
    private sealed record CompletedBookingReviewEligibility(
        ViewingBookingStatus Status,
        bool HasReview);
}
