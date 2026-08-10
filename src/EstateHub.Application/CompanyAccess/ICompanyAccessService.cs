namespace EstateHub.Application.CompanyAccess;

public interface ICompanyAccessService
{
    Task<CompanyAccessContext?> GetCurrentContextAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyAccessScope?> GetAuthorizedScopeAsync(
        Guid applicationUserId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}
