using EstateHub.Api.Authorization.CompanyPermissions;
using EstateHub.Api.RateLimiting;
using EstateHub.Infrastructure;
using EstateHub.Infrastructure.Accounts;
using EstateHub.Infrastructure.Authentication;
using EstateHub.Infrastructure.CatalogLookups;
using EstateHub.Infrastructure.Companies;
using EstateHub.Infrastructure.CompanyAccess;
using EstateHub.Infrastructure.CompanyManagement;
using EstateHub.Infrastructure.CompanyProjects;
using EstateHub.Infrastructure.CompanyRoles;
using EstateHub.Infrastructure.Customers;
using EstateHub.Infrastructure.Email;
using EstateHub.Infrastructure.Listings;
using EstateHub.Infrastructure.Projects;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Configure 'ConnectionStrings:DefaultConnection' before starting EstateHub.Api.");
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddInfrastructureAuthentication(builder.Configuration);
builder.Services.AddInfrastructureEmail(builder.Configuration);
builder.Services.AddInfrastructureAccounts(builder.Configuration);
builder.Services.AddInfrastructureCatalogLookups();
builder.Services.AddInfrastructureCompanies();
builder.Services.AddInfrastructureCompanyAccess();
builder.Services.AddInfrastructureCompanyManagement();
builder.Services.AddInfrastructureCompanyProjectManagement();
builder.Services.AddInfrastructureCompanyRoles();
builder.Services.AddInfrastructureCustomers();
builder.Services.AddInfrastructureListings();
builder.Services.AddInfrastructureProjects();
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddCompanyPermissionAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
