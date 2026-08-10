# EstateHub Backend Project Handoff

> **Handoff date:** 2026-08-11  
> **Backend workspace:** `F:\EstateHub`  
> **Backend stack:** .NET 10, ASP.NET Core Web API, EF Core 10, SQL Server  
> **Current API count:** 47 implemented endpoints  
> **Current position:** Step 8A-1 has not started; the next action is Step 8A-1A (company project core endpoints).

---

## 1. Executive summary

EstateHub is a real-estate aggregation platform. Customers can discover real-estate companies, browse their projects and listings, manage favorites and saved searches, and later book viewings and submit reviews. Real-estate companies receive a tenant-scoped back office for employees, roles, projects, units, listings, bookings, leads, subscriptions, billing, and promotions.

The backend uses a four-layer architecture:

1. `EstateHub.Domain`
2. `EstateHub.Application`
3. `EstateHub.Infrastructure`
4. `EstateHub.Api`

The foundation is substantially complete:

- Solution and architecture created.
- Domain model: 49 business entities and 33 enums.
- Infrastructure Identity boundary implemented.
- All 49 Domain entities mapped with EF Core Fluent API.
- SQL Server schema created through `InitialCreate`.
- The MonsterASP database is online and has both approved migrations applied.
- Email/password authentication, email confirmation, JWT access tokens, refresh-token rotation, logout, password recovery, SMTP email, and authentication rate limiting are implemented.
- Public discovery APIs are implemented for companies, projects, listings, and catalog lookups.
- Customer self-service APIs are implemented for profile, favorites, and saved searches.
- Company context, database-backed permission authorization, Owner bootstrap, role management, company profile, and employee management are implemented.
- 47 endpoints currently exist in source.

Important qualification: most steps were validated through restore/build, EF model checks, SQL translation checks, and static audits. Runtime smoke tests and integration tests were deliberately postponed. “Implemented” does not mean every route has been exercised against the real database.

The latest attempted step was company project management. The original 11-endpoint task was too large for the Codex execution window. Codex removed its partial files, so **Step 8A-1 has not changed source code**. It must continue as three smaller slices.

---

## 2. Product and business scope

### 2.1 Core product

- Aggregate real-estate companies in one application.
- Show each company’s public profile.
- Show developer projects.
- Show units and public sale/rent listings.
- Allow filtering and searching across the public inventory.
- Support customer favorites and saved searches.
- Support viewing bookings and company follow-up.
- Support reviews associated with bookings.
- Support company employees, roles, and tenant permissions.

### 2.2 Revenue model

The approved business directions are:

1. **B2B subscription:** a company subscription controls listing/unit quota. Higher capacity requires a higher plan.
2. **Booking/request charge:** a company may be charged when a customer request or viewing booking is generated through EstateHub.
3. **Promotions/advertising:** companies can purchase listing promotion placements.
4. **MVP payments:** the current Domain enum contains only `PaymentProvider.Fake`. A real gateway has not been integrated.

### 2.3 AI scope

The team proposed:

- Natural-language property search, such as budget/location/property requirements.
- A company-side chatbot that helps employees understand a customer’s requirements and find matching inventory.

These features belong primarily to the AI developer. Backend integration contracts and AI endpoints have not been finalized or implemented. Do not invent the AI payloads before agreeing a contract with the AI team.

### 2.4 Explicitly unfinished external integrations

- Google sign-in: not implemented.
- Microsoft sign-in: not implemented.
- SMS/phone OTP: not implemented.
- Real payment gateway: not implemented.
- AI service integration: not implemented.

The approved current authentication path is email/password with email confirmation.

---

## 3. Team and ownership

The project is an internship team project with:

- React frontend
- .NET backend
- UI/UX
- Data Analysis
- Testing
- AI developers

The backend work described here was owned by Ahmed, working primarily by preparing strict prompts for Codex, reviewing Codex’s report/artifacts, and then moving to the next bounded step.

The UI/UX source was provided through Figma. The original ERD was derived from the UI and then refined through backend business-rule discussions. The repository originally contained an `ERD` directory before the solution was created.

---

## 4. Technology and environment

### 4.1 SDK and framework

- Target framework: `net10.0`
- Stable SDK selected: `10.0.302`
- EF CLI: `dotnet-ef 10.0.10`
- EF Core packages: `10.0.10`

The repository contains `global.json`:

```json
{
  "sdk": {
    "version": "10.0.302",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

The machine also had a preview .NET 10 SDK installed, but `global.json` prevents it from being selected for EstateHub.

### 4.2 Database and hosting

- Database provider: SQL Server.
- Hosted database: MonsterASP Free.
- The MonsterASP connection string is stored externally using User Secrets.
- Never commit, print, or paste the production connection string into source or this handoff.
- The API itself has not yet been reported as deployed to MonsterASP; only the SQL Server database and migrations are confirmed.

### 4.3 Direct package references

Infrastructure direct packages:

- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.10
- `Microsoft.EntityFrameworkCore.Design` 10.0.10
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.10
- `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.10
- `MailKit` 4.17.0

API direct packages:

- `Microsoft.EntityFrameworkCore.Design` 10.0.10, `PrivateAssets=all`
- `Swashbuckle.AspNetCore.Swagger` 10.2.3
- `Swashbuckle.AspNetCore.SwaggerGen` 10.2.3
- `Swashbuckle.AspNetCore.SwaggerUI` 10.2.3

