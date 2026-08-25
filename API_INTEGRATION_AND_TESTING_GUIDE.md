# EstateHub API Integration and Testing Guide

This guide summarizes the current API flows for frontend, QA, backend, and AI developers. The [Master Reference](API_MASTER_REFERENCE.md) remains authoritative for endpoint-specific behavior.

## Runtime and frontend prerequisites

- The API is configured for stable .NET 10 and ASP.NET Core controllers.
- Startup fails fast unless `ConnectionStrings:DefaultConnection`, JWT options, email options, rate-limit windows, file-storage options, and at least one valid `Cors:AllowedOrigins` entry satisfy their option validators.
- CORS origins must be absolute origins, must equal their URI authority, and must not end in `/`. The named `EstateHubFrontend` policy allows any request header and method from configured origins and does **not** call `AllowCredentials`.
- Middleware order is routing → CORS → rate limiter → authentication → authorization → mapped controllers. Keep this order when integrating a browser frontend.
- Current committed CORS default is `http://localhost:5173`; production must supply the intended origin through configuration. An origin is scheme + host + optional port, not a path.
- Swagger is presently enabled by active middleware lines regardless of environment because the Development guard is commented. See the Findings document; this documentation step does not alter runtime behavior.

## Architecture traceability

Each controller depends on Application abstractions, not EF Core. The Infrastructure implementation owns projected EF queries, tenant filters, transactions and writes. Links below are the current source trace for every controller.

