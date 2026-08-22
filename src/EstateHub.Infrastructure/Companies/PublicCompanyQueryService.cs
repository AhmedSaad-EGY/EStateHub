using EstateHub.Application.Common;
using EstateHub.Application.Companies;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.Companies;

public sealed class PublicCompanyQueryService(
    EstateHubDbContext dbContext,
    TimeProvider timeProvider) : IPublicCompanyQueryService
{
    public async Task<PagedResult<CompanyDirectoryItem>> GetCompaniesAsync(
        CompanyDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var utcNow = timeProvider.GetUtcNow();
        var companies = GetPublicCompanies();

        if (query.Search is not null)
        {
            companies = companies.Where(company =>
                company.DisplayName.Contains(query.Search)
                || company.LegalName.Contains(query.Search));
        }

        if (query.CompanyType is not null)
        {
            companies = companies.Where(company =>
                company.CompanyType == query.CompanyType.Value);
        }

        if (query.LocationId is not null)
        {
            companies = companies.Where(company =>
                company.Address.LocationId == query.LocationId.Value);
        }

        var totalCount = await companies.CountAsync(cancellationToken);
        var offset = (query.PageNumber - 1) * query.PageSize;

        var items = await companies
            .OrderBy(company => company.DisplayName)
            .ThenBy(company => company.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(company => new CompanyDirectoryItem(
                company.Id,
                company.Slug,
                company.DisplayName,
                company.CompanyType,
                company.LogoFileAssetId,
                new CompanyLocationSummary(
                    company.Address.Location.Id,
                    company.Address.Location.Type,
                    company.Address.Location.NameEn,
                    company.Address.Location.NameAr,
                    company.Address.Location.Slug),
                company.Projects.Count(project =>
                    project.ProjectStatus == ProjectStatus.Published),
                dbContext.Set<Listing>()
                    .AsNoTracking()
                    .WherePublic(utcNow)
                    .Count(listing => listing.CompanyId == company.Id)))
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyDirectoryItem>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public Task<CompanyDetails?> GetCompanyBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var utcNow = timeProvider.GetUtcNow();

        return GetPublicCompanies()
            .Where(company => company.Slug == slug)
            .Select(company => new CompanyDetails(
                company.Id,
                company.Slug,
                company.DisplayName,
                company.LegalName,
                company.CompanyType,
                company.BusinessEmail,
                company.SupportPhone,
                company.Website,
                company.LogoFileAssetId,
                company.CoverFileAssetId,
                company.TimeZoneId,
                company.BaseCurrencyCode,
                company.VerifiedAt!.Value,
                new CompanyAddressDetails(
                    company.Address.AddressLine1,
                    company.Address.AddressLine2,
                    company.Address.PostalCode,
                    company.Address.Latitude,
                    company.Address.Longitude,
                    new CompanyLocationSummary(
                        company.Address.Location.Id,
                        company.Address.Location.Type,
                        company.Address.Location.NameEn,
                        company.Address.Location.NameAr,
                        company.Address.Location.Slug)),
                company.Projects.Count(project =>
                    project.ProjectStatus == ProjectStatus.Published),
                dbContext.Set<Listing>()
                    .AsNoTracking()
                    .WherePublic(utcNow)
                    .Count(listing => listing.CompanyId == company.Id)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Company> GetPublicCompanies()
    {
        return dbContext.Set<Company>()
            .AsNoTracking()
            .Where(company =>
                company.Status == CompanyStatus.Active
                && company.VerifiedAt != null);
    }
}