Swagger was intentionally added manually by Ahmed. Preserve it.

### 4.4 Known local tooling issue

Visual Studio or a running API process can lock API output DLLs and make CLI builds fail with `MSB3021/MSB3027` copy-lock errors. Before asking Codex to build:

1. Stop the Visual Studio debug session.
2. Ensure `EstateHub.Api.exe` is not running.
3. Then run restore/build.

NuGet also experienced occasional repository-signature/TLS errors inside Codex’s sandbox. Normal local restore/build succeeded after the Visual Studio/NuGet state was repaired. Do not weaken NuGet signature or TLS validation.

---

## 5. Solution architecture

```text
EstateHub/
├── global.json
├── .gitignore
├── EstateHub.slnx
├── ERD/
└── src/
    ├── EstateHub.Domain/
    ├── EstateHub.Application/
    ├── EstateHub.Infrastructure/
    └── EstateHub.Api/
```

### 5.1 Project references

```text
EstateHub.Domain
└── no project references

EstateHub.Application
└── EstateHub.Domain

EstateHub.Infrastructure
├── EstateHub.Application
└── EstateHub.Domain

EstateHub.Api
├── EstateHub.Application
└── EstateHub.Infrastructure
```

### 5.2 Layer responsibilities

#### Domain

- Business entities and enums only.
- No EF Core, ASP.NET Core, Identity, Infrastructure, or DataAnnotations dependencies.
- Infrastructure-owned identity entities do not appear here.

#### Application

- Framework-independent request models, result models, paging types, and service interfaces.
- May reference Domain.
- Must not reference EF Core, ASP.NET Core, Identity, SQL Server, MailKit, or Infrastructure.

#### Infrastructure

- EF Core DbContext and mappings.
- ASP.NET Core Identity store.
- JWT/refresh token implementation.
- SMTP implementation.
- Application service implementations.
- Tenant-safe SQL queries and transactions.

#### API

- Controllers, HTTP request/response contracts, validation, policy attributes, ProblemDetails, middleware, Swagger, and DI wiring.
- Controllers should depend on Application abstractions, not directly on `EstateHubDbContext`.

### 5.3 Repeated implementation conventions

- Use `AsNoTracking()` for reads.
- Project only required columns.
- Avoid `Include` for large public graphs and avoid N+1 queries.
- Perform pagination in SQL.
- Use deterministic ordering with an ID tie-breaker.
- Use cancellation tokens.
- Use explicit API-layer enum-to-text switches; no global string-enum converter.
- Tenant IDs are resolved from the database using the authenticated `sub`; they are not accepted from request payloads.
- Business permissions are resolved from database assignments, never JWT permission claims.
- Authenticated personal/company responses use `Cache-Control: no-store`.
- Use `TimeProvider` instead of directly reading system time.
- Preserve history by setting `RevokedAt`/`EndedAt` instead of deleting historical assignment records.
- Use SQL rowversion for entities already configured with optimistic concurrency.

---

## 6. Domain model and ERD

The Domain has exactly **49 entities** and **33 enums**.

### 6.1 Entities by module

#### Users — 3

- `CustomerProfile`
- `UserConsent`
- `Notification`

#### Companies and RBAC — 12

- `CompanyApplication`
- `CompanyDocument`
- `ApplicationStatusHistory`
- `FileAsset`
- `Company`
- `CompanyEmployee`
- `Address`
- `CompanyRole`
- `CompanyEmployeeRole`
- `PermissionGroup`
- `Permission`
- `CompanyRolePermission`

#### Catalog — 15

- `Location`
- `Currency`
- `Project`
- `UnitType`
- `Unit`
- `Listing`
- `ListingPriceHistory`
- `PaymentPlan`
- `Amenity`
- `ProjectAmenity`
- `UnitAmenity`
- `ProjectMedia`
- `ListingMedia`
- `NearbyPlace`
- `ListingViewEvent`

#### Discovery/CRM — 6

- `Favorite`
- `SavedSearch`
- `Lead`
- `CustomerRequirement`
- `LeadInterest`
- `CRMActivity`

#### Bookings and reviews — 6

- `ViewingSlot`
- `ViewingBooking`
- `BookingStatusHistory`
- `BookingRescheduleHistory`
- `BookingCharge`
- `CompanyReview`

#### Billing — 7

- `SubscriptionPlan`
- `CompanySubscription`
- `PromotionPackage`
- `ListingPromotion`
- `BillingInvoice`
- `BillingInvoiceLine`
- `PaymentTransaction`

### 6.2 Infrastructure-owned Identity entities

These exist only in Infrastructure:

- `ApplicationUser : IdentityUser<Guid>`
- `RefreshSession`
- `SecurityEvent`
- `ApplicationUserAccountStatus`

Domain user relationships use scalar `Guid` identifiers rather than navigating to `ApplicationUser`.

### 6.3 Enums