| Controller | Application abstraction(s) | Infrastructure implementation(s) |
|---|---|---|
| [`AuthController`](../src/EstateHub.Api/Controllers/AuthController.cs) | [`IAccountService`](../src/EstateHub.Application/Accounts/IAccountService.cs), [`IAuthenticationTokenService`](../src/EstateHub.Application/Authentication/IAuthenticationTokenService.cs) | [`AccountService`](../src/EstateHub.Infrastructure/Accounts/AccountService.cs), [`AuthenticationTokenService`](../src/EstateHub.Infrastructure/Authentication/AuthenticationTokenService.cs) |
| [`CatalogLookupsController`](../src/EstateHub.Api/Controllers/CatalogLookupsController.cs) | [`IPublicCatalogLookupService`](../src/EstateHub.Application/CatalogLookups/IPublicCatalogLookupService.cs) | [`PublicCatalogLookupService`](../src/EstateHub.Infrastructure/CatalogLookups/PublicCatalogLookupService.cs) |
| [`CompaniesController`](../src/EstateHub.Api/Controllers/CompaniesController.cs) | [`IPublicCompanyQueryService`](../src/EstateHub.Application/Companies/IPublicCompanyQueryService.cs) | [`PublicCompanyQueryService`](../src/EstateHub.Infrastructure/Companies/PublicCompanyQueryService.cs) |
| [`CompanyBillingController`](../src/EstateHub.Api/Controllers/CompanyBillingController.cs) | [`ICompanyBillingService`](../src/EstateHub.Application/Billing/ICompanyBillingService.cs) | [`CompanyBillingService`](../src/EstateHub.Infrastructure/Billing/CompanyBillingService.cs) |
| [`CompanyContextController`](../src/EstateHub.Api/Controllers/CompanyContextController.cs) | [`ICompanyAccessService`](../src/EstateHub.Application/CompanyAccess/ICompanyAccessService.cs), [`ICompanyRoleManagementService`](../src/EstateHub.Application/CompanyRoles/ICompanyRoleManagementService.cs) | [`CompanyAccessService`](../src/EstateHub.Infrastructure/CompanyAccess/CompanyAccessService.cs), [`CompanyRoleManagementService`](../src/EstateHub.Infrastructure/CompanyRoles/CompanyRoleManagementService.cs) |
| [`CompanyEmployeesController`](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs) | [`ICompanyManagementService`](../src/EstateHub.Application/CompanyManagement/ICompanyManagementService.cs) | [`CompanyManagementService`](../src/EstateHub.Infrastructure/CompanyManagement/CompanyManagementService.cs) |
| [`CompanyLeadsController`](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs) | [`ICompanyLeadManagementService`](../src/EstateHub.Application/CompanyLeads/ICompanyLeadManagementService.cs) | [`CompanyLeadManagementService`](../src/EstateHub.Infrastructure/CompanyLeads/CompanyLeadManagementService.cs) |
| [`CompanyListingPromotionsController`](../src/EstateHub.Api/Controllers/CompanyListingPromotionsController.cs) | [`IPromotionService`](../src/EstateHub.Application/Promotions/IPromotionService.cs) | [`PromotionService`](../src/EstateHub.Infrastructure/Promotions/PromotionService.cs) |
| [`CompanyListingsController`](../src/EstateHub.Api/Controllers/CompanyListingsController.cs) | [`ICompanyListingManagementService`](../src/EstateHub.Application/CompanyListings/ICompanyListingManagementService.cs) | [`CompanyListingManagementService`](../src/EstateHub.Infrastructure/CompanyListings/CompanyListingManagementService.cs) |
| [`CompanyProfileController`](../src/EstateHub.Api/Controllers/CompanyProfileController.cs) | [`ICompanyManagementService`](../src/EstateHub.Application/CompanyManagement/ICompanyManagementService.cs) | [`CompanyManagementService`](../src/EstateHub.Infrastructure/CompanyManagement/CompanyManagementService.cs) |
| [`CompanyProjectManagementController`](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs) | [`ICompanyProjectManagementService`](../src/EstateHub.Application/CompanyProjects/ICompanyProjectManagementService.cs) | [`CompanyProjectManagementService`](../src/EstateHub.Infrastructure/CompanyProjects/CompanyProjectManagementService.cs) |
| [`CompanyProjectsController`](../src/EstateHub.Api/Controllers/CompanyProjectsController.cs) | [`IPublicProjectQueryService`](../src/EstateHub.Application/Projects/IPublicProjectQueryService.cs) | [`PublicProjectQueryService`](../src/EstateHub.Infrastructure/Projects/PublicProjectQueryService.cs) |
| [`CompanyRolesController`](../src/EstateHub.Api/Controllers/CompanyRolesController.cs) | [`ICompanyRoleManagementService`](../src/EstateHub.Application/CompanyRoles/ICompanyRoleManagementService.cs) | [`CompanyRoleManagementService`](../src/EstateHub.Infrastructure/CompanyRoles/CompanyRoleManagementService.cs) |
| [`CompanySubscriptionsController`](../src/EstateHub.Api/Controllers/CompanySubscriptionsController.cs) | [`ISubscriptionService`](../src/EstateHub.Application/Subscriptions/ISubscriptionService.cs) | [`SubscriptionService`](../src/EstateHub.Infrastructure/Subscriptions/SubscriptionService.cs) |
| [`CompanyUnitsController`](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs) | [`ICompanyUnitManagementService`](../src/EstateHub.Application/CompanyUnits/ICompanyUnitManagementService.cs) | [`CompanyUnitManagementService`](../src/EstateHub.Infrastructure/CompanyUnits/CompanyUnitManagementService.cs) |
| [`CompanyViewingBookingsController`](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs) | [`ICompanyViewingBookingManagementService`](../src/EstateHub.Application/CompanyViewingBookings/ICompanyViewingBookingManagementService.cs) | [`CompanyViewingBookingManagementService`](../src/EstateHub.Infrastructure/CompanyViewingBookings/CompanyViewingBookingManagementService.cs) |
| [`CompanyViewingSlotsController`](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs) | [`ICompanyViewingSlotManagementService`](../src/EstateHub.Application/CompanyViewingSlots/ICompanyViewingSlotManagementService.cs) | [`CompanyViewingSlotManagementService`](../src/EstateHub.Infrastructure/CompanyViewingSlots/CompanyViewingSlotManagementService.cs) |
| [`FilesController`](../src/EstateHub.Api/Controllers/FilesController.cs) | [`IFileAssetService`](../src/EstateHub.Application/Files/IFileAssetService.cs) | [`FileAssetService`](../src/EstateHub.Infrastructure/Files/FileAssetService.cs) |
| [`ListingsController`](../src/EstateHub.Api/Controllers/ListingsController.cs) | [`IPublicListingQueryService`](../src/EstateHub.Application/Listings/IPublicListingQueryService.cs) | [`PublicListingQueryService`](../src/EstateHub.Infrastructure/Listings/PublicListingQueryService.cs) |
| [`MeController`](../src/EstateHub.Api/Controllers/MeController.cs) | [`ICustomerSelfService`](../src/EstateHub.Application/Customers/ICustomerSelfService.cs) | [`CustomerSelfService`](../src/EstateHub.Infrastructure/Customers/CustomerSelfService.cs) |
| [`MyCompanyApplicationsController`](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs) | [`ICompanyApplicationService`](../src/EstateHub.Application/CompanyApplications/ICompanyApplicationService.cs) | [`CompanyApplicationService`](../src/EstateHub.Infrastructure/CompanyApplications/CompanyApplicationService.cs) |
| [`MyFilesController`](../src/EstateHub.Api/Controllers/MyFilesController.cs) | [`IFileAssetService`](../src/EstateHub.Application/Files/IFileAssetService.cs) | [`FileAssetService`](../src/EstateHub.Infrastructure/Files/FileAssetService.cs) |
| [`MyNotificationsController`](../src/EstateHub.Api/Controllers/MyNotificationsController.cs) | [`INotificationService`](../src/EstateHub.Application/Notifications/INotificationService.cs) | [`NotificationService`](../src/EstateHub.Infrastructure/Notifications/NotificationService.cs) |
| [`MyReviewsController`](../src/EstateHub.Api/Controllers/MyReviewsController.cs) | [`IReviewService`](../src/EstateHub.Application/Reviews/IReviewService.cs) | [`ReviewService`](../src/EstateHub.Infrastructure/Reviews/ReviewService.cs) |
| [`MyViewingBookingsController`](../src/EstateHub.Api/Controllers/MyViewingBookingsController.cs) | [`ICustomerViewingBookingService`](../src/EstateHub.Application/ViewingBookings/CustomerViewingBookingModels.cs) | [`CustomerViewingBookingService`](../src/EstateHub.Infrastructure/ViewingBookings/CustomerViewingBookingService.cs) |
| [`PlatformCompanyApplicationsController`](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs) | [`IPlatformCompanyApplicationService`](../src/EstateHub.Application/PlatformCompanyApplications/IPlatformCompanyApplicationService.cs) | [`PlatformCompanyApplicationService`](../src/EstateHub.Infrastructure/PlatformCompanyApplications/PlatformCompanyApplicationService.cs) |
| [`PlatformReviewsController`](../src/EstateHub.Api/Controllers/PlatformReviewsController.cs) | [`IPlatformReviewModerationService`](../src/EstateHub.Application/PlatformReviews/IPlatformReviewModerationService.cs) | [`PlatformReviewModerationService`](../src/EstateHub.Infrastructure/PlatformReviews/PlatformReviewModerationService.cs) |
| [`PromotedListingsController`](../src/EstateHub.Api/Controllers/PromotedListingsController.cs) | [`IPromotionService`](../src/EstateHub.Application/Promotions/IPromotionService.cs) | [`PromotionService`](../src/EstateHub.Infrastructure/Promotions/PromotionService.cs) |
| [`PromotionPackagesController`](../src/EstateHub.Api/Controllers/PromotionPackagesController.cs) | [`IPromotionService`](../src/EstateHub.Application/Promotions/IPromotionService.cs) | [`PromotionService`](../src/EstateHub.Infrastructure/Promotions/PromotionService.cs) |
| [`PublicCompanyReviewsController`](../src/EstateHub.Api/Controllers/PublicCompanyReviewsController.cs) | [`IReviewService`](../src/EstateHub.Application/Reviews/IReviewService.cs) | [`ReviewService`](../src/EstateHub.Infrastructure/Reviews/ReviewService.cs) |
| [`PublicViewingSlotsController`](../src/EstateHub.Api/Controllers/PublicViewingSlotsController.cs) | [`IPublicViewingSlotQueryService`](../src/EstateHub.Application/ViewingBookings/PublicViewingSlotModels.cs) | [`PublicViewingSlotQueryService`](../src/EstateHub.Infrastructure/ViewingBookings/PublicViewingSlotQueryService.cs) |
| [`SubscriptionPlansController`](../src/EstateHub.Api/Controllers/SubscriptionPlansController.cs) | [`ISubscriptionService`](../src/EstateHub.Application/Subscriptions/ISubscriptionService.cs) | [`SubscriptionService`](../src/EstateHub.Infrastructure/Subscriptions/SubscriptionService.cs) |

