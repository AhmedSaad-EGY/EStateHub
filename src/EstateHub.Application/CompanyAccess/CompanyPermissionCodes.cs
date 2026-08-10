namespace EstateHub.Application.CompanyAccess;

public static class CompanyPermissionCodes
{
    public const string CompanyProfileRead = "company.profile.read";
    public const string CompanyProfileManage = "company.profile.manage";
    public const string EmployeesRead = "company.employees.read";
    public const string EmployeesManage = "company.employees.manage";
    public const string RolesRead = "company.roles.read";
    public const string RolesManage = "company.roles.manage";
    public const string ProjectsRead = "company.projects.read";
    public const string ProjectsManage = "company.projects.manage";
    public const string UnitsRead = "company.units.read";
    public const string UnitsManage = "company.units.manage";
    public const string ListingsRead = "company.listings.read";
    public const string ListingsManage = "company.listings.manage";
    public const string BookingsRead = "company.bookings.read";
    public const string BookingsManage = "company.bookings.manage";
    public const string LeadsRead = "company.leads.read";
    public const string LeadsManage = "company.leads.manage";
    public const string BillingRead = "company.billing.read";
    public const string BillingManage = "company.billing.manage";

    private static readonly HashSet<string> DefinedCodes = new(
        StringComparer.Ordinal)
    {
        CompanyProfileRead,
        CompanyProfileManage,
        EmployeesRead,
        EmployeesManage,
        RolesRead,
        RolesManage,
        ProjectsRead,
        ProjectsManage,
        UnitsRead,
        UnitsManage,
        ListingsRead,
        ListingsManage,
        BookingsRead,
        BookingsManage,
        LeadsRead,
        LeadsManage,
        BillingRead,
        BillingManage
    };

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        CompanyProfileRead,
        CompanyProfileManage,
        EmployeesRead,
        EmployeesManage,
        RolesRead,
        RolesManage,
        ProjectsRead,
        ProjectsManage,
        UnitsRead,
        UnitsManage,
        ListingsRead,
        ListingsManage,
        BookingsRead,
        BookingsManage,
        LeadsRead,
        LeadsManage,
        BillingRead,
        BillingManage
    ]);

    public static bool IsDefined(string? permissionCode)
    {
        return !string.IsNullOrWhiteSpace(permissionCode)
            && DefinedCodes.Contains(permissionCode);
    }
}