| Enum | Values |
|---|---|
| `CompanyType` | Developer, BrokerAgency |
| `CustomerPersona` | Buyer, Renter, Agent |
| `CompanyApplicationStatus` | Draft, Submitted, UnderReview, NeedsChanges, Approved, Rejected |
| `LocationType` | Country, Governorate, City, District |
| `UnitStatus` | Available, Reserved, Sold, Rented, Withdrawn |
| `ListingType` | Sale, Rent |
| `ListingPublicationStatus` | Draft, Pending, Published, Archived |
| `AmenityScope` | Project, Unit, Both |
| `CustomerIntent` | Buy, Rent, Invest |
| `ViewingBookingStatus` | Pending, Confirmed, CheckedIn, Completed, Rejected, Cancelled, NoShow |
| `BookingCancellationSource` | Customer, Company, System |
| `BookingActorType` | Customer, Employee, Platform, System |
| `BookingChargeStatus` | Billable, Invoiced, Void |
| `CompanyReviewStatus` | Visible, Hidden, PendingModeration |
| `BillingInvoiceStatus` | Draft, Open, Paid, Void |
| `BillingLineType` | Subscription, BookingCharge, Promotion |
| `DocumentVerificationStatus` | Pending, Approved, Rejected |
| `FileType` | Image, Document |
| `CompanyStatus` | Active, Suspended, Deactivated |
| `CompanyEmployeeStatus` | Active, Suspended, Ended |
| `ProjectStatus` | Draft, Published, Archived |
| `ProjectDeliveryStatus` | Planned, UnderConstruction, ReadyToMove, Delivered |
| `FinishingType` | Unfinished, SemiFinished, Finished |
| `FurnishedStatus` | Unfurnished, SemiFurnished, Furnished |
| `RentPeriod` | Monthly, Yearly |
| `InstallmentFrequency` | Monthly, Quarterly, SemiAnnual, Annual |
| `LeadPriority` | Low, Medium, High |
| `LeadStage` | New, Contacted, Qualified, ViewingScheduled, Negotiation, Won, Lost |
| `ViewingSlotStatus` | Open, Closed, Cancelled |
| `CompanySubscriptionStatus` | Active, Expired, Cancelled |
| `ListingPromotionStatus` | Scheduled, Active, Completed, Cancelled |
| `PaymentTransactionStatus` | Pending, Succeeded, Failed |
| `PaymentProvider` | Fake |

### 6.4 Important schema decisions

- `Currency.Code` is a string primary key of length 3.
- `ProjectAmenity(ProjectId, AmenityId)` is a composite primary key.
- `UnitAmenity(UnitId, AmenityId)` is a composite primary key.
- `Unit.ProjectId` is nullable to support standalone units.
- Tenant-safe alternate keys exist for:
  - `CompanyEmployee(Id, CompanyId)`
  - `CompanyRole(Id, CompanyId)`
  - `Unit(Id, ManagingCompanyId)`
  - `Listing(Id, CompanyId)`
- Tenant-safe composite foreign keys protect Listing, Lead owner, ListingPromotion, and employee-role relationships.
- One open employee membership per user is enforced with a filtered unique index on `ApplicationUserId WHERE EndedAt IS NULL`.
- An employee may join another company after the previous membership has ended.
- One current primary contact per company is enforced by a filtered unique index.
- All business/historical relationships use Restrict/NoAction deletion. Cascades are limited to expected ASP.NET Identity tables.
- Money and area use `decimal(18,2)`.
- Percentages use `decimal(5,2)`.
- Coordinates use `decimal(9,6)`.
- The generated schema contains 62 check constraints and 12 rowversion tables.
- FileAsset currently has uploader identity but no CompanyId. Company-level file ownership therefore requires careful application-level validation.

The current code supports both Developer and BrokerAgency. Public project endpoints only expose projects owned by eligible Developer companies. If product management later decides there should be only one company type, that is a product/model change and must not be silently assumed.

---

## 7. Persistence, schema, and migrations

### 7.1 DbContext

`EstateHubDbContext` inherits:

```csharp
IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

It calls Identity’s base model configuration and discovers Domain/Infrastructure configurations through assembly scanning.

### 7.2 Final EF configuration count

- Users/Companies/RBAC: 15
- Catalog: 15
- Discovery/Bookings: 12
- Billing: 7
- Total Domain configurations: 49
- Identity support configurations are additional Infrastructure configurations.

### 7.3 Generated tables

`InitialCreate` generated 58 tables:

- 7 ASP.NET Identity tables
- 2 Infrastructure identity-support tables
- 49 Domain tables

Identity tables:

- `AspNetRoles`
- `AspNetUsers`
- `AspNetRoleClaims`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserRoles`
- `AspNetUserTokens`

Infrastructure support:

- `RefreshSessions`
- `SecurityEvents`

### 7.4 Migrations

#### 1. Initial schema

- ID: `20260807115808_InitialCreate`
- Creates all 58 tables.
- Applied to MonsterASP.

#### 2. Permission catalog

- ID: `20260809220431_SeedCompanyPermissionCatalog`
- Data-only migration.
- Inserts 9 permission groups and 18 global permissions.
- Applied to MonsterASP.

Current expected migration state:

```text
InitialCreate                         Applied
SeedCompanyPermissionCatalog         Applied
Pending migrations                   0
Pending model changes                0
```

### 7.5 Legacy permission cleanup

The database initially contained an unused manually inserted catalog:

- 5 legacy permission groups
- 30 legacy permissions such as `VIEW_0`, `CREATE_1`, and `APPROVE_4`
- Zero `CompanyRolePermission` references

