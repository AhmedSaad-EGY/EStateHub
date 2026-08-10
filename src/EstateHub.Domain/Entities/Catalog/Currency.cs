using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Discovery;

namespace EstateHub.Domain.Entities.Catalog;

public class Currency
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Company> CompaniesUsingAsBaseCurrency { get; set; } = [];
    public ICollection<Listing> Listings { get; set; } = [];
    public ICollection<ListingPriceHistory> ListingPriceHistories { get; set; } = [];
    public ICollection<PaymentPlan> PaymentPlans { get; set; } = [];
    public ICollection<CustomerRequirement> CustomerRequirements { get; set; } = [];
    public ICollection<BookingCharge> BookingCharges { get; set; } = [];
    public ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = [];
    public ICollection<CompanySubscription> CompanySubscriptions { get; set; } = [];
    public ICollection<PromotionPackage> PromotionPackages { get; set; } = [];
    public ICollection<ListingPromotion> ListingPromotions { get; set; } = [];
    public ICollection<BillingInvoice> BillingInvoices { get; set; } = [];
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}
