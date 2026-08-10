namespace EstateHub.Api.Contracts.Companies;

public sealed class CompanyDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? CompanyType { get; init; }
    public Guid? LocationId { get; init; }
}