The legacy rows were backed up, deleted transactionally, and replaced by the approved fixed catalog.

Backup path on the original development machine:

```text
C:\Users\pcc\AppData\Local\EstateHub\Backups\permission-catalog-20260810200435872.json
```

The backup contains no credentials.

### 7.6 Last known MonsterASP aggregate state

After the permission migration:

| Table/record type | Count |
|---|---:|
| Migration history records | 2 |
| PermissionGroup | 9 |
| Permission | 18 |
| Company | 5 |
| CompanyEmployee | 5 |
| CompanyRole | 0 |
| CompanyEmployeeRole | 0 |
| CompanyRolePermission | 0 |

The user explicitly chose to preserve the existing five companies and five employees. A full database reset was not performed.

Counts for other tables were not included in the final report and must not be assumed to be zero.

---

## 8. Authentication, email, and security

### 8.1 Identity configuration

- `ApplicationUser : IdentityUser<Guid>`
- Unique email required.
- Confirmed email required before login.
- Phone confirmation not required.
- Password policy:
  - minimum 8 characters
  - uppercase required
  - lowercase required
  - digit required
  - non-alphanumeric not required
- Lockout:
  - 5 failed attempts
  - 5-minute lockout

### 8.2 JWT

JWT access tokens are HS256 signed and contain only:

- `sub`
- `email`
- `jti`
- standard issuer/audience/time metadata

They intentionally do not contain:

- CompanyId
- employee ID
- company role
- permission codes

Authorization always returns to the database for company context and permissions.

Expected signing key:

- external configuration key: `Jwt__SigningKey`
- minimum 32 bytes
- never commit the signing key

### 8.3 Refresh tokens

- 64 cryptographically random bytes encoded with Base64Url.
- Only uppercase SHA-256 hashes are stored.
- Raw refresh tokens never enter the database.
- Default absolute lifetime:
  - 7 days
  - 30 days when Remember Me is selected
- Rotation preserves the original absolute session expiration.
- Rotation uses a transaction and conditional update to prevent concurrent reuse.
- Logout revokes only the supplied refresh session.
- Logout is idempotent and does not require a valid access JWT.

### 8.4 Password reset

- Forgot-password response is enumeration-resistant.
- Reset tokens come from Identity and are Base64Url encoded into frontend links.
- Successful reset clears lockout/failed count and revokes every active refresh session.
- Existing stateless access JWTs are not denylisted and can survive until their normal 15-minute expiry.

### 8.5 Email

- MailKit SMTP implementation.
- Gmail SMTP host: `smtp.gmail.com`
- Port: 587
- STARTTLS required.
- Fresh SMTP client per send.
- No TLS bypass.
- Email confirmation and password reset links are HTML/query encoded.
- Tokens and final URLs are not persisted or logged.

Required external email settings:

```text
Email:Username
Email:Password
Email:FromAddress
```

The Gmail account uses an app password; do not use the normal Gmail password.

### 8.6 Authentication rate limiting

Built-in ASP.NET Core fixed-window policies:

| Policy | Limit | Window | Routes |
|---|---:|---:|---|
| Registration | 5 | 600 seconds | register |
| Login | 10 | 60 seconds | login |
| Email delivery | 3 | 900 seconds | resend confirmation, forgot password |
| Token lifecycle | 30 | 60 seconds | refresh, logout |
| Verification | 10 | 600 seconds | confirm email, reset password |

Partitioning uses `RemoteIpAddress`, mapped to canonical IPv6. It does not trust a user-supplied forwarded header.

Known production limitation: the limiter is in-memory per process. Trusted proxy/forwarded-header configuration is still required after confirming MonsterASP’s actual proxy topology.

### 8.7 Required external configuration

Do not list User Secrets in shared logs. The expected secret/configuration keys include:

```text
ConnectionStrings:DefaultConnection
Jwt:SigningKey
Email:Username
Email:Password
Email:FromAddress
Frontend:BaseUrl
```

The original machine set `Frontend:BaseUrl` to `http://localhost:5173` for development. The React frontend was not ready at that time.

---

## 9. Company RBAC

### 9.1 Permission catalog

The global catalog contains these exact ordinal-sensitive codes:

```text
company.profile.read
company.profile.manage
company.employees.read
company.employees.manage
company.roles.read
company.roles.manage
company.projects.read
company.projects.manage
company.units.read
company.units.manage
company.listings.read
company.listings.manage
company.bookings.read
company.bookings.manage
company.leads.read
company.leads.manage
company.billing.read
company.billing.manage
```

Groups use deterministic IDs beginning with `10000000-...`, and permissions use deterministic IDs beginning with `20000000-...`.

### 9.2 Authorization flow

`RequireCompanyPermissionAttribute` produces policies in this form:

```text
CompanyPermission:company.projects.manage
```

The handler requires:

1. Authenticated principal.
2. Valid GUID `sub`.
3. Active/unended employee membership.
4. Active and verified company.
5. Active role assignment.
6. Active company role.
7. Non-revoked permission grant.
8. Active global permission with the exact requested code.

There is no JWT-permission, primary-contact, built-in-role, route-company-ID, or cache bypass in the authorization handler.

### 9.3 Owner bootstrap

Because current companies initially have no roles, an authenticated current primary contact can call the one-time endpoint:

