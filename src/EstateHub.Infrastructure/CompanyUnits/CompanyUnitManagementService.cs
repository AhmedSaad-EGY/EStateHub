using System.Data.Common;
using EstateHub.Application.Common;
using EstateHub.Application.CompanyAccess;
using EstateHub.Application.CompanyUnits;
using EstateHub.Domain.Entities.Catalog;
using EstateHub.Domain.Entities.Companies;
using EstateHub.Domain.Enums;
using EstateHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EstateHub.Infrastructure.CompanyUnits;

public sealed class CompanyUnitManagementService(
    EstateHubDbContext dbContext,
    ICompanyAccessService companyAccessService,
    TimeProvider timeProvider) : ICompanyUnitManagementService
{
    public async Task<PagedResult<CompanyUnitDetails>?> GetUnitsAsync(Guid userId, CompanyUnitDirectoryQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsRead, cancellationToken);
        if (scope is null) return null;

        var units = GetDetailsQuery(scope.CompanyId);
        if (query.Search is not null) units = units.Where(x => x.UnitCode.Contains(query.Search));
        if (query.Status is not null) units = units.Where(x => x.Status == query.Status);
        if (query.ProjectId is not null) units = units.Where(x => x.ProjectId == query.ProjectId);
        if (query.UnitTypeId is not null) units = units.Where(x => x.UnitType.Id == query.UnitTypeId);
        if (query.LocationId is not null) units = units.Where(x => x.Location.Id == query.LocationId);

        var total = await units.CountAsync(cancellationToken);
        var items = await units.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<CompanyUnitDetails>(items, query.PageNumber, query.PageSize, total);
    }

    public async Task<CompanyUnitManagementDetails?> GetUnitAsync(Guid userId, Guid unitId, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsRead, cancellationToken);
        if (scope is null) return null;
        var header = await GetDetailsQuery(scope.CompanyId).SingleOrDefaultAsync(x => x.Id == unitId, cancellationToken);
        if (header is null) return null;
        var amenities = await dbContext.Set<UnitAmenity>().AsNoTracking()
            .Where(x => x.UnitId == unitId && x.Unit.ManagingCompanyId == scope.CompanyId)
            .OrderBy(x => x.Amenity.Code).ThenBy(x => x.AmenityId)
            .Select(x => new CompanyUnitManagementAmenity(x.Amenity.Id, x.Amenity.Code, x.Amenity.NameEn, x.Amenity.NameAr, x.Amenity.IconKey, x.Amenity.Scope, x.Amenity.IsActive))
            .ToListAsync(cancellationToken);
        var nearbyPlaces = await dbContext.Set<NearbyPlace>().AsNoTracking()
            .Where(x => x.UnitId == unitId && x.ProjectId == null && x.Unit!.ManagingCompanyId == scope.CompanyId)
            .OrderBy(x => x.Category).ThenBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new CompanyUnitNearbyPlace(x.Id, x.Category, x.Name, x.DistanceMeters, x.TravelMinutes, x.Latitude, x.Longitude))
            .ToListAsync(cancellationToken);
        return new CompanyUnitManagementDetails(header, amenities, nearbyPlaces);
    }

    public Task<CompanyUnitMutationResult> CreateUnitAsync(Guid userId, CompanyUnitCommand command, CancellationToken cancellationToken = default) => SaveAsync(userId, Guid.Empty, command, true, cancellationToken);
    public Task<CompanyUnitMutationResult> UpdateUnitAsync(Guid userId, Guid unitId, CompanyUnitCommand command, CancellationToken cancellationToken = default) => SaveAsync(userId, unitId, command, false, cancellationToken);

    public async Task<CompanyUnitAmenitiesResult> ReplaceAmenitiesAsync(Guid userId, Guid unitId, IReadOnlyList<Guid> amenityIds, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        if (amenityIds.Any(x => x == Guid.Empty) || amenityIds.Distinct().Count() != amenityIds.Count) return new(CompanyUnitOperationStatus.InvalidReference);
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsManage, cancellationToken);
        if (scope is null) return new(CompanyUnitOperationStatus.NotFound);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var unit = await dbContext.Set<Unit>().SingleOrDefaultAsync(x => x.Id == unitId && x.ManagingCompanyId == scope.CompanyId, cancellationToken);
            if (unit is null) return new(CompanyUnitOperationStatus.NotFound);

            var amenities = await dbContext.Set<Amenity>().AsNoTracking()
                .Where(x => amenityIds.Contains(x.Id) && x.IsActive && (x.Scope == AmenityScope.Unit || x.Scope == AmenityScope.Both))
                .OrderBy(x => x.Code).ThenBy(x => x.Id)
                .Select(x => new CompanyUnitAmenity(x.Id, x.Code, x.NameEn, x.NameAr, x.IconKey, x.Scope))
                .ToListAsync(cancellationToken);
            if (amenities.Count != amenityIds.Count) return new(CompanyUnitOperationStatus.InvalidReference);

            dbContext.Entry(unit).Property(x => x.RowVersion).OriginalValue = rowVersion;
            var current = await dbContext.Set<UnitAmenity>().Where(x => x.UnitId == unitId).ToListAsync(cancellationToken);
            dbContext.RemoveRange(current.Where(x => !amenityIds.Contains(x.AmenityId)));
            dbContext.AddRange(amenityIds.Where(id => current.All(x => x.AmenityId != id)).Select(id => new UnitAmenity { UnitId = unitId, AmenityId = id }));
            unit.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CompanyUnitOperationStatus.Succeeded, unit.RowVersion, amenities);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyUnitOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
    }

    public Task<CompanyUnitNearbyPlaceResult> CreateNearbyPlaceAsync(Guid userId, Guid unitId, CompanyUnitNearbyPlaceCommand command, byte[] rowVersion, CancellationToken cancellationToken = default) => MutateNearbyPlaceAsync(userId, unitId, Guid.Empty, command, rowVersion, true, cancellationToken);
    public Task<CompanyUnitNearbyPlaceResult> UpdateNearbyPlaceAsync(Guid userId, Guid unitId, Guid nearbyPlaceId, CompanyUnitNearbyPlaceCommand command, byte[] rowVersion, CancellationToken cancellationToken = default) => MutateNearbyPlaceAsync(userId, unitId, nearbyPlaceId, command, rowVersion, false, cancellationToken);

    public async Task<CompanyUnitOperationStatus> DeleteNearbyPlaceAsync(Guid userId, Guid unitId, Guid nearbyPlaceId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsManage, cancellationToken);
        if (scope is null) return CompanyUnitOperationStatus.NotFound;
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var unit = await dbContext.Set<Unit>().SingleOrDefaultAsync(x => x.Id == unitId && x.ManagingCompanyId == scope.CompanyId, cancellationToken);
            if (unit is null) return CompanyUnitOperationStatus.NotFound;
            var nearbyPlace = await dbContext.Set<NearbyPlace>().SingleOrDefaultAsync(x => x.Id == nearbyPlaceId && x.UnitId == unitId && x.ProjectId == null && x.Unit!.ManagingCompanyId == scope.CompanyId, cancellationToken);
            if (nearbyPlace is null) return CompanyUnitOperationStatus.NotFound;
            dbContext.Entry(unit).Property(x => x.RowVersion).OriginalValue = rowVersion;
            dbContext.Remove(nearbyPlace);
            unit.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompanyUnitOperationStatus.Succeeded;
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return CompanyUnitOperationStatus.Conflict; }
        catch (DbUpdateException) { return CompanyUnitOperationStatus.ServiceUnavailable; }
        catch (DbException) { return CompanyUnitOperationStatus.ServiceUnavailable; }
    }

    private async Task<CompanyUnitNearbyPlaceResult> MutateNearbyPlaceAsync(Guid userId, Guid unitId, Guid nearbyPlaceId, CompanyUnitNearbyPlaceCommand command, byte[] rowVersion, bool create, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsManage, cancellationToken);
        if (scope is null) return new(CompanyUnitOperationStatus.NotFound);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var unit = await dbContext.Set<Unit>().SingleOrDefaultAsync(x => x.Id == unitId && x.ManagingCompanyId == scope.CompanyId, cancellationToken);
            if (unit is null) return new(CompanyUnitOperationStatus.NotFound);

            NearbyPlace nearbyPlace;
            if (create)
            {
                nearbyPlace = new NearbyPlace { Id = Guid.NewGuid(), UnitId = unitId, ProjectId = null, Category = command.Category, Name = command.Name, DistanceMeters = command.DistanceMeters, TravelMinutes = command.TravelMinutes, Latitude = command.Latitude, Longitude = command.Longitude };
                dbContext.Add(nearbyPlace);
            }
            else
            {
                var existingPlace = await dbContext.Set<NearbyPlace>().SingleOrDefaultAsync(x => x.Id == nearbyPlaceId && x.UnitId == unitId && x.ProjectId == null && x.Unit!.ManagingCompanyId == scope.CompanyId, cancellationToken);
                if (existingPlace is null) return new(CompanyUnitOperationStatus.NotFound);
                nearbyPlace = existingPlace;
                nearbyPlace.Category = command.Category; nearbyPlace.Name = command.Name; nearbyPlace.DistanceMeters = command.DistanceMeters; nearbyPlace.TravelMinutes = command.TravelMinutes; nearbyPlace.Latitude = command.Latitude; nearbyPlace.Longitude = command.Longitude;
            }

            dbContext.Entry(unit).Property(x => x.RowVersion).OriginalValue = rowVersion;
            unit.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CompanyUnitOperationStatus.Succeeded, unit.RowVersion, new CompanyUnitNearbyPlace(nearbyPlace.Id, nearbyPlace.Category, nearbyPlace.Name, nearbyPlace.DistanceMeters, nearbyPlace.TravelMinutes, nearbyPlace.Latitude, nearbyPlace.Longitude));
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyUnitOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
    }

    public async Task<CompanyUnitStatusMutationResult> UpdateUnitStatusAsync(Guid userId, Guid unitId, UnitStatus status, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsManage, cancellationToken);
        if (scope is null) return new(CompanyUnitOperationStatus.NotFound);
        var unit = await dbContext.Set<Unit>().SingleOrDefaultAsync(x => x.Id == unitId && x.ManagingCompanyId == scope.CompanyId, cancellationToken);
        if (unit is null) return new(CompanyUnitOperationStatus.NotFound);
        if (!CanTransition(unit.Status, status)) return new(CompanyUnitOperationStatus.Conflict);

        dbContext.Entry(unit).Property(x => x.RowVersion).OriginalValue = rowVersion;
        unit.Status = status;
        unit.UpdatedAt = timeProvider.GetUtcNow();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(CompanyUnitOperationStatus.Succeeded, unit.Status, unit.UpdatedAt, unit.RowVersion);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyUnitOperationStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return new(CompanyUnitOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
    }

    private async Task<CompanyUnitMutationResult> SaveAsync(Guid userId, Guid unitId, CompanyUnitCommand command, bool create, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(userId, CompanyPermissionCodes.UnitsManage, cancellationToken);
        if (scope is null) return new(CompanyUnitOperationStatus.NotFound);
        if (!await ReferencesAreAvailableAsync(scope, command, cancellationToken)) return new(CompanyUnitOperationStatus.InvalidReference);
        if (await CodeExistsAsync(scope.CompanyId, command.ProjectId, command.UnitCode, create ? null : unitId, cancellationToken)) return new(CompanyUnitOperationStatus.Conflict);

        Unit unit;
        if (create)
        {
            var now = timeProvider.GetUtcNow();
            unit = new Unit
            {
                Id = Guid.NewGuid(), ManagingCompanyId = scope.CompanyId, ProjectId = command.ProjectId,
                LocationId = command.LocationId, UnitTypeId = command.UnitTypeId, UnitCode = command.UnitCode,
                FinishingType = command.FinishingType, Bedrooms = command.Bedrooms, Bathrooms = command.Bathrooms,
                FloorNumber = command.FloorNumber, TotalFloors = command.TotalFloors, BuiltUpArea = command.BuiltUpArea,
                LandArea = command.LandArea, FurnishedStatus = command.FurnishedStatus, Status = UnitStatus.Available,
                CreatedAt = now, UpdatedAt = now
            };
            dbContext.Add(unit);
        }
        else
        {
            var existingUnit = await dbContext.Set<Unit>().SingleOrDefaultAsync(x => x.Id == unitId && x.ManagingCompanyId == scope.CompanyId, cancellationToken);
            if (existingUnit is null) return new(CompanyUnitOperationStatus.NotFound);
            unit = existingUnit;
            dbContext.Entry(unit).Property(x => x.RowVersion).OriginalValue = command.RowVersion!;
            unit.ProjectId = command.ProjectId; unit.LocationId = command.LocationId; unit.UnitTypeId = command.UnitTypeId;
            unit.UnitCode = command.UnitCode; unit.FinishingType = command.FinishingType; unit.Bedrooms = command.Bedrooms;
            unit.Bathrooms = command.Bathrooms; unit.FloorNumber = command.FloorNumber; unit.TotalFloors = command.TotalFloors;
            unit.BuiltUpArea = command.BuiltUpArea; unit.LandArea = command.LandArea; unit.FurnishedStatus = command.FurnishedStatus;
            unit.UpdatedAt = timeProvider.GetUtcNow();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            var details = await GetDetailsQuery(scope.CompanyId).SingleAsync(x => x.Id == unit.Id, cancellationToken);
            return new(CompanyUnitOperationStatus.Succeeded, details);
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateConcurrencyException) { return new(CompanyUnitOperationStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception)) { return new(CompanyUnitOperationStatus.Conflict); }
        catch (DbUpdateException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
        catch (DbException) { return new(CompanyUnitOperationStatus.ServiceUnavailable); }
    }

    private async Task<bool> ReferencesAreAvailableAsync(CompanyUnitScope scope, CompanyUnitCommand command, CancellationToken cancellationToken)
    {
        if (!await dbContext.Set<Location>().AsNoTracking().AnyAsync(x => x.Id == command.LocationId && x.IsActive, cancellationToken)
            || !await dbContext.Set<UnitType>().AsNoTracking().AnyAsync(x => x.Id == command.UnitTypeId && x.IsActive, cancellationToken)) return false;
        if (command.ProjectId is null) return true;

        var projects = dbContext.Set<Project>().AsNoTracking();
        return scope.CompanyType == CompanyType.Developer
            ? await projects.AnyAsync(x => x.Id == command.ProjectId && x.DeveloperCompanyId == scope.CompanyId && x.ProjectStatus != ProjectStatus.Archived, cancellationToken)
            : await projects.AnyAsync(x => x.Id == command.ProjectId && x.ProjectStatus == ProjectStatus.Published && x.DeveloperCompany.CompanyType == CompanyType.Developer && x.DeveloperCompany.Status == CompanyStatus.Active && x.DeveloperCompany.VerifiedAt != null, cancellationToken);
    }

    private Task<bool> CodeExistsAsync(Guid companyId, Guid? projectId, string unitCode, Guid? exceptId, CancellationToken cancellationToken) =>
        dbContext.Set<Unit>().AsNoTracking().AnyAsync(x => x.ManagingCompanyId == companyId && x.UnitCode == unitCode && (exceptId == null || x.Id != exceptId) && (projectId == null ? x.ProjectId == null : x.ProjectId == projectId), cancellationToken);

    private IQueryable<CompanyUnitDetails> GetDetailsQuery(Guid companyId) => dbContext.Set<Unit>().AsNoTracking().Where(x => x.ManagingCompanyId == companyId).Select(x => new CompanyUnitDetails(
        x.Id, x.ProjectId, x.UnitCode, x.FinishingType, x.Bedrooms, x.Bathrooms, x.FloorNumber, x.TotalFloors, x.BuiltUpArea, x.LandArea, x.FurnishedStatus, x.Status,
        new CompanyUnitLocationSummary(x.Location.Id, x.Location.Type, x.Location.NameEn, x.Location.NameAr, x.Location.Slug),
        new CompanyUnitTypeSummary(x.UnitType.Id, x.UnitType.Code, x.UnitType.NameEn, x.UnitType.NameAr),
        x.ProjectId == null ? null : new CompanyUnitProjectSummary(x.Project!.Id, x.Project.Slug, x.Project.Name, x.Project.DeliveryStatus, x.Project.ProjectStatus),
        x.Listings.Count(), x.Listings.Count(l => l.PublicationStatus == ListingPublicationStatus.Published), x.CreatedAt, x.UpdatedAt, x.RowVersion));

    private async Task<CompanyUnitScope?> GetScopeAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var scope = await companyAccessService.GetAuthorizedScopeAsync(userId, permission, cancellationToken);
        if (scope is null) return null;
        return await dbContext.Set<Company>().AsNoTracking().Where(x => x.Id == scope.CompanyId && x.Status == CompanyStatus.Active && x.VerifiedAt != null && (x.CompanyType == CompanyType.Developer || x.CompanyType == CompanyType.BrokerAgency)).Select(x => new CompanyUnitScope(scope.CompanyId, x.CompanyType)).SingleOrDefaultAsync(cancellationToken);
    }

    private static bool CanTransition(UnitStatus current, UnitStatus next) => current != next && current switch
    {
        UnitStatus.Available => next is UnitStatus.Reserved or UnitStatus.Sold or UnitStatus.Rented or UnitStatus.Withdrawn,
        UnitStatus.Reserved => next is UnitStatus.Available or UnitStatus.Sold or UnitStatus.Rented or UnitStatus.Withdrawn,
        UnitStatus.Withdrawn => next == UnitStatus.Available,
        UnitStatus.Sold or UnitStatus.Rented => false,
        _ => false
    };

    private static bool IsUniqueViolation(DbUpdateException exception) => exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
    private sealed record CompanyUnitScope(Guid CompanyId, CompanyType CompanyType);
}
