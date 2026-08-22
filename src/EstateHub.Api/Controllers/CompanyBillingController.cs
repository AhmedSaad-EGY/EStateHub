using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Contracts.Billing;
using EstateHub.Application.Billing;
using EstateHub.Application.CompanyAccess;
using EstateHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateHub.Api.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/company/billing")]
public sealed class CompanyBillingController(ICompanyBillingService service) : ControllerBase
{
    [HttpGet("invoices")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<BillingInvoiceDirectoryResponse>> GetInvoices([FromQuery] InvoiceDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        ValidatePageAndDates(request.PageNumber, request.PageSize, request.CreatedFrom, request.CreatedTo);
        var search = NormalizeSearch(request.Search, "search", 100);
        var currency = NormalizeCurrency(request.CurrencyCode);
        var status = ParseEnum<BillingInvoiceStatus>(request.Status, "status");
        var lineType = ParseEnum<BillingLineType>(request.LineType, "lineType");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetInvoicesAsync(userId, new InvoiceDirectoryQuery(request.PageNumber, request.PageSize, status, lineType, currency, request.CreatedFrom, request.CreatedTo, search), cancellationToken);
        return result.Status switch { BillingOperationStatus.Succeeded => Ok(BillingInvoiceDirectoryResponse.From(result.Value!)), BillingOperationStatus.NotFound => ResourceNotFound(), _ => BillingUnavailable() };
    }

    [HttpGet("invoices/{invoiceId}")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<BillingInvoiceDetailsResponse>> GetInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (invoiceId == Guid.Empty) { ModelState.AddModelError("invoiceId", "InvoiceId is required."); return ValidationProblem(ModelState); }
        var result = await service.GetInvoiceAsync(userId, invoiceId, cancellationToken);
        return InvoiceResult(result);
    }

    [HttpGet("booking-charges")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<BookingChargeDirectoryResponse>> GetBookingCharges([FromQuery] BookingChargeDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        ValidatePageAndDates(request.PageNumber, request.PageSize, request.CreatedFrom, request.CreatedTo);
        var search = NormalizeSearch(request.Search, "search", 100);
        var currency = NormalizeCurrency(request.CurrencyCode);
        var status = ParseEnum<BookingChargeStatus>(request.Status, "status");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetBookingChargesAsync(userId, new BookingChargeDirectoryQuery(request.PageNumber, request.PageSize, status, currency, request.CreatedFrom, request.CreatedTo, search), cancellationToken);
        return result.Status switch { BillingOperationStatus.Succeeded => Ok(BookingChargeDirectoryResponse.From(result.Value!)), BillingOperationStatus.NotFound => ResourceNotFound(), _ => BillingUnavailable() };
    }

    [HttpGet("payment-transactions")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<PaymentTransactionDirectoryResponse>> GetPaymentTransactions([FromQuery] PaymentTransactionDirectoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        ValidatePageAndDates(request.PageNumber, request.PageSize, request.CreatedFrom, request.CreatedTo);
        var currency = NormalizeCurrency(request.CurrencyCode);
        var status = ParseEnum<PaymentTransactionStatus>(request.Status, "status");
        var provider = ParseEnum<PaymentProvider>(request.Provider, "provider");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.GetPaymentTransactionsAsync(userId, new PaymentDirectoryQuery(request.PageNumber, request.PageSize, status, provider, currency, request.CreatedFrom, request.CreatedTo), cancellationToken);
        return result.Status switch { BillingOperationStatus.Succeeded => Ok(PaymentTransactionDirectoryResponse.From(result.Value!)), BillingOperationStatus.NotFound => ResourceNotFound(), _ => BillingUnavailable() };
    }

    [HttpPost("booking-charges/preview")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingRead)]
    public async Task<ActionResult<BookingChargePreviewResponse>> PreviewBookingCharges(BookingChargeSelectionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var ids = ValidateChargeIds(request.BookingChargeIds);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.PreviewBookingChargesAsync(userId, ids!, cancellationToken);
        return result.Status switch { BillingOperationStatus.Succeeded => Ok(BookingChargePreviewResponse.From(result.Value!)), BillingOperationStatus.NotFound => ResourceNotFound(), BillingOperationStatus.Conflict => BillingConflict(), _ => BillingUnavailable() };
    }

    [HttpPost("booking-charges/checkout")]
    [RequireCompanyPermission(CompanyPermissionCodes.BillingManage)]
    public async Task<ActionResult<BillingInvoiceDetailsResponse>> CheckoutBookingCharges(BookingChargeSelectionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var ids = ValidateChargeIds(request.BookingChargeIds);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.CheckoutBookingChargesAsync(userId, ids!, cancellationToken);
        return result.Status switch
        {
            BillingOperationStatus.Succeeded => CreatedAtAction(nameof(GetInvoice), new { invoiceId = result.Value!.Invoice.Id }, BillingInvoiceDetailsResponse.From(result.Value)),
            BillingOperationStatus.NotFound => ResourceNotFound(),
            BillingOperationStatus.Conflict => BillingConflict(),
            _ => BillingUnavailable()
        };
    }

    private ActionResult<BillingInvoiceDetailsResponse> InvoiceResult(BillingResult<BillingInvoiceDetails> result) => result.Status switch { BillingOperationStatus.Succeeded => Ok(BillingInvoiceDetailsResponse.From(result.Value!)), BillingOperationStatus.NotFound => ResourceNotFound(), _ => BillingUnavailable() };
    private void ValidatePageAndDates(int pageNumber, int pageSize, DateTimeOffset? from, DateTimeOffset? to)
    {
        if (pageNumber is < 1 or > 10000) ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.");
        if (pageSize is < 1 or > 50) ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.");
        if (from is not null && from.Value.Offset != TimeSpan.Zero) ModelState.AddModelError("createdFrom", "CreatedFrom must be UTC.");
        if (to is not null && to.Value.Offset != TimeSpan.Zero) ModelState.AddModelError("createdTo", "CreatedTo must be UTC.");
        if (from is not null && to is not null && from >= to) ModelState.AddModelError("createdTo", "CreatedTo must be later than CreatedFrom.");
    }
    private string? NormalizeSearch(string? value, string key, int maximum)
    {
        if (value is null) return null; var normalized = value.Trim();
        if (normalized.Length == 0) { ModelState.AddModelError(key, $"{key} cannot be blank when supplied."); return null; }
        if (normalized.Length > maximum) ModelState.AddModelError(key, $"{key} must not exceed {maximum} characters."); return normalized;
    }
    private string? NormalizeCurrency(string? value)
    {
        if (value is null) return null; var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z')) ModelState.AddModelError("currencyCode", "CurrencyCode must contain exactly three ASCII letters.");
        return normalized;
    }
    private T? ParseEnum<T>(string? value, string key) where T : struct, Enum
    {
        if (value is null) return null; var normalized = value.Trim();
        if (normalized.Length != 0 && Enum.TryParse<T>(normalized, true, out var candidate) && Enum.IsDefined(candidate) && string.Equals(normalized, candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate;
        ModelState.AddModelError(key, $"{key} is invalid."); return null;
    }
    private IReadOnlyCollection<Guid>? ValidateChargeIds(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Count) { ModelState.AddModelError("bookingChargeIds", "BookingChargeIds must contain 1 to 100 unique, non-empty values."); return null; }
        return ids;
    }
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
    private ObjectResult ResourceNotFound() => Problem(statusCode: StatusCodes.Status404NotFound, title: "Billing resource not found.");
    private ObjectResult BillingConflict() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Billing operation conflict.");
    private ObjectResult BillingUnavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Billing operation is temporarily unavailable.");
}
