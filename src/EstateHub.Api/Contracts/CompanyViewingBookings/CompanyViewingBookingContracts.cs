using EstateHub.Application.Common;
using EstateHub.Application.CompanyViewingBookings;
using EstateHub.Domain.Enums;

namespace EstateHub.Api.Contracts.CompanyViewingBookings;

public sealed class CompanyViewingBookingDirectoryRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public Guid? ListingId { get; init; }
    public Guid? ViewingSlotId { get; init; }
    public Guid? AssignedEmployeeId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed record CompanyViewingBookingAssignmentRequest(
    Guid? EmployeeId,
    string? RowVersion);

public sealed record CompanyViewingBookingLifecycleRequest(
    string? RowVersion,
    string? Reason);

public sealed record CompanyViewingBookingRescheduleRequest(
    Guid ToViewingSlotId,
    string? RowVersion,
    string? Reason);

public sealed record CompanyViewingBookingCustomerResponse(
    Guid Id,
    string FullName,
    string Persona);

public sealed record CompanyViewingBookingSlotResponse(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string MeetingPoint,
    string Status);

public sealed record CompanyViewingBookingListingResponse(
    Guid Id,
    string ListingCode,
    string Slug,
    string Title,
    string ListingType);

public sealed record CompanyViewingBookingEmployeeResponse(
    Guid Id,
    string FullName,
    string? JobTitle);

public sealed record CompanyViewingBookingChargeResponse(
    Guid Id,
    decimal AmountSnapshot,
    string CurrencyCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VoidedAt,
    string? VoidReason);

public sealed record CompanyViewingBookingResponse(
    Guid Id,
    string BookingCode,
    string Status,
    int VisitorCount,
    string ContactPhone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? CompletedAt,
    string RowVersion,
    CompanyViewingBookingCustomerResponse Customer,
    CompanyViewingBookingSlotResponse Slot,
    CompanyViewingBookingListingResponse Listing,
    CompanyViewingBookingEmployeeResponse? AssignedEmployee,
    CompanyViewingBookingChargeResponse? Charge)
{
    public static CompanyViewingBookingResponse From(CompanyViewingBookingSummary value) => new(
        value.Id,
        value.BookingCode,
        CompanyViewingBookingEnumText.BookingStatus(value.Status),
        value.VisitorCount,
        value.ContactPhone,
        value.CreatedAt,
        value.ConfirmedAt,
        value.CheckedInAt,
        value.CancelledAt,
        value.CompletedAt,
        Convert.ToBase64String(value.RowVersion),
        new CompanyViewingBookingCustomerResponse(
            value.Customer.Id,
            value.Customer.FullName,
            CompanyViewingBookingEnumText.CustomerPersona(value.Customer.Persona)),
        new CompanyViewingBookingSlotResponse(
            value.Slot.Id,
            value.Slot.StartsAt,
            value.Slot.EndsAt,
            value.Slot.MeetingPoint,
            CompanyViewingBookingEnumText.SlotStatus(value.Slot.Status)),
        new CompanyViewingBookingListingResponse(
            value.Listing.Id,
            value.Listing.ListingCode,
            value.Listing.Slug,
            value.Listing.Title,
            CompanyViewingBookingEnumText.ListingType(value.Listing.ListingType)),
        value.AssignedEmployee is null
            ? null
            : new CompanyViewingBookingEmployeeResponse(
                value.AssignedEmployee.Id,
                value.AssignedEmployee.FullName,
                value.AssignedEmployee.JobTitle),
        value.Charge is null
            ? null
            : new CompanyViewingBookingChargeResponse(
                value.Charge.Id,
                value.Charge.AmountSnapshot,
                value.Charge.CurrencyCode,
                CompanyViewingBookingEnumText.ChargeStatus(value.Charge.Status),
                value.Charge.CreatedAt,
                value.Charge.VoidedAt,
                value.Charge.VoidReason));
}

public sealed record CompanyViewingBookingsResponse(
    IReadOnlyList<CompanyViewingBookingResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static CompanyViewingBookingsResponse From(
        PagedResult<CompanyViewingBookingSummary> value) => new(
            value.Items.Select(CompanyViewingBookingResponse.From).ToList(),
            value.PageNumber,
            value.PageSize,
            value.TotalCount,
            value.TotalPages,
            value.HasPreviousPage,
            value.HasNextPage);
}

public sealed record CompanyViewingBookingStatusHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ActorType,
    DateTimeOffset ChangedAt,
    string? Reason)
{
    public static CompanyViewingBookingStatusHistoryResponse From(
        CompanyViewingBookingStatusHistoryItem value) => new(
            value.Id,
            value.FromStatus is null
                ? null
                : CompanyViewingBookingEnumText.BookingStatus(value.FromStatus.Value),
            CompanyViewingBookingEnumText.BookingStatus(value.ToStatus),
            CompanyViewingBookingEnumText.ActorType(value.ActorType),
            value.ChangedAt,
            value.Reason);
}

