using EstateHub.Application.Common;
using EstateHub.Domain.Enums;

namespace EstateHub.Application.CompanyViewingSlots;

public sealed record CompanyViewingSlotDetails(Guid Id, Guid ListingId, string ListingCode, string ListingSlug, string ListingTitle, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, string MeetingPoint, ViewingSlotStatus Status, int ActiveBookingCount, int ReservedVisitorCount, byte[] RowVersion);
public sealed record CompanyViewingSlotDirectoryQuery(int PageNumber, int PageSize, Guid? ListingId, ViewingSlotStatus? Status, DateTimeOffset? From, DateTimeOffset? To);
public sealed record CompanyViewingSlotCommand(Guid ListingId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, string MeetingPoint, byte[]? RowVersion);
public enum CompanyViewingSlotOperationStatus { Succeeded, NotFound, Conflict, InvalidRequest, ServiceUnavailable }
public sealed record CompanyViewingSlotMutationResult(CompanyViewingSlotOperationStatus Status, CompanyViewingSlotDetails? Slot = null);