### Persistence and Domain trace

Current source inventory is 49 Domain entities, 33 Domain enums, 49 EF configurations, four logical migrations and one model snapshot. The documentation reads this model but does not alter it.

| Feature | Primary persisted model and invariant themes |
|---|---|
| Identity/accounts | ApplicationUser, CustomerProfile, RefreshSession and SecurityEvent; confirmed/active account checks, Identity password/lockout, opaque refresh rotation and revocation. |
| Company tenancy/RBAC | Company, CompanyEmployee, CompanyRole, PermissionGroup, Permission, CompanyEmployeeRole and CompanyRolePermission; composite tenant-safe keys, active/revoked chains and deterministic permission catalog. |
| Public inventory | Company, Address, Location, Project, Unit, UnitType, Listing and Currency; active/verified/published/subscription eligibility, global listing slug, company/project scoping and explicit status text. |
| Media/places/amenities | FileAsset, ProjectMedia, ListingMedia, ProjectAmenity, UnitAmenity, Amenity and NearbyPlace; one-cover filtered uniqueness, composite joins, active/scope filters, ordered projections and decimal(9,6) coordinates. |
| Customer self-service | Favorite and SavedSearch plus CustomerProfile; subject-owned uniqueness and no caller-selected profile ID. |
| Viewing | ViewingSlot, ViewingBooking, BookingStatusHistory and BookingRescheduleHistory; capacity/active-booking uniqueness, rowversion and serializable contention paths. |
| CRM/reviews | Lead, LeadActivity, CustomerRequirement, LeadListingInterest and CompanyReview; tenant-safe ownership, source/XOR rules where configured, review one-to-one and moderation visibility. |
| Commercial/billing | SubscriptionPlan, CompanySubscription, PromotionPackage, ListingPromotion, BillingInvoice, BillingInvoiceLine, PaymentTransaction, BookingCharge and PaymentPlan; current-subscription/promotion/charge uniqueness, money precision, source checks and Restrict/NoAction history. |
| Applications/notifications | CompanyApplication, CompanyApplicationDocument, CompanyApplicationStatusHistory and Notification; applicant/platform ownership, status history, deterministic approval bootstrap and recipient-scoped notification state. |