public sealed record CompanyViewingBookingRescheduleHistoryResponse(
    Guid Id,
    Guid FromViewingSlotId,
    DateTimeOffset FromStartsAt,
    DateTimeOffset FromEndsAt,
    Guid ToViewingSlotId,
    DateTimeOffset ToStartsAt,
    DateTimeOffset ToEndsAt,
    DateTimeOffset ChangedAt,
    string? Reason)
{
    public static CompanyViewingBookingRescheduleHistoryResponse From(
        CompanyViewingBookingRescheduleHistoryItem value) => new(
            value.Id,
            value.FromViewingSlotId,
            value.FromStartsAt,
            value.FromEndsAt,
            value.ToViewingSlotId,
            value.ToStartsAt,
            value.ToEndsAt,
            value.ChangedAt,
            value.Reason);
}

public sealed record CompanyViewingBookingDetailsResponse(
    Guid Id,
    string BookingCode,
    string Status,
    int VisitorCount,
    string ContactPhone,
    string? SpecialRequests,
    string? CancellationSource,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? CompletedAt,
    string RowVersion,
    CompanyViewingBookingCustomerResponse Customer,
    CompanyViewingBookingSlotResponse Slot,
    CompanyViewingBookingListingResponse Listing,
    CompanyViewingBookingEmployeeResponse? AssignedEmployee,
    CompanyViewingBookingChargeResponse? Charge,
    IReadOnlyList<CompanyViewingBookingStatusHistoryResponse> StatusHistory,
    IReadOnlyList<CompanyViewingBookingRescheduleHistoryResponse> RescheduleHistory)
{
    public static CompanyViewingBookingDetailsResponse From(
        CompanyViewingBookingDetails value)
    {
        var header = CompanyViewingBookingResponse.From(value.Header.Summary);
        return new CompanyViewingBookingDetailsResponse(
            header.Id,
            header.BookingCode,
            header.Status,
            header.VisitorCount,
            header.ContactPhone,
            value.Header.SpecialRequests,
            value.Header.CancellationSource is null
                ? null
                : CompanyViewingBookingEnumText.CancellationSource(
                    value.Header.CancellationSource.Value),
            header.CreatedAt,
            header.ConfirmedAt,
            header.CheckedInAt,
            header.CancelledAt,
            header.CompletedAt,
            header.RowVersion,
            header.Customer,
            header.Slot,
            header.Listing,
            header.AssignedEmployee,
            header.Charge,
            value.StatusHistory.Select(CompanyViewingBookingStatusHistoryResponse.From).ToList(),
            value.RescheduleHistory.Select(CompanyViewingBookingRescheduleHistoryResponse.From).ToList());
    }
}

internal static class CompanyViewingBookingEnumText
{
    public static string BookingStatus(ViewingBookingStatus value) => value switch
    {
        ViewingBookingStatus.Pending => "Pending",
        ViewingBookingStatus.Confirmed => "Confirmed",
        ViewingBookingStatus.CheckedIn => "CheckedIn",
        ViewingBookingStatus.Completed => "Completed",
        ViewingBookingStatus.Rejected => "Rejected",
        ViewingBookingStatus.Cancelled => "Cancelled",
        ViewingBookingStatus.NoShow => "NoShow",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static string SlotStatus(ViewingSlotStatus value) => value switch
    {
        ViewingSlotStatus.Open => "Open",
        ViewingSlotStatus.Closed => "Closed",
        ViewingSlotStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static string CancellationSource(BookingCancellationSource value) => value switch
    {
        BookingCancellationSource.Customer => "Customer",
        BookingCancellationSource.Company => "Company",
        BookingCancellationSource.System => "System",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static string ActorType(BookingActorType value) => value switch
    {
        BookingActorType.Customer => "Customer",
        BookingActorType.Employee => "Employee",
        BookingActorType.Platform => "Platform",
        BookingActorType.System => "System",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static string ChargeStatus(BookingChargeStatus value) => value switch
    {
        BookingChargeStatus.Billable => "Billable",
        BookingChargeStatus.Invoiced => "Invoiced",
        BookingChargeStatus.Void => "Void",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static string CustomerPersona(CustomerPersona value) => value switch
    {
        Domain.Enums.CustomerPersona.Buyer => "Buyer",
        Domain.Enums.CustomerPersona.Renter => "Renter",
        Domain.Enums.CustomerPersona.Agent => "Agent",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static string ListingType(ListingType value) => value switch
    {
        Domain.Enums.ListingType.Sale => "Sale",
        Domain.Enums.ListingType.Rent => "Rent",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}

