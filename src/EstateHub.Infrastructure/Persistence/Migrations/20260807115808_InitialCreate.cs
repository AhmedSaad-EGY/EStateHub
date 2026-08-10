using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstateHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Amenity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountStatus = table.Column<int>(type: "int", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Location_Location_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Persona = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerProfile_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileAsset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    UploadedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAsset", x => x.Id);
                    table.CheckConstraint("CK_FileAssets_Height_Positive", "[Height] IS NULL OR [Height] > 0");
                    table.CheckConstraint("CK_FileAssets_SizeBytes_Positive", "[SizeBytes] > 0");
                    table.CheckConstraint("CK_FileAssets_Width_Positive", "[Width] IS NULL OR [Width] > 0");
                    table.ForeignKey(
                        name: "FK_FileAsset_AspNetUsers_UploadedByApplicationUserId",
                        column: x => x.UploadedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_AspNetUsers_RecipientApplicationUserId",
                        column: x => x.RecipientApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RememberMe = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshSessions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SourceIpHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityEvents_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserConsent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsentSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsent_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionPackage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Placement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionPackage", x => x.Id);
                    table.CheckConstraint("CK_PromotionPackages_DurationDays_Positive", "[DurationDays] > 0");
                    table.CheckConstraint("CK_PromotionPackages_Price_NonNegative", "[Price] >= 0");
                    table.ForeignKey(
                        name: "FK_PromotionPackage_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MaxPublishedListings = table.Column<int>(type: "int", nullable: true),
                    BookingFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlan", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlans_BookingFeeAmount_NonNegative", "[BookingFeeAmount] >= 0");
                    table.CheckConstraint("CK_SubscriptionPlans_MaxPublishedListings_NonNegative", "[MaxPublishedListings] IS NULL OR [MaxPublishedListings] >= 0");
                    table.CheckConstraint("CK_SubscriptionPlans_MonthlyPrice_NonNegative", "[MonthlyPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_SubscriptionPlan_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.CheckConstraint("CK_Addresses_Latitude_Range", "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");
                    table.CheckConstraint("CK_Addresses_Longitude_Range", "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");
                    table.ForeignKey(
                        name: "FK_Address_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_PermissionGroup_PermissionGroupId",
                        column: x => x.PermissionGroupId,
                        principalTable: "PermissionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilterSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    AlertsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedSearch_CustomerProfile_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyType = table.Column<int>(type: "int", nullable: false),
                    BusinessEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SupportPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogoFileAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverFileAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Company_Address_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_Currency_BaseCurrencyCode",
                        column: x => x.BaseCurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_FileAsset_CoverFileAssetId",
                        column: x => x.CoverFileAssetId,
                        principalTable: "FileAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_FileAsset_LogoFileAssetId",
                        column: x => x.LogoFileAssetId,
                        principalTable: "FileAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoice", x => x.Id);
                    table.CheckConstraint("CK_BillingInvoices_Period_Range", "[PeriodStart] IS NULL OR [PeriodEnd] IS NULL OR [PeriodStart] < [PeriodEnd]");
                    table.CheckConstraint("CK_BillingInvoices_Subtotal_NonNegative", "[Subtotal] >= 0");
                    table.CheckConstraint("CK_BillingInvoices_Total_NonNegative", "[Total] >= 0");
                    table.ForeignKey(
                        name: "FK_BillingInvoice_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoice_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyApplication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BusinessEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CompanyType = table.Column<int>(type: "int", nullable: false),
                    OfficeAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LocationText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EstimatedPropertyRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyApplication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyApplication_AspNetUsers_ReviewedByApplicationUserId",
                        column: x => x.ReviewedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyApplication_AspNetUsers_SubmittedByApplicationUserId",
                        column: x => x.SubmittedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyApplication_Company_ApprovedCompanyId",
                        column: x => x.ApprovedCompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmployee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsPrimaryContact = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmployee", x => x.Id);
                    table.UniqueConstraint("AK_CompanyEmployee_Id_CompanyId", x => new { x.Id, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_CompanyEmployee_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyEmployee_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRole", x => x.Id);
                    table.UniqueConstraint("AK_CompanyRole_Id_CompanyId", x => new { x.Id, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_CompanyRole_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentPeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MonthlyPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MaxPublishedListingsSnapshot = table.Column<int>(type: "int", nullable: true),
                    BookingFeeSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CancellationRequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscription", x => x.Id);
                    table.CheckConstraint("CK_CompanySubscriptions_BookingFeeSnapshot_NonNegative", "[BookingFeeSnapshot] >= 0");
                    table.CheckConstraint("CK_CompanySubscriptions_CurrentPeriod_Range", "[CurrentPeriodStart] < [CurrentPeriodEnd]");
                    table.CheckConstraint("CK_CompanySubscriptions_MaxPublishedListingsSnapshot_NonNegative", "[MaxPublishedListingsSnapshot] IS NULL OR [MaxPublishedListingsSnapshot] >= 0");
                    table.CheckConstraint("CK_CompanySubscriptions_MonthlyPriceSnapshot_NonNegative", "[MonthlyPriceSnapshot] >= 0");
                    table.ForeignKey(
                        name: "FK_CompanySubscription_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySubscription_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanySubscription_SubscriptionPlan_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DeliveryStatus = table.Column<int>(type: "int", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProjectStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_Company_DeveloperCompanyId",
                        column: x => x.DeveloperCompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Project_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.Id);
                    table.CheckConstraint("CK_PaymentTransactions_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_PaymentTransactions_CompletedAt_NotBeforeCreatedAt", "[CompletedAt] IS NULL OR [CompletedAt] >= [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_BillingInvoice_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusHistory_AspNetUsers_ChangedByApplicationUserId",
                        column: x => x.ChangedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusHistory_CompanyApplication_CompanyApplicationId",
                        column: x => x.CompanyApplicationId,
                        principalTable: "CompanyApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDocument_AspNetUsers_ReviewedByApplicationUserId",
                        column: x => x.ReviewedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyDocument_CompanyApplication_CompanyApplicationId",
                        column: x => x.CompanyApplicationId,
                        principalTable: "CompanyApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyDocument_FileAsset_FileAssetId",
                        column: x => x.FileAssetId,
                        principalTable: "FileAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEmployeeRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssignedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEmployeeRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyEmployeeRole_AspNetUsers_AssignedByApplicationUserId",
                        column: x => x.AssignedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyEmployeeRole_CompanyEmployee_CompanyEmployeeId_CompanyId",
                        columns: x => new { x.CompanyEmployeeId, x.CompanyId },
                        principalTable: "CompanyEmployee",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyEmployeeRole_CompanyRole_CompanyRoleId_CompanyId",
                        columns: x => new { x.CompanyRoleId, x.CompanyId },
                        principalTable: "CompanyRole",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyEmployeeRole_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyRolePermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GrantedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyRolePermission_AspNetUsers_GrantedByApplicationUserId",
                        column: x => x.GrantedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyRolePermission_CompanyRole_CompanyRoleId",
                        column: x => x.CompanyRoleId,
                        principalTable: "CompanyRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyRolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAmenity",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAmenity", x => new { x.ProjectId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_ProjectAmenity_Amenity_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAmenity_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMedia", x => x.Id);
                    table.CheckConstraint("CK_ProjectMedia_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProjectMedia_FileAsset_FileAssetId",
                        column: x => x.FileAssetId,
                        principalTable: "FileAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMedia_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagingCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FinishingType = table.Column<int>(type: "int", nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: true),
                    TotalFloors = table.Column<int>(type: "int", nullable: true),
                    BuiltUpArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LandArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FurnishedStatus = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unit", x => x.Id);
                    table.UniqueConstraint("AK_Unit_Id_ManagingCompanyId", x => new { x.Id, x.ManagingCompanyId });
                    table.CheckConstraint("CK_Units_Bathrooms_NonNegative", "[Bathrooms] >= 0");
                    table.CheckConstraint("CK_Units_Bedrooms_NonNegative", "[Bedrooms] >= 0");
                    table.CheckConstraint("CK_Units_BuiltUpArea_Positive", "[BuiltUpArea] > 0");
                    table.CheckConstraint("CK_Units_LandArea_Positive", "[LandArea] IS NULL OR [LandArea] > 0");
                    table.CheckConstraint("CK_Units_TotalFloors_Positive", "[TotalFloors] IS NULL OR [TotalFloors] > 0");
                    table.ForeignKey(
                        name: "FK_Unit_Company_ManagingCompanyId",
                        column: x => x.ManagingCompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Unit_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Unit_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Unit_UnitType_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Listing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ListingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ListingType = table.Column<int>(type: "int", nullable: false),
                    AskingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RentPeriod = table.Column<int>(type: "int", nullable: true),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listing", x => x.Id);
                    table.UniqueConstraint("AK_Listing_Id_CompanyId", x => new { x.Id, x.CompanyId });
                    table.CheckConstraint("CK_Listings_AskingPrice_Positive", "[AskingPrice] > 0");
                    table.ForeignKey(
                        name: "FK_Listing_CompanyEmployee_CreatedByEmployeeId_CompanyId",
                        columns: x => new { x.CreatedByEmployeeId, x.CompanyId },
                        principalTable: "CompanyEmployee",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Listing_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Listing_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Listing_Unit_UnitId_CompanyId",
                        columns: x => new { x.UnitId, x.CompanyId },
                        principalTable: "Unit",
                        principalColumns: new[] { "Id", "ManagingCompanyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NearbyPlace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DistanceMeters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TravelMinutes = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearbyPlace", x => x.Id);
                    table.CheckConstraint("CK_NearbyPlaces_DistanceMeters_NonNegative", "[DistanceMeters] IS NULL OR [DistanceMeters] >= 0");
                    table.CheckConstraint("CK_NearbyPlaces_Latitude_Range", "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");
                    table.CheckConstraint("CK_NearbyPlaces_Longitude_Range", "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");
                    table.CheckConstraint("CK_NearbyPlaces_ProjectOrUnit_Xor", "(([ProjectId] IS NOT NULL AND [UnitId] IS NULL) OR ([ProjectId] IS NULL AND [UnitId] IS NOT NULL))");
                    table.CheckConstraint("CK_NearbyPlaces_TravelMinutes_NonNegative", "[TravelMinutes] IS NULL OR [TravelMinutes] >= 0");
                    table.ForeignKey(
                        name: "FK_NearbyPlace_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NearbyPlace_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitAmenity",
                columns: table => new
                {
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitAmenity", x => new { x.UnitId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_UnitAmenity_Amenity_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitAmenity_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favorite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorite_CustomerProfile_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favorite_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListingMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingMedia", x => x.Id);
                    table.CheckConstraint("CK_ListingMedia_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ListingMedia_FileAsset_FileAssetId",
                        column: x => x.FileAssetId,
                        principalTable: "FileAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingMedia_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListingPriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingPriceHistory", x => x.Id);
                    table.CheckConstraint("CK_ListingPriceHistories_EffectiveDates", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                    table.CheckConstraint("CK_ListingPriceHistories_Price_Positive", "[Price] > 0");
                    table.ForeignKey(
                        name: "FK_ListingPriceHistory_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingPriceHistory_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListingPromotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PlacementSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingPromotion", x => x.Id);
                    table.CheckConstraint("CK_ListingPromotions_AmountSnapshot_NonNegative", "[AmountSnapshot] >= 0");
                    table.CheckConstraint("CK_ListingPromotions_TimeRange", "[StartsAt] < [EndsAt]");
                    table.ForeignKey(
                        name: "FK_ListingPromotion_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingPromotion_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingPromotion_Listing_ListingId_CompanyId",
                        columns: x => new { x.ListingId, x.CompanyId },
                        principalTable: "Listing",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingPromotion_PromotionPackage_PromotionPackageId",
                        column: x => x.PromotionPackageId,
                        principalTable: "PromotionPackage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListingViewEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewerApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnonymousSessionHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ViewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingViewEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingViewEvent_AspNetUsers_ViewerApplicationUserId",
                        column: x => x.ViewerApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingViewEvent_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DownPaymentPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    InstallmentFrequency = table.Column<int>(type: "int", nullable: false),
                    CashDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlan", x => x.Id);
                    table.CheckConstraint("CK_PaymentPlans_CashDiscountPercentage_Range", "[CashDiscountPercentage] IS NULL OR ([CashDiscountPercentage] >= 0 AND [CashDiscountPercentage] <= 100)");
                    table.CheckConstraint("CK_PaymentPlans_DownPaymentPercentage_Range", "[DownPaymentPercentage] >= 0 AND [DownPaymentPercentage] <= 100");
                    table.CheckConstraint("CK_PaymentPlans_DurationMonths_Positive", "[DurationMonths] > 0");
                    table.CheckConstraint("CK_PaymentPlans_TotalPrice_Positive", "[TotalPrice] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentPlan_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentPlan_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViewingSlot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    MeetingPoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewingSlot", x => x.Id);
                    table.CheckConstraint("CK_ViewingSlots_Capacity_Positive", "[Capacity] > 0");
                    table.CheckConstraint("CK_ViewingSlots_TimeRange", "[StartsAt] < [EndsAt]");
                    table.ForeignKey(
                        name: "FK_ViewingSlot_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViewingBooking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ViewingSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitorCount = table.Column<int>(type: "int", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancellationSource = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewingBooking", x => x.Id);
                    table.CheckConstraint("CK_ViewingBookings_VisitorCount_Positive", "[VisitorCount] > 0");
                    table.ForeignKey(
                        name: "FK_ViewingBooking_CompanyEmployee_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "CompanyEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViewingBooking_CustomerProfile_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViewingBooking_ViewingSlot_ViewingSlotId",
                        column: x => x.ViewingSlotId,
                        principalTable: "ViewingSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingCharge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewingBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanySubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VoidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingCharge", x => x.Id);
                    table.CheckConstraint("CK_BookingCharges_AmountSnapshot_NonNegative", "[AmountSnapshot] >= 0");
                    table.ForeignKey(
                        name: "FK_BookingCharge_CompanySubscription_CompanySubscriptionId",
                        column: x => x.CompanySubscriptionId,
                        principalTable: "CompanySubscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingCharge_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingCharge_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingCharge_ViewingBooking_ViewingBookingId",
                        column: x => x.ViewingBookingId,
                        principalTable: "ViewingBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingRescheduleHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewingBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromViewingSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToViewingSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRescheduleHistory", x => x.Id);
                    table.CheckConstraint("CK_BookingRescheduleHistories_DistinctSlots", "[FromViewingSlotId] <> [ToViewingSlotId]");
                    table.ForeignKey(
                        name: "FK_BookingRescheduleHistory_AspNetUsers_ChangedByApplicationUserId",
                        column: x => x.ChangedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRescheduleHistory_ViewingBooking_ViewingBookingId",
                        column: x => x.ViewingBookingId,
                        principalTable: "ViewingBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRescheduleHistory_ViewingSlot_FromViewingSlotId",
                        column: x => x.FromViewingSlotId,
                        principalTable: "ViewingSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRescheduleHistory_ViewingSlot_ToViewingSlotId",
                        column: x => x.ToViewingSlotId,
                        principalTable: "ViewingSlot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewingBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorType = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistory_AspNetUsers_ChangedByApplicationUserId",
                        column: x => x.ChangedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistory_ViewingBooking_ViewingBookingId",
                        column: x => x.ViewingBookingId,
                        principalTable: "ViewingBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyReview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewingBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModeratedByApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModeratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModerationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyReview", x => x.Id);
                    table.CheckConstraint("CK_CompanyReviews_Rating_Range", "[Rating] >= 1 AND [Rating] <= 5");
                    table.ForeignKey(
                        name: "FK_CompanyReview_AspNetUsers_ModeratedByApplicationUserId",
                        column: x => x.ModeratedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyReview_ViewingBooking_ViewingBookingId",
                        column: x => x.ViewingBookingId,
                        principalTable: "ViewingBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lead",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    OwnerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lead", x => x.Id);
                    table.CheckConstraint("CK_Leads_UsableContact", "[CustomerProfileId] IS NOT NULL OR [ContactPhone] IS NOT NULL OR [ContactEmail] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Lead_CompanyEmployee_OwnerEmployeeId_CompanyId",
                        columns: x => new { x.OwnerEmployeeId, x.CompanyId },
                        principalTable: "CompanyEmployee",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lead_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lead_CustomerProfile_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lead_ViewingBooking_SourceBookingId",
                        column: x => x.SourceBookingId,
                        principalTable: "ViewingBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoiceLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    CompanySubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingChargeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ListingPromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DescriptionSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoiceLine", x => x.Id);
                    table.CheckConstraint("CK_BillingInvoiceLines_LineTotal_NonNegative", "[LineTotal] >= 0");
                    table.CheckConstraint("CK_BillingInvoiceLines_LineType_Source", "(([LineType] = 0 AND [CompanySubscriptionId] IS NOT NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NULL) OR ([LineType] = 1 AND [CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NOT NULL AND [ListingPromotionId] IS NULL) OR ([LineType] = 2 AND [CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NOT NULL))");
                    table.CheckConstraint("CK_BillingInvoiceLines_Quantity_Positive", "[Quantity] > 0");
                    table.CheckConstraint("CK_BillingInvoiceLines_Source_Xor", "(([CompanySubscriptionId] IS NOT NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NULL) OR ([CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NOT NULL AND [ListingPromotionId] IS NULL) OR ([CompanySubscriptionId] IS NULL AND [BookingChargeId] IS NULL AND [ListingPromotionId] IS NOT NULL))");
                    table.CheckConstraint("CK_BillingInvoiceLines_UnitAmount_NonNegative", "[UnitAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_BillingInvoiceLine_BillingInvoice_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceLine_BookingCharge_BookingChargeId",
                        column: x => x.BookingChargeId,
                        principalTable: "BookingCharge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceLine_CompanySubscription_CompanySubscriptionId",
                        column: x => x.CompanySubscriptionId,
                        principalTable: "CompanySubscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceLine_ListingPromotion_ListingPromotionId",
                        column: x => x.ListingPromotionId,
                        principalTable: "ListingPromotion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CRMActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRMActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CRMActivity_CompanyEmployee_PerformedByEmployeeId",
                        column: x => x.PerformedByEmployeeId,
                        principalTable: "CompanyEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CRMActivity_Lead_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Lead",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRequirement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Intent = table.Column<int>(type: "int", nullable: false),
                    OriginalQuery = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StructuredCriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriteriaSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    MinBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    MinBedrooms = table.Column<int>(type: "int", nullable: true),
                    MinBathrooms = table.Column<int>(type: "int", nullable: true),
                    MinArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentPlanMonths = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExtractedByAI = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRequirement", x => x.Id);
                    table.CheckConstraint("CK_CustomerRequirements_Area_Range", "[MinArea] IS NULL OR [MaxArea] IS NULL OR [MinArea] <= [MaxArea]");
                    table.CheckConstraint("CK_CustomerRequirements_Budget_Range", "[MinBudget] IS NULL OR [MaxBudget] IS NULL OR [MinBudget] <= [MaxBudget]");
                    table.CheckConstraint("CK_CustomerRequirements_BudgetRequiresCurrency", "([MinBudget] IS NULL AND [MaxBudget] IS NULL) OR [CurrencyCode] IS NOT NULL");
                    table.CheckConstraint("CK_CustomerRequirements_MaxArea_Positive", "[MaxArea] IS NULL OR [MaxArea] > 0");
                    table.CheckConstraint("CK_CustomerRequirements_MaxBudget_NonNegative", "[MaxBudget] IS NULL OR [MaxBudget] >= 0");
                    table.CheckConstraint("CK_CustomerRequirements_MinArea_Positive", "[MinArea] IS NULL OR [MinArea] > 0");
                    table.CheckConstraint("CK_CustomerRequirements_MinBathrooms_NonNegative", "[MinBathrooms] IS NULL OR [MinBathrooms] >= 0");
                    table.CheckConstraint("CK_CustomerRequirements_MinBedrooms_NonNegative", "[MinBedrooms] IS NULL OR [MinBedrooms] >= 0");
                    table.CheckConstraint("CK_CustomerRequirements_MinBudget_NonNegative", "[MinBudget] IS NULL OR [MinBudget] >= 0");
                    table.CheckConstraint("CK_CustomerRequirements_PaymentPlanMonths_Positive", "[PaymentPlanMonths] IS NULL OR [PaymentPlanMonths] > 0");
                    table.ForeignKey(
                        name: "FK_CustomerRequirement_CompanyEmployee_ConfirmedByEmployeeId",
                        column: x => x.ConfirmedByEmployeeId,
                        principalTable: "CompanyEmployee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerRequirement_Currency_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currency",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerRequirement_Lead_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Lead",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeadInterest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterestType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadInterest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadInterest_Lead_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Lead",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeadInterest_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Address_LocationId",
                table: "Address",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "UX_Amenities_Code",
                table: "Amenity",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistory_ChangedByApplicationUserId",
                table: "ApplicationStatusHistory",
                column: "ChangedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistory_CompanyApplicationId",
                table: "ApplicationStatusHistory",
                column: "CompanyApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoice_CompanyId",
                table: "BillingInvoice",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoice_CurrencyCode",
                table: "BillingInvoice",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "UX_BillingInvoices_InvoiceNumber",
                table: "BillingInvoice",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceLine_BillingInvoiceId",
                table: "BillingInvoiceLine",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceLine_CompanySubscriptionId",
                table: "BillingInvoiceLine",
                column: "CompanySubscriptionId");

            migrationBuilder.CreateIndex(
                name: "UX_BillingInvoiceLines_BookingChargeId",
                table: "BillingInvoiceLine",
                column: "BookingChargeId",
                unique: true,
                filter: "[BookingChargeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BillingInvoiceLines_ListingPromotionId",
                table: "BillingInvoiceLine",
                column: "ListingPromotionId",
                unique: true,
                filter: "[ListingPromotionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharge_CompanyId",
                table: "BookingCharge",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharge_CompanySubscriptionId",
                table: "BookingCharge",
                column: "CompanySubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharge_CurrencyCode",
                table: "BookingCharge",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "UX_BookingCharges_ViewingBookingId",
                table: "BookingCharge",
                column: "ViewingBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRescheduleHistory_ChangedByApplicationUserId",
                table: "BookingRescheduleHistory",
                column: "ChangedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRescheduleHistory_FromViewingSlotId",
                table: "BookingRescheduleHistory",
                column: "FromViewingSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRescheduleHistory_ToViewingSlotId",
                table: "BookingRescheduleHistory",
                column: "ToViewingSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRescheduleHistory_ViewingBookingId",
                table: "BookingRescheduleHistory",
                column: "ViewingBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistory_ChangedByApplicationUserId",
                table: "BookingStatusHistory",
                column: "ChangedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistory_ViewingBookingId",
                table: "BookingStatusHistory",
                column: "ViewingBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_BaseCurrencyCode",
                table: "Company",
                column: "BaseCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CoverFileAssetId",
                table: "Company",
                column: "CoverFileAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_LogoFileAssetId",
                table: "Company",
                column: "LogoFileAssetId");

            migrationBuilder.CreateIndex(
                name: "UX_Companies_AddressId",
                table: "Company",
                column: "AddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Companies_RegistrationNumber",
                table: "Company",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Companies_Slug",
                table: "Company",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Companies_TaxId",
                table: "Company",
                column: "TaxId",
                unique: true,
                filter: "[TaxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyApplication_ReviewedByApplicationUserId",
                table: "CompanyApplication",
                column: "ReviewedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyApplication_SubmittedByApplicationUserId",
                table: "CompanyApplication",
                column: "SubmittedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "UX_CompanyApplications_ApprovedCompanyId",
                table: "CompanyApplication",
                column: "ApprovedCompanyId",
                unique: true,
                filter: "[ApprovedCompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocument_CompanyApplicationId",
                table: "CompanyDocument",
                column: "CompanyApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocument_FileAssetId",
                table: "CompanyDocument",
                column: "FileAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDocument_ReviewedByApplicationUserId",
                table: "CompanyDocument",
                column: "ReviewedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "UX_CompanyEmployees_OpenMembership",
                table: "CompanyEmployee",
                column: "ApplicationUserId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_CompanyEmployees_OpenPrimaryContact",
                table: "CompanyEmployee",
                column: "CompanyId",
                unique: true,
                filter: "[IsPrimaryContact] = 1 AND [EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeRole_AssignedByApplicationUserId",
                table: "CompanyEmployeeRole",
                column: "AssignedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeRole_CompanyEmployeeId_CompanyId",
                table: "CompanyEmployeeRole",
                columns: new[] { "CompanyEmployeeId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeRole_CompanyId",
                table: "CompanyEmployeeRole",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEmployeeRole_CompanyRoleId_CompanyId",
                table: "CompanyEmployeeRole",
                columns: new[] { "CompanyRoleId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "UX_CompanyEmployeeRoles_ActiveAssignment",
                table: "CompanyEmployeeRole",
                columns: new[] { "CompanyEmployeeId", "CompanyRoleId" },
                unique: true,
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyReview_ModeratedByApplicationUserId",
                table: "CompanyReview",
                column: "ModeratedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "UX_CompanyReviews_ViewingBookingId",
                table: "CompanyReview",
                column: "ViewingBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CompanyRoles_Company_NormalizedName",
                table: "CompanyRole",
                columns: new[] { "CompanyId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRolePermission_GrantedByApplicationUserId",
                table: "CompanyRolePermission",
                column: "GrantedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRolePermission_PermissionId",
                table: "CompanyRolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "UX_CompanyRolePermissions_ActiveAssignment",
                table: "CompanyRolePermission",
                columns: new[] { "CompanyRoleId", "PermissionId" },
                unique: true,
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscription_CurrencyCode",
                table: "CompanySubscription",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscription_SubscriptionPlanId",
                table: "CompanySubscription",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "UX_CompanySubscriptions_OpenCompanyId",
                table: "CompanySubscription",
                column: "CompanyId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CRMActivity_LeadId",
                table: "CRMActivity",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_CRMActivity_PerformedByEmployeeId",
                table: "CRMActivity",
                column: "PerformedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerProfiles_ApplicationUserId",
                table: "CustomerProfile",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRequirement_ConfirmedByEmployeeId",
                table: "CustomerRequirement",
                column: "ConfirmedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRequirement_CurrencyCode",
                table: "CustomerRequirement",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRequirement_LeadId",
                table: "CustomerRequirement",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorite_ListingId",
                table: "Favorite",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "UX_Favorites_CustomerProfileId_ListingId",
                table: "Favorite",
                columns: new[] { "CustomerProfileId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAsset_UploadedByApplicationUserId",
                table: "FileAsset",
                column: "UploadedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "UX_FileAssets_StorageKey",
                table: "FileAsset",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lead_CompanyId",
                table: "Lead",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_CustomerProfileId",
                table: "Lead",
                column: "CustomerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_OwnerEmployeeId_CompanyId",
                table: "Lead",
                columns: new[] { "OwnerEmployeeId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "UX_Leads_SourceBookingId",
                table: "Lead",
                column: "SourceBookingId",
                unique: true,
                filter: "[SourceBookingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeadInterest_ListingId",
                table: "LeadInterest",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "UX_LeadInterests_LeadId_ListingId_InterestType",
                table: "LeadInterest",
                columns: new[] { "LeadId", "ListingId", "InterestType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listing_CreatedByEmployeeId_CompanyId",
                table: "Listing",
                columns: new[] { "CreatedByEmployeeId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_CurrencyCode",
                table: "Listing",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_UnitId_CompanyId",
                table: "Listing",
                columns: new[] { "UnitId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "UX_Listings_CompanyId_ListingCode",
                table: "Listing",
                columns: new[] { "CompanyId", "ListingCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Listings_Published_UnitId",
                table: "Listing",
                column: "UnitId",
                unique: true,
                filter: "[PublicationStatus] = 2");

            migrationBuilder.CreateIndex(
                name: "UX_Listings_Slug",
                table: "Listing",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingMedia_FileAssetId",
                table: "ListingMedia",
                column: "FileAssetId");

            migrationBuilder.CreateIndex(
                name: "UX_ListingMedia_Cover_ListingId",
                table: "ListingMedia",
                column: "ListingId",
                unique: true,
                filter: "[IsCover] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ListingMedia_ListingId_FileAssetId",
                table: "ListingMedia",
                columns: new[] { "ListingId", "FileAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingPriceHistory_CurrencyCode",
                table: "ListingPriceHistory",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "UX_ListingPriceHistories_OpenListingId",
                table: "ListingPriceHistory",
                column: "ListingId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPromotion_CompanyId",
                table: "ListingPromotion",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPromotion_CurrencyCode",
                table: "ListingPromotion",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ListingPromotion_ListingId_CompanyId",
                table: "ListingPromotion",
                columns: new[] { "ListingId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingPromotion_PromotionPackageId",
                table: "ListingPromotion",
                column: "PromotionPackageId");

            migrationBuilder.CreateIndex(
                name: "UX_ListingPromotions_Active_ListingId_PlacementSnapshot",
                table: "ListingPromotion",
                columns: new[] { "ListingId", "PlacementSnapshot" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ListingViewEvent_ViewerApplicationUserId",
                table: "ListingViewEvent",
                column: "ViewerApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingViewEvents_ListingId_ViewedAt",
                table: "ListingViewEvent",
                columns: new[] { "ListingId", "ViewedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Locations_ParentLocationId_Type_Slug",
                table: "Location",
                columns: new[] { "ParentLocationId", "Type", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NearbyPlace_ProjectId",
                table: "NearbyPlace",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_NearbyPlace_UnitId",
                table: "NearbyPlace",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RecipientApplicationUserId",
                table: "Notification",
                column: "RecipientApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlan_CurrencyCode",
                table: "PaymentPlan",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlan_ListingId",
                table: "PaymentPlan",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_BillingInvoiceId",
                table: "PaymentTransaction",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_CurrencyCode",
                table: "PaymentTransaction",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentTransactions_Provider_ProviderReference",
                table: "PaymentTransaction",
                columns: new[] { "Provider", "ProviderReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permission_PermissionGroupId",
                table: "Permission",
                column: "PermissionGroupId");

            migrationBuilder.CreateIndex(
                name: "UX_Permissions_Code",
                table: "Permission",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionGroups_Name",
                table: "PermissionGroup",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_LocationId",
                table: "Project",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "UX_Projects_DeveloperCompanyId_Slug",
                table: "Project",
                columns: new[] { "DeveloperCompanyId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAmenity_AmenityId",
                table: "ProjectAmenity",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMedia_FileAssetId",
                table: "ProjectMedia",
                column: "FileAssetId");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectMedia_Cover_ProjectId",
                table: "ProjectMedia",
                column: "ProjectId",
                unique: true,
                filter: "[IsCover] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectMedia_ProjectId_FileAssetId",
                table: "ProjectMedia",
                columns: new[] { "ProjectId", "FileAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionPackage_CurrencyCode",
                table: "PromotionPackage",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "UX_PromotionPackages_Name",
                table: "PromotionPackage",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_ApplicationUserId",
                table: "RefreshSessions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_TokenHash",
                table: "RefreshSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearch_CustomerProfileId",
                table: "SavedSearch",
                column: "CustomerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_ApplicationUserId",
                table: "SecurityEvents",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OccurredAt",
                table: "SecurityEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlan_CurrencyCode",
                table: "SubscriptionPlan",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPlans_Name",
                table: "SubscriptionPlan",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unit_LocationId",
                table: "Unit",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Unit_ProjectId",
                table: "Unit",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Unit_UnitTypeId",
                table: "Unit",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_Units_ManagingCompanyId_ProjectId_UnitCode",
                table: "Unit",
                columns: new[] { "ManagingCompanyId", "ProjectId", "UnitCode" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Units_ManagingCompanyId_StandaloneUnitCode",
                table: "Unit",
                columns: new[] { "ManagingCompanyId", "UnitCode" },
                unique: true,
                filter: "[ProjectId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnitAmenity_AmenityId",
                table: "UnitAmenity",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "UX_UnitTypes_Code",
                table: "UnitType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserConsents_User_Policy_Version",
                table: "UserConsent",
                columns: new[] { "ApplicationUserId", "PolicyType", "PolicyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViewingBooking_AssignedEmployeeId",
                table: "ViewingBooking",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewingBooking_CustomerProfileId",
                table: "ViewingBooking",
                column: "CustomerProfileId");

            migrationBuilder.CreateIndex(
                name: "UX_ViewingBookings_Active_CustomerSlot",
                table: "ViewingBooking",
                columns: new[] { "ViewingSlotId", "CustomerProfileId" },
                unique: true,
                filter: "[Status] IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "UX_ViewingBookings_BookingCode",
                table: "ViewingBooking",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ViewingSlots_ListingId_StartsAt",
                table: "ViewingSlot",
                columns: new[] { "ListingId", "StartsAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationStatusHistory");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BillingInvoiceLine");

            migrationBuilder.DropTable(
                name: "BookingRescheduleHistory");

            migrationBuilder.DropTable(
                name: "BookingStatusHistory");

            migrationBuilder.DropTable(
                name: "CompanyDocument");

            migrationBuilder.DropTable(
                name: "CompanyEmployeeRole");

            migrationBuilder.DropTable(
                name: "CompanyReview");

            migrationBuilder.DropTable(
                name: "CompanyRolePermission");

            migrationBuilder.DropTable(
                name: "CRMActivity");

            migrationBuilder.DropTable(
                name: "CustomerRequirement");

            migrationBuilder.DropTable(
                name: "Favorite");

            migrationBuilder.DropTable(
                name: "LeadInterest");

            migrationBuilder.DropTable(
                name: "ListingMedia");

            migrationBuilder.DropTable(
                name: "ListingPriceHistory");

            migrationBuilder.DropTable(
                name: "ListingViewEvent");

            migrationBuilder.DropTable(
                name: "NearbyPlace");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PaymentPlan");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "ProjectAmenity");

            migrationBuilder.DropTable(
                name: "ProjectMedia");

            migrationBuilder.DropTable(
                name: "RefreshSessions");

            migrationBuilder.DropTable(
                name: "SavedSearch");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "UnitAmenity");

            migrationBuilder.DropTable(
                name: "UserConsent");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "BookingCharge");

            migrationBuilder.DropTable(
                name: "ListingPromotion");

            migrationBuilder.DropTable(
                name: "CompanyApplication");

            migrationBuilder.DropTable(
                name: "CompanyRole");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "Lead");

            migrationBuilder.DropTable(
                name: "BillingInvoice");

            migrationBuilder.DropTable(
                name: "Amenity");

            migrationBuilder.DropTable(
                name: "CompanySubscription");

            migrationBuilder.DropTable(
                name: "PromotionPackage");

            migrationBuilder.DropTable(
                name: "PermissionGroup");

            migrationBuilder.DropTable(
                name: "ViewingBooking");

            migrationBuilder.DropTable(
                name: "SubscriptionPlan");

            migrationBuilder.DropTable(
                name: "CustomerProfile");

            migrationBuilder.DropTable(
                name: "ViewingSlot");

            migrationBuilder.DropTable(
                name: "Listing");

            migrationBuilder.DropTable(
                name: "CompanyEmployee");

            migrationBuilder.DropTable(
                name: "Unit");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "UnitType");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "FileAsset");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