The canonical EF mappings are under [Persistence/Configurations](../src/EstateHub.Infrastructure/Persistence/Configurations); the current snapshot is [EstateHubDbContextModelSnapshot.cs](../src/EstateHub.Infrastructure/Persistence/Migrations/EstateHubDbContextModelSnapshot.cs). SQL schema is authoritative only after the corresponding approved migrations are applied; database deployment state was not inspected in this documentation task.

## Authentication flows

### Customer registration and verification

1. Submit `POST /api/auth/register` with the exact request contract. Persona is strict textual Buyer/Renter/Agent; terms must be accepted.
2. Treat `202 Accepted` as “registration accepted and verification delivery attempted.” A delivery failure follows the source-defined 503 branch.
3. Confirm using `POST /api/auth/confirm-email` and the opaque user ID/code from the confirmation URL. Never decode or normalize the token client-side.
4. `POST /api/auth/resend-confirmation` intentionally returns a generic accepted result so clients cannot enumerate accounts.

### Login, refresh, and logout

1. `POST /api/auth/login` requires an active, confirmed account. A failed credential/account check is generic.
2. Store access/refresh tokens only in the client security design approved for the application. Do not place them in URLs, telemetry, screenshots, AI prompts, or logs.
3. The access token lifetime defaults to 15 minutes. Refresh lifetimes default to 7 days or 30 days when Remember Me applies.
4. `POST /api/auth/refresh` rotates the refresh token. Replace both client-held tokens atomically after a successful response; replaying a rotated/revoked token is rejected.
5. `POST /api/auth/logout` is deliberately idempotent for unknown/absent token state and returns no content.

### Password recovery

`forgot-password` is enumeration-safe and `reset-password` consumes the opaque Identity code. A successful reset revokes applicable session state in the account service transaction. Password defaults require at least eight characters, upper/lowercase and a digit; non-alphanumeric characters are not required.

### JWT validation

The bearer handler disables inbound claim remapping and validates issuer, audience, HMAC-SHA256 signature, signed-token requirement, expiration and lifetime with 30 seconds of clock skew. Authenticated application controllers independently require a non-empty GUID `sub`; a malformed/missing subject yields 401 or leaves authorization unsatisfied.

## Authorization and tenancy

### Personal endpoints

Routes under `/api/me` resolve the current `ApplicationUser` only from `sub`. Request payloads never select an ApplicationUser or CustomerProfile for the caller. Personal controllers use `Cache-Control: no-store`.

### Company permissions

