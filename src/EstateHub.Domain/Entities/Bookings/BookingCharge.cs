using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Bookings;

public class BookingCharge
{
    public Guid Id { get; set; }
    public Guid ViewingBookingId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CompanySubscriptionId { get; set; }
    public decimal AmountSnapshot { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public BookingChargeStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidReason { get; set; }

    public ViewingBooking ViewingBooking { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public CompanySubscription? CompanySubscription { get; set; }
    public Currency Currency { get; set; } = null!;
    public BillingInvoiceLine? InvoiceLine { get; set; }
}
