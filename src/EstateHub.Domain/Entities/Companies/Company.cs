using EstateHub.Domain.Entities.Billing;
using EstateHub.Domain.Entities.Bookings;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Enums;

namespace EstateHub.Domain.Entities.Companies;

public class Company
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public CompanyType CompanyType { get; set; }
    public string BusinessEmail { get; set; } = string.Empty;
    public string SupportPhone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public Guid AddressId { get; set; }
    public Guid? LogoFileAssetId { get; set; }
    public Guid? CoverFileAssetId { get; set; }
    public CompanyStatus Status { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string BaseCurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Address Address { get; set; } = null!;
    public FileAsset? LogoFileAsset { get; set; }
    public FileAsset? CoverFileAsset { get; set; }
    public Currency BaseCurrency { get; set; } = null!;
    public CompanyApplication? ApprovedFromApplication { get; set; }
    public ICollection<CompanyEmployee> Employees { get; set; } = [];
    public ICollection<CompanyRole> Roles { get; set; } = [];
    public ICollection<CompanyEmployeeRole> EmployeeRoleAssignments { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<Unit> Units { get; set; } = [];
    public ICollection<Listing> Listings { get; set; } = [];
    public ICollection<Lead> Leads { get; set; } = [];
    public ICollection<CompanySubscription> Subscriptions { get; set; } = [];
    public ICollection<ListingPromotion> ListingPromotions { get; set; } = [];
    public ICollection<BookingCharge> BookingCharges { get; set; } = [];
    public ICollection<BillingInvoice> BillingInvoices { get; set; } = [];
}