```http
POST /api/me/company-context/bootstrap-owner
```

It is plain `[Authorize]`, not permission-protected, because no role exists yet. It is serializable and tightly constrained. It creates:

- Built-in active `Owner` role.
- All 18 active grants.
- One active Owner assignment to the current primary-contact employee.

Important operational point: the last database report still showed zero roles and zero assignments. Therefore, before testing protected company endpoints, each relevant company’s primary contact must successfully authenticate and call this bootstrap endpoint, or the company approval flow later must invoke equivalent provisioning.

---

## 10. Implemented API inventory — 47 endpoints

### 10.1 Authentication — 8

| Method | Route | Authentication | Notes |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Creates user, customer profile, and two legal consents; sends confirmation email |
| POST | `/api/auth/confirm-email` | Anonymous | Confirms Identity email token |
| POST | `/api/auth/resend-confirmation` | Anonymous | Generic enumeration-resistant response |
| POST | `/api/auth/login` | Anonymous | Returns access/refresh pair; requires confirmed email |
| POST | `/api/auth/refresh` | Anonymous | One-time refresh rotation |
| POST | `/api/auth/logout` | Anonymous | Idempotent refresh-session revocation |
| POST | `/api/auth/forgot-password` | Anonymous | Generic accepted response |
| POST | `/api/auth/reset-password` | Anonymous | Resets password and revokes refresh sessions |

### 10.2 Public catalog and discovery — 10

| Method | Route | Notes |
|---|---|---|
| GET | `/api/catalog/locations` | Optional parent filter; active hierarchy |
| GET | `/api/catalog/unit-types` | Active lookup |
| GET | `/api/catalog/currencies` | Currency lookup |
| GET | `/api/catalog/amenities` | Optional Project/Unit appliesTo filter |
| GET | `/api/companies` | Public paginated directory |
| GET | `/api/companies/{slug}` | Public company details |
| GET | `/api/companies/{companySlug}/projects` | Published public projects |
| GET | `/api/companies/{companySlug}/projects/{projectSlug}` | Public project details |
| GET | `/api/listings` | Public paginated listing search/filter/sort |
| GET | `/api/listings/{slug}` | Public listing details |

### 10.3 Customer self-service — 9

All use authenticated JWT `sub` and no-store responses.

| Method | Route |
|---|---|
| GET | `/api/me/profile` |
| PUT | `/api/me/profile` |
| GET | `/api/me/favorites` |
| PUT | `/api/me/favorites/{listingId}` |
| DELETE | `/api/me/favorites/{listingId}` |
| GET | `/api/me/saved-searches` |
| POST | `/api/me/saved-searches` |
| PUT | `/api/me/saved-searches/{savedSearchId}` |
| DELETE | `/api/me/saved-searches/{savedSearchId}` |

Saved-search filters are canonical JSON objects, not escaped JSON strings.

### 10.4 Company context — 1

| Method | Route | Notes |
|---|---|---|
| GET | `/api/me/company-context` | Returns active company, employee, roles, and permission codes |

### 10.5 Owner bootstrap and role management — 8

| Method | Route | Permission |
|---|---|---|
| POST | `/api/me/company-context/bootstrap-owner` | Plain authenticated one-time bootstrap |
| GET | `/api/company/permissions` | `company.roles.read` |
| GET | `/api/company/roles` | `company.roles.read` |
| GET | `/api/company/roles/{roleId}` | `company.roles.read` |
| POST | `/api/company/roles` | `company.roles.manage` |
| PUT | `/api/company/roles/{roleId}` | `company.roles.manage` |
| PUT | `/api/company/roles/{roleId}/permissions` | `company.roles.manage` |
| DELETE | `/api/company/roles/{roleId}` | `company.roles.manage` |

Built-in Owner cannot be renamed, deactivated, or have its permissions replaced. Custom-role deactivation revokes active grants/employee assignments while preserving historical rows.

### 10.6 Company profile and employees — 11

| Method | Route | Permission |
|---|---|---|
| GET | `/api/company/profile` | `company.profile.read` |
| PUT | `/api/company/profile` | `company.profile.manage` |
| GET | `/api/company/employees` | `company.employees.read` |
| GET | `/api/company/employees/{employeeId}` | `company.employees.read` |
| POST | `/api/company/employees` | `company.employees.manage` |
| PUT | `/api/company/employees/{employeeId}` | `company.employees.manage` |
| POST | `/api/company/employees/{employeeId}/suspend` | `company.employees.manage` |
| POST | `/api/company/employees/{employeeId}/activate` | `company.employees.manage` |
| POST | `/api/company/employees/{employeeId}/end` | `company.employees.manage` |
| PUT | `/api/company/employees/{employeeId}/roles` | Both employees.manage and roles.manage |
| PUT | `/api/company/employees/{employeeId}/primary-contact` | Both employees.manage and roles.manage |

Key rules:

- Profile and address update transactionally.
- Company and employee writes use Base64-encoded 8-byte rowversion concurrency.
- Employee can have only one open membership across companies.
- Ended membership remains as history.
- Primary contact cannot be suspended or ended before transfer.
- Ending an employee revokes active role assignments.
- Primary-contact transfer ensures the target has Owner access.

---

## 11. Completed project steps

### Step 1 — Solution scaffold

