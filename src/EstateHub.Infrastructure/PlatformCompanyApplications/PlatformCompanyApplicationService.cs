using System.Data;
using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.Notifications;
using EstateHub.Application.PlatformCompanyApplications;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Entities.Users;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Files;
using EstateHub.Infrastructure.Identity;
using EstateHub.Infrastructure.Identity.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.PlatformCompanyApplications;

internal sealed class PlatformCompanyApplicationService(
    EstateHubDbContext dbContext,
    ILocalFileStorage fileStorage,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider) : IPlatformCompanyApplicationService
{
    private const string PdfContentType = "application/pdf";

    public async Task<PlatformCompanyApplicationResult<PagedResult<PlatformCompanyApplicationSummary>>> GetApplicationsAsync(
        PlatformCompanyApplicationDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var applications = dbContext.Set<CompanyApplication>().AsNoTracking();
            if (query.Search is not null)
            {
                applications = applications.Where(application =>
                    application.LegalName.Contains(query.Search)
                    || application.RegistrationNumber.Contains(query.Search)
                    || application.BusinessEmail.Contains(query.Search)
                    || dbContext.Set<CustomerProfile>().Any(profile =>
                        profile.ApplicationUserId == application.SubmittedByApplicationUserId
                        && profile.FullName.Contains(query.Search))
                    || dbContext.Set<ApplicationUser>().Any(user =>
                        user.Id == application.SubmittedByApplicationUserId
                        && user.Email != null
                        && user.Email.Contains(query.Search)));
            }

            if (query.Status is not null)
            {
                applications = applications.Where(application => application.Status == query.Status.Value);
            }

            if (query.CompanyType is not null)
            {
                applications = applications.Where(application => application.CompanyType == query.CompanyType.Value);
            }

            if (query.SubmittedFrom is not null)
            {
                applications = applications.Where(application => application.SubmittedAt >= query.SubmittedFrom.Value);
            }

            if (query.SubmittedTo is not null)
            {
                applications = applications.Where(application => application.SubmittedAt <= query.SubmittedTo.Value);
            }

            var totalCount = await applications.CountAsync(cancellationToken);
            var items = await applications
                .OrderBy(application => application.SubmittedAt == null)
                .ThenByDescending(application => application.SubmittedAt)
                .ThenByDescending(application => application.Id)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(application => new PlatformCompanyApplicationSummary(
                    application.Id,
                    application.SubmittedByApplicationUserId,
                    dbContext.Set<CustomerProfile>()
                        .Where(profile => profile.ApplicationUserId == application.SubmittedByApplicationUserId)
                        .Select(profile => profile.FullName)
                        .SingleOrDefault(),
                    dbContext.Set<ApplicationUser>()
                        .Where(user => user.Id == application.SubmittedByApplicationUserId)
                        .Select(user => user.Email)
                        .SingleOrDefault(),
                    application.LegalName,
                    application.RegistrationNumber,
                    application.BusinessEmail,
                    application.CompanyType,
                    application.Status,
                    application.SubmittedAt,
                    application.ReviewedAt,
                    application.ReviewedByApplicationUserId,
                    application.DecisionReason,
                    application.Documents.Count,
                    application.Documents.Count(document => document.VerificationStatus == DocumentVerificationStatus.Pending),
                    application.Documents.Count(document => document.VerificationStatus == DocumentVerificationStatus.Approved),
                    application.Documents.Count(document => document.VerificationStatus == DocumentVerificationStatus.Rejected),
                    application.ApprovedCompanyId,
                    application.RowVersion))
                .ToListAsync(cancellationToken);

            return new(
                PlatformCompanyApplicationOperationStatus.Succeeded,
                new PagedResult<PlatformCompanyApplicationSummary>(
                    items,
                    query.PageNumber,
                    query.PageSize,
                    totalCount));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<PlatformCompanyApplicationResult<PlatformCompanyApplicationDetails>> GetApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var header = await dbContext.Set<CompanyApplication>()
                .AsNoTracking()
                .Where(application => application.Id == applicationId)
                .Select(application => new ApplicationHeader(
                    application.Id,
                    application.SubmittedByApplicationUserId,
                    dbContext.Set<CustomerProfile>()
                        .Where(profile => profile.ApplicationUserId == application.SubmittedByApplicationUserId)
                        .Select(profile => profile.FullName)
                        .SingleOrDefault(),
                    dbContext.Set<ApplicationUser>()
                        .Where(user => user.Id == application.SubmittedByApplicationUserId)
                        .Select(user => user.Email)
                        .SingleOrDefault(),
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
                    application.ReviewedByApplicationUserId,
                    application.DecisionReason,
                    application.ApprovedCompany == null
                        ? null
                        : new PlatformApprovedCompanySummary(
                            application.ApprovedCompany.Id,
                            application.ApprovedCompany.Slug,
                            application.ApprovedCompany.DisplayName),
                    application.RowVersion))
                .SingleOrDefaultAsync(cancellationToken);
            if (header is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.NotFound);
            }

            var documents = await DocumentsQuery(applicationId).ToListAsync(cancellationToken);
            var history = await dbContext.Set<ApplicationStatusHistory>()
                .AsNoTracking()
                .Where(item => item.CompanyApplicationId == applicationId)
                .OrderBy(item => item.ChangedAt)
                .ThenBy(item => item.Id)
                .Select(item => new PlatformCompanyApplicationStatusChange(
                    item.Id,
                    item.FromStatus,
                    item.ToStatus,
                    item.ChangedByApplicationUserId,
                    item.ChangedAt,
                    item.Reason))
                .ToListAsync(cancellationToken);

            return new(
                PlatformCompanyApplicationOperationStatus.Succeeded,
                header.ToDetails(documents, history));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<PlatformCompanyApplicationResult<PlatformCompanyApplicationContent>> GetDocumentContentAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = await dbContext.Set<CompanyDocument>()
                .AsNoTracking()
                .Where(document =>
                    document.Id == documentId
                    && document.CompanyApplicationId == applicationId)
                .Select(document => new StoredDocument(
                    document.FileAssetId,
                    document.FileAsset.StorageKey,
                    document.FileAsset.FileType,
                    document.FileAsset.OriginalFileName,
                    document.FileAsset.ContentType,
                    document.FileAsset.SizeBytes))
                .SingleOrDefaultAsync(cancellationToken);
            if (stored is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.NotFound);
            }

            if (stored.FileType != FileType.Document || stored.ContentType != PdfContentType)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var stream = await fileStorage.OpenReadAsync(
                stored.StorageKey,
                stored.FileType,
                stored.ContentType,
                stored.SizeBytes,
                cancellationToken);
            if (stream is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var downloadName = FileAssetService.TryNormalizeFileName(
                stored.OriginalFileName,
                out var originalFileName)
                ? originalFileName
                : $"file-{stored.FileAssetId:D}.pdf";
            return new(
                PlatformCompanyApplicationOperationStatus.Succeeded,
                new PlatformCompanyApplicationContent(
                    stream,
                    stored.ContentType,
                    downloadName));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public Task<PlatformCompanyApplicationOperationStatus> StartReviewAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            actingPlatformAdminId,
            applicationId,
            rowVersion,
            CompanyApplicationStatus.Submitted,
            CompanyApplicationStatus.UnderReview,
            null,
            cancellationToken);

    public async Task<PlatformCompanyApplicationResult<PlatformDocumentDecisionResult>> VerifyDocumentAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        Guid documentId,
        PlatformDocumentDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);
            if (application is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.NotFound);
            }

            if (application.Status != CompanyApplicationStatus.UnderReview)
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var document = await dbContext.Set<CompanyDocument>()
                .SingleOrDefaultAsync(candidate =>
                    candidate.Id == documentId
                    && candidate.CompanyApplicationId == applicationId,
                    cancellationToken);
            if (document is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.NotFound);
            }

            var asset = await dbContext.Set<FileAsset>()
                .AsNoTracking()
                .Where(file => file.Id == document.FileAssetId)
                .Select(file => new DocumentFileMetadata(
                    file.OriginalFileName,
                    file.ContentType,
                    file.SizeBytes,
                    file.FileType))
                .SingleOrDefaultAsync(cancellationToken);
            if (asset is null
                || asset.FileType != FileType.Document
                || asset.ContentType != PdfContentType)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            if (document.VerificationStatus == command.VerificationStatus
                && string.Equals(document.RejectionReason, command.RejectionReason, StringComparison.Ordinal))
            {
                if (document.ReviewedAt is null || document.ReviewedByApplicationUserId is null)
                {
                    return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
                }

                return new(
                    PlatformCompanyApplicationOperationStatus.Succeeded,
                    new PlatformDocumentDecisionResult(
                        application.RowVersion,
                        MapDocument(document, asset)));
            }

            var utcNow = timeProvider.GetUtcNow();
            dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = command.ApplicationRowVersion;
            dbContext.Entry(application).Property(candidate => candidate.Status).IsModified = true;
            document.VerificationStatus = command.VerificationStatus;
            document.ReviewedAt = utcNow;
            document.ReviewedByApplicationUserId = actingPlatformAdminId;
            document.RejectionReason = command.RejectionReason;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new(
                PlatformCompanyApplicationOperationStatus.Succeeded,
                new PlatformDocumentDecisionResult(
                    application.RowVersion,
                    MapDocument(document, asset)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(PlatformCompanyApplicationOperationStatus.Conflict);
        }
        catch
        {
            return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    public async Task<PlatformCompanyApplicationOperationStatus> RequestChangesAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);
            if (application is null)
            {
                return PlatformCompanyApplicationOperationStatus.NotFound;
            }

            var hasRejectedDocument = await dbContext.Set<CompanyDocument>()
                .AsNoTracking()
                .AnyAsync(document =>
                    document.CompanyApplicationId == applicationId
                    && document.VerificationStatus == DocumentVerificationStatus.Rejected,
                    cancellationToken);
            if (application.Status == CompanyApplicationStatus.NeedsChanges)
            {
                var hasConsistentHistory = await dbContext.Set<ApplicationStatusHistory>()
                    .AsNoTracking()
                    .AnyAsync(history =>
                        history.CompanyApplicationId == applicationId
                        && history.FromStatus == CompanyApplicationStatus.UnderReview
                        && history.ToStatus == CompanyApplicationStatus.NeedsChanges
                        && history.Reason == application.DecisionReason,
                        cancellationToken);
                return hasRejectedDocument
                       && application.ReviewedAt is not null
                       && application.ReviewedByApplicationUserId is not null
                       && !string.IsNullOrWhiteSpace(application.DecisionReason)
                       && hasConsistentHistory
                    ? PlatformCompanyApplicationOperationStatus.Succeeded
                    : PlatformCompanyApplicationOperationStatus.ServiceUnavailable;
            }

            if (application.Status != CompanyApplicationStatus.UnderReview || !hasRejectedDocument)
            {
                return PlatformCompanyApplicationOperationStatus.Conflict;
            }

            return await CompleteDecisionTransitionAsync(
                transaction,
                application,
                actingPlatformAdminId,
                rowVersion,
                CompanyApplicationStatus.NeedsChanges,
                reason,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return PlatformCompanyApplicationOperationStatus.Conflict;
        }
        catch
        {
            return PlatformCompanyApplicationOperationStatus.ServiceUnavailable;
        }
    }

    public async Task<PlatformCompanyApplicationOperationStatus> RejectAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);
            if (application is null)
            {
                return PlatformCompanyApplicationOperationStatus.NotFound;
            }

            if (application.Status == CompanyApplicationStatus.Rejected)
            {
                var hasConsistentHistory = await dbContext.Set<ApplicationStatusHistory>()
                    .AsNoTracking()
                    .AnyAsync(history =>
                        history.CompanyApplicationId == applicationId
                        && history.FromStatus == CompanyApplicationStatus.UnderReview
                        && history.ToStatus == CompanyApplicationStatus.Rejected
                        && history.Reason == application.DecisionReason,
                        cancellationToken);
                return application.ApprovedCompanyId is null
                       && application.ReviewedAt is not null
                       && application.ReviewedByApplicationUserId is not null
                       && !string.IsNullOrWhiteSpace(application.DecisionReason)
                       && hasConsistentHistory
                    ? PlatformCompanyApplicationOperationStatus.Succeeded
                    : PlatformCompanyApplicationOperationStatus.ServiceUnavailable;
            }

            if (application.Status != CompanyApplicationStatus.UnderReview)
            {
                return PlatformCompanyApplicationOperationStatus.Conflict;
            }

            return await CompleteDecisionTransitionAsync(
                transaction,
                application,
                actingPlatformAdminId,
                rowVersion,
                CompanyApplicationStatus.Rejected,
                reason,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return PlatformCompanyApplicationOperationStatus.Conflict;
        }
        catch
        {
            return PlatformCompanyApplicationOperationStatus.ServiceUnavailable;
        }
    }

    public async Task<PlatformCompanyApplicationResult<PlatformApprovedCompanyResult>> ApproveAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        PlatformCompanyApprovalCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var applicantId = await dbContext.Set<CompanyApplication>()
                .AsNoTracking()
                .Where(application => application.Id == applicationId)
                .Select(application => (Guid?)application.SubmittedByApplicationUserId)
                .SingleOrDefaultAsync(cancellationToken);
            if (applicantId is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.NotFound);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var lockedUsers = await dbContext.Set<ApplicationUser>()
                .Where(user => user.Id == applicantId.Value)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(user => user.UpdatedAt, user => user.UpdatedAt),
                    cancellationToken);
            if (lockedUsers != 1)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);
            if (application is null || application.SubmittedByApplicationUserId != applicantId.Value)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            if (application.Status == CompanyApplicationStatus.Approved)
            {
                var approved = await GetApprovedStateAsync(application, cancellationToken);
                return approved is null
                    ? new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable)
                    : new(PlatformCompanyApplicationOperationStatus.Succeeded, approved);
            }

            if (application.Status != CompanyApplicationStatus.UnderReview)
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            if (application.ApprovedCompanyId is not null)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var userState = await dbContext.Set<ApplicationUser>()
                .AsNoTracking()
                .Where(user => user.Id == applicantId.Value)
                .Select(user => new ApplicantUserState(user.AccountStatus, user.EmailConfirmed))
                .SingleOrDefaultAsync(cancellationToken);
            if (userState is null)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            if (userState.AccountStatus != ApplicationUserAccountStatus.Active || !userState.EmailConfirmed)
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var applicantName = await dbContext.Set<CustomerProfile>()
                .AsNoTracking()
                .Where(profile => profile.ApplicationUserId == applicantId.Value)
                .Select(profile => profile.FullName)
                .SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(applicantName) || applicantName.Length > 200)
            {
                return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
            }

            var documents = await dbContext.Set<CompanyDocument>()
                .AsNoTracking()
                .Where(document => document.CompanyApplicationId == applicationId)
                .Select(document => new ApprovalDocumentState(
                    document.VerificationStatus,
                    document.FileAsset.FileType,
                    document.FileAsset.ContentType))
                .ToListAsync(cancellationToken);
            if (documents.Count == 0
                || documents.Any(document =>
                    document.VerificationStatus != DocumentVerificationStatus.Approved
                    || document.FileType != FileType.Document
                    || document.ContentType != PdfContentType))
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var hasCurrentMembership = await dbContext.Set<CompanyEmployee>()
                .AsNoTracking()
                .AnyAsync(employee =>
                    employee.ApplicationUserId == applicantId.Value
                    && employee.EndedAt == null,
                    cancellationToken);
            if (hasCurrentMembership)
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var locationExists = await dbContext.Set<Location>()
                .AsNoTracking()
                .AnyAsync(location => location.Id == command.LocationId && location.IsActive, cancellationToken);
            var currencyExists = await dbContext.Set<Currency>()
                .AsNoTracking()
                .AnyAsync(currency => currency.Code == command.BaseCurrencyCode && currency.IsActive, cancellationToken);
            if (!locationExists || !currencyExists)
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var companyConflict = await dbContext.Set<Company>()
                .AsNoTracking()
                .AnyAsync(company =>
                    company.Slug == command.Slug
                    || company.RegistrationNumber == application.RegistrationNumber
                    || (application.TaxId != null && company.TaxId == application.TaxId),
                    cancellationToken);
            var applicationConflict = await dbContext.Set<CompanyApplication>()
                .AsNoTracking()
                .AnyAsync(other =>
                    other.Id != application.Id
                    && (other.Status == CompanyApplicationStatus.Draft
                        || other.Status == CompanyApplicationStatus.Submitted
                        || other.Status == CompanyApplicationStatus.UnderReview
                        || other.Status == CompanyApplicationStatus.NeedsChanges)
                    && (other.RegistrationNumber == application.RegistrationNumber
                        || (application.TaxId != null && other.TaxId == application.TaxId)),
                    cancellationToken);
            if (companyConflict || applicationConflict || !HasUsableCompanyFields(application))
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var addressId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var addressConflict = await dbContext.Set<Address>()
                .AsNoTracking()
                .AnyAsync(address => address.Id == addressId, cancellationToken)
                || await dbContext.Set<Company>()
                    .AsNoTracking()
                    .AnyAsync(company => company.AddressId == addressId, cancellationToken);
            if (addressConflict)
            {
                return new(PlatformCompanyApplicationOperationStatus.Conflict);
            }

            var utcNow = timeProvider.GetUtcNow();
            dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion;
            dbContext.Add(new Address
            {
                Id = addressId,
                LocationId = command.LocationId,
                AddressLine1 = command.AddressLine1,
                AddressLine2 = command.AddressLine2,
                PostalCode = command.PostalCode,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            });
            dbContext.Add(new Company
            {
                Id = companyId,
                Slug = command.Slug,
                LegalName = application.LegalName,
                DisplayName = command.DisplayName,
                RegistrationNumber = application.RegistrationNumber,
                TaxId = application.TaxId,
                CompanyType = application.CompanyType,
                BusinessEmail = application.BusinessEmail,
                SupportPhone = application.PhoneNumber,
                Website = application.Website,
                AddressId = addressId,
                LogoFileAssetId = null,
                CoverFileAssetId = null,
                Status = CompanyStatus.Active,
                VerifiedAt = utcNow,
                TimeZoneId = command.TimeZoneId,
                BaseCurrencyCode = command.BaseCurrencyCode,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
            dbContext.Add(new CompanyEmployee
            {
                Id = employeeId,
                ApplicationUserId = applicantId.Value,
                CompanyId = companyId,
                FullName = applicantName.Trim(),
                JobTitle = null,
                IsPrimaryContact = true,
                Status = CompanyEmployeeStatus.Active,
                JoinedAt = utcNow,
                EndedAt = null
            });
            application.Status = CompanyApplicationStatus.Approved;
            application.ApprovedCompanyId = companyId;
            application.ReviewedAt = utcNow;
            application.ReviewedByApplicationUserId = actingPlatformAdminId;
            application.DecisionReason = null;
            dbContext.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                CompanyApplicationId = application.Id,
                FromStatus = CompanyApplicationStatus.UnderReview,
                ToStatus = CompanyApplicationStatus.Approved,
                ChangedByApplicationUserId = actingPlatformAdminId,
                ChangedAt = utcNow,
                Reason = null
            });
            AddCompanyApplicationStatusNotification(
                applicantId.Value,
                application.Id,
                CompanyApplicationStatus.Approved);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(
                PlatformCompanyApplicationOperationStatus.Succeeded,
                new PlatformApprovedCompanyResult(
                    companyId,
                    command.Slug,
                    command.DisplayName,
                    employeeId,
                    applicantName.Trim(),
                    true));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(PlatformCompanyApplicationOperationStatus.Conflict);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return new(PlatformCompanyApplicationOperationStatus.Conflict);
        }
        catch
        {
            return new(PlatformCompanyApplicationOperationStatus.ServiceUnavailable);
        }
    }

    private async Task<PlatformCompanyApplicationOperationStatus> TransitionAsync(
        Guid actingPlatformAdminId,
        Guid applicationId,
        byte[] rowVersion,
        CompanyApplicationStatus fromStatus,
        CompanyApplicationStatus toStatus,
        string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var application = await dbContext.Set<CompanyApplication>()
                .SingleOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);
            if (application is null)
            {
                return PlatformCompanyApplicationOperationStatus.NotFound;
            }

            if (application.Status == toStatus)
            {
                return application.ReviewedAt is not null
                       && application.ReviewedByApplicationUserId is not null
                       && application.DecisionReason == reason
                    ? PlatformCompanyApplicationOperationStatus.Succeeded
                    : PlatformCompanyApplicationOperationStatus.ServiceUnavailable;
            }

            if (application.Status != fromStatus)
            {
                return PlatformCompanyApplicationOperationStatus.Conflict;
            }

            var utcNow = timeProvider.GetUtcNow();
            dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
            application.Status = toStatus;
            application.ReviewedAt = utcNow;
            application.ReviewedByApplicationUserId = actingPlatformAdminId;
            application.DecisionReason = reason;
            dbContext.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                CompanyApplicationId = application.Id,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedByApplicationUserId = actingPlatformAdminId,
                ChangedAt = utcNow,
                Reason = reason
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return PlatformCompanyApplicationOperationStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return PlatformCompanyApplicationOperationStatus.Conflict;
        }
        catch
        {
            return PlatformCompanyApplicationOperationStatus.ServiceUnavailable;
        }
    }

    private async Task<PlatformCompanyApplicationOperationStatus> CompleteDecisionTransitionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CompanyApplication application,
        Guid actingPlatformAdminId,
        byte[] rowVersion,
        CompanyApplicationStatus toStatus,
        string reason,
        CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        dbContext.Entry(application).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
        application.Status = toStatus;
        application.ReviewedAt = utcNow;
        application.ReviewedByApplicationUserId = actingPlatformAdminId;
        application.DecisionReason = reason;
        dbContext.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            CompanyApplicationId = application.Id,
            FromStatus = CompanyApplicationStatus.UnderReview,
            ToStatus = toStatus,
            ChangedByApplicationUserId = actingPlatformAdminId,
            ChangedAt = utcNow,
            Reason = reason
        });
        AddCompanyApplicationStatusNotification(
            application.SubmittedByApplicationUserId,
            application.Id,
            toStatus);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PlatformCompanyApplicationOperationStatus.Succeeded;
    }

    private void AddCompanyApplicationStatusNotification(
        Guid recipientApplicationUserId,
        Guid applicationId,
        CompanyApplicationStatus status)
    {
        var (title, body) = status switch
        {
            CompanyApplicationStatus.NeedsChanges =>
                ("Company application needs changes", "Your company application requires changes before it can proceed."),
            CompanyApplicationStatus.Rejected =>
                ("Company application rejected", "Your company application was rejected."),
            CompanyApplicationStatus.Approved =>
                ("Company application approved", "Your company application was approved."),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported notification status.")
        };

        notificationWriter.Add(new NotificationWriteCommand(
            recipientApplicationUserId,
            title,
            body,
            new CompanyApplicationStatusChangedNotificationPayload(
                applicationId,
                status)));
    }

    private async Task<PlatformApprovedCompanyResult?> GetApprovedStateAsync(
        CompanyApplication application,
        CancellationToken cancellationToken)
    {
        if (application.ApprovedCompanyId is null
            || application.ReviewedAt is null
            || application.ReviewedByApplicationUserId is null
            || application.DecisionReason is not null)
        {
            return null;
        }

        var company = await dbContext.Set<Company>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == application.ApprovedCompanyId.Value
                && candidate.Status == CompanyStatus.Active
                && candidate.VerifiedAt != null
                && candidate.LegalName == application.LegalName
                && candidate.RegistrationNumber == application.RegistrationNumber
                && candidate.TaxId == application.TaxId
                && candidate.CompanyType == application.CompanyType
                && candidate.BusinessEmail == application.BusinessEmail
                && candidate.Address != null)
            .Select(candidate => new ApprovedCompanyState(
                candidate.Id,
                candidate.Slug,
                candidate.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            return null;
        }

        var employee = await dbContext.Set<CompanyEmployee>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.CompanyId == company.Id
                && candidate.ApplicationUserId == application.SubmittedByApplicationUserId
                && candidate.IsPrimaryContact
                && candidate.Status == CompanyEmployeeStatus.Active
                && candidate.EndedAt == null)
            .Select(candidate => new ApprovedEmployeeState(candidate.Id, candidate.FullName))
            .SingleOrDefaultAsync(cancellationToken);
        return employee is null
            ? null
            : new PlatformApprovedCompanyResult(
                company.Id,
                company.Slug,
                company.DisplayName,
                employee.Id,
                employee.FullName,
                true);
    }

    private IQueryable<PlatformCompanyApplicationDocument> DocumentsQuery(Guid applicationId) =>
        dbContext.Set<CompanyDocument>()
            .AsNoTracking()
            .Where(document => document.CompanyApplicationId == applicationId)
            .OrderBy(document => document.DocumentType)
            .ThenBy(document => document.Id)
            .Select(document => new PlatformCompanyApplicationDocument(
                document.Id,
                document.DocumentType,
                document.FileAssetId,
                document.FileAsset.OriginalFileName,
                document.FileAsset.ContentType,
                document.FileAsset.SizeBytes,
                document.VerificationStatus,
                document.ReviewedAt,
                document.ReviewedByApplicationUserId,
                document.RejectionReason));

    private static PlatformCompanyApplicationDocument MapDocument(
        CompanyDocument document,
        DocumentFileMetadata asset) => new(
            document.Id,
            document.DocumentType,
            document.FileAssetId,
            asset.OriginalFileName,
            asset.ContentType,
            asset.SizeBytes,
            document.VerificationStatus,
            document.ReviewedAt,
            document.ReviewedByApplicationUserId,
            document.RejectionReason);

    private static bool HasUsableCompanyFields(CompanyApplication application) =>
        IsRequired(application.LegalName, 200)
        && IsRequired(application.BusinessEmail, 256)
        && IsRequired(application.PhoneNumber, 32)
        && IsRequired(application.RegistrationNumber, 100)
        && IsOptional(application.TaxId, 100)
        && IsOptional(application.Website, 2048)
        && application.CompanyType is CompanyType.Developer or CompanyType.BrokerAgency;

    private static bool IsRequired(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;

    private static bool IsOptional(string? value, int maximumLength) =>
        value is null || (!string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private sealed record ApplicationHeader(
        Guid Id,
        Guid ApplicantApplicationUserId,
        string? ApplicantName,
        string? ApplicantEmail,
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
        Guid? ReviewedByApplicationUserId,
        string? DecisionReason,
        PlatformApprovedCompanySummary? ApprovedCompany,
        byte[] RowVersion)
    {
        public PlatformCompanyApplicationDetails ToDetails(
            IReadOnlyList<PlatformCompanyApplicationDocument> documents,
            IReadOnlyList<PlatformCompanyApplicationStatusChange> history) => new(
                Id,
                ApplicantApplicationUserId,
                ApplicantName,
                ApplicantEmail,
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
                ReviewedByApplicationUserId,
                DecisionReason,
                ApprovedCompany,
                RowVersion,
                documents,
                history);
    }

    private sealed record StoredDocument(
        Guid FileAssetId,
        string StorageKey,
        FileType FileType,
        string? OriginalFileName,
        string ContentType,
        long SizeBytes);

    private sealed record DocumentFileMetadata(
        string? OriginalFileName,
        string ContentType,
        long SizeBytes,
        FileType FileType);

    private sealed record ApplicantUserState(
        ApplicationUserAccountStatus AccountStatus,
        bool EmailConfirmed);

    private sealed record ApprovalDocumentState(
        DocumentVerificationStatus VerificationStatus,
        FileType FileType,
        string ContentType);

    private sealed record ApprovedCompanyState(Guid Id, string Slug, string DisplayName);
    private sealed record ApprovedEmployeeState(Guid Id, string FullName);
}
