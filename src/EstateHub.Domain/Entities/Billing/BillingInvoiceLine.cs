using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Billing;

public class BillingInvoiceLine
{
    public Guid Id { get; set; }
    public Guid BillingInvoiceId { get; set; }
    public BillingLineType LineType { get; set; }
    public Guid? CompanySubscriptionId { get; set; }
    public Guid? BookingChargeId { get; set; }
    public Guid? ListingPromotionId { get; set; }
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal LineTotal { get; set; }

    public BillingInvoice BillingInvoice { get; set; } = null!;
    public CompanySubscription? CompanySubscription { get; set; }
    public BookingCharge? BookingCharge { get; set; }
    public ListingPromotion? ListingPromotion { get; set; }
}
