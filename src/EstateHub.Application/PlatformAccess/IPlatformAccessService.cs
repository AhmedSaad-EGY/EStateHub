namespace EstateHub.Application.PlatformAccess;

public interface IPlatformAccessService
{
    Task<bool> IsPlatformAdminAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken = default);
}

