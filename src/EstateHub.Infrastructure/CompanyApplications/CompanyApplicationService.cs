using System.Data;
using System.Data.Common;
using System.Net.Mail;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyApplications;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Identity.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyApplications;

public sealed class CompanyApplicationService(
    EstateHubDbContext dbContext,
    TimeProvider timeProvider) : ICompanyApplicationService
{
    private const string PdfContentType = "application/pdf";

    public async Task<PagedResult<CompanyApplicationSummary>> GetApplicationsAsync(
        Guid applicationUserId,
        CompanyApplicationDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var applications = dbContext.Set<CompanyApplication>()
            .AsNoTracking()
            .Where(application => application.SubmittedByApplicationUserId == applicationUserId);

        if (query.Status is not null)
        {
            applications = applications.Where(application => application.Status == query.Status.Value);
        }

        var totalCount = await applications.CountAsync(cancellationToken);
        var items = await applications
            .OrderByDescending(application =>
                application.SubmittedAt
                ?? application.StatusHistory
                    .OrderBy(history => history.ChangedAt)
                    .Select(history => (DateTimeOffset?)history.ChangedAt)
                    .FirstOrDefault()
                ?? DateTimeOffset.MinValue)
            .ThenByDescending(application => application.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(application => new CompanyApplicationSummary(
                application.Id,
                application.LegalName,
                application.CompanyType,
                application.Status,
                application.SubmittedAt,
                application.ReviewedAt,
                application.DecisionReason,
                application.Documents.Count,
                application.ApprovedCompanyId,
                application.RowVersion))
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyApplicationSummary>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<CompanyApplicationDetails?> GetApplicationAsync(
        Guid applicationUserId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var header = await dbContext.Set<CompanyApplication>()
            .AsNoTracking()
            .Where(application =>
                application.Id == applicationId
                && application.SubmittedByApplicationUserId == applicationUserId)
            .Select(application => new ApplicationHeader(
                application.Id,
                application.LegalName,
                application.BusinessEmail,
                application.PhoneNumber,
                application.RegistrationNumber,
                application.TaxId,
                application.Website,
                application.CompanyType,
                application.OfficeAddress,
                application.LocationText,
                application.EstimatedPropertyRange,
                application.Status,
                application.SubmittedAt,
                application.ReviewedAt,
                application.DecisionReason,
                application.ApprovedCompany == null
                    ? null
                    : new CompanyApplicationApprovedCompany(
                        application.ApprovedCompany.Id,
                        application.ApprovedCompany.Slug,
                        application.ApprovedCompany.DisplayName),
                application.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
        if (header is null)
        {
            return null;
        }

        var documents = await DocumentsQuery(applicationId, applicationUserId)
            .ToListAsync(cancellationToken);
        var statusHistory = await dbContext.Set<ApplicationStatusHistory>()
            .AsNoTracking()
            .Where(history =>
                history.CompanyApplicationId == applicationId
                && history.CompanyApplication.SubmittedByApplicationUserId == applicationUserId)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .Select(history => new CompanyApplicationStatusChange(
                history.Id,
                history.FromStatus,
                history.ToStatus,
                history.ChangedAt,
                history.Reason))
            .ToListAsync(cancellationToken);

        return header.ToDetails(documents, statusHistory);
    }

    public async Task<CompanyApplicationMutationResult> CreateApplicationAsync(
        Guid applicationUserId,
        CompanyApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var lockedUsers = await dbContext.Set<ApplicationUser>()
                .Where(user =>
                    user.Id == applicationUserId
                    && user.AccountStatus == ApplicationUserAccountStatus.Active
                    && user.EmailConfirmed)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(user => user.UpdatedAt, user => user.UpdatedAt),
                    cancellationToken);
            if (lockedUsers != 1)
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            var hasCurrentMembership = await dbContext.Set<CompanyEmployee>()
                .AsNoTracking()
                .AnyAsync(employee =>
                    employee.ApplicationUserId == applicationUserId
                    && employee.EndedAt == null,
                    cancellationToken);
            if (hasCurrentMembership)
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            var hasApprovedApplication = await dbContext.Set<CompanyApplication>()
                .AsNoTracking()
                .AnyAsync(application =>
                    application.SubmittedByApplicationUserId == applicationUserId
                    && application.Status == CompanyApplicationStatus.Approved,
                    cancellationToken);
            if (hasApprovedApplication)
            {
                return new(CompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var hasActiveApplication = await ActiveApplications(applicationUserId)
                .AnyAsync(cancellationToken);
            if (hasActiveApplication || await HasIdentityConflictAsync(command, null, cancellationToken))
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            var utcNow = timeProvider.GetUtcNow();
            var application = new CompanyApplication
            {
                Id = Guid.NewGuid(),
                SubmittedByApplicationUserId = applicationUserId,
                ApprovedCompanyId = null,
                LegalName = command.LegalName,
                BusinessEmail = command.BusinessEmail,
                PhoneNumber = command.PhoneNumber,
                RegistrationNumber = command.RegistrationNumber,
                TaxId = command.TaxId,
                Website = command.Website,
                CompanyType = command.CompanyType,
                OfficeAddress = command.OfficeAddress,
                LocationText = command.LocationText,
                EstimatedPropertyRange = command.EstimatedPropertyRange,
                Status = CompanyApplicationStatus.Draft,
                SubmittedAt = null,
                ReviewedAt = null,
                ReviewedByApplicationUserId = null,
                DecisionReason = null
            };
            dbContext.Add(application);
            dbContext.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                CompanyApplicationId = application.Id,
                FromStatus = null,
                ToStatus = CompanyApplicationStatus.Draft,
                ChangedByApplicationUserId = applicationUserId,
                ChangedAt = utcNow,
                Reason = null
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var created = await GetApplicationAsync(applicationUserId, application.Id, cancellationToken);
            return created is null
                ? new(CompanyApplicationOperationStatus.ServiceUnavailable)
                : new(CompanyApplicationOperationStatus.Succeeded, created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return new(CompanyApplicationOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(CompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<CompanyApplicationMutationResult> UpdateApplicationAsync(
        Guid applicationUserId,
        Guid applicationId,
        CompanyApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == applicationId
                    && candidate.SubmittedByApplicationUserId == applicationUserId,
                    cancellationToken);
            if (application is null)
            {
                return new(CompanyApplicationOperationStatus.NotFound);
            }

            if (application.Status is not (CompanyApplicationStatus.Draft or CompanyApplicationStatus.NeedsChanges))
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            if (await HasIdentityConflictAsync(command, application.Id, cancellationToken))
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion!;
            ApplyCommand(application, command);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var updated = await GetApplicationAsync(applicationUserId, application.Id, cancellationToken);
            return updated is null
                ? new(CompanyApplicationOperationStatus.ServiceUnavailable)
                : new(CompanyApplicationOperationStatus.Succeeded, updated);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CompanyApplicationOperationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            return new(CompanyApplicationOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(CompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<CompanyApplicationDocumentsResult> ReplaceDocumentsAsync(
        Guid applicationUserId,
        Guid applicationId,
        IReadOnlyList<CompanyApplicationDocumentCommand> documents,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (documents.Count > 20
            || documents.Any(document => document.FileAssetId == Guid.Empty || string.IsNullOrWhiteSpace(document.DocumentType) || document.DocumentType.Length > 100)
            || documents.Select(document => document.FileAssetId).Distinct().Count() != documents.Count
            || documents.Select(document => (document.FileAssetId, document.DocumentType)).Distinct().Count() != documents.Count)
        {
            return new(CompanyApplicationOperationStatus.Conflict);
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == applicationId
                    && candidate.SubmittedByApplicationUserId == applicationUserId,
                    cancellationToken);
            if (application is null)
            {
                return new(CompanyApplicationOperationStatus.NotFound);
            }

            if (application.Status is not (CompanyApplicationStatus.Draft or CompanyApplicationStatus.NeedsChanges))
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            if (application.Status == CompanyApplicationStatus.NeedsChanges && documents.Count == 0)
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            var requestedAssetIds = documents.Select(document => document.FileAssetId).ToList();
            var assets = await dbContext.Set<FileAsset>()
                .AsNoTracking()
                .Where(file => requestedAssetIds.Contains(file.Id))
                .Select(file => new DocumentAsset(
                    file.Id,
                    file.UploadedByApplicationUserId,
                    file.FileType,
                    file.ContentType))
                .ToListAsync(cancellationToken);
            if (assets.Count != requestedAssetIds.Count
                || assets.Any(asset => asset.UploadedByApplicationUserId != applicationUserId))
            {
                return new(CompanyApplicationOperationStatus.NotFound);
            }

            if (assets.Any(asset => asset.FileType != FileType.Document || asset.ContentType != PdfContentType))
            {
                return new(CompanyApplicationOperationStatus.Conflict);
            }

            var currentDocuments = await dbContext.Set<CompanyDocument>()
                .Where(document => document.CompanyApplicationId == applicationId)
                .ToListAsync(cancellationToken);
            var currentKeys = currentDocuments
                .Select(document => (document.FileAssetId, document.DocumentType))
                .ToList();
            if (currentKeys.Distinct().Count() != currentKeys.Count)
            {
                return new(CompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var requestedKeys = documents
                .Select(document => (document.FileAssetId, document.DocumentType))
                .ToHashSet();
            dbContext.RemoveRange(currentDocuments.Where(document =>
                !requestedKeys.Contains((document.FileAssetId, document.DocumentType))));

            var utcNow = timeProvider.GetUtcNow();
            foreach (var document in documents.Where(document =>
                         !currentKeys.Contains((document.FileAssetId, document.DocumentType))))
            {
                dbContext.Add(new CompanyDocument
                {
                    Id = Guid.NewGuid(),
                    CompanyApplicationId = applicationId,
                    FileAssetId = document.FileAssetId,
                    DocumentType = document.DocumentType,
                    VerificationStatus = DocumentVerificationStatus.Pending,
                    ReviewedAt = null,
                    ReviewedByApplicationUserId = null,
                    RejectionReason = null,
                    CreatedAt = utcNow
                });
            }

            dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
            dbContext.Entry(application).Property(candidate => candidate.Status).IsModified = true;
            await dbContext.SaveChangesAsync(cancellationToken);

            var refreshedDocuments = await DocumentsQuery(applicationId, applicationUserId)
                .ToListAsync(cancellationToken);
            var refreshedRowVersion = application.RowVersion;
            await transaction.CommitAsync(cancellationToken);
            return new(
                CompanyApplicationOperationStatus.Succeeded,
                refreshedRowVersion,
                refreshedDocuments);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(CompanyApplicationOperationStatus.Conflict);
        }
        catch (DbUpdateException)
        {
            return new(CompanyApplicationOperationStatus.ServiceUnavailable);
        }
        catch (DbException)
        {
            return new(CompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<CompanyApplicationOperationStatus> SubmitApplicationAsync(
        Guid applicationUserId,
        Guid applicationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == applicationId
                    && candidate.SubmittedByApplicationUserId == applicationUserId,
                    cancellationToken);
            if (application is null)
            {
                return CompanyApplicationOperationStatus.NotFound;
            }

            if (application.Status == CompanyApplicationStatus.Submitted)
            {
                return CompanyApplicationOperationStatus.Succeeded;
            }

            if (application.Status is not (CompanyApplicationStatus.Draft or CompanyApplicationStatus.NeedsChanges))
            {
                return CompanyApplicationOperationStatus.Conflict;
            }

            var documentEligibility = await dbContext.Set<CompanyDocument>()
                .AsNoTracking()
                .Where(document => document.CompanyApplicationId == applicationId)
                .Select(document => new SubmissionDocument(
                    document.VerificationStatus,
                    document.FileAsset.UploadedByApplicationUserId == applicationUserId,
                    document.FileAsset.FileType,
                    document.FileAsset.ContentType))
                .ToListAsync(cancellationToken);
            if (documentEligibility.Count == 0
                || documentEligibility.Any(document =>
                    !document.IsOwnedByApplicant
                    || document.FileType != FileType.Document
                    || document.ContentType != PdfContentType
                    || document.VerificationStatus == DocumentVerificationStatus.Rejected))
            {
                return CompanyApplicationOperationStatus.Conflict;
            }

            if (documentEligibility.Any(document => document.VerificationStatus is not (DocumentVerificationStatus.Pending or DocumentVerificationStatus.Approved)))
            {
                throw new ArgumentOutOfRangeException(nameof(DocumentVerificationStatus), "Unknown document verification status.");
            }

            if (!IsValidForSubmission(application)
                || await HasIdentityConflictAsync(ToCommand(application), application.Id, cancellationToken))
            {
                return CompanyApplicationOperationStatus.Conflict;
            }

            var previousStatus = application.Status;
            var utcNow = timeProvider.GetUtcNow();
            dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
            application.Status = CompanyApplicationStatus.Submitted;
            application.SubmittedAt = utcNow;
            application.ReviewedAt = null;
            application.ReviewedByApplicationUserId = null;
            application.DecisionReason = null;
            dbContext.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                CompanyApplicationId = application.Id,
                FromStatus = previousStatus,
                ToStatus = CompanyApplicationStatus.Submitted,
                ChangedByApplicationUserId = applicationUserId,
                ChangedAt = utcNow,
                Reason = null
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyApplicationOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompanyApplicationOperationStatus.Conflict;
        }
        catch (DbUpdateException)
        {
            return CompanyApplicationOperationStatus.ServiceUnavailable;
        }
        catch (DbException)
        {
            return CompanyApplicationOperationStatus.ServiceUnavailable;
        }
    }

    private IQueryable<CompanyApplication> ActiveApplications(Guid applicationUserId) =>
        dbContext.Set<CompanyApplication>()
            .AsNoTracking()
            .Where(application =>
                application.SubmittedByApplicationUserId == applicationUserId
                && (application.Status == CompanyApplicationStatus.Draft
                    || application.Status == CompanyApplicationStatus.Submitted
                    || application.Status == CompanyApplicationStatus.UnderReview
                    || application.Status == CompanyApplicationStatus.NeedsChanges));

    private async Task<bool> HasIdentityConflictAsync(
        CompanyApplicationCommand command,
        Guid? excludedApplicationId,
        CancellationToken cancellationToken)
    {
        var companyConflict = await dbContext.Set<Company>()
            .AsNoTracking()
            .AnyAsync(company =>
                company.RegistrationNumber == command.RegistrationNumber
                || (command.TaxId != null && company.TaxId == command.TaxId),
                cancellationToken);
        if (companyConflict)
        {
            return true;
        }

        return await dbContext.Set<CompanyApplication>()
            .AsNoTracking()
            .AnyAsync(application =>
                application.Id != excludedApplicationId
                && (application.Status == CompanyApplicationStatus.Draft
                    || application.Status == CompanyApplicationStatus.Submitted
                    || application.Status == CompanyApplicationStatus.UnderReview
                    || application.Status == CompanyApplicationStatus.NeedsChanges)
                && (application.RegistrationNumber == command.RegistrationNumber
                    || (command.TaxId != null && application.TaxId == command.TaxId)),
                cancellationToken);
    }

    private IQueryable<CompanyApplicationDocument> DocumentsQuery(
        Guid applicationId,
        Guid applicationUserId) =>
        dbContext.Set<CompanyDocument>()
            .AsNoTracking()
            .Where(document =>
                document.CompanyApplicationId == applicationId
                && document.CompanyApplication.SubmittedByApplicationUserId == applicationUserId)
            .OrderBy(document => document.DocumentType)
            .ThenBy(document => document.Id)
            .Select(document => new CompanyApplicationDocument(
                document.Id,
                document.DocumentType,
                document.FileAssetId,
                document.FileAsset.OriginalFileName,
                document.FileAsset.ContentType,
                document.FileAsset.SizeBytes,
                document.VerificationStatus,
                document.ReviewedAt,
                document.RejectionReason));

    private static void ApplyCommand(
        CompanyApplication application,
        CompanyApplicationCommand command)
    {
        application.LegalName = command.LegalName;
        application.BusinessEmail = command.BusinessEmail;
        application.PhoneNumber = command.PhoneNumber;
        application.RegistrationNumber = command.RegistrationNumber;
        application.TaxId = command.TaxId;
        application.Website = command.Website;
        application.CompanyType = command.CompanyType;
        application.OfficeAddress = command.OfficeAddress;
        application.LocationText = command.LocationText;
        application.EstimatedPropertyRange = command.EstimatedPropertyRange;
    }

    private static CompanyApplicationCommand ToCommand(CompanyApplication application) => new(
        application.LegalName,
        application.BusinessEmail,
        application.PhoneNumber,
        application.RegistrationNumber,
        application.TaxId,
        application.Website,
        application.CompanyType,
        application.OfficeAddress,
        application.LocationText,
        application.EstimatedPropertyRange,
        application.RowVersion);

    private static bool IsValidForSubmission(CompanyApplication application) =>
        IsRequiredText(application.LegalName, 200)
        && IsRequiredText(application.BusinessEmail, 256)
        && MailAddress.TryCreate(application.BusinessEmail, out var email)
        && string.Equals(email.Address, application.BusinessEmail, StringComparison.OrdinalIgnoreCase)
        && IsRequiredText(application.PhoneNumber, 32)
        && IsRequiredText(application.RegistrationNumber, 100)
        && IsOptionalText(application.TaxId, 100)
        && IsValidWebsite(application.Website)
        && application.CompanyType is CompanyType.Developer or CompanyType.BrokerAgency
        && IsRequiredText(application.OfficeAddress, 500)
        && IsRequiredText(application.LocationText, 250)
        && IsOptionalText(application.EstimatedPropertyRange, 100);

    private static bool IsRequiredText(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= maximumLength
        && value == value.Trim();

    private static bool IsOptionalText(string? value, int maximumLength) =>
        value is null
        || (!string.IsNullOrWhiteSpace(value)
            && value.Length <= maximumLength
            && value == value.Trim());

    private static bool IsValidWebsite(string? website)
    {
        if (!IsOptionalText(website, 2048))
        {
            return false;
        }

        return website is null
            || (Uri.TryCreate(website, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
    }

    private sealed record ApplicationHeader(
        Guid Id,
        string LegalName,
        string BusinessEmail,
        string PhoneNumber,
        string RegistrationNumber,
        string? TaxId,
        string? Website,
        CompanyType CompanyType,
        string OfficeAddress,
        string LocationText,
        string? EstimatedPropertyRange,
        CompanyApplicationStatus Status,
        DateTimeOffset? SubmittedAt,
        DateTimeOffset? ReviewedAt,
        string? DecisionReason,
        CompanyApplicationApprovedCompany? ApprovedCompany,
        byte[] RowVersion)
    {
        public CompanyApplicationDetails ToDetails(
            IReadOnlyList<CompanyApplicationDocument> documents,
            IReadOnlyList<CompanyApplicationStatusChange> statusHistory) => new(
                Id,
                LegalName,
                BusinessEmail,
                PhoneNumber,
                RegistrationNumber,
                TaxId,
                Website,
                CompanyType,
                OfficeAddress,
                LocationText,
                EstimatedPropertyRange,
                Status,
                SubmittedAt,
                ReviewedAt,
                DecisionReason,
                ApprovedCompany,
                RowVersion,
                documents,
                statusHistory);
    }

    private sealed record DocumentAsset(
        Guid Id,
        Guid UploadedByApplicationUserId,
        FileType FileType,
        string ContentType);

    private sealed record SubmissionDocument(
        DocumentVerificationStatus VerificationStatus,
        bool IsOwnedByApplicant,
        FileType FileType,
        string ContentType);
}