The dynamic policy name is `CompanyPermission:<permission-code>`. Matching is ordinal and exact. The scoped handler calls `ICompanyAccessService.GetAuthorizedScopeAsync` for every authorization decision; it succeeds only for a current active employee, active/verified company and active non-revoked role/permission chain. Primary-contact and built-in-role flags do not bypass the permission check. No company ID is trusted from route/query/body and decisions are not cached.

| Permission code | Read/manage meaning | Current endpoint mappings |
|---|---|---:|
| `company.profile.read` | Read current-company profile | 1 |
| `company.profile.manage` | Update current-company profile | 1 |
| `company.employees.read` | Read employee directory/details | 2 |
| `company.employees.manage` | Employee lifecycle/role assignment | 7 |
| `company.roles.read` | Read roles/permission catalog | 3 |
| `company.roles.manage` | Role bootstrap/create/permission/deactivation | 4 |
| `company.projects.read` | Read owned projects | 2 |
| `company.projects.manage` | Project lifecycle/amenity/media/place writes | 9 |
| `company.units.read` | Read owned units | 2 |
| `company.units.manage` | Unit lifecycle/amenity/place writes | 7 |
| `company.listings.read` | Read owned listings | 2 |
| `company.listings.manage` | Listing lifecycle/media/payment-plan writes | 8 |
| `company.bookings.read` | Read viewing slots/bookings | 4 |
| `company.bookings.manage` | Viewing-slot and booking mutations | 13 |
| `company.leads.read` | Lead/detail/activity reads | 3 |
| `company.leads.manage` | Lead/stage/requirement/interest/activity writes | 9 |
| `company.billing.read` | Billing, subscription and promotion reads | 9 |
| `company.billing.manage` | Checkout/cancel/resume/settlement writes | 6 |

Multiple permission attributes follow ASP.NET Core AND semantics. Unknown/malformed policy names delegate to the default provider and fail closed.

### Platform administration

The `PlatformAdmin` policy is separate from company RBAC. Its scoped handler rechecks the deterministic Identity role through `IPlatformAccessService` using the current GUID subject on every request. Current platform actions number 11: eight company-application review routes and three company-review moderation routes. Company permissions never imply platform access.

## Rate limiting

Exactly five fixed-window policies are registered and currently used only by the eight Auth actions. Partitions are the remote IP mapped to its IPv6 string, or `unknown` when absent. Queue limit is zero; rejection is 429 `application/problem+json` with title “Too many requests.” and a `Retry-After` header when the limiter supplies one.

| Policy | Current configured permits | Window | Auth actions |
|---|---:|---:|---|
| `auth-registration` | 5 | 600 seconds | register |
| `auth-login` | 10 | 60 seconds | login |
| `auth-email-delivery` | 3 | 900 seconds | resend-confirmation, forgot-password |
| `auth-token-lifecycle` | 30 | 60 seconds | refresh, logout |
| `auth-verification` | 10 | 600 seconds | confirm-email, reset-password |

Configuration permits are validated in the range 1–1000 and windows in 1–86400 seconds. Reverse proxies must preserve the intended remote-IP behavior; forwarded-header configuration is not specified by current source.

## Public eligibility and privacy

### Cache-Control inventory

| Scope | Current behavior |
|---|---|
| Auth login and refresh | Set Cache-Control: no-store on the successful token response. |
| Personal controllers | Me, MyCompanyApplications, MyFiles, MyNotifications, MyReviews, and MyViewingBookings apply controller-level no-store. |
| Company controllers | Context, Profile, Roles, Employees, ProjectManagement, Units, Listings, ViewingSlots, ViewingBookings, Leads, Billing, ListingPromotions, and Subscriptions apply controller-level no-store. |
| Platform controllers | CompanyApplications and Reviews apply controller-level no-store. |
| Public file content | A successful public image response sets Cache-Control: public, max-age=3600. |
| Other public/auth actions | No explicit Cache-Control contract is specified by the current controller source. |

- Public companies/projects/reviews/catalogs apply their service-level active/verified/published filters before projection.
- Public listings, listing details, promoted listings and public viewing slots share `PublicListingEligibility`: published listing; active, verified owner company; active company subscription; and for project units a published project owned by an active, verified Developer.
- Unavailable/nonexistent resources use generic not-found behavior where the controller specifies it. Do not reveal which eligibility predicate failed.
- File public access returns only eligible public images. Owner and platform document access use authenticated scoped services. Storage keys, physical paths, uploader/security metadata and original filenames are not public-image metadata.
- Public responses are projections. Fields absent from a response—tenant IDs, internal statuses, rowversions, user data, StorageKey, AI scores, ROI—must not be inferred.

