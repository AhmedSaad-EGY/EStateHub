using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.ViewingBookings;

internal static class BookingLeadIntegration
{
    internal const string LeadSource = "ViewingBooking";
    internal const string InterestType = "ViewingBooking";
    internal const string RequestedActivityType = "BookingRequested";
    internal const string ConfirmedActivityType = "BookingConfirmed";

    internal static IQueryable<BookingLeadCustomerIdentity> CustomerIdentityQuery(
        EstateHubDbContext dbContext,
        Guid applicationUserId) =>
        CustomerIdentities(dbContext)
            .Where(customer => customer.ApplicationUserId == applicationUserId)
            .Select(customer => new BookingLeadCustomerIdentity(
                customer.CustomerProfileId,
                customer.FullName,
                customer.Email));

    internal static IQueryable<BookingLeadCustomerIdentity> CustomerIdentityByProfileQuery(
        EstateHubDbContext dbContext,
        Guid customerProfileId) =>
        CustomerIdentities(dbContext)
            .Where(customer => customer.CustomerProfileId == customerProfileId)
            .Select(customer => new BookingLeadCustomerIdentity(
                customer.CustomerProfileId,
                customer.FullName,
                customer.Email));

    private static IQueryable<BookingLeadCustomerIdentityProjection> CustomerIdentities(
        EstateHubDbContext dbContext) =>
        from profile in dbContext.Set<CustomerProfile>().AsNoTracking()
        join user in dbContext.Set<ApplicationUser>().AsNoTracking()
            on profile.ApplicationUserId equals user.Id
        select new BookingLeadCustomerIdentityProjection(
            profile.ApplicationUserId,
            profile.Id,
            profile.FullName,
            string.IsNullOrEmpty(user.Email) ? null : user.Email);

    internal static Lead CreateRequestedLead(
        Guid companyId,
        BookingLeadCustomerIdentity customer,
        Guid bookingId,
        Guid listingId,
        string contactPhone,
        DateTimeOffset utcNow)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerProfileId = customer.CustomerProfileId,
            SourceBookingId = bookingId,
            ContactName = customer.FullName,
            ContactPhone = contactPhone,
            ContactEmail = customer.Email,
            Source = LeadSource,
            Stage = LeadStage.New,
            Priority = LeadPriority.Medium,
            OwnerEmployeeId = null,
            CreatedAt = utcNow,
            LastActivityAt = utcNow,
            ClosedAt = null
        };
        lead.Interests.Add(CreateInterest(lead.Id, listingId, utcNow));
        lead.Activities.Add(CreateRequestedActivity(lead.Id, utcNow));
        return lead;
    }

    internal static LeadInterest CreateInterest(
        Guid leadId,
        Guid listingId,
        DateTimeOffset utcNow) => new()
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            ListingId = listingId,
            InterestType = InterestType,
            CreatedAt = utcNow
        };

    internal static CRMActivity CreateRequestedActivity(
        Guid leadId,
        DateTimeOffset utcNow) => new()
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            PerformedByEmployeeId = null,
            ActivityType = RequestedActivityType,
            OccurredAt = utcNow,
            Notes = null,
            MetadataJson = null
        };

    internal static CRMActivity CreateConfirmedActivity(
        Guid leadId,
        Guid actingEmployeeId,
        DateTimeOffset utcNow,
        string? reason) => new()
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            PerformedByEmployeeId = actingEmployeeId,
            ActivityType = ConfirmedActivityType,
            OccurredAt = utcNow,
            Notes = reason,
            MetadataJson = null
        };

    internal static LeadStage AdvanceStage(LeadStage currentStage) => currentStage switch
    {
        LeadStage.New => LeadStage.ViewingScheduled,
        LeadStage.Contacted => LeadStage.ViewingScheduled,
        LeadStage.Qualified => LeadStage.ViewingScheduled,
        LeadStage.ViewingScheduled => LeadStage.ViewingScheduled,
        LeadStage.Negotiation => LeadStage.Negotiation,
        LeadStage.Won => LeadStage.Won,
        LeadStage.Lost => LeadStage.Lost,
        _ => throw new ArgumentOutOfRangeException(nameof(currentStage), currentStage, null)
    };
}

internal sealed record BookingLeadCustomerIdentity(
    Guid CustomerProfileId,
    string FullName,
    string? Email);

internal sealed record BookingLeadCustomerIdentityProjection(
    Guid ApplicationUserId,
    Guid CustomerProfileId,
    string FullName,
    string? Email);