- Created the solution and four projects.
- Added references in the approved direction.
- Added `.gitignore`.
- Removed template WeatherForecast/sample files.
- Build: 0 warnings/errors.

### Step 2B — Domain

- Created 49 entities and 33 enums.
- Kept Identity types out of Domain.
- Applied approved ERD overrides.
- Build: 0 warnings/errors.

### Step 3A — Identity/persistence foundation

- Added ApplicationUser, RefreshSession, SecurityEvent.
- Added Identity DbContext base and configurations.

### Steps 3B-1 through 3B-4 — EF mappings

- Added all 49 Domain Fluent configurations.
- Validated keys, filtered indexes, tenant-safe FKs, checks, precision, rowversion, and deletion behavior.

### Step 4A — DbContext DI

- Added Infrastructure SQL Server registration.
- Added API fail-fast check for `DefaultConnection`.

### Steps 4B and 4C — Initial migration/database

- Added API Design package required by EF tools.
- Generated and audited `InitialCreate`.
- Applied `InitialCreate` to MonsterASP.

### Steps 5A through 5F — Authentication/security

- Identity + JWT registration.
- Refresh-token engine.
- Gmail SMTP email sender.
- Registration/email confirmation/login.
- Refresh/logout routes.
- Password reset routes.
- Authentication rate limiting.

### Steps 6A through 6D — Public read APIs

- Public companies.
- Public projects.
- Public listing search/details.
- Catalog lookups.

### Step 7A — Customer self-service

- Customer profile.
- Favorites.
- Saved searches.

### Steps 7B-1 through 7B-4B — Company access and permission catalog

- Current company context.
- Dynamic authorization policies.
- Fixed permission catalog migration.
- Read-only legacy catalog audit.
- Legacy cleanup and migration application.

### Step 7C-1 — Owner bootstrap and role management

- 8 routes.
- Database-backed tenant-safe role lifecycle.

### Step 7C-2 — Company profile and employee management

- 11 routes.
- Rowversion and employee state/role invariants.

### Step 8A-1 — Not started

The original request contained 11 company-project endpoints. It failed twice before completing:

1. First attempt: the API was running in Visual Studio and locked the output DLLs.
2. Second attempt: Codex exceeded its execution window after creating partial files.

Codex removed the partial files and reported that no source changes remained. Treat Step 8A-1 as not started. Do not assume any CompanyProjects management file exists without checking the actual worktree.

---

## 12. Immediate next step

Split project management into three bounded tasks.

### Step 8A-1A — Project core — next task

Implement exactly:

```http
GET  /api/company/projects
GET  /api/company/projects/{projectId}
POST /api/company/projects
PUT  /api/company/projects/{projectId}
POST /api/company/projects/{projectId}/publish
POST /api/company/projects/{projectId}/archive
```

Permissions:

- Reads: `company.projects.read`
- Mutations: `company.projects.manage`

Core rules already agreed:

- Only Developer companies manage projects.
- Never accept CompanyId.
- Resolve tenant through `ICompanyAccessService`.
- Support paginated list/search/status/delivery/location filtering.
- Strict textual enum input.
- Draft creation.
- Draft/Published update.
- Published slug is immutable.
- Draft → Published.
- Draft/Published → Archived.
- Base64 8-byte Project rowversion.
- Active Location required.
- Slug lowercase kebab-case and unique per developer company.
- Use TimeProvider UTC.
- No media/amenity/nearby mutation in this slice.

After 8A-1A:

### Step 8A-1B — Project amenities and media

```http
PUT /api/company/projects/{projectId}/amenities
PUT /api/company/projects/{projectId}/media
```

### Step 8A-1C — Project nearby places

```http
POST   /api/company/projects/{projectId}/nearby-places
PUT    /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}
DELETE /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}
```

The split is required because the 11-endpoint version exceeded Codex’s execution window.

---

## 13. Remaining backend roadmap

Ahmed explicitly requested that the next engineer implement **all endpoints required by the approved ERD/business scope**, not only a reduced demo subset.

The remaining count is an estimate because final controller grouping and bulk-vs-individual operations can change it. At the handoff point, approximately **60–70 endpoints** remained beyond the 47 implemented routes.

Recommended order:

### 13.1 Projects

- Complete Steps 8A-1A/B/C — 11 total endpoints.

### 13.2 Units

Expected capabilities:

- Company unit directory/detail.
- Create/update unit.
- Project-backed and standalone units.
- Unit status transitions.
- Unit amenities.
- Unit nearby places.
- Tenant-safe project/managing-company validation.
- Rowversion concurrency.

Estimated: 8–10 endpoints.

### 13.3 Listings

Expected capabilities:

- Company listing directory/detail.
- Create/update listing.
- Draft/pending/published/archive lifecycle.
- Price-history management.
- Payment-plan management.
- Listing media.
- Subscription quota enforcement.
- Broker/developer management rules.

Estimated: 9–12 endpoints.

### 13.4 File assets

Needed by:

- Company logo/cover.
- Company documents.
- Project media.
- Listing media.

Decide and document storage before implementation. MonsterASP filesystem persistence and public URL behavior must be verified. Do not expose `StorageKey` directly. FileAsset lacks CompanyId, so company ownership must be handled carefully.

Estimated: 3–5 endpoints.

