using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EstateHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCompanyPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PermissionGroup",
                columns: new[] { "Id", "Description", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), "Company profile permissions.", "Company Profile", 10 },
                    { new Guid("10000000-0000-4000-8000-000000000002"), "Employee membership permissions.", "Employees", 20 },
                    { new Guid("10000000-0000-4000-8000-000000000003"), "Company role permissions.", "Roles", 30 },
                    { new Guid("10000000-0000-4000-8000-000000000004"), "Project management permissions.", "Projects", 40 },
                    { new Guid("10000000-0000-4000-8000-000000000005"), "Unit management permissions.", "Units", 50 },
                    { new Guid("10000000-0000-4000-8000-000000000006"), "Listing management permissions.", "Listings", 60 },
                    { new Guid("10000000-0000-4000-8000-000000000007"), "Booking management permissions.", "Bookings", 70 },
                    { new Guid("10000000-0000-4000-8000-000000000008"), "Lead management permissions.", "Leads", 80 },
                    { new Guid("10000000-0000-4000-8000-000000000009"), "Billing management permissions.", "Billing", 90 }
                });

            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Id", "Code", "Description", "IsActive", "Name", "PermissionGroupId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-4000-8000-000000000001"), "company.profile.read", "View the company profile.", true, "View company profile", new Guid("10000000-0000-4000-8000-000000000001") },
                    { new Guid("20000000-0000-4000-8000-000000000002"), "company.profile.manage", "Manage the company profile.", true, "Manage company profile", new Guid("10000000-0000-4000-8000-000000000001") },
                    { new Guid("20000000-0000-4000-8000-000000000003"), "company.employees.read", "View company employees.", true, "View employees", new Guid("10000000-0000-4000-8000-000000000002") },
                    { new Guid("20000000-0000-4000-8000-000000000004"), "company.employees.manage", "Manage company employees.", true, "Manage employees", new Guid("10000000-0000-4000-8000-000000000002") },
                    { new Guid("20000000-0000-4000-8000-000000000005"), "company.roles.read", "View company roles.", true, "View roles", new Guid("10000000-0000-4000-8000-000000000003") },
                    { new Guid("20000000-0000-4000-8000-000000000006"), "company.roles.manage", "Manage company roles.", true, "Manage roles", new Guid("10000000-0000-4000-8000-000000000003") },
                    { new Guid("20000000-0000-4000-8000-000000000007"), "company.projects.read", "View company projects.", true, "View projects", new Guid("10000000-0000-4000-8000-000000000004") },
                    { new Guid("20000000-0000-4000-8000-000000000008"), "company.projects.manage", "Manage company projects.", true, "Manage projects", new Guid("10000000-0000-4000-8000-000000000004") },
                    { new Guid("20000000-0000-4000-8000-000000000009"), "company.units.read", "View company units.", true, "View units", new Guid("10000000-0000-4000-8000-000000000005") },
                    { new Guid("20000000-0000-4000-8000-000000000010"), "company.units.manage", "Manage company units.", true, "Manage units", new Guid("10000000-0000-4000-8000-000000000005") },
                    { new Guid("20000000-0000-4000-8000-000000000011"), "company.listings.read", "View company listings.", true, "View listings", new Guid("10000000-0000-4000-8000-000000000006") },
                    { new Guid("20000000-0000-4000-8000-000000000012"), "company.listings.manage", "Manage company listings.", true, "Manage listings", new Guid("10000000-0000-4000-8000-000000000006") },
                    { new Guid("20000000-0000-4000-8000-000000000013"), "company.bookings.read", "View company bookings.", true, "View bookings", new Guid("10000000-0000-4000-8000-000000000007") },
                    { new Guid("20000000-0000-4000-8000-000000000014"), "company.bookings.manage", "Manage company bookings.", true, "Manage bookings", new Guid("10000000-0000-4000-8000-000000000007") },
                    { new Guid("20000000-0000-4000-8000-000000000015"), "company.leads.read", "View company leads.", true, "View leads", new Guid("10000000-0000-4000-8000-000000000008") },
                    { new Guid("20000000-0000-4000-8000-000000000016"), "company.leads.manage", "Manage company leads.", true, "Manage leads", new Guid("10000000-0000-4000-8000-000000000008") },
                    { new Guid("20000000-0000-4000-8000-000000000017"), "company.billing.read", "View company billing.", true, "View billing", new Guid("10000000-0000-4000-8000-000000000009") },
                    { new Guid("20000000-0000-4000-8000-000000000018"), "company.billing.manage", "Manage company billing.", true, "Manage billing", new Guid("10000000-0000-4000-8000-000000000009") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "Permission",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "PermissionGroup",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-4000-8000-000000000009"));
        }
    }
}