## Pagination, filtering, enums, and rowversions

- Defaults and bounds are endpoint-specific and fully listed in the Master Reference. Most directories use `pageNumber=1`, `pageSize=20`, maximum page number 10,000 and maximum page size 50, but callers must use each action’s documented validation.
- Filtering and ordering occur before SQL projection. Paged totals describe the already-authorized/eligible query.
- There is no global `JsonStringEnumConverter`. Controllers use focused parsers and explicit output mappers. Treat spelling/case/trim behavior as endpoint-specific and never send numeric enum strings.
- Mutation rowversions are Base64 strings which commonly must decode to exactly eight bytes. Preserve them byte-for-byte from the latest response. A stale value maps to 409 when the service/controller exposes optimistic-concurrency conflict.
- Timestamps are `DateTimeOffset`; production writes use injected `TimeProvider` where the implementation needs current UTC time.

## Lifecycle map

| Area | Source-backed transitions/actions |
|---|---|
| Projects | Create as Draft; publish; archive; published slug immutability and archived-write restrictions are enforced by the service. |
| Units | Create/update; change explicit market status; maintain unit amenities and nearby places. |
| Listings | Create/update; replace media; create/update/delete active payment plans; direct publish subject to shared subscription-aware eligibility; archive. |
| Viewing slots | Create/update; close; reopen; cancel. Capacity/reservation and temporal rules are service-enforced. |
| Customer bookings | Create under serializable capacity protection; customer cancellation with rowversion. |
| Company bookings | Assign employee; confirm/reject/check-in/complete/no-show/cancel/reschedule with history, concurrency and notification behavior. |
| Leads | Create/update; change stage; requirement create/update/confirm/delete; replace listing interests; append activities. |
| Subscriptions | Checkout under serializable company lock; cancel; resume; current/history reads. |
| Promotions | Checkout under serializable protection; cancel; public placement reads only active eligible promotions/listings. |
| Company applications | Applicant create/update/document replacement/submit; platform start review, verify document, request changes, reject and approve. Approval creates the approved company/employee/role state atomically. |
| Company reviews | Customer create/update/delete; platform moderation updates visibility/status and notifies the author. |

## Transactions and consistency

Current Infrastructure source contains 61 explicit `BeginTransactionAsync` sites. Serializable transactions are used for contention-sensitive creation/checkout/approval/capacity workflows, including customer booking creation, company booking rescheduling, subscription checkout, promotion checkout, billing charge checkout, company-application create/update/submit/approval, primary-contact transfer, owner bootstrap, listing publication, and safe file deletion. Other explicit transactions group related entity/history/notification/media/amenity/role/lead/review writes. Operations without an explicit transaction rely on one EF `SaveChanges` atomic unit per save.

Never implement client retries for mutations blindly: re-read state after timeouts/conflicts, compare rowversion/current lifecycle, and retry only when the operation is safe. Database exceptions and cancellation are not generally swallowed into success.
### Explicit transaction catalog

The table is generated from every current Infrastructure BeginTransactionAsync call. Serializable identifies methods whose call explicitly requests IsolationLevel.Serializable; other listed methods use the provider/default isolation level.

