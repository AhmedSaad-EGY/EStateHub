using System.Data.Common;
using System.Text.Json;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyLeads;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Discovery;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyLeads;

public sealed class CompanyLeadManagementService(
    EstateHubDbContext dbContext, ICompanyAccessService companyAccessService,
    TimeProvider timeProvider) : ICompanyLeadManagementService
{
    private const string ManualSource = "Manual";

    public async Task<CompanyLeadQueryResult<PagedResult<CompanyLeadSummary>>> GetLeadsAsync(Guid userId, CompanyLeadDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsRead, cancellationToken);
        if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        try
        {
            var leads = LeadQuery(scope.CompanyId);
            if (query.Search is not null) leads = leads.Where(x => (x.ContactName != null && x.ContactName.Contains(query.Search)) || (x.ContactPhone != null && x.ContactPhone.Contains(query.Search)) || (x.ContactEmail != null && x.ContactEmail.Contains(query.Search)));
            if (query.Stage is not null) leads = leads.Where(x => x.Stage == query.Stage);
            if (query.Priority is not null) leads = leads.Where(x => x.Priority == query.Priority);
            if (query.OwnerEmployeeId is not null) leads = leads.Where(x => x.OwnerEmployeeId == query.OwnerEmployeeId);
            if (query.UnassignedOnly == true) leads = leads.Where(x => x.OwnerEmployeeId == null);
            if (query.Source is not null) leads = leads.Where(x => x.Source == query.Source);
            if (query.CreatedFrom is not null) leads = leads.Where(x => x.CreatedAt >= query.CreatedFrom);
            if (query.CreatedTo is not null) leads = leads.Where(x => x.CreatedAt < query.CreatedTo);
            var count = await leads.CountAsync(cancellationToken);
            var page = leads.OrderBy(x => x.LastActivityAt == null).ThenByDescending(x => x.LastActivityAt).ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize);
            var items = await ProjectHeaders(page).ToListAsync(cancellationToken);
            return new(CompanyLeadOperationStatus.Succeeded, new PagedResult<CompanyLeadSummary>(items, query.PageNumber, query.PageSize, count));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadQueryResult<CompanyLeadDetails>> GetLeadAsync(Guid userId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsRead, cancellationToken);
        if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        try { return await DetailsAsync(scope.CompanyId, leadId, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadMutationResult> CreateLeadAsync(Guid userId, CreateCompanyLeadCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken);
        if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (!await OwnerEligibleAsync(scope.CompanyId, command.OwnerEmployeeId, cancellationToken)) return new(CompanyLeadOperationStatus.NotFound);
            var lead = new Lead { Id = Guid.NewGuid(), CompanyId = scope.CompanyId, CustomerProfileId = null, SourceBookingId = null, ContactName = command.ContactName, ContactPhone = command.ContactPhone, ContactEmail = command.ContactEmail, Source = ManualSource, Stage = LeadStage.New, Priority = command.Priority, OwnerEmployeeId = command.OwnerEmployeeId, CreatedAt = now, LastActivityAt = now, ClosedAt = null };
            dbContext.Add(lead);
            dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, "LeadCreated", now));
            await dbContext.SaveChangesAsync(cancellationToken);
            var details = await DetailsAsync(scope.CompanyId, lead.Id, cancellationToken);
            if (details.Status != CompanyLeadOperationStatus.Succeeded) return new(CompanyLeadOperationStatus.ServiceUnavailable);
            await tx.CommitAsync(cancellationToken);
            return new(CompanyLeadOperationStatus.Succeeded, details.Value);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException e) when (Unique(e)) { return new(CompanyLeadOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadMutationResult> UpdateLeadAsync(Guid userId, Guid leadId, UpdateCompanyLeadCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken);
        if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken);
            if (lead is null) return new(CompanyLeadOperationStatus.NotFound);
            if (lead.Stage is LeadStage.Won or LeadStage.Lost) return new(CompanyLeadOperationStatus.Conflict);
            if (lead.CustomerProfileId is null && command.ContactPhone is null && command.ContactEmail is null) return new(CompanyLeadOperationStatus.InvalidRequest);
            if (!RowVersion(lead, command.RowVersion)) return new(CompanyLeadOperationStatus.Conflict);
            if (!await OwnerEligibleAsync(scope.CompanyId, command.OwnerEmployeeId, cancellationToken)) return new(CompanyLeadOperationStatus.NotFound);
            ApplyVersion(lead, command.RowVersion);
            lead.ContactName = command.ContactName; lead.ContactPhone = command.ContactPhone; lead.ContactEmail = command.ContactEmail; lead.Priority = command.Priority; lead.OwnerEmployeeId = command.OwnerEmployeeId; lead.LastActivityAt = now;
            dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, "LeadUpdated", now));
            await dbContext.SaveChangesAsync(cancellationToken);
            var details = await DetailsAsync(scope.CompanyId, lead.Id, cancellationToken);
            if (details.Status != CompanyLeadOperationStatus.Succeeded) return new(CompanyLeadOperationStatus.ServiceUnavailable);
            await tx.CommitAsync(cancellationToken);
            return new(CompanyLeadOperationStatus.Succeeded, details.Value);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyLeadOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadOperationStatus> ChangeStageAsync(Guid userId, Guid leadId, ChangeCompanyLeadStageCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken);
        if (scope is null) return CompanyLeadOperationStatus.NotFound;
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken);
            if (lead is null) return CompanyLeadOperationStatus.NotFound;
            if (lead.Stage == command.Stage) return CompanyLeadOperationStatus.Succeeded;
            if (!Forward(lead.Stage, command.Stage) || !RowVersion(lead, command.RowVersion)) return CompanyLeadOperationStatus.Conflict;
            ApplyVersion(lead, command.RowVersion);
            var from = lead.Stage; lead.Stage = command.Stage; lead.LastActivityAt = now; lead.ClosedAt = command.Stage is LeadStage.Won or LeadStage.Lost ? now : null;
            var metadata = JsonSerializer.Serialize(new Dictionary<string, string> { ["fromStage"] = StageText(from), ["toStage"] = StageText(command.Stage) });
            dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, "StageChanged", now, command.Reason, metadata));
            await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); return CompanyLeadOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CompanyLeadOperationStatus.Conflict; }
        catch (DbUpdateException) { return CompanyLeadOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CompanyLeadOperationStatus.ServiceUnavailable; }
    }

    public Task<CompanyLeadRequirementResult> CreateRequirementAsync(Guid userId, Guid leadId, CompanyLeadRequirementCommand command, CancellationToken cancellationToken = default) => RequirementMutationAsync(userId, leadId, null, command, false, cancellationToken);
    public Task<CompanyLeadRequirementResult> UpdateRequirementAsync(Guid userId, Guid leadId, Guid requirementId, CompanyLeadRequirementCommand command, CancellationToken cancellationToken = default) => RequirementMutationAsync(userId, leadId, requirementId, command, true, cancellationToken);

    private async Task<CompanyLeadRequirementResult> RequirementMutationAsync(Guid userId, Guid leadId, Guid? requirementId, CompanyLeadRequirementCommand command, bool update, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken);
        if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken);
            if (lead is null) return new(CompanyLeadOperationStatus.NotFound);
            if (lead.Stage is LeadStage.Won or LeadStage.Lost || !RowVersion(lead, command.LeadRowVersion)) return new(CompanyLeadOperationStatus.Conflict);
            if (command.CurrencyCode is not null && !await dbContext.Set<Currency>().AsNoTracking().AnyAsync(x => x.Code == command.CurrencyCode, cancellationToken)) return new(CompanyLeadOperationStatus.NotFound);
            CustomerRequirement requirement;
            if (update)
            {
                requirement = await dbContext.Set<CustomerRequirement>().SingleOrDefaultAsync(x => x.Id == requirementId && x.LeadId == lead.Id, cancellationToken) ?? null!;
                if (requirement is null) return new(CompanyLeadOperationStatus.NotFound);
            }
            else
            {
                requirement = new CustomerRequirement { Id = Guid.NewGuid(), LeadId = lead.Id, CreatedAt = now };
                dbContext.Add(requirement);
            }
            ApplyRequirement(requirement, command, now); requirement.ConfirmedByEmployeeId = null; requirement.ConfirmedAt = null;
            ApplyVersion(lead, command.LeadRowVersion); lead.LastActivityAt = now;
            dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, update ? "RequirementUpdated" : "RequirementCreated", now));
            await dbContext.SaveChangesAsync(cancellationToken);
            var projected = await RequirementQuery(lead.Id).SingleAsync(x => x.Id == requirement.Id, cancellationToken);
            var mapped = MapRequirement(projected);
            if (mapped is null) return new(CompanyLeadOperationStatus.ServiceUnavailable);
            await tx.CommitAsync(cancellationToken);
            return new(CompanyLeadOperationStatus.Succeeded, mapped, lead.RowVersion);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyLeadOperationStatus.Conflict); }
        catch (DbUpdateException e) when (Unique(e)) { return new(CompanyLeadOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadOperationStatus> ConfirmRequirementAsync(Guid userId, Guid leadId, Guid requirementId, byte[] leadRowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken); if (scope is null) return CompanyLeadOperationStatus.NotFound;
        var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken); if (lead is null) return CompanyLeadOperationStatus.NotFound;
            var req = await dbContext.Set<CustomerRequirement>().SingleOrDefaultAsync(x => x.Id == requirementId && x.LeadId == lead.Id, cancellationToken); if (req is null) return CompanyLeadOperationStatus.NotFound;
            if (lead.Stage is LeadStage.Won or LeadStage.Lost) return CompanyLeadOperationStatus.Conflict;
            if (!StoredRequirementValid(req)) return CompanyLeadOperationStatus.ServiceUnavailable;
            if (req.CurrencyCode is not null && !await dbContext.Set<Currency>().AsNoTracking().AnyAsync(x => x.Code == req.CurrencyCode, cancellationToken)) return CompanyLeadOperationStatus.ServiceUnavailable;
            if ((req.ConfirmedByEmployeeId is null) != (req.ConfirmedAt is null)) return CompanyLeadOperationStatus.ServiceUnavailable;
            if (req.ConfirmedByEmployeeId is not null && !await dbContext.Set<CompanyEmployee>().AsNoTracking().AnyAsync(x => x.Id == req.ConfirmedByEmployeeId && x.CompanyId == scope.CompanyId, cancellationToken)) return CompanyLeadOperationStatus.ServiceUnavailable;
            if (req.ConfirmedAt is not null) return CompanyLeadOperationStatus.Succeeded;
            if (!RowVersion(lead, leadRowVersion)) return CompanyLeadOperationStatus.Conflict;
            ApplyVersion(lead, leadRowVersion); req.ConfirmedByEmployeeId = scope.CompanyEmployeeId; req.ConfirmedAt = now; req.UpdatedAt = now; lead.LastActivityAt = now;
            dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, "RequirementConfirmed", now)); await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); return CompanyLeadOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; } catch (DbUpdateConcurrencyException) { return CompanyLeadOperationStatus.Conflict; } catch (DbUpdateException) { return CompanyLeadOperationStatus.ServiceUnavailable; } catch (DbException) { return CompanyLeadOperationStatus.ServiceUnavailable; }
    }

    public async Task<CompanyLeadOperationStatus> DeleteRequirementAsync(Guid userId, Guid leadId, Guid requirementId, byte[] leadRowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken); if (scope is null) return CompanyLeadOperationStatus.NotFound; var now = timeProvider.GetUtcNow();
        try { await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken); var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken); if (lead is null) return CompanyLeadOperationStatus.NotFound; var req = await dbContext.Set<CustomerRequirement>().SingleOrDefaultAsync(x => x.Id == requirementId && x.LeadId == lead.Id, cancellationToken); if (req is null) return CompanyLeadOperationStatus.NotFound; if (lead.Stage is LeadStage.Won or LeadStage.Lost || !RowVersion(lead, leadRowVersion)) return CompanyLeadOperationStatus.Conflict; ApplyVersion(lead, leadRowVersion); lead.LastActivityAt = now; dbContext.Remove(req); dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, "RequirementDeleted", now)); await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); return CompanyLeadOperationStatus.Succeeded; }
        catch (OperationCanceledException) { throw; } catch (DbUpdateConcurrencyException) { return CompanyLeadOperationStatus.Conflict; } catch (DbUpdateException) { return CompanyLeadOperationStatus.ServiceUnavailable; } catch (DbException) { return CompanyLeadOperationStatus.ServiceUnavailable; }
    }

    public async Task<CompanyLeadInterestsResult> ReplaceInterestsAsync(Guid userId, Guid leadId, ReplaceCompanyLeadInterestsCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken); if (scope is null) return new(CompanyLeadOperationStatus.NotFound); var now = timeProvider.GetUtcNow();
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken); var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken); if (lead is null) return new(CompanyLeadOperationStatus.NotFound); if (lead.Stage is LeadStage.Won or LeadStage.Lost || !RowVersion(lead, command.LeadRowVersion)) return new(CompanyLeadOperationStatus.Conflict);
            var listingIds = command.Items.Select(x => x.ListingId).Distinct().ToList(); var ownedCount = await dbContext.Set<Listing>().AsNoTracking().CountAsync(x => listingIds.Contains(x.Id) && x.CompanyId == scope.CompanyId, cancellationToken); if (ownedCount != listingIds.Count) return new(CompanyLeadOperationStatus.NotFound);
            var existing = await dbContext.Set<LeadInterest>().Where(x => x.LeadId == lead.Id).ToListAsync(cancellationToken);
            foreach (var old in existing.Where(x => !command.Items.Any(i => i.ListingId == x.ListingId && i.InterestType == x.InterestType))) dbContext.Remove(old);
            foreach (var item in command.Items.Where(i => !existing.Any(x => x.ListingId == i.ListingId && x.InterestType == i.InterestType))) dbContext.Add(new LeadInterest { Id = Guid.NewGuid(), LeadId = lead.Id, ListingId = item.ListingId, InterestType = item.InterestType, CreatedAt = now });
            ApplyVersion(lead, command.LeadRowVersion); lead.LastActivityAt = now; dbContext.Add(Activity(lead.Id, scope.CompanyEmployeeId, "InterestsReplaced", now)); await dbContext.SaveChangesAsync(cancellationToken);
            var interests = await InterestQuery(lead.Id).ToListAsync(cancellationToken); await tx.CommitAsync(cancellationToken); return new(CompanyLeadOperationStatus.Succeeded, interests, lead.RowVersion);
        }
        catch (OperationCanceledException) { throw; } catch (DbUpdateConcurrencyException) { return new(CompanyLeadOperationStatus.Conflict); } catch (DbUpdateException e) when (Unique(e)) { return new(CompanyLeadOperationStatus.Conflict); } catch (DbUpdateException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); } catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadQueryResult<PagedResult<CompanyLeadActivity>>> GetActivitiesAsync(Guid userId, Guid leadId, CompanyLeadActivityQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsRead, cancellationToken); if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        try
        {
            if (!await LeadQuery(scope.CompanyId).AnyAsync(x => x.Id == leadId, cancellationToken)) return new(CompanyLeadOperationStatus.NotFound);
            var activities = ActivityEntityQuery(leadId); if (query.ActivityType is not null) activities = activities.Where(x => x.ActivityType == query.ActivityType); if (query.From is not null) activities = activities.Where(x => x.OccurredAt >= query.From); if (query.To is not null) activities = activities.Where(x => x.OccurredAt < query.To);
            var count = await activities.CountAsync(cancellationToken); var page = activities.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize); var rows = await ProjectActivities(page).ToListAsync(cancellationToken);
            var mapped = new List<CompanyLeadActivity>(rows.Count); foreach (var row in rows) { var item = MapActivity(row); if (item is null) return new(CompanyLeadOperationStatus.ServiceUnavailable); mapped.Add(item); }
            return new(CompanyLeadOperationStatus.Succeeded, new PagedResult<CompanyLeadActivity>(mapped, query.PageNumber, query.PageSize, count));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyLeadActivityResult> CreateActivityAsync(Guid userId, Guid leadId, CreateCompanyLeadActivityCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(userId, CompanyPermissionCodes.LeadsManage, cancellationToken); if (scope is null) return new(CompanyLeadOperationStatus.NotFound);
        var now = timeProvider.GetUtcNow(); var occurredAt = command.OccurredAt ?? now; if (occurredAt > now) return new(CompanyLeadOperationStatus.InvalidRequest);
        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken); var lead = await OwnedLeadAsync(scope.CompanyId, leadId, cancellationToken); if (lead is null) return new(CompanyLeadOperationStatus.NotFound); if (!RowVersion(lead, command.LeadRowVersion)) return new(CompanyLeadOperationStatus.Conflict); ApplyVersion(lead, command.LeadRowVersion);
            if (lead.LastActivityAt is null || occurredAt > lead.LastActivityAt) lead.LastActivityAt = occurredAt; else dbContext.Entry(lead).Property(x => x.LastActivityAt).IsModified = true;
            var activity = Activity(lead.Id, scope.CompanyEmployeeId, command.ActivityType, occurredAt, command.Notes, command.MetadataJson); dbContext.Add(activity); await dbContext.SaveChangesAsync(cancellationToken);
            var row = await ProjectActivities(ActivityEntityQuery(lead.Id).Where(x => x.Id == activity.Id)).SingleAsync(cancellationToken); var mapped = MapActivity(row); if (mapped is null) return new(CompanyLeadOperationStatus.ServiceUnavailable); await tx.CommitAsync(cancellationToken); return new(CompanyLeadOperationStatus.Succeeded, mapped, lead.RowVersion);
        }
        catch (OperationCanceledException) { throw; } catch (DbUpdateConcurrencyException) { return new(CompanyLeadOperationStatus.Conflict); } catch (DbUpdateException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); } catch (DbException) { return new(CompanyLeadOperationStatus.ServiceUnavailable); }
    }

    private async Task<CompanyLeadQueryResult<CompanyLeadDetails>> DetailsAsync(Guid companyId, Guid leadId, CancellationToken cancellationToken)
    {
        var header = await ProjectHeaders(LeadQuery(companyId).Where(x => x.Id == leadId)).SingleOrDefaultAsync(cancellationToken); if (header is null) return new(CompanyLeadOperationStatus.NotFound);
        var reqRows = await RequirementQuery(leadId).ToListAsync(cancellationToken); var requirements = new List<CompanyLeadRequirement>(reqRows.Count); foreach (var row in reqRows) { var item = MapRequirement(row); if (item is null) return new(CompanyLeadOperationStatus.ServiceUnavailable); requirements.Add(item); }
        var interests = await InterestQuery(leadId).ToListAsync(cancellationToken); return new(CompanyLeadOperationStatus.Succeeded, new CompanyLeadDetails(header, requirements, interests));
    }

    private IQueryable<Lead> LeadQuery(Guid companyId) => dbContext.Set<Lead>().AsNoTracking().Where(x => x.CompanyId == companyId);
    private static IQueryable<CompanyLeadSummary> ProjectHeaders(IQueryable<Lead> leads) => leads.Select(x => new CompanyLeadSummary(x.Id, x.Stage, x.Priority, x.ContactName, x.ContactPhone, x.ContactEmail, x.Source, x.CreatedAt, x.LastActivityAt, x.ClosedAt, x.RowVersion, x.Requirements.Count, x.Interests.Count, x.Activities.Count,
        x.CustomerProfile == null ? null : new CompanyLeadCustomerSummary(x.CustomerProfile.Id, x.CustomerProfile.FullName, x.CustomerProfile.Persona),
        x.SourceBooking == null ? null : new CompanyLeadBookingSummary(x.SourceBooking.Id, x.SourceBooking.BookingCode, x.SourceBooking.Status),
        x.OwnerEmployee == null ? null : new CompanyLeadEmployeeSummary(x.OwnerEmployee.Id, x.OwnerEmployee.FullName, x.OwnerEmployee.JobTitle)));
    private IQueryable<RequirementRow> RequirementQuery(Guid leadId) => dbContext.Set<CustomerRequirement>().AsNoTracking().Where(x => x.LeadId == leadId).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new RequirementRow(x.Id, x.Intent, x.OriginalQuery, x.StructuredCriteriaJson, x.CriteriaSchemaVersion, x.MinBudget, x.MaxBudget, x.CurrencyCode, x.MinBedrooms, x.MinBathrooms, x.MinArea, x.MaxArea, x.PaymentPlanMonths, x.Notes, x.ExtractedByAI, x.ConfirmedByEmployee == null ? null : new CompanyLeadEmployeeSummary(x.ConfirmedByEmployee.Id, x.ConfirmedByEmployee.FullName, x.ConfirmedByEmployee.JobTitle), x.ConfirmedAt, x.CreatedAt, x.UpdatedAt));
    private IQueryable<CompanyLeadInterest> InterestQuery(Guid leadId) => dbContext.Set<LeadInterest>().AsNoTracking().Where(x => x.LeadId == leadId).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new CompanyLeadInterest(x.Id, x.ListingId, x.Listing.ListingCode, x.Listing.Slug, x.Listing.Title, x.InterestType, x.CreatedAt));
    private IQueryable<CRMActivity> ActivityEntityQuery(Guid leadId) => dbContext.Set<CRMActivity>().AsNoTracking().Where(x => x.LeadId == leadId);
    private static IQueryable<ActivityRow> ProjectActivities(IQueryable<CRMActivity> activities) => activities.Select(x => new ActivityRow(x.Id, x.ActivityType, x.OccurredAt, x.Notes, x.MetadataJson, x.PerformedByEmployee == null ? null : new CompanyLeadEmployeeSummary(x.PerformedByEmployee.Id, x.PerformedByEmployee.FullName, x.PerformedByEmployee.JobTitle)));
    private Task<Lead?> OwnedLeadAsync(Guid companyId, Guid leadId, CancellationToken cancellationToken) => dbContext.Set<Lead>().SingleOrDefaultAsync(x => x.Id == leadId && x.CompanyId == companyId, cancellationToken);
    private Task<CompanyAccessScope?> ScopeAsync(Guid userId, string permission, CancellationToken token) => companyAccessService.GetAuthorizedScopeAsync(userId, permission, token);
    private async Task<bool> OwnerEligibleAsync(Guid companyId, Guid? employeeId, CancellationToken token) => employeeId is null || await dbContext.Set<CompanyEmployee>().AsNoTracking().AnyAsync(x => x.Id == employeeId && x.CompanyId == companyId && x.Status == CompanyEmployeeStatus.Active && x.EndedAt == null, token);
    private static bool RowVersion(Lead lead, byte[] value) => lead.RowVersion.AsSpan().SequenceEqual(value);
    private void ApplyVersion(Lead lead, byte[] value) => dbContext.Entry(lead).Property(x => x.RowVersion).OriginalValue = value;
    private static CRMActivity Activity(Guid leadId, Guid employeeId, string type, DateTimeOffset at, string? notes = null, string? metadata = null) => new() { Id = Guid.NewGuid(), LeadId = leadId, PerformedByEmployeeId = employeeId, ActivityType = type, OccurredAt = at, Notes = notes, MetadataJson = metadata };
    private static void ApplyRequirement(CustomerRequirement r, CompanyLeadRequirementCommand c, DateTimeOffset now) { r.Intent = c.Intent; r.OriginalQuery = c.OriginalQuery; r.StructuredCriteriaJson = c.StructuredCriteriaJson; r.CriteriaSchemaVersion = c.CriteriaSchemaVersion; r.MinBudget = c.MinBudget; r.MaxBudget = c.MaxBudget; r.CurrencyCode = c.CurrencyCode; r.MinBedrooms = c.MinBedrooms; r.MinBathrooms = c.MinBathrooms; r.MinArea = c.MinArea; r.MaxArea = c.MaxArea; r.PaymentPlanMonths = c.PaymentPlanMonths; r.Notes = c.Notes; r.ExtractedByAI = c.ExtractedByAI; r.UpdatedAt = now; }
    private static bool StoredRequirementValid(CustomerRequirement r) => Enum.IsDefined(r.Intent) && r.StructuredCriteriaJson.Length <= 16000 && TryObject(r.StructuredCriteriaJson, out _) && r.CriteriaSchemaVersion > 0 && Decimal18_2(r.MinBudget) && Decimal18_2(r.MaxBudget) && r.MinBudget is null or >= 0 && r.MaxBudget is null or >= 0 && (r.MinBudget is null || r.MaxBudget is null || r.MinBudget <= r.MaxBudget) && ((r.MinBudget is null && r.MaxBudget is null) || ValidCurrencyCode(r.CurrencyCode)) && (r.CurrencyCode is null || ValidCurrencyCode(r.CurrencyCode)) && r.MinBedrooms is null or >= 0 && r.MinBathrooms is null or >= 0 && Decimal18_2(r.MinArea) && Decimal18_2(r.MaxArea) && r.MinArea is null or > 0 && r.MaxArea is null or > 0 && (r.MinArea is null || r.MaxArea is null || r.MinArea <= r.MaxArea) && r.PaymentPlanMonths is null or > 0;
    private static CompanyLeadRequirement? MapRequirement(RequirementRow r) => TryObject(r.Json, out var json) ? new(r.Id, r.Intent, r.OriginalQuery, json, r.Schema, r.MinBudget, r.MaxBudget, r.Currency, r.MinBedrooms, r.MinBathrooms, r.MinArea, r.MaxArea, r.Months, r.Notes, r.Ai, r.Employee, r.ConfirmedAt, r.CreatedAt, r.UpdatedAt) : null;
    private static CompanyLeadActivity? MapActivity(ActivityRow r) { if (r.Metadata is null) return new(r.Id, r.Type, r.At, r.Notes, null, r.Employee); return TryObject(r.Metadata, out var json) ? new(r.Id, r.Type, r.At, r.Notes, json, r.Employee) : null; }
    private static bool TryObject(string json, out JsonElement element) { try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.ValueKind == JsonValueKind.Object) { element = doc.RootElement.Clone(); return true; } } catch (JsonException) { } element = default; return false; }
    private static bool Decimal18_2(decimal? value) => value is null || (decimal.Round(value.Value, 2) == value.Value && decimal.Abs(value.Value) <= 9999999999999999.99m);
    private static bool ValidCurrencyCode(string? value) => value is { Length: 3 } && value.All(x => x is >= 'A' and <= 'Z');
    private static bool Forward(LeadStage from, LeadStage to) => from switch { LeadStage.New => to is not LeadStage.New, LeadStage.Contacted => to is LeadStage.Qualified or LeadStage.ViewingScheduled or LeadStage.Negotiation or LeadStage.Won or LeadStage.Lost, LeadStage.Qualified => to is LeadStage.ViewingScheduled or LeadStage.Negotiation or LeadStage.Won or LeadStage.Lost, LeadStage.ViewingScheduled => to is LeadStage.Negotiation or LeadStage.Won or LeadStage.Lost, LeadStage.Negotiation => to is LeadStage.Won or LeadStage.Lost, _ => false };
    private static string StageText(LeadStage x) => x switch { LeadStage.New => "New", LeadStage.Contacted => "Contacted", LeadStage.Qualified => "Qualified", LeadStage.ViewingScheduled => "ViewingScheduled", LeadStage.Negotiation => "Negotiation", LeadStage.Won => "Won", LeadStage.Lost => "Lost", _ => throw new ArgumentOutOfRangeException(nameof(x), x, null) };
    private static bool Unique(DbUpdateException e) => e.GetBaseException() is SqlException { Number: 2601 or 2627 };
    private sealed record RequirementRow(Guid Id, CustomerIntent Intent, string? OriginalQuery, string Json, int Schema, decimal? MinBudget, decimal? MaxBudget, string? Currency, int? MinBedrooms, int? MinBathrooms, decimal? MinArea, decimal? MaxArea, int? Months, string? Notes, bool Ai, CompanyLeadEmployeeSummary? Employee, DateTimeOffset? ConfirmedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
    private sealed record ActivityRow(Guid Id, string Type, DateTimeOffset At, string? Notes, string? Metadata, CompanyLeadEmployeeSummary? Employee);
}