### 13.5 Company application and platform administration

- Applicant draft/update/submit/status.
- Company documents.
- Admin directory/detail.
- Under-review/needs-changes/reject/approve transitions.
- Approval should create Company, primary employee, and Owner role/grants/assignment transactionally.
- Platform-admin authorization must be designed; company permissions are not platform-admin permissions.

Estimated: 8–12 endpoints.

### 13.6 Viewing slots, bookings, and reviews

- Company viewing-slot management.
- Customer booking create/list/detail/cancel/reschedule.
- Company booking confirmation/rejection/assignment/check-in/completion/no-show.
- Status/reschedule history.
- Capacity and state-transition rules.
- Booking charge creation/void workflow.
- Review create/read/moderation eligibility.

Estimated: 12–16 endpoints.

### 13.7 Leads and CRM

- Company lead directory/detail.
- Ownership/assignment.
- Stage/priority changes.
- Customer requirements.
- Listing interests.
- CRM activities.
- Booking-to-lead linkage and tenant checks.

Estimated: 8–12 endpoints.

### 13.8 Subscription, billing, promotion, and fake payment

- Public/admin subscription-plan lookup as appropriate.
- Company current subscription and usage/quota.
- Subscribe/change/cancel flow using fake provider.
- Promotion-package lookup.
- Listing promotion create/cancel.
- Invoice directory/detail.
- Fake payment transaction processing.
- Booking charge/invoice linkage.
- Billing-line XOR and state consistency.

Estimated: 10–14 endpoints.

### 13.9 Notifications

- Customer/company notification directory.
- Unread count.
- Mark one/all as read.

Estimated: 3–4 endpoints.

### 13.10 Final administration/moderation

- Company suspension/deactivation if required by UI.
- Review moderation.
- Catalog administration only if included in UI.
- Security event inspection only if approved for platform admins.

Do not automatically expose every entity as CRUD. Historical/security entities often need read-only internal workflows rather than public CRUD.

---

## 14. Testing status and required work

### 14.1 What has been validated

- Repeated `dotnet restore` and solution builds.
- Most final builds reported 0 warnings and 0 errors.
- EF full-model construction.
- `has-pending-model-changes` checks.
- Disposable SQL translation checks for public/read queries.
- Migration code audits.
- Database aggregate checks during migration deployment.

Disposable validator projects were removed after use.

### 14.2 What has not been completed

- No permanent unit-test project was reported.
- No permanent integration-test project was reported.
- Runtime smoke tests were intentionally postponed.
- Auth/email routes have not been fully exercised end-to-end in the shared reports.
- Company role/profile/employee APIs have not been reported as runtime-tested against MonsterASP.
- SMTP was configured but no production send was performed during implementation reports.
- Frontend integration is not complete.
- No load/security testing.

### 14.3 Minimum final test plan

1. Add Application/Infrastructure unit tests for state transitions and validation.
2. Add API integration tests with a disposable SQL Server database or container.
3. Create a deterministic seed for test companies/users only in the test environment.
4. Smoke-test every Swagger route.
5. Test tenant isolation with two companies.
6. Test concurrency with duplicate refresh rotation, role assignment, rowversion, and booking capacity.
7. Test auth rate-limit 429 responses.
8. Verify email links with the real frontend base URL.
9. Test migration from a blank database to latest.
10. Test the MonsterASP deployment separately from local integration tests.

DeepSeek/OpenCode was discussed as a possible fallback for generating tests if Codex quota became exhausted. If used, give it a strict, bounded prompt, actual source files, and require build/test evidence; do not trust its assumptions about the project.

---

## 15. Git and repository warning

