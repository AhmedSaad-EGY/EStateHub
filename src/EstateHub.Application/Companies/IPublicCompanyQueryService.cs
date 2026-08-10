using EstateHub.Application.Common;

namespace EstateHub.Application.Companies;

public interface IPublicCompanyQueryService
{
    Task<PagedResult<CompanyDirectoryItem>> GetCompaniesAsync(
        CompanyDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<CompanyDetails?> GetCompanyBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
