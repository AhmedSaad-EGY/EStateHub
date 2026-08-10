using EstateHub.Application.Common;
using EstateHub.Application.Projects;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Projects;

public sealed class PublicProjectQueryService(
    EstateHubDbContext dbContext) : IPublicProjectQueryService
{
    public async Task<PagedResult<ProjectDirectoryItem>?> GetProjectsAsync(
        string companySlug,
        ProjectDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companySlug);
        ArgumentNullException.ThrowIfNull(query);

        var parentCompany = await dbContext.Set<Company>()
            .AsNoTracking()
            .Where(company =>
                company.Slug == companySlug
                && company.Status == CompanyStatus.Active
                && company.VerifiedAt != null)
            .Select(company => new PublicCompanySummary(
                company.Id,
                company.CompanyType))
            .SingleOrDefaultAsync(cancellationToken);

        if (parentCompany is null)
        {
            return null;
        }

        if (parentCompany.CompanyType != CompanyType.Developer)
        {
            return new PagedResult<ProjectDirectoryItem>(
                Array.Empty<ProjectDirectoryItem>(),
                query.PageNumber,
                query.PageSize,
                totalCount: 0);
        }

        var projects = GetPublicProjects()
            .Where(project => project.DeveloperCompanyId == parentCompany.Id);

        if (query.Search is not null)
        {
            projects = projects.Where(project =>
                project.Name.Contains(query.Search)
                || (project.Description != null
                    && project.Description.Contains(query.Search)));
        }

        if (query.DeliveryStatus is not null)
        {
            projects = projects.Where(project =>
                project.DeliveryStatus == query.DeliveryStatus.Value);
        }

        if (query.LocationId is not null)
        {
            projects = projects.Where(project =>
                project.LocationId == query.LocationId.Value);
        }

        var totalCount = await projects.CountAsync(cancellationToken);
        var offset = (query.PageNumber - 1) * query.PageSize;

        var items = await projects
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(project => new ProjectDirectoryItem(
                project.Id,
                project.Slug,
                project.Name,
                project.DeliveryStatus,
                project.ExpectedDeliveryDate,
                project.Media
                    .Where(media => media.IsCover)
                    .Select(media => (Guid?)media.FileAssetId)
                    .FirstOrDefault(),
                new ProjectLocationSummary(
                    project.Location.Id,
                    project.Location.Type,
                    project.Location.NameEn,
                    project.Location.NameAr,
                    project.Location.Slug),
                project.Units
                    .SelectMany(unit => unit.Listings)
                    .Count(listing =>
                        listing.PublicationStatus == ListingPublicationStatus.Published
                        && listing.Company.Status == CompanyStatus.Active
                        && listing.Company.VerifiedAt != null)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectDirectoryItem>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<ProjectDetails?> GetProjectBySlugAsync(
        string companySlug,
        string projectSlug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companySlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectSlug);

        var project = await GetPublicProjects()
            .Where(candidate =>
                candidate.DeveloperCompany.Slug == companySlug
                && candidate.Slug == projectSlug)
            .Select(candidate => new ProjectDetailsHeader(
                candidate.Id,
                candidate.Slug,
                candidate.Name,
                candidate.Description,
                candidate.DeliveryStatus,
                candidate.ExpectedDeliveryDate,
                candidate.Media
                    .Where(media => media.IsCover)
                    .Select(media => (Guid?)media.FileAssetId)
                    .FirstOrDefault(),
                candidate.Units
                    .SelectMany(unit => unit.Listings)
                    .Count(listing =>
                        listing.PublicationStatus == ListingPublicationStatus.Published
                        && listing.Company.Status == CompanyStatus.Active
                        && listing.Company.VerifiedAt != null),
                new ProjectDeveloperSummary(
                    candidate.DeveloperCompany.Id,
                    candidate.DeveloperCompany.Slug,
                    candidate.DeveloperCompany.DisplayName,
                    candidate.DeveloperCompany.LogoFileAssetId),
                new ProjectLocationSummary(
                    candidate.Location.Id,
                    candidate.Location.Type,
                    candidate.Location.NameEn,
                    candidate.Location.NameAr,
                    candidate.Location.Slug)))
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return null;
        }

        var media = await dbContext.Set<ProjectMedia>()
            .AsNoTracking()
            .Where(media => media.ProjectId == project.Id)
            .OrderBy(media => media.SortOrder)
            .ThenBy(media => media.Id)
            .Select(media => new ProjectMediaItem(
                media.FileAssetId,
                media.SortOrder,
                media.IsCover,
                media.Caption))
            .ToListAsync(cancellationToken);

        var amenities = await dbContext.Set<ProjectAmenity>()
            .AsNoTracking()
            .Where(projectAmenity =>
                projectAmenity.ProjectId == project.Id
                && projectAmenity.Amenity.IsActive
                && (projectAmenity.Amenity.Scope == AmenityScope.Project
                    || projectAmenity.Amenity.Scope == AmenityScope.Both))
            .OrderBy(projectAmenity => projectAmenity.Amenity.NameEn)
            .ThenBy(projectAmenity => projectAmenity.Amenity.Id)
            .Select(projectAmenity => new ProjectAmenityItem(
                projectAmenity.Amenity.Id,
                projectAmenity.Amenity.Code,
                projectAmenity.Amenity.NameEn,
                projectAmenity.Amenity.NameAr,
                projectAmenity.Amenity.IconKey))
            .ToListAsync(cancellationToken);

        var nearbyPlaces = await dbContext.Set<NearbyPlace>()
            .AsNoTracking()
            .Where(place => place.ProjectId == project.Id)
            .OrderBy(place => place.Category)
            .ThenBy(place => place.Name)
            .ThenBy(place => place.Id)
            .Select(place => new ProjectNearbyPlaceItem(
                place.Id,
                place.Category,
                place.Name,
                place.DistanceMeters,
                place.TravelMinutes,
                place.Latitude,
                place.Longitude))
            .ToListAsync(cancellationToken);

        return new ProjectDetails(
            project.Id,
            project.Slug,
            project.Name,
            project.Description,
            project.DeliveryStatus,
            project.ExpectedDeliveryDate,
            project.CoverFileAssetId,
            project.PublishedListingCount,
            project.DeveloperCompany,
            project.Location,
            media,
            amenities,
            nearbyPlaces);
    }

    private IQueryable<Project> GetPublicProjects()
    {
        return dbContext.Set<Project>()
            .AsNoTracking()
            .Where(project =>
                project.ProjectStatus == ProjectStatus.Published
                && project.DeveloperCompany.Status == CompanyStatus.Active
                && project.DeveloperCompany.VerifiedAt != null
                && project.DeveloperCompany.CompanyType == CompanyType.Developer);
    }

    private sealed record PublicCompanySummary(
        Guid Id,
        CompanyType CompanyType);

    private sealed record ProjectDetailsHeader(
        Guid Id,
        string Slug,
        string Name,
        string? Description,
        ProjectDeliveryStatus DeliveryStatus,
        DateOnly? ExpectedDeliveryDate,
        Guid? CoverFileAssetId,
        int PublishedListingCount,
        ProjectDeveloperSummary DeveloperCompany,
        ProjectLocationSummary Location);
}