| Infrastructure service | Methods with explicit transaction | Explicitly serializable methods |
|---|---|---|
| [`AccountService`](../src/EstateHub.Infrastructure/Accounts/AccountService.cs) | `RegisterCustomerAsync`, `ResetPasswordAsync` | — |
| [`AuthenticationTokenService`](../src/EstateHub.Infrastructure/Authentication/AuthenticationTokenService.cs) | `RotateRefreshTokenAsync` | — |
| [`CompanyApplicationService`](../src/EstateHub.Infrastructure/CompanyApplications/CompanyApplicationService.cs) | `CreateApplicationAsync`, `ReplaceDocumentsAsync`, `SubmitApplicationAsync`, `UpdateApplicationAsync` | `CreateApplicationAsync`, `SubmitApplicationAsync`, `UpdateApplicationAsync` |
| [`CompanyBillingService`](../src/EstateHub.Infrastructure/Billing/CompanyBillingService.cs) | `CheckoutBookingChargesAsync` | `CheckoutBookingChargesAsync` |
| [`CompanyLeadManagementService`](../src/EstateHub.Infrastructure/CompanyLeads/CompanyLeadManagementService.cs) | `ChangeStageAsync`, `ConfirmRequirementAsync`, `CreateActivityAsync`, `CreateLeadAsync`, `DeleteRequirementAsync`, `ReplaceInterestsAsync`, `RequirementMutationAsync`, `UpdateLeadAsync` | — |
| [`CompanyListingManagementService`](../src/EstateHub.Infrastructure/CompanyListings/CompanyListingManagementService.cs) | `ArchiveListingAsync`, `DeletePaymentPlanAsync`, `PublishListingAsync`, `ReplaceMediaAsync`, `SaveAsync`, `SavePaymentPlanAsync` | `PublishListingAsync` |
| [`CompanyManagementService`](../src/EstateHub.Infrastructure/CompanyManagement/CompanyManagementService.cs) | `EndEmployeeAsync`, `ReplaceEmployeeRolesAsync`, `TransferPrimaryContactAsync`, `UpdateProfileAsync` | `TransferPrimaryContactAsync` |
| [`CompanyProjectManagementService`](../src/EstateHub.Infrastructure/CompanyProjects/CompanyProjectManagementService.cs) | `DeleteNearbyPlaceAsync`, `MutateNearbyPlace`, `ReplaceAmenitiesAsync`, `ReplaceMediaAsync` | — |
| [`CompanyRoleManagementService`](../src/EstateHub.Infrastructure/CompanyRoles/CompanyRoleManagementService.cs) | `BootstrapOwnerAsync`, `CreateRoleAsync`, `DeactivateRoleAsync`, `ReplaceRolePermissionsAsync` | `BootstrapOwnerAsync` |
| [`CompanyUnitManagementService`](../src/EstateHub.Infrastructure/CompanyUnits/CompanyUnitManagementService.cs) | `DeleteNearbyPlaceAsync`, `MutateNearbyPlaceAsync`, `ReplaceAmenitiesAsync` | — |
| [`CompanyViewingBookingManagementService`](../src/EstateHub.Infrastructure/CompanyViewingBookings/CompanyViewingBookingManagementService.cs) | `AssignEmployeeAsync`, `ConfirmAsync`, `RescheduleAsync`, `TransitionAsync` | `RescheduleAsync` |
| [`CompanyViewingSlotManagementService`](../src/EstateHub.Infrastructure/CompanyViewingSlots/CompanyViewingSlotManagementService.cs) | `CreateSlotAsync`, `TransitionAsync`, `UpdateSlotAsync` | — |
| [`CustomerSelfService`](../src/EstateHub.Infrastructure/Customers/CustomerSelfService.cs) | `UpdateProfileAsync` | — |
| [`CustomerViewingBookingService`](../src/EstateHub.Infrastructure/ViewingBookings/CustomerViewingBookingService.cs) | `CancelBookingAsync`, `CreateBookingAsync` | `CreateBookingAsync` |
| [`FileAssetService`](../src/EstateHub.Infrastructure/Files/FileAssetService.cs) | `DeleteOwnedFileAsync` | `DeleteOwnedFileAsync` |
| [`PlatformCompanyApplicationService`](../src/EstateHub.Infrastructure/PlatformCompanyApplications/PlatformCompanyApplicationService.cs) | `ApproveAsync`, `RejectAsync`, `RequestChangesAsync`, `TransitionAsync`, `VerifyDocumentAsync` | `ApproveAsync` |
| [`PlatformReviewModerationService`](../src/EstateHub.Infrastructure/PlatformReviews/PlatformReviewModerationService.cs) | `ModerateAsync` | — |
| [`PromotionService`](../src/EstateHub.Infrastructure/Promotions/PromotionService.cs) | `CancelAsync`, `CheckoutAsync` | `CheckoutAsync` |
| [`ReviewService`](../src/EstateHub.Infrastructure/Reviews/ReviewService.cs) | `CreateReviewAsync`, `DeleteReviewAsync`, `UpdateReviewAsync` | — |
| [`SubscriptionService`](../src/EstateHub.Infrastructure/Subscriptions/SubscriptionService.cs) | `ChangeCancellationAsync`, `CheckoutAsync` | `CheckoutAsync` |

## Notifications

Five personal routes list/count/read/read-all/archive notifications. Notification type codes currently cover company-application status changes, viewing-booking status changes, viewing-booking rescheduling and company-review moderation. Writers validate allowed type/title/body/link shapes and add notifications to the caller’s surrounding EF unit/transaction; they are not a separate out-of-band delivery guarantee. A mutation success response is authoritative even if the frontend has not yet refreshed notifications.

