using EstateHub.Application.Common;

namespace EstateHub.Application.CompanyViewingSlots;

public interface ICompanyViewingSlotManagementService
{
    Task<PagedResult<CompanyViewingSlotDetails>?> GetSlotsAsync(Guid applicationUserId, CompanyViewingSlotDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<CompanyViewingSlotDetails?> GetSlotAsync(Guid applicationUserId, Guid slotId, CancellationToken cancellationToken = default);
    Task<CompanyViewingSlotMutationResult> CreateSlotAsync(Guid applicationUserId, CompanyViewingSlotCommand command, CancellationToken cancellationToken = default);
    Task<CompanyViewingSlotMutationResult> UpdateSlotAsync(Guid applicationUserId, Guid slotId, CompanyViewingSlotCommand command, CancellationToken cancellationToken = default);
    Task<CompanyViewingSlotOperationStatus> CloseSlotAsync(Guid applicationUserId, Guid slotId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyViewingSlotOperationStatus> ReopenSlotAsync(Guid applicationUserId, Guid slotId, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<CompanyViewingSlotOperationStatus> CancelSlotAsync(Guid applicationUserId, Guid slotId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
