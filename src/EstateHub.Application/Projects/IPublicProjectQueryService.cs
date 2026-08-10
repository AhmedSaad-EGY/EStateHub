using EstateHub.Application.Common;

namespace EstateHub.Application.Projects;

public interface IPublicProjectQueryService
{
    Task<PagedResult<ProjectDirectoryItem>?> GetProjectsAsync(
        string companySlug,
        ProjectDirectoryQuery query,
        CancellationToken cancellationToken = default);

    Task<ProjectDetails?> GetProjectBySlugAsync(
        string companySlug,
        string projectSlug,
        CancellationToken cancellationToken = default);
}