At multiple checkpoints, `F:\EstateHub` was reported as untracked inside a parent Git repository rooted above it at `F:\`, which contained many unrelated changes.

The next engineer must verify immediately:

```powershell
git -C F:\EstateHub status
git -C F:\ rev-parse --show-toplevel
```

Recommended action:

- Put EstateHub in its own dedicated Git repository or confirm the intended repository root.
- Do not commit unrelated parent-drive changes.
- Ensure `bin/`, `obj/`, secrets, backup files, and user-specific IDE files are ignored.
- Commit completed slices in small, named commits.

This is one of the highest-priority handoff risks.

---

## 16. Working method used with Codex

### 16.1 Normal cycle

For each slice:

1. Define exact routes and business rules.
2. Select a bounded module.
3. Send Codex a strict prompt.
4. Codex performs preflight and implementation.
5. Require 0-warning/0-error build.
6. Require no pending EF model changes unless a migration is the explicit goal.
7. Review Codex’s report and, when available, the source archive.
8. Only then move to the next slice.

### 16.2 Model selection

Typical choice:

```text
Model: gpt-5.6-terra
Effort: High
Fast mode: Off
```

Use a stronger `sol` model only for exceptionally complex authorization, financial, concurrency, or difficult debugging work. Standard CRUD/read slices should remain on Terra to preserve quota.

### 16.3 Execution-window lesson

Do not send more than roughly 5–7 substantial endpoints in one implementation turn when they include multiple transactions and contracts. The 11-endpoint project-management task timed out twice.

Preferred splitting:

- Core CRUD/lifecycle.
- Child collection replacement.
- Smaller nested resource CRUD.

### 16.4 Prompt rules that worked well

- State “Implement Step X only.”
- List exact routes.
- List exact permission policy per route.
- Prohibit CompanyId input for tenant routes.
- Define allowed state transitions.
- Define generic error behavior.
- Define concurrency/transaction boundaries.
- Explicitly state what must not be started.
- Avoid broad file hashes except migrations/security-critical files.
- Do not connect to MonsterASP unless the step explicitly authorizes it.
- Never inspect or display User Secrets.
- Do not let temporary validators remain in the repository.

### 16.5 Definition of done for a normal code slice

- Exact requested endpoints implemented—no extras.
- Layer boundaries preserved.
- Tenant isolation present in SQL queries.
- Input validation explicit.
- No secret or connection-string change.
- No migration unless explicitly required.
- Build 0 warnings/errors.
- EF reports no pending model changes.
- No API/database/external-service runtime action unless explicitly approved.
- Concise report listing files, routes, behavior, and verification.

---

## 17. Known risks and technical debt

1. **Runtime testing gap:** 47 routes exist, but the project lacks full permanent automated/runtime validation.
2. **Owner provisioning:** current database reports zero company roles. Protected company APIs are unusable until Owner bootstrap or approval provisioning runs.
3. **Platform admin authorization:** company RBAC is not sufficient for platform-wide review/approval/moderation.
4. **File ownership/storage:** FileAsset has no CompanyId and storage/deployment strategy is unfinished.
5. **In-memory rate limiting:** unsuitable for multiple API instances and requires trusted proxy configuration.
6. **JWT revocation:** password reset revokes refresh tokens, but already-issued access tokens survive up to 15 minutes.
7. **Email delivery:** synchronous SMTP; no outbox/background retry worker.
8. **No Google/Microsoft OAuth:** email/password only.
9. **No phone verification:** no SMS provider or OTP flow.
10. **Fake payments only:** no real payment reconciliation/webhooks/refunds.
11. **AI integration missing:** chatbot/search contracts not defined.
12. **Git-root risk:** project may still be untracked inside an unrelated parent repository.
13. **MonsterASP Free limitations:** connection limits, filesystem behavior, proxy topology, scheduled/background work, and deployment capabilities must be verified.
14. **Existing tenant data:** five companies and five employees were intentionally preserved; do not assume a blank database.
15. **Swagger security setup:** verify Swagger JWT Bearer support and production exposure before deployment.
16. **No full database backup process:** only the removed legacy permission catalog has a confirmed local backup.

---

## 18. New engineer onboarding checklist

### First hour

- [ ] Confirm the intended Git repository root.
- [ ] Create/check out a dedicated feature branch.
- [ ] Confirm `dotnet --version` returns 10.0.302.
- [ ] Confirm `dotnet ef --version` returns 10.0.10.
- [ ] Stop any running Visual Studio/API process.
- [ ] Run restore and build.
- [ ] Confirm exactly two local migrations.
- [ ] Run pending-model check using a safe local/dummy design-time connection.
- [ ] Do not list or print User Secrets.
- [ ] Inspect current `git status` for any leftover partial CompanyProjects files.
- [ ] Confirm Step 8A-1 partial files truly do not remain.

Recommended commands:

```powershell
cd F:\EstateHub
dotnet --version
dotnet ef --version
dotnet restore .\EstateHub.slnx
dotnet build .\EstateHub.slnx --no-restore
dotnet ef migrations list --no-connect `
  --project .\src\EstateHub.Infrastructure `
  --startup-project .\src\EstateHub.Api `
  --context EstateHubDbContext `
  --no-build
```

### Before testing company endpoints

- [ ] Identify an active verified company’s primary-contact user.
- [ ] Confirm the user can log in and has confirmed email.
- [ ] Call `POST /api/me/company-context/bootstrap-owner` once.
- [ ] Confirm company context now returns Owner and all 18 permissions.
- [ ] Test tenant isolation with a second company.

### Before database deployment

- [ ] Review generated migrations manually.
- [ ] Generate and inspect SQL script.
- [ ] Check migration history and conflicts read-only.
- [ ] Back up affected data where practical.
- [ ] Apply a named migration, never an ambiguous destructive command.
- [ ] Recheck model parity and aggregate counts.

---

## 19. Source-of-truth priority

When this handoff and the code disagree, use this order:

1. Current checked-in source and migration files.
2. Actual database migration history and schema.
3. Approved ERD/business decisions.
4. This handoff document.
5. Historical Codex reports.

Do not silently change business rules to make an implementation easier. Record decisions, update this handoff, and update tests/contracts together.

---

## 20. Final handoff status

```text
Architecture                         Complete
Domain model                         Complete: 49 entities / 33 enums
EF mappings                          Complete: 49 Domain configurations
Identity/JWT/email/rate limiting     Implemented
Initial schema migration             Applied
Permission catalog migration         Applied
Public discovery endpoints           Implemented
Customer self-service                Implemented
Company RBAC foundation               Implemented
Company profile/employees            Implemented
Company project management           Not started
Remaining business modules           Not implemented
Permanent automated tests            Not implemented
Runtime smoke-test pass               Not completed
API deployment                        Not confirmed
```

**Immediate continuation:** implement Step 8A-1A as six project core endpoints, verify build/model parity, then complete 8A-1B and 8A-1C as separate turns.
