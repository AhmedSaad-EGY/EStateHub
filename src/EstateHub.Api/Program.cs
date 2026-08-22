using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.Authorization.Platform;
using EstateHub.Api.RateLimiting;
using EstateHub.Infrastructure;
using EstateHub.Infrastructure.Accounts;
using EstateHub.Infrastructure.Authentication;
using EstateHub.Infrastructure.Billing;
using EstateHub.Infrastructure.CatalogLookups;
using EstateHub.Infrastructure.Companies;
using EstateHub.Infrastructure.CompanyAccess;
using EstateHub.Infrastructure.CompanyApplications;
using EstateHub.Infrastructure.CompanyListings;
using EstateHub.Infrastructure.CompanyLeads;
using EstateHub.Infrastructure.CompanyManagement;
using EstateHub.Infrastructure.CompanyProjects;
using EstateHub.Infrastructure.CompanyRoles;
using EstateHub.Infrastructure.CompanyUnits;
using EstateHub.Infrastructure.CompanyViewingBookings;
using EstateHub.Infrastructure.CompanyViewingSlots;
using EstateHub.Infrastructure.Customers;
using EstateHub.Infrastructure.Email;
using EstateHub.Infrastructure.Files;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Notifications;
using EstateHub.Infrastructure.PlatformAccess;
using EstateHub.Infrastructure.PlatformCompanyApplications;
using EstateHub.Infrastructure.PlatformReviews;
using EstateHub.Infrastructure.Projects;
using EstateHub.Infrastructure.Promotions;
using EstateHub.Infrastructure.Reviews;
using EstateHub.Infrastructure.Subscriptions;
using EstateHub.Infrastructure.ViewingBookings;

var builder = WebApplication.CreateBuilder(args);
const string EstateHubFrontendCorsPolicy = "EstateHubFrontend";

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
});

var allowedCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray()
    ?? [];

if (allowedCorsOrigins.Length == 0
    || allowedCorsOrigins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        || !string.Equals(
            uri.GetLeftPart(UriPartial.Authority),
            origin,
            StringComparison.Ordinal)
        || origin.EndsWith("/", StringComparison.Ordinal)))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must contain one or more absolute origins without trailing slashes.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(EstateHubFrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Configure 'ConnectionStrings:DefaultConnection' before starting EstateHub.Api.");
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddInfrastructureAuthentication(builder.Configuration);
builder.Services.AddInfrastructureEmail(builder.Configuration);
builder.Services.AddInfrastructureFileAssets(builder.Configuration, builder.Environment.ContentRootPath, builder.Environment.WebRootPath);
builder.Services.AddInfrastructureAccounts(builder.Configuration);
builder.Services.AddInfrastructureBilling();
builder.Services.AddInfrastructureCatalogLookups();
builder.Services.AddInfrastructureCompanies();
builder.Services.AddInfrastructureCompanyAccess();
builder.Services.AddInfrastructureCompanyApplications();
builder.Services.AddInfrastructureCompanyListingManagement();
builder.Services.AddInfrastructureCompanyLeadManagement();
builder.Services.AddInfrastructureCompanyManagement();
builder.Services.AddInfrastructureCompanyProjectManagement();
builder.Services.AddInfrastructureCompanyRoles();
builder.Services.AddInfrastructureCompanyUnitManagement();
builder.Services.AddInfrastructureCompanyViewingBookingManagement();
builder.Services.AddInfrastructureCompanyViewingSlotManagement();
builder.Services.AddInfrastructureCustomers();
builder.Services.AddInfrastructureListings();
builder.Services.AddInfrastructureNotifications();
builder.Services.AddInfrastructurePlatformAccess();
builder.Services.AddInfrastructurePlatformCompanyApplications();
builder.Services.AddInfrastructurePlatformReviewModeration();
builder.Services.AddInfrastructureProjects();
builder.Services.AddInfrastructurePromotions();
builder.Services.AddInfrastructureReviews();
builder.Services.AddInfrastructureSubscriptions();
builder.Services.AddInfrastructureViewingBookings();
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddPlatformAdminAuthorization();
builder.Services.AddCompanyPermissionAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(EstateHubFrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