## Error handling

| Status | Client meaning |
|---:|---|
| 400 | Binding/validation or an explicitly rejected request contract. Read `ValidationProblemDetails.errors` or generic ProblemDetails. |
| 401 | Missing/invalid/expired bearer token or malformed/missing GUID subject. Refresh once when appropriate; otherwise sign in. |
| 403 | Authenticated principal lacks current company permission or platform role. Do not retry as another tenant. |
| 404 | Generic unavailable/not-owned/not-public/notexistent result. Do not use it to infer hidden state. |
| 409 | Lifecycle, uniqueness, optimistic concurrency, dependency or race conflict. Re-read before retrying. |
| 429 | Fixed-window auth rate limit. Honor `Retry-After` when present. |
| 503 | Source-defined temporary delivery/persistence/storage failure. Do not reinterpret it as validation or not-found. |

Unhandled database, SMTP, storage and cancellation failures propagate through normal ASP.NET behavior unless the endpoint entry identifies a translated status. Do not parse English titles as stable machine codes unless the API introduces such a contract later.

## Frontend integration checklist

1. Configure the frontend origin exactly in `Cors:AllowedOrigins`; do not add a trailing slash.
2. Use `Authorization: Bearer <access-token>` for every personal/company/platform route.
3. Centralize 401 refresh rotation and ensure only one refresh occurs at a time.
4. Keep customer/company/platform navigation distinct; use 403 rather than UI assumptions as the final permission decision.
5. Bind filters to the exact textual enum values and paging limits in the endpoint entry.
6. Preserve IDs, slugs, tokens and rowversions exactly.
7. Treat no-store payloads as sensitive and avoid durable caches/service-worker storage.
8. Use file content endpoints as streams; do not manufacture a StorageKey URL.
9. Refresh detail/list/notification state after successful mutations.
10. Never expose ProblemDetails, tokens, personal fields or file metadata to analytics/AI systems without an approved data policy.

## Manual QA suites

- **Route registry:** assert all 164 distinct verb/route pairs and zero duplicates.
- **Authentication:** registration/confirmation/resend/login/refresh rotation/logout/password reset, lockout, confirmation requirement and all five 429 policies.
- **Subject integrity:** absent, empty, malformed and empty-GUID `sub` on personal/company/platform controllers.
- **Company RBAC:** each of 18 exact codes; suspension, deactivation, verification, revocation and cross-company denial take effect on the next request.
- **Platform role:** deterministic active role assignment succeeds; company roles do not.
- **Read projections:** deterministic ordering/paging, generic not-found, inactive/unpublished/subscription-ineligible exclusion, and forbidden-field absence.
- **Mutations:** lifecycle guards, tenant ownership, reference validation, duplicate constraints, stale rowversion, cancellation and translated 409/503 paths.
- **Transactions:** force a late failure and verify no partial history/notification/assignment/media/catalog write remains.
- **Files:** size and magic bytes, filename normalization, traversal attempts, owner scope, public-image eligibility, referenced-delete rejection and storage/database compensation.
- **Notifications:** recipient isolation, unread count, idempotent read/read-all/archive and atomic creation with source mutation.
- **CORS:** allowed origin preflight succeeds; unconfigured origin lacks CORS authorization; invalid startup origin configuration fails fast.
- **Privacy:** no secrets, credentials, StorageKey, internal tenant IDs, hidden eligibility reason or unrelated user/company data in success or errors.

## Guidance for AI-assisted clients and test generation

AI may generate typed clients, fixtures and assertions from the Master, Matrix and Contracts files, but must:

- use the Matrix as the route allow-list and never invent an endpoint;
- use the contract catalog’s nullability/types and the Master’s endpoint-specific validation together;
- generate placeholder secrets/tokens/IDs only;
- preserve generic security responses instead of “helpfully” distinguishing hidden states;
- avoid fabricating financial projections, AI scores, ROI, recommendations or file URLs;
- mark behavior not provable from source as **Not specified by current source.**;
- require human review for mutation retry logic, authorization, PII handling, payment/billing and destructive file/delete workflows.

## Documentation verification

The final audit compares normalized `VERB /complete/route` keys from controller and action attributes against Master headings and Matrix rows. It separately verifies contract declarations, 33 enums, 18 permission codes, five rate-limit policies, Markdown links/tables, and duplicate/missing/nonexistent routes.
