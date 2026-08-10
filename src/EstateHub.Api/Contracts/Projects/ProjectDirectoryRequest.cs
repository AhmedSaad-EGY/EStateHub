namespace EstateHub.Api.Contracts.Projects;

public sealed class ProjectDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? DeliveryStatus { get; init; }
    public Guid? LocationId { get; init; }
}
