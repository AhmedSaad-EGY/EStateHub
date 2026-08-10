using EstateHub.Application.CompanyAccess;
using EstateHub.Domain.Entities.Companies;

namespace EstateHub.Infrastructure.Persistence.Seed;

internal static class CompanyPermissionCatalogSeed
{
    internal static readonly PermissionGroup[] PermissionGroups =
    [
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000001"),
            Name = "Company Profile",
            Description = "Company profile permissions.",
            SortOrder = 10
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000002"),
            Name = "Employees",
            Description = "Employee membership permissions.",
            SortOrder = 20
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000003"),
            Name = "Roles",
            Description = "Company role permissions.",
            SortOrder = 30
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000004"),
            Name = "Projects",
            Description = "Project management permissions.",
            SortOrder = 40
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000005"),
            Name = "Units",
            Description = "Unit management permissions.",
            SortOrder = 50
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000006"),
            Name = "Listings",
            Description = "Listing management permissions.",
            SortOrder = 60
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000007"),
            Name = "Bookings",
            Description = "Booking management permissions.",
            SortOrder = 70
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000008"),
            Name = "Leads",
            Description = "Lead management permissions.",
            SortOrder = 80
        },
        new PermissionGroup
        {
            Id = new Guid("10000000-0000-4000-8000-000000000009"),
            Name = "Billing",
            Description = "Billing management permissions.",
            SortOrder = 90
        }
    ];

    internal static readonly Permission[] Permissions =
    [
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000001"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000001"),
            Code = CompanyPermissionCodes.CompanyProfileRead,
            Name = "View company profile",
            Description = "View the company profile.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000002"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000001"),
            Code = CompanyPermissionCodes.CompanyProfileManage,
            Name = "Manage company profile",
            Description = "Manage the company profile.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000003"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000002"),
            Code = CompanyPermissionCodes.EmployeesRead,
            Name = "View employees",
            Description = "View company employees.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000004"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000002"),
            Code = CompanyPermissionCodes.EmployeesManage,
            Name = "Manage employees",
            Description = "Manage company employees.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000005"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000003"),
            Code = CompanyPermissionCodes.RolesRead,
            Name = "View roles",
            Description = "View company roles.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000006"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000003"),
            Code = CompanyPermissionCodes.RolesManage,
            Name = "Manage roles",
            Description = "Manage company roles.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000007"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000004"),
            Code = CompanyPermissionCodes.ProjectsRead,
            Name = "View projects",
            Description = "View company projects.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000008"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000004"),
            Code = CompanyPermissionCodes.ProjectsManage,
            Name = "Manage projects",
            Description = "Manage company projects.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000009"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000005"),
            Code = CompanyPermissionCodes.UnitsRead,
            Name = "View units",
            Description = "View company units.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000010"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000005"),
            Code = CompanyPermissionCodes.UnitsManage,
            Name = "Manage units",
            Description = "Manage company units.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000011"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000006"),
            Code = CompanyPermissionCodes.ListingsRead,
            Name = "View listings",
            Description = "View company listings.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000012"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000006"),
            Code = CompanyPermissionCodes.ListingsManage,
            Name = "Manage listings",
            Description = "Manage company listings.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000013"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000007"),
            Code = CompanyPermissionCodes.BookingsRead,
            Name = "View bookings",
            Description = "View company bookings.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000014"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000007"),
            Code = CompanyPermissionCodes.BookingsManage,
            Name = "Manage bookings",
            Description = "Manage company bookings.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000015"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000008"),
            Code = CompanyPermissionCodes.LeadsRead,
            Name = "View leads",
            Description = "View company leads.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000016"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000008"),
            Code = CompanyPermissionCodes.LeadsManage,
            Name = "Manage leads",
            Description = "Manage company leads.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000017"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000009"),
            Code = CompanyPermissionCodes.BillingRead,
            Name = "View billing",
            Description = "View company billing.",
            IsActive = true
        },
        new Permission
        {
            Id = new Guid("20000000-0000-4000-8000-000000000018"),
            PermissionGroupId = new Guid("10000000-0000-4000-8000-000000000009"),
            Code = CompanyPermissionCodes.BillingManage,
            Name = "Manage billing",
            Description = "Manage company billing.",
            IsActive = true
        }
    ];
}
