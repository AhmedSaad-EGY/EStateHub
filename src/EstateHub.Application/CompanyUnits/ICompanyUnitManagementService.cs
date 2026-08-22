using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyUnits;

public interface ICompanyUnitManagementService
{
    Task<PagedResult<CompanyUnitDetails>?> GetUnitsAsync(Guid applicationUserId, CompanyUnitDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<CompanyUnitManagementDetails?> GetUnitAsync(Guid applicationUserId, Guid unitId, CancellationToken cancellationToken = default);
    Task<CompanyUnitMutationResult> CreateUnitAsync(Guid applicationUserId, CompanyUnitCommand command, CancellationToken cancellationToken = default);
    Task<CompanyUnitMutationResult> UpdateUnitAsync(Guid applicationUserId, Guid unitId, CompanyUnitCommand command, CancellationToken cancellationToken = default);
    Task<CompanyUnitStatusMutationResult> UpdateUnitStatusAsync(Guid applicationUserId, Guid unitId, UnitStatus status, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyUnitAmenitiesResult> ReplaceAmenitiesAsync(Guid applicationUserId, Guid unitId, IReadOnlyList<Guid> amenityIds, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyUnitNearbyPlaceResult> CreateNearbyPlaceAsync(Guid applicationUserId, Guid unitId, CompanyUnitNearbyPlaceCommand command, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyUnitNearbyPlaceResult> UpdateNearbyPlaceAsync(Guid applicationUserId, Guid unitId, Guid nearbyPlaceId, CompanyUnitNearbyPlaceCommand command, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyUnitOperationStatus> DeleteNearbyPlaceAsync(Guid applicationUserId, Guid unitId, Guid nearbyPlaceId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
