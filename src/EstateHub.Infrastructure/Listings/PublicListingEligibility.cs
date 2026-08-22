using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Enums;

namespace EstateHub.Infrastructure.Listings;

internal static class PublicListingEligibility
{
    public static IQueryable<Listing> WherePublic(
        this IQueryable<Listing> listings,
        DateTimeOffset utcNow)
    {
        return listings.Where(listing =>
            listing.PublicationStatus == ListingPublicationStatus.Published
            && listing.Company.Status == CompanyStatus.Active
            && listing.Company.VerifiedAt != null
            && listing.Company.Subscriptions.Any(subscription =>
                subscription.Status == CompanySubscriptionStatus.Active
                && subscription.EndedAt == null
                && subscription.CurrentPeriodStart <= utcNow
                && subscription.CurrentPeriodEnd > utcNow)
            && (listing.Unit.ProjectId == null
                || (listing.Unit.Project!.ProjectStatus == ProjectStatus.Published
                    && listing.Unit.Project.DeveloperCompany.Status == CompanyStatus.Active
                    && listing.Unit.Project.DeveloperCompany.VerifiedAt != null
                    && listing.Unit.Project.DeveloperCompany.CompanyType == CompanyType.Developer)));
    }
}
