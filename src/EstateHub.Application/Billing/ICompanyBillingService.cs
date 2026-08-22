using EstateHub.Application.Common;

namespace EstateHub.Application.Billing;

public interface ICompanyBillingService
{
    Task<BillingResult<PagedResult<BillingInvoiceSummary>>> GetInvoicesAsync(Guid applicationUserId, InvoiceDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<BillingResult<BillingInvoiceDetails>> GetInvoiceAsync(Guid applicationUserId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<BillingResult<PagedResult<BookingChargeDirectoryItem>>> GetBookingChargesAsync(Guid applicationUserId, BookingChargeDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<BillingResult<PagedResult<BillingPaymentItem>>> GetPaymentTransactionsAsync(Guid applicationUserId, PaymentDirectoryQuery query, CancellationToken cancellationToken = default);
    Task<BillingResult<BookingChargePreview>> PreviewBookingChargesAsync(Guid applicationUserId, IReadOnlyCollection<Guid> bookingChargeIds, CancellationToken cancellationToken = default);
    Task<BillingResult<BillingInvoiceDetails>> CheckoutBookingChargesAsync(Guid applicationUserId, IReadOnlyCollection<Guid> bookingChargeIds, CancellationToken cancellationToken = default);
}
