# EstateHub API Contracts Reference

This reference describes reusable API request and response shapes. Endpoint-specific validation and business rules remain in the [Master Reference](API_MASTER_REFERENCE.md).

## Wire conventions

| Concept | Wire representation | Rules |
|---|---|---|
| GUID | JSON string / route token | Canonical GUID parsing; empty GUIDs are rejected where explicitly validated. |
| Date/time | ISO-8601 string | `DateTimeOffset`; UTC offset is required by date filters where controllers enforce it. |
| Date | `YYYY-MM-DD` | `DateOnly` where used. |
| Money/decimal | JSON number | No currency conversion; precision follows EF mappings and endpoint validation. |
| Rowversion | Base64 string | Eight decoded bytes where controller validation is present; preserve values exactly between read and mutation. |
| Enum | JSON/query string | Explicit API text mapping; accepted casing is stated per endpoint. Do not send numeric enum values unless source explicitly permits them. |
| Problem | `application/problem+json` | `ProblemDetails` or `ValidationProblemDetails`; callers must not infer hidden eligibility/ownership facts from generic errors. |


## Batch 1 contracts

### Authentication requests

| Contract | Fields and source-backed constraints |
|---|---|
| `RegisterCustomerRequest` | `fullName` required after trim; `email` required and syntactically valid; `password` required and passed to Identity; `phoneNumber` optional; `persona` must be textual Buyer, Renter, or Agent (case-insensitive, numeric strings rejected). |
| `ConfirmEmailRequest` | Non-empty `userId`; nonblank encoded `code` with controller maximum. |
| `ResendConfirmationRequest`, `ForgotPasswordRequest` | Required syntactically valid `email`; enumeration-safe result. |
| `LoginRequest` | Required valid `email` and nonblank `password`. |
| `RefreshTokenRequest` | Refresh/logout token string; refresh rejects blank/oversized, logout preserves idempotent semantics. |
| `ResetPasswordRequest` | Non-empty `userId`, nonblank bounded `code`, required password/confirmation interpreted by the account service. |

### Authentication token response

`AuthenticationTokenResponse` contains the bearer access token, its expiration, the opaque refresh token, and its expiration. Tokens are secrets: keep them out of logs, URLs, analytics, and AI prompts.

### Public directory contracts

| Family | Reusable fields |
|---|---|
| Catalog lookups | Locations expose identifier, hierarchy/type and localized names/slugs; unit types expose id/code/localized names; currencies expose code/name/symbol/decimal places; amenities expose id/code/localized names/icon/scope. |
| Company directory/details | Public id, slug, display/profile/contact/logo fields plus projected type and public project/listing counts where defined. Internal status, registration/tax, rowversion and employee data are excluded. |
| Project directory/details | Public id/slug/name, location, delivery state/dates, developer/company summary and approved media/amenity/place projections. Internal project status and rowversion are excluded. |
| Listing directory/details | Public listing, currency, company, unit/unit-type/location/project summaries; detail adds ordered media, active payment plans, separate unit/project amenities and nearby places. No storage key, internal status, AI score, ROI, lead, booking or billing data. |
| Promoted listings | Placement-filtered paged listing summaries using the same public eligibility predicate and active promotion window/package rules. |
| Paged responses | Items plus `pageNumber`, `pageSize`, `totalCount`, and derived `totalPages`; caller must not infer totals outside applied public/tenant filters. |

### Batch 1 enum text

| Context | Accepted/emitted values |
|---|---|
| Customer persona | Buyer, Renter, Agent |
| Company type filter | Developer, BrokerAgency |
| Listing type | Sale, Rent |
| Rent period | Monthly, Yearly |
| Location type | Country, Governorate, City, District |
| Finishing | Unfinished, SemiFinished, Finished |
| Furnishing | Unfurnished, SemiFurnished, Furnished |
| Unit market status | Available, Reserved, Sold, Rented, Withdrawn |
| Project delivery | Planned, UnderConstruction, ReadyToMove, Delivered |
| Installment frequency | Monthly, Quarterly, SemiAnnual, Annual |

## Batch 2 contracts

The route table below is a source index, not a substitute for endpoint validation in the Master Reference. Contract types are framework-independent Application models or API transport records unless the action uses scalar/query binding.

| Route | Bound request/query signature | Declared action result | Application service call(s) |
|---|---|---|---|
| `GET /api/me/profile` | `None` | `Task<ActionResult<CustomerProfileResponse>>` | `customerSelfService.GetProfileAsync |
| `PUT /api/me/profile` | `UpdateCustomerProfileRequest request` | `Task<ActionResult<CustomerProfileResponse>>` | `customerSelfService.UpdateProfileAsync |
| `GET /api/me/favorites` | `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20` | `Task<ActionResult<CustomerFavoritesResponse>>` | `customerSelfService.GetFavoritesAsync |
| `PUT /api/me/favorites/{listingId}` | `Guid listingId` | `Task<IActionResult>` | `customerSelfService.AddFavoriteAsync |
| `DELETE /api/me/favorites/{listingId}` | `Guid listingId` | `Task<IActionResult>` | `customerSelfService.RemoveFavoriteAsync |
| `GET /api/me/saved-searches` | `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20` | `Task<ActionResult<SavedSearchesResponse>>` | `customerSelfService.GetSavedSearchesAsync |
| `POST /api/me/saved-searches` | `SaveSearchRequest request` | `Task<ActionResult<SavedSearchResponse>>` | `customerSelfService.CreateSavedSearchAsync |
| `PUT /api/me/saved-searches/{savedSearchId}` | `Guid savedSearchId, SaveSearchRequest request` | `Task<ActionResult<SavedSearchResponse>>` | `customerSelfService.UpdateSavedSearchAsync |
| `DELETE /api/me/saved-searches/{savedSearchId}` | `Guid savedSearchId` | `Task<IActionResult>` | `customerSelfService.RemoveSavedSearchAsync |
| `POST /api/me/files` | `[FromForm] UploadFileAssetRequest request` | `Task<ActionResult<FileAssetMetadataResponse>>` | `service.UploadAsync |
| `GET /api/me/files` | `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? fileType = null, [FromQuery] string? search = null` | `Task<ActionResult<FileAssetDirectoryResponse>>` | `service.GetOwnedFilesAsync |
| `GET /api/me/files/{fileAssetId}` | `Guid fileAssetId` | `Task<ActionResult<FileAssetMetadataResponse>>` | `service.GetOwnedFileAsync |
| `GET /api/me/files/{fileAssetId}/content` | `Guid fileAssetId` | `Task<IActionResult>` | `service.GetOwnedContentAsync |
| `DELETE /api/me/files/{fileAssetId}` | `Guid fileAssetId` | `Task<IActionResult>` | `service.DeleteOwnedFileAsync |
| `GET /api/me/notifications` | `[FromQuery] NotificationDirectoryRequest request` | `Task<ActionResult<NotificationDirectoryResponse>>` | `notificationService.GetNotificationsAsync |
| `GET /api/me/notifications/unread-count` | `None` | `Task<ActionResult<NotificationUnreadCountResponse>>` | `notificationService.GetUnreadCountAsync |
| `PUT /api/me/notifications/{notificationId}/read` | `Guid notificationId` | `Task<IActionResult>` | `notificationService.MarkAllReadAsync |
| `PUT /api/me/notifications/read-all` | `None` | `Task<IActionResult>` | `notificationService.MarkAllReadAsync |
| `PUT /api/me/notifications/{notificationId}/archive` | `Guid notificationId` | `Task<IActionResult>` | ` |
| `GET /api/me/viewing-bookings/{bookingId}/review` | `Guid bookingId` | `Task<ActionResult<CustomerReviewResponse>>` | `reviewService.GetCustomerReviewAsync |
| `POST /api/me/viewing-bookings/{bookingId}/review` | `Guid bookingId, ReviewRequest request` | `Task<ActionResult<CustomerReviewResponse>>` | `reviewService.CreateReviewAsync |
| `PUT /api/me/reviews/{reviewId}` | `Guid reviewId, ReviewRequest request` | `Task<ActionResult<CustomerReviewResponse>>` | `reviewService.UpdateReviewAsync |
| `DELETE /api/me/reviews/{reviewId}` | `Guid reviewId` | `Task<IActionResult>` | `reviewService.DeleteReviewAsync |
| `GET /api/me/viewing-bookings` | `[FromQuery] CustomerViewingBookingDirectoryRequest request` | `Task<ActionResult<CustomerViewingBookingsResponse>>` | `service.GetBookingsAsync |
| `GET /api/me/viewing-bookings/{bookingId}` | `Guid bookingId` | `Task<ActionResult<CustomerViewingBookingDetailsResponse>>` | `service.GetBookingAsync |
| `POST /api/me/viewing-bookings` | `CreateCustomerViewingBookingRequest request` | `Task<ActionResult<CustomerViewingBookingDetailsResponse>>` | `service.CreateBookingAsync |
| `POST /api/me/viewing-bookings/{bookingId}/cancel` | `Guid bookingId, CancelCustomerViewingBookingRequest request` | `Task<IActionResult>` | `service.CancelBookingAsync |

## Batch 3 contracts

The route table below is a source index, not a substitute for endpoint validation in the Master Reference. Contract types are framework-independent Application models or API transport records unless the action uses scalar/query binding.

| Route | Bound request/query signature | Declared action result | Application service call(s) |
|---|---|---|---|
| `GET /api/me/company-context` | `None` | `Task<ActionResult<CompanyAccessContextResponse>>` | `companyAccessService.GetCurrentContextAsync |
| `POST /api/me/company-context/bootstrap-owner` | `None` | `Task<IActionResult>` | `companyRoleManagementService.BootstrapOwnerAsync |
| `GET /api/company/profile` | `None` | `Task<ActionResult<CompanyProfileResponse>>` | `companyManagementService.GetProfileAsync |
| `PUT /api/company/profile` | `UpdateCompanyProfileRequest request` | `Task<ActionResult<CompanyProfileResponse>>` | `companyManagementService.UpdateProfileAsync |
| `GET /api/company/permissions` | `None` | `Task<ActionResult<IReadOnlyList<CompanyPermissionGroupResponse>>>` | `companyRoleManagementService.GetPermissionsAsync |
| `GET /api/company/roles` | `[FromQuery] bool includeInactive = false` | `Task<ActionResult<IReadOnlyList<CompanyRoleSummaryResponse>>>` | `companyRoleManagementService.GetRolesAsync |
| `GET /api/company/roles/{roleId}` | `Guid roleId` | `Task<ActionResult<CompanyRoleDetailsResponse>>` | `companyRoleManagementService.GetRoleAsync |
| `POST /api/company/roles` | `CreateCompanyRoleRequest request` | `Task<ActionResult<CompanyRoleDetailsResponse>>` | `companyRoleManagementService.CreateRoleAsync |
| `PUT /api/company/roles/{roleId}` | `Guid roleId, UpdateCompanyRoleRequest request` | `Task<ActionResult<CompanyRoleDetailsResponse>>` | `companyRoleManagementService.UpdateRoleAsync |
| `PUT /api/company/roles/{roleId}/permissions` | `Guid roleId, ReplaceCompanyRolePermissionsRequest request` | `Task<ActionResult<CompanyRoleDetailsResponse>>` | `companyRoleManagementService.ReplaceRolePermissionsAsync |
| `DELETE /api/company/roles/{roleId}` | `Guid roleId` | `Task<IActionResult>` | `companyRoleManagementService.DeactivateRoleAsync |
| `GET /api/company/employees` | `[FromQuery] EmployeeDirectoryRequest request` | `Task<ActionResult<CompanyEmployeesResponse>>` | `companyManagementService.GetEmployeesAsync |
| `GET /api/company/employees/{employeeId}` | `Guid employeeId` | `Task<ActionResult<CompanyEmployeeResponse>>` | `companyManagementService.GetEmployeeAsync |
| `POST /api/company/employees` | `AddCompanyEmployeeRequest request` | `Task<ActionResult<CompanyEmployeeResponse>>` | `companyManagementService.AddEmployeeAsync |
| `PUT /api/company/employees/{employeeId}` | `Guid employeeId, UpdateCompanyEmployeeRequest request` | `Task<ActionResult<CompanyEmployeeResponse>>` | `companyManagementService.UpdateEmployeeAsync |
| `POST /api/company/employees/{employeeId}/suspend` | `Guid employeeId, EmployeeRowVersionRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/employees/{employeeId}/activate` | `Guid employeeId, EmployeeRowVersionRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/employees/{employeeId}/end` | `Guid employeeId, EmployeeRowVersionRequest request` | `Task<IActionResult>` | `companyManagementService.EndEmployeeAsync |
| `PUT /api/company/employees/{employeeId}/roles` | `Guid employeeId, ReplaceCompanyEmployeeRolesRequest request` | `Task<ActionResult<CompanyEmployeeResponse>>` | `companyManagementService.ReplaceEmployeeRolesAsync |
| `PUT /api/company/employees/{employeeId}/primary-contact` | `Guid employeeId, TransferPrimaryContactRequest request` | `Task<IActionResult>` | `companyManagementService.TransferPrimaryContactAsync |

## Batch 4 contracts

The route table below is a source index, not a substitute for endpoint validation in the Master Reference. Contract types are framework-independent Application models or API transport records unless the action uses scalar/query binding.

| Route | Bound request/query signature | Declared action result | Application service call(s) |
|---|---|---|---|
| `GET /api/company/projects` | `[FromQuery]CompanyProjectDirectoryRequest r` | `Task<ActionResult<CompanyProjectsResponse>>` | `svc.GetProjectsAsync |
| `GET /api/company/projects/{projectId}` | `Guid projectId` | `Task<ActionResult<CompanyProjectManagementDetailsResponse>>` | `svc.GetProjectAsync |
| `POST /api/company/projects` | `CompanyProjectRequest r` | `Task<ActionResult<CompanyProjectResponse>>` | ` |
| `PUT /api/company/projects/{projectId}` | `Guid projectId,CompanyProjectRequest r` | `Task<ActionResult<CompanyProjectResponse>>` | ` |
| `POST /api/company/projects/{projectId}/publish` | `Guid projectId,RowVersionRequest r` | `Task<IActionResult>` | ` |
| `POST /api/company/projects/{projectId}/archive` | `Guid projectId,RowVersionRequest r` | `Task<IActionResult>` | `svc.CreateProjectAsync, svc.UpdateProjectAsync |
| `PUT /api/company/projects/{projectId}/amenities` | `Guid projectId,ReplaceProjectAmenitiesRequest request` | `Task<ActionResult<ReplaceProjectAmenitiesResponse>>` | `svc.ReplaceAmenitiesAsync |
| `PUT /api/company/projects/{projectId}/media` | `Guid projectId,ReplaceProjectMediaRequest request` | `Task<ActionResult<ReplaceProjectMediaResponse>>` | `svc.ReplaceMediaAsync |
| `POST /api/company/projects/{projectId}/nearby-places` | `Guid projectId,ProjectNearbyPlaceRequest request` | `Task<ActionResult<ProjectNearbyPlaceMutationResponse>>` | `svc.CreateNearbyPlaceAsync |
| `PUT /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}` | `Guid projectId,Guid nearbyPlaceId,ProjectNearbyPlaceRequest request` | `Task<ActionResult<ProjectNearbyPlaceMutationResponse>>` | `svc.UpdateNearbyPlaceAsync |
| `DELETE /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}` | `Guid projectId,Guid nearbyPlaceId,[FromQuery]string? rowVersion` | `Task<IActionResult>` | `svc.DeleteNearbyPlaceAsync |
| `GET /api/company/units` | `[FromQuery] CompanyUnitDirectoryRequest request` | `Task<ActionResult<CompanyUnitsResponse>>` | `service.GetUnitsAsync |
| `GET /api/company/units/{unitId}` | `Guid unitId` | `Task<ActionResult<CompanyUnitManagementDetailsResponse>>` | `service.GetUnitAsync |
| `POST /api/company/units` | `CreateCompanyUnitRequest request` | `Task<ActionResult<CompanyUnitDetailsResponse>>` | `service.CreateUnitAsync |
| `PUT /api/company/units/{unitId}` | `Guid unitId, UpdateCompanyUnitRequest request` | `Task<ActionResult<CompanyUnitDetailsResponse>>` | `service.UpdateUnitAsync |
| `PUT /api/company/units/{unitId}/status` | `Guid unitId, UpdateCompanyUnitStatusRequest request` | `Task<ActionResult<CompanyUnitStatusResponse>>` | `service.UpdateUnitStatusAsync |
| `PUT /api/company/units/{unitId}/amenities` | `Guid unitId, ReplaceCompanyUnitAmenitiesRequest request` | `Task<ActionResult<ReplaceCompanyUnitAmenitiesResponse>>` | `service.ReplaceAmenitiesAsync |
| `POST /api/company/units/{unitId}/nearby-places` | `Guid unitId, CompanyUnitNearbyPlaceRequest request` | `Task<ActionResult<CompanyUnitNearbyPlaceMutationResponse>>` | `service.CreateNearbyPlaceAsync |
| `PUT /api/company/units/{unitId}/nearby-places/{nearbyPlaceId}` | `Guid unitId, Guid nearbyPlaceId, CompanyUnitNearbyPlaceRequest request` | `Task<ActionResult<CompanyUnitNearbyPlaceMutationResponse>>` | `service.UpdateNearbyPlaceAsync |
| `DELETE /api/company/units/{unitId}/nearby-places/{nearbyPlaceId}` | `Guid unitId, Guid nearbyPlaceId, [FromQuery] string? rowVersion` | `Task<IActionResult>` | `service.DeleteNearbyPlaceAsync |
| `GET /api/company/listings` | `[FromQuery] CompanyListingDirectoryRequest request` | `Task<ActionResult<CompanyListingsResponse>>` | `service.GetListingsAsync |
| `GET /api/company/listings/{listingId}` | `Guid listingId` | `Task<ActionResult<CompanyListingDetailsResponse>>` | `service.GetListingAsync |
| `POST /api/company/listings` | `CreateCompanyListingRequest request` | `Task<ActionResult<CompanyListingDetailsResponse>>` | `service.CreateListingAsync |
| `PUT /api/company/listings/{listingId}` | `Guid listingId, UpdateCompanyListingRequest request` | `Task<ActionResult<CompanyListingDetailsResponse>>` | `service.UpdateListingAsync |
| `PUT /api/company/listings/{listingId}/media` | `Guid listingId, ReplaceCompanyListingMediaRequest request` | `Task<ActionResult<ReplaceCompanyListingMediaResponse>>` | `service.ReplaceMediaAsync |
| `POST /api/company/listings/{listingId}/payment-plans` | `Guid listingId, CreateCompanyListingPaymentPlanRequest request` | `Task<ActionResult<CompanyListingPaymentPlanMutationResponse>>` | `service.CreatePaymentPlanAsync |
| `PUT /api/company/listings/{listingId}/payment-plans/{paymentPlanId}` | `Guid listingId, Guid paymentPlanId, UpdateCompanyListingPaymentPlanRequest request` | `Task<ActionResult<CompanyListingPaymentPlanMutationResponse>>` | `service.UpdatePaymentPlanAsync |
| `DELETE /api/company/listings/{listingId}/payment-plans/{paymentPlanId}` | `Guid listingId, Guid paymentPlanId, [FromQuery] string? rowVersion` | `Task<IActionResult>` | `service.DeletePaymentPlanAsync |
| `POST /api/company/listings/{listingId}/publish` | `Guid listingId, CompanyListingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/listings/{listingId}/archive` | `Guid listingId, CompanyListingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `GET /api/company/viewing-slots` | `[FromQuery] CompanyViewingSlotDirectoryRequest request` | `Task<ActionResult<CompanyViewingSlotsResponse>>` | `service.GetSlotsAsync |
| `GET /api/company/viewing-slots/{slotId}` | `Guid slotId` | `Task<ActionResult<CompanyViewingSlotResponse>>` | `service.GetSlotAsync |
| `POST /api/company/viewing-slots` | `CreateCompanyViewingSlotRequest request` | `Task<ActionResult<CompanyViewingSlotResponse>>` | `service.CreateSlotAsync |
| `PUT /api/company/viewing-slots/{slotId}` | `Guid slotId, UpdateCompanyViewingSlotRequest request` | `Task<ActionResult<CompanyViewingSlotResponse>>` | `service.UpdateSlotAsync |
| `POST /api/company/viewing-slots/{slotId}/close` | `Guid slotId, CompanyViewingSlotLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-slots/{slotId}/reopen` | `Guid slotId, CompanyViewingSlotLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-slots/{slotId}/cancel` | `Guid slotId, CompanyViewingSlotLifecycleRequest request` | `Task<IActionResult>` | `service.CloseSlotAsync, service.ReopenSlotAsync, service.CancelSlotAsync |

## Batch 5 contracts

The route table below is a source index, not a substitute for endpoint validation in the Master Reference. Contract types are framework-independent Application models or API transport records unless the action uses scalar/query binding.

| Route | Bound request/query signature | Declared action result | Application service call(s) |
|---|---|---|---|
| `GET /api/company/viewing-bookings` | `[FromQuery] CompanyViewingBookingDirectoryRequest request` | `Task<ActionResult<CompanyViewingBookingsResponse>>` | `service.GetBookingsAsync |
| `GET /api/company/viewing-bookings/{bookingId}` | `Guid bookingId` | `Task<ActionResult<CompanyViewingBookingDetailsResponse>>` | `service.GetBookingAsync |
| `PUT /api/company/viewing-bookings/{bookingId}/assignment` | `Guid bookingId, CompanyViewingBookingAssignmentRequest request` | `Task<ActionResult<CompanyViewingBookingDetailsResponse>>` | `service.AssignEmployeeAsync |
| `POST /api/company/viewing-bookings/{bookingId}/confirm` | `Guid bookingId, CompanyViewingBookingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-bookings/{bookingId}/reject` | `Guid bookingId, CompanyViewingBookingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-bookings/{bookingId}/check-in` | `Guid bookingId, CompanyViewingBookingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-bookings/{bookingId}/complete` | `Guid bookingId, CompanyViewingBookingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-bookings/{bookingId}/no-show` | `Guid bookingId, CompanyViewingBookingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-bookings/{bookingId}/cancel` | `Guid bookingId, CompanyViewingBookingLifecycleRequest request` | `Task<IActionResult>` | ` |
| `POST /api/company/viewing-bookings/{bookingId}/reschedule` | `Guid bookingId, CompanyViewingBookingRescheduleRequest request` | `Task<ActionResult<CompanyViewingBookingDetailsResponse>>` | `service.RescheduleAsync |
| `GET /api/company/leads` | `[FromQuery] CompanyLeadDirectoryRequest request` | `Task<ActionResult<CompanyLeadsResponse>>` | `service.GetLeadsAsync |
| `GET /api/company/leads/{leadId}` | `Guid leadId` | `Task<ActionResult<CompanyLeadDetailsResponse>>` | `service.GetLeadAsync |
| `POST /api/company/leads` | `CreateCompanyLeadRequest request` | `Task<ActionResult<CompanyLeadDetailsResponse>>` | `service.CreateLeadAsync |
| `PUT /api/company/leads/{leadId}` | `Guid leadId, UpdateCompanyLeadRequest request` | `Task<ActionResult<CompanyLeadDetailsResponse>>` | `service.UpdateLeadAsync |
| `PUT /api/company/leads/{leadId}/stage` | `Guid leadId, ChangeCompanyLeadStageRequest request` | `Task<IActionResult>` | `service.ChangeStageAsync |
| `POST /api/company/leads/{leadId}/requirements` | `Guid leadId, CompanyLeadRequirementRequest request` | `Task<ActionResult<CompanyLeadRequirementMutationResponse>>` | `service.CreateRequirementAsync |
| `PUT /api/company/leads/{leadId}/requirements/{requirementId}` | `Guid leadId, Guid requirementId, CompanyLeadRequirementRequest request` | `Task<ActionResult<CompanyLeadRequirementMutationResponse>>` | `service.UpdateRequirementAsync |
| `POST /api/company/leads/{leadId}/requirements/{requirementId}/confirm` | `Guid leadId, Guid requirementId, ConfirmCompanyLeadRequirementRequest request` | `Task<IActionResult>` | `service.ConfirmRequirementAsync |
| `DELETE /api/company/leads/{leadId}/requirements/{requirementId}` | `Guid leadId, Guid requirementId, [FromQuery] string? leadRowVersion` | `Task<IActionResult>` | `service.DeleteRequirementAsync |
| `PUT /api/company/leads/{leadId}/interests` | `Guid leadId, ReplaceCompanyLeadInterestsRequest request` | `Task<ActionResult<CompanyLeadInterestsMutationResponse>>` | `service.ReplaceInterestsAsync |
| `GET /api/company/leads/{leadId}/activities` | `Guid leadId, [FromQuery] CompanyLeadActivityDirectoryRequest request` | `Task<ActionResult<CompanyLeadActivitiesResponse>>` | `service.GetActivitiesAsync |
| `POST /api/company/leads/{leadId}/activities` | `Guid leadId, CreateCompanyLeadActivityRequest request` | `Task<ActionResult<CompanyLeadActivityMutationResponse>>` | `service.CreateActivityAsync |
| `GET /api/listings/{listingSlug}/viewing-slots` | `string? listingSlug, [FromQuery] PublicViewingSlotRequest request` | `Task<ActionResult<PublicViewingSlotsResponse>>` | `service.GetSlotsAsync |
| `GET /api/companies/{companySlug}/reviews` | `string? companySlug, [FromQuery] PublicCompanyReviewRequest request` | `Task<ActionResult<PublicCompanyReviewsResponse>>` | `reviewService.GetPublicCompanyReviewsAsync |

## Batch 6 contracts

The route table below is a source index, not a substitute for endpoint validation in the Master Reference. Contract types are framework-independent Application models or API transport records unless the action uses scalar/query binding.

| Route | Bound request/query signature | Declared action result | Application service call(s) |
|---|---|---|---|
| `GET /api/company/billing/invoices` | `[FromQuery] InvoiceDirectoryRequest request` | `Task<ActionResult<BillingInvoiceDirectoryResponse>>` | `service.GetInvoicesAsync |
| `GET /api/company/billing/invoices/{invoiceId}` | `Guid invoiceId` | `Task<ActionResult<BillingInvoiceDetailsResponse>>` | `service.GetInvoiceAsync |
| `GET /api/company/billing/booking-charges` | `[FromQuery] BookingChargeDirectoryRequest request` | `Task<ActionResult<BookingChargeDirectoryResponse>>` | `service.GetBookingChargesAsync |
| `GET /api/company/billing/payment-transactions` | `[FromQuery] PaymentTransactionDirectoryRequest request` | `Task<ActionResult<PaymentTransactionDirectoryResponse>>` | `service.GetPaymentTransactionsAsync |
| `POST /api/company/billing/booking-charges/preview` | `BookingChargeSelectionRequest request` | `Task<ActionResult<BookingChargePreviewResponse>>` | `service.PreviewBookingChargesAsync |
| `POST /api/company/billing/booking-charges/checkout` | `BookingChargeSelectionRequest request` | `Task<ActionResult<BillingInvoiceDetailsResponse>>` | `service.CheckoutBookingChargesAsync |
| `GET /api/company/listing-promotions` | `[FromQuery] CompanyListingPromotionDirectoryRequest request` | `Task<ActionResult<CompanyListingPromotionsResponse>>` | `service.GetCompanyPromotionsAsync |
| `GET /api/company/listing-promotions/{promotionId}` | `Guid promotionId` | `Task<ActionResult<CompanyListingPromotionResponse>>` | `service.GetCompanyPromotionAsync |
| `POST /api/company/listing-promotions/checkout` | `CheckoutPromotionRequest request` | `Task<ActionResult<PromotionCheckoutResponse>>` | `service.CheckoutAsync |
| `POST /api/company/listing-promotions/{promotionId}/cancel` | `Guid promotionId, PromotionCancellationRequest request` | `Task<IActionResult>` | `service.CancelAsync |
| `GET /api/company/subscription` | `None` | `Task<ActionResult<CompanySubscriptionResponse>>` | `service.GetCurrentAsync |
| `GET /api/company/subscriptions` | `[FromQuery] CompanySubscriptionHistoryRequest request` | `Task<ActionResult<CompanySubscriptionHistoryResponse>>` | `service.GetHistoryAsync |
| `POST /api/company/subscription/checkout` | `CheckoutSubscriptionRequest request` | `Task<ActionResult<CheckoutSubscriptionResponse>>` | `service.CheckoutAsync |
| `POST /api/company/subscription/cancel` | `SubscriptionLifecycleRequest request` | `Task<IActionResult>` | `service.CancelAsync, service.ResumeAsync |
| `POST /api/company/subscription/resume` | `SubscriptionLifecycleRequest request` | `Task<IActionResult>` | `service.CancelAsync, service.ResumeAsync |
| `GET /api/subscription-plans` | `None` | `Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>>` | ` |
| `GET /api/subscription-plans/{planId}` | `Guid planId` | `Task<ActionResult<SubscriptionPlanResponse>>` | `service.GetPublicPlanAsync |
| `GET /api/promotion-packages` | `None` | `Task<ActionResult<IReadOnlyList<PromotionPackageResponse>>>` | ` |
| `GET /api/promotion-packages/{packageId}` | `Guid packageId` | `Task<ActionResult<PromotionPackageResponse>>` | `service.GetPublicPackageAsync |

## Batch 7 contracts

The route table below is a source index, not a substitute for endpoint validation in the Master Reference. Contract types are framework-independent Application models or API transport records unless the action uses scalar/query binding.

| Route | Bound request/query signature | Declared action result | Application service call(s) |
|---|---|---|---|
| `GET /api/me/company-applications` | `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null` | `Task<ActionResult<CompanyApplicationDirectoryResponse>>` | `applicationService.GetApplicationsAsync |
| `GET /api/me/company-applications/{applicationId}` | `Guid applicationId` | `Task<ActionResult<CompanyApplicationDetailsResponse>>` | `applicationService.GetApplicationAsync |
| `POST /api/me/company-applications` | `CreateCompanyApplicationRequest request` | `Task<ActionResult<CompanyApplicationDetailsResponse>>` | `applicationService.CreateApplicationAsync |
| `PUT /api/me/company-applications/{applicationId}` | `Guid applicationId, UpdateCompanyApplicationRequest request` | `Task<ActionResult<CompanyApplicationDetailsResponse>>` | `applicationService.UpdateApplicationAsync |
| `PUT /api/me/company-applications/{applicationId}/documents` | `Guid applicationId, ReplaceCompanyApplicationDocumentsRequest request` | `Task<ActionResult<CompanyApplicationDocumentsResponse>>` | `applicationService.ReplaceDocumentsAsync |
| `POST /api/me/company-applications/{applicationId}/submit` | `Guid applicationId, SubmitCompanyApplicationRequest request` | `Task<IActionResult>` | `applicationService.SubmitApplicationAsync |
| `GET /api/platform/company-applications` | `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? companyType = null, [FromQuery] DateTimeOffset? submittedFrom = null, [FromQuery] DateTimeOffset? submittedTo = null` | `Task<ActionResult<PlatformCompanyApplicationDirectoryResponse>>` | `applicationService.GetApplicationsAsync |
| `GET /api/platform/company-applications/{applicationId}` | `Guid applicationId` | `Task<ActionResult<PlatformCompanyApplicationDetailsResponse>>` | `applicationService.GetApplicationAsync |
| `GET /api/platform/company-applications/{applicationId}/documents/{documentId}/content` | `Guid applicationId, Guid documentId` | `Task<IActionResult>` | `applicationService.GetDocumentContentAsync |
| `POST /api/platform/company-applications/{applicationId}/start-review` | `Guid applicationId, PlatformRowVersionRequest request` | `Task<IActionResult>` | `applicationService.StartReviewAsync |
| `PUT /api/platform/company-applications/{applicationId}/documents/{documentId}/verification` | `Guid applicationId, Guid documentId, PlatformDocumentVerificationRequest request` | `Task<ActionResult<PlatformDocumentDecisionResponse>>` | `applicationService.VerifyDocumentAsync |
| `POST /api/platform/company-applications/{applicationId}/request-changes` | `Guid applicationId, PlatformDecisionRequest request` | `Task<IActionResult>` | ` |
| `POST /api/platform/company-applications/{applicationId}/reject` | `Guid applicationId, PlatformDecisionRequest request` | `Task<IActionResult>` | ` |
| `POST /api/platform/company-applications/{applicationId}/approve` | `Guid applicationId, PlatformCompanyApprovalRequest request` | `Task<ActionResult<PlatformApprovedCompanyResponse>>` | `applicationService.ApproveAsync |
| `GET /api/platform/reviews` | `[FromQuery] PlatformReviewDirectoryRequest request` | `Task<ActionResult<PlatformReviewDirectoryResponse>>` | `reviewService.GetReviewsAsync |
| `GET /api/platform/reviews/{reviewId}` | `Guid reviewId` | `Task<ActionResult<PlatformReviewDetailsResponse>>` | `reviewService.GetReviewAsync |
| `PUT /api/platform/reviews/{reviewId}/moderation` | `Guid reviewId, PlatformReviewModerationRequest request` | `Task<IActionResult>` | `reviewService.ModerateAsync |
| `GET /api/files/{fileAssetId}` | `Guid fileAssetId` | `Task<IActionResult>` | `service.GetPublicImageAsync |

## Controller validation message catalog

This catalog centralizes reusable controller-side validation expressions so endpoint entries can cross-link them without duplicating helper logic. Application-service ownership, state and reference checks remain endpoint-specific in the Master Reference.

### AuthController

- `ModelState.AddModelError(key, "A valid email address of at most 256 characters is required.")`
- `ModelState.AddModelError(key, $"A value of at most {maximumLength} characters is required.")`
- `ModelState.AddModelError(nameof(request.AcceptTerms), "AcceptTerms must be true.")`
- `ModelState.AddModelError(nameof(request.Code), $"Code is required and must not exceed {MaximumConfirmationCodeLength} characters.")`
- `ModelState.AddModelError(nameof(request.ConfirmPassword), "ConfirmPassword must match NewPassword.")`
- `ModelState.AddModelError(nameof(request.ConfirmPassword), "ConfirmPassword must match Password.")`
- `ModelState.AddModelError(nameof(request.NewPassword), "NewPassword is required and must contain between 8 and 128 characters.")`
- `ModelState.AddModelError(nameof(request.Password), "Password is required and must contain between 8 and 128 characters.")`
- `ModelState.AddModelError(nameof(request.Password), "Password is required and must not exceed 128 characters.")`
- `ModelState.AddModelError(nameof(request.Persona), "Persona must be Buyer, Renter, or Agent.")`
- `ModelState.AddModelError(nameof(request.UserId), "UserId is required.")`

### CatalogLookupsController

- `ModelState.AddModelError(nameof(appliesTo), "AppliesTo must be Project or Unit.")`

### CompaniesController

- `ModelState.AddModelError(nameof(request.CompanyType), "CompanyType must be Developer or BrokerAgency.")`
- `ModelState.AddModelError(nameof(request.PageNumber), $"PageNumber must be between 1 and {MaximumPageNumber}.")`
- `ModelState.AddModelError(nameof(request.PageSize), $"PageSize must be between 1 and {MaximumPageSize}.")`
- `ModelState.AddModelError(nameof(request.Search), $"Search must not exceed {MaximumSearchLength} characters.")`
- `ModelState.AddModelError(nameof(slug), $"Slug is required and must not exceed {MaximumSlugLength} characters.")`

### CompanyBillingController

- `ModelState.AddModelError("bookingChargeIds", "BookingChargeIds must contain 1 to 100 unique, non-empty values.")`
- `ModelState.AddModelError("createdFrom", "CreatedFrom must be UTC.")`
- `ModelState.AddModelError("createdTo", "CreatedTo must be later than CreatedFrom.")`
- `ModelState.AddModelError("createdTo", "CreatedTo must be UTC.")`
- `ModelState.AddModelError("currencyCode", "CurrencyCode must contain exactly three ASCII letters.")`
- `ModelState.AddModelError("invoiceId", "InvoiceId is required.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError(key, $"{key} cannot be blank when supplied.")`
- `ModelState.AddModelError(key, $"{key} is invalid.")`
- `ModelState.AddModelError(key, $"{key} must not exceed {maximum} characters.")`

### CompanyEmployeesController

- `ModelState.AddModelError("email", "Email must be valid and at most 256 characters.")`
- `ModelState.AddModelError("employeeId", "EmployeeId is required.")`
- `ModelState.AddModelError("pageNumber", $"PageNumber must be between 1 and {MaximumPageNumber}.")`
- `ModelState.AddModelError("pageSize", $"PageSize must be between 1 and {MaximumPageSize}.")`
- `ModelState.AddModelError("roleIds", "RoleIds must contain at most 100 unique non-empty IDs.")`
- `ModelState.AddModelError("search", "Search must be non-blank and at most 100 characters when supplied.")`
- `ModelState.AddModelError("status", "Status must be Active, Suspended, or Ended.")`
- `ModelState.AddModelError(key, "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError(key, $"{key} is required and must not exceed {maximumLength} characters.")`
- `ModelState.AddModelError(key, $"{key} must not exceed {maximumLength} characters.")`

### CompanyLeadsController

- `ModelState.AddModelError("area", "Area values must be positive decimal(18,2) values in a valid range.")`
- `ModelState.AddModelError("bedrooms", "Bedroom and bathroom minimums must be non-negative.")`
- `ModelState.AddModelError("budget", "Budget values must be non-negative decimal(18,2) values in a valid range.")`
- `ModelState.AddModelError("contact", "ContactPhone or ContactEmail is required.")`
- `ModelState.AddModelError("contact", "The Lead must retain a customer profile, contact phone, or contact email.")`
- `ModelState.AddModelError("contactEmail", "ContactEmail is invalid.")`
- `ModelState.AddModelError("createdTo", "CreatedTo must be later than CreatedFrom.")`
- `ModelState.AddModelError("criteriaSchemaVersion", "CriteriaSchemaVersion must be positive.")`
- `ModelState.AddModelError("currencyCode", "CurrencyCode is required with a budget.")`
- `ModelState.AddModelError("currencyCode", "CurrencyCode must contain exactly three ASCII letters.")`
- `ModelState.AddModelError("items", "Duplicate listing and interest type pairs are not allowed.")`
- `ModelState.AddModelError("items", "Items is required.")`
- `ModelState.AddModelError("items", "Items must not exceed 100 entries.")`
- `ModelState.AddModelError("items", "ListingId is required.")`
- `ModelState.AddModelError("metadata", "Canonical metadata must not exceed 16000 characters.")`
- `ModelState.AddModelError("metadata", "Metadata must be a JSON object.")`
- `ModelState.AddModelError("occurredAt", "OccurredAt must not be later than the current UTC time.")`
- `ModelState.AddModelError("ownerEmployeeId", "OwnerEmployeeId cannot be empty.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("paymentPlanMonths", "PaymentPlanMonths must be positive.")`
- `ModelState.AddModelError("structuredCriteria", "Canonical criteria must not exceed 16000 characters.")`
- `ModelState.AddModelError("structuredCriteria", "StructuredCriteria must be a JSON object.")`
- `ModelState.AddModelError("to", "To must be later than From.")`
- `ModelState.AddModelError("unassignedOnly", "OwnerEmployeeId and UnassignedOnly cannot be combined.")`
- `ModelState.AddModelError(key, $"{key} is invalid.")`
- `ModelState.AddModelError(key, $"{key} is required.")`
- `ModelState.AddModelError(key, $"{key} must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError(key, $"{key} must not exceed {max} characters.")`

### CompanyListingPromotionsController

- `ModelState.AddModelError("listingId", "ListingId cannot be empty when supplied.")`
- `ModelState.AddModelError("listingId", "ListingId is required.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("placement", "Placement must not exceed 100 characters.")`
- `ModelState.AddModelError("promotionId", "PromotionId is required.")`
- `ModelState.AddModelError("promotionPackageId", "PromotionPackageId is required.")`
- `ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("status", "Status must be Scheduled, Active, Completed, or Cancelled.")`
- `ModelState.AddModelError("to", "To must be later than From.")`

### CompanyListingsController

- `ModelState.AddModelError("askingPrice", "AskingPrice must be a positive decimal(18,2) value.")`
- `ModelState.AddModelError("cashDiscountPercentage", "CashDiscountPercentage must be a decimal(5,2) percentage between 0 and 100.")`
- `ModelState.AddModelError("description", "Description is required and must not exceed 4000 characters.")`
- `ModelState.AddModelError("downPaymentPercentage", "DownPaymentPercentage must be a decimal(5,2) percentage between 0 and 100.")`
- `ModelState.AddModelError("durationMonths", "DurationMonths must be greater than zero.")`
- `ModelState.AddModelError("isActive", "IsActive is required.")`
- `ModelState.AddModelError("items", "Items are invalid.")`
- `ModelState.AddModelError("listingCode", "ListingCode is required and must not exceed 100 characters.")`
- `ModelState.AddModelError("listingId", "ListingId is required.")`
- `ModelState.AddModelError("name", "Name is required and must not exceed 200 characters.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("paymentPlanId", "PaymentPlanId is required.")`
- `ModelState.AddModelError("priceChangeReason", "PriceChangeReason must not exceed 500 characters.")`
- `ModelState.AddModelError("rentPeriod", "RentPeriod must not be supplied for a sale listing.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("search", "Search must not exceed 100 characters.")`
- `ModelState.AddModelError("slug", "Slug must be lowercase kebab-case and must not exceed 200 characters.")`
- `ModelState.AddModelError("title", "Title is required and must not exceed 300 characters.")`
- `ModelState.AddModelError("totalPrice", "TotalPrice must be a positive decimal(18,2) value.")`
- `ModelState.AddModelError("unitId", "UnitId is required.")`
- `ModelState.AddModelError(key, "CurrencyCode is required.")`
- `ModelState.AddModelError(key, "CurrencyCode must contain exactly three ASCII letters.")`
- `ModelState.AddModelError(key, $"{key} cannot be empty when supplied.")`
- `ModelState.AddModelError(key, $"{key} is invalid.")`
- `ModelState.AddModelError(key, $"{key} is required.")`
- `ModelState.AddModelError(key, message)`

### CompanyProfileController

- `ModelState.AddModelError("addressLocationId", "AddressLocationId is required.")`
- `ModelState.AddModelError("baseCurrencyCode", "BaseCurrencyCode must contain exactly three ASCII letters.")`
- `ModelState.AddModelError("businessEmail", "BusinessEmail must be a valid email address of at most 256 characters.")`
- `ModelState.AddModelError("coordinates", "Latitude and longitude must both be null or within their valid ranges.")`
- `ModelState.AddModelError("website", "Website must be an absolute HTTP or HTTPS URL of at most 2048 characters.")`
- `ModelState.AddModelError(key, "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError(key, $"{key} is required and must not exceed {maximumLength} characters.")`
- `ModelState.AddModelError(key, $"{key} must not exceed {maximumLength} characters.")`

### CompanyProjectManagementController

- `ModelState.AddModelError("amenityIds","AmenityIds must contain unique non-empty IDs.")`
- `ModelState.AddModelError("filter","Invalid status.")`
- `ModelState.AddModelError("items","Items are invalid.")`
- `ModelState.AddModelError("nearbyPlace","Invalid nearby place.")`
- `ModelState.AddModelError("nearbyPlaceId","Invalid nearby-place ID.")`
- `ModelState.AddModelError("pageNumber","Invalid page number.")`
- `ModelState.AddModelError("pageSize","Invalid page size.")`
- `ModelState.AddModelError("project","Invalid project.")`
- `ModelState.AddModelError("projectId","Invalid project ID.")`
- `ModelState.AddModelError("rowVersion","Invalid row version.")`
- `ModelState.AddModelError("search","Invalid search.")`

### CompanyProjectsController

- `ModelState.AddModelError(key, $"{key} is required and must not exceed {MaximumSlugLength} characters.")`
- `ModelState.AddModelError(nameof(request.DeliveryStatus), "DeliveryStatus must be Planned, UnderConstruction, ReadyToMove, or Delivered.")`
- `ModelState.AddModelError(nameof(request.PageNumber), $"PageNumber must be between 1 and {MaximumPageNumber}.")`
- `ModelState.AddModelError(nameof(request.PageSize), $"PageSize must be between 1 and {MaximumPageSize}.")`
- `ModelState.AddModelError(nameof(request.Search), $"Search must not exceed {MaximumSearchLength} characters.")`

### CompanyRolesController

- `ModelState.AddModelError("name", "Owner is reserved.")`
- `ModelState.AddModelError("name", $"Name is required and must not exceed {MaximumRoleNameLength} characters.")`
- `ModelState.AddModelError("permissionCodes", "PermissionCodes is required.")`
- `ModelState.AddModelError("permissionCodes", "PermissionCodes must contain unique, active, defined company permission codes.")`
- `ModelState.AddModelError("permissionCodes", $"PermissionCodes must contain at most {CompanyPermissionCodes.All.Count} items.")`
- `ModelState.AddModelError(nameof(roleId), "RoleId is required.")`

### CompanySubscriptionsController

- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("status", "Status must be Active, Expired, or Cancelled.")`
- `ModelState.AddModelError("subscriptionPlanId", "SubscriptionPlanId is required.")`

### CompanyUnitsController

- `ModelState.AddModelError("amenityIds", "AmenityIds must contain unique non-empty IDs.")`
- `ModelState.AddModelError("locationId", "LocationId is required.")`
- `ModelState.AddModelError("nearbyPlace", "Nearby place is invalid.")`
- `ModelState.AddModelError("nearbyPlaceId", "NearbyPlaceId is required.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("projectId", "ProjectId cannot be empty when supplied.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("search", "Search must not exceed 100 characters.")`
- `ModelState.AddModelError("unit", "Unit numeric fields are invalid.")`
- `ModelState.AddModelError("unitCode", "UnitCode is required and must not exceed 100 characters.")`
- `ModelState.AddModelError("unitId", "UnitId is required.")`
- `ModelState.AddModelError("unitTypeId", "UnitTypeId is required.")`
- `ModelState.AddModelError(key, $"{key} cannot be empty when supplied.")`
- `ModelState.AddModelError(key, $"{key} is invalid.")`
- `ModelState.AddModelError(key, $"{key} is required.")`

### CompanyViewingBookingsController

- `ModelState.AddModelError("assignedEmployeeId", "AssignedEmployeeId cannot be empty when supplied.")`
- `ModelState.AddModelError("bookingId", "BookingId is required.")`
- `ModelState.AddModelError("employeeId", "EmployeeId cannot be empty when supplied.")`
- `ModelState.AddModelError("listingId", "ListingId cannot be empty when supplied.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("search", "Search must not exceed 100 characters.")`
- `ModelState.AddModelError("status", "Status is invalid.")`
- `ModelState.AddModelError("to", "To must be later than From.")`
- `ModelState.AddModelError("toViewingSlotId", "ToViewingSlotId is required.")`
- `ModelState.AddModelError("viewingSlotId", "ViewingSlotId cannot be empty when supplied.")`
- `ModelState.AddModelError(key, message)`

### CompanyViewingSlotsController

- `ModelState.AddModelError("capacity", "Capacity must be positive.")`
- `ModelState.AddModelError("endsAt", "EndsAt must be later than StartsAt.")`
- `ModelState.AddModelError("listingId", "ListingId cannot be empty when supplied.")`
- `ModelState.AddModelError("listingId", "ListingId is required.")`
- `ModelState.AddModelError("meetingPoint", "MeetingPoint is required and must not exceed 500 characters.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("slotId", "SlotId is required.")`
- `ModelState.AddModelError("to", "To must be later than From.")`
- `ModelState.AddModelError(key, $"{key} is invalid.")`
- `ModelState.AddModelError(key, $"{key} is required.")`
- `ModelState.AddModelError(key, message)`

### ListingsController

- `ModelState.AddModelError(maximumKey, $"Maximum {label.ToLowerInvariant()} must be non-negative.")`
- `ModelState.AddModelError(maximumKey, $"Minimum {label.ToLowerInvariant()} must not exceed maximum {label.ToLowerInvariant()}.")`
- `ModelState.AddModelError(minimumKey, $"Minimum {label.ToLowerInvariant()} must be non-negative.")`
- `ModelState.AddModelError(nameof(pageNumber), $"PageNumber must be between 1 and {MaximumPageNumber}.")`
- `ModelState.AddModelError(nameof(pageSize), $"PageSize must be between 1 and {MaximumPageSize}.")`
- `ModelState.AddModelError(nameof(request.CurrencyCode), "CurrencyCode is required when filtering by price.")`
- `ModelState.AddModelError(nameof(request.CurrencyCode), "CurrencyCode is required when sorting by price.")`
- `ModelState.AddModelError(nameof(request.CurrencyCode), "CurrencyCode must contain exactly three ASCII letters.")`
- `ModelState.AddModelError(nameof(request.ListingType), "ListingType must be Sale or Rent.")`
- `ModelState.AddModelError(nameof(request.Search), $"Search must not exceed {MaximumSearchLength} characters.")`
- `ModelState.AddModelError(nameof(request.Sort), "Sort must be Newest, PriceLowToHigh, PriceHighToLow, AreaLowToHigh, or AreaHighToLow.")`
- `ModelState.AddModelError(nameof(slug), $"Slug is required and must not exceed {MaximumSlugLength} characters.")`

### MeController

- `ModelState.AddModelError(key, $"A value of at most {maximumLength} characters is required.")`
- `ModelState.AddModelError(nameof(listingId), "ListingId is required.")`
- `ModelState.AddModelError(nameof(pageNumber), $"PageNumber must be between 1 and {MaximumPageNumber}.")`
- `ModelState.AddModelError(nameof(pageSize), $"PageSize must be between 1 and {MaximumPageSize}.")`
- `ModelState.AddModelError(nameof(request.Filters), "Filters must be a JSON object.")`
- `ModelState.AddModelError(nameof(request.Filters), $"Filters must not exceed {MaximumSavedSearchFilterLength} characters.")`
- `ModelState.AddModelError(nameof(request.Persona), "Persona must be Buyer, Renter, or Agent.")`
- `ModelState.AddModelError(nameof(savedSearchId), "SavedSearchId is required.")`

### MyCompanyApplicationsController

- `ModelState.AddModelError("applicationId", "ApplicationId is required.")`
- `ModelState.AddModelError("businessEmail", "BusinessEmail is invalid.")`
- `ModelState.AddModelError("companyType", "CompanyType must be Developer or BrokerAgency.")`
- `ModelState.AddModelError("documents", "Documents are required.")`
- `ModelState.AddModelError("documents", "Duplicate document entries are not allowed.")`
- `ModelState.AddModelError("documents", "Duplicate FileAssetIds are not allowed.")`
- `ModelState.AddModelError("documents", "No more than 20 documents are allowed.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("rowVersion", "RowVersion is required.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be valid Base64 representing exactly 8 bytes.")`
- `ModelState.AddModelError("status", "Status must be Draft, Submitted, UnderReview, NeedsChanges, Approved, or Rejected.")`
- `ModelState.AddModelError("website", "Website must be an absolute HTTP or HTTPS URL.")`
- `ModelState.AddModelError($"documents[{index}].documentType", "DocumentType is required.")`
- `ModelState.AddModelError($"documents[{index}].documentType", "DocumentType must not exceed 100 characters.")`
- `ModelState.AddModelError($"documents[{index}].fileAssetId", "FileAssetId is required.")`
- `ModelState.AddModelError(key, $"{displayName} is required.")`
- `ModelState.AddModelError(key, $"{displayName} must not exceed {maximumLength} characters.")`

### MyFilesController

- `ModelState.AddModelError("file", "Exactly one non-empty file is required.")`
- `ModelState.AddModelError("file", "The uploaded file is invalid or unsupported.")`
- `ModelState.AddModelError("fileAssetId", "FileAssetId is required.")`
- `ModelState.AddModelError("fileType", "FileType must be Image or Document without surrounding whitespace.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("search", "Search must not exceed 100 characters.")`

### MyNotificationsController

- `ModelState.AddModelError("notificationId", "NotificationId is required.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("state", "State must be Unread, Read, or Archived.")`

### MyReviewsController

- `ModelState.AddModelError("bookingId", "BookingId is required.")`
- `ModelState.AddModelError("comment", "Comment must not exceed 4000 characters.")`
- `ModelState.AddModelError("rating", "Rating must be between 1 and 5.")`
- `ModelState.AddModelError("reviewId", "ReviewId is required.")`
- `ModelState.AddModelError(key, message)`

### MyViewingBookingsController

- `ModelState.AddModelError("bookingId", "BookingId is required.")`
- `ModelState.AddModelError("contactPhone", "ContactPhone is required and must not exceed 32 characters.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.")`
- `ModelState.AddModelError("rowVersion", "RowVersion must be Base64 encoded eight bytes.")`
- `ModelState.AddModelError("specialRequests", "SpecialRequests must not exceed 4000 characters.")`
- `ModelState.AddModelError("status", "Status is invalid.")`
- `ModelState.AddModelError("viewingSlotId", "ViewingSlotId is required.")`
- `ModelState.AddModelError("visitorCount", "VisitorCount must be positive.")`
- `ModelState.AddModelError(key, message)`

### PlatformCompanyApplicationsController

- `ModelState.AddModelError("applicationId", "ApplicationId is required.")`
- `ModelState.AddModelError("baseCurrencyCode", "BaseCurrencyCode must contain exactly three ASCII letters.")`
- `ModelState.AddModelError("companyType", "CompanyType must be Developer or BrokerAgency.")`
- `ModelState.AddModelError("coordinates", "Latitude and Longitude must both be supplied or both be omitted.")`
- `ModelState.AddModelError("documentId", "DocumentId is required.")`
- `ModelState.AddModelError("locationId", "LocationId is required.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("rejectionReason", "RejectionReason is required when rejecting a document.")`
- `ModelState.AddModelError("rejectionReason", "RejectionReason must be omitted when approving a document.")`
- `ModelState.AddModelError("search", "Search must not exceed 100 characters.")`
- `ModelState.AddModelError("slug", "Slug must be lowercase kebab-case and must not exceed 200 characters.")`
- `ModelState.AddModelError("status", "Status is invalid.")`
- `ModelState.AddModelError("submittedFrom", "SubmittedFrom must not be later than SubmittedTo.")`
- `ModelState.AddModelError("submittedFrom", "SubmittedFrom must use UTC.")`
- `ModelState.AddModelError("submittedTo", "SubmittedTo must use UTC.")`
- `ModelState.AddModelError("verificationStatus", "VerificationStatus must be Approved or Rejected.")`
- `ModelState.AddModelError(key, "RowVersion must be valid Base64 representing exactly 8 bytes.")`
- `ModelState.AddModelError(key, $"{name} is required.")`
- `ModelState.AddModelError(key, $"{name} must be within range and use no more than six decimal places.")`
- `ModelState.AddModelError(key, $"{name} must not exceed {maximumLength} characters.")`
- `ModelState.AddModelError(key, message)`

### PlatformReviewsController

- `ModelState.AddModelError("companyId", "CompanyId must be a non-empty GUID.")`
- `ModelState.AddModelError("createdFrom", "CreatedFrom must not be later than CreatedTo.")`
- `ModelState.AddModelError("createdFrom", "CreatedFrom must use UTC.")`
- `ModelState.AddModelError("createdTo", "CreatedTo must use UTC.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("rating", "Rating must be between 1 and 5.")`
- `ModelState.AddModelError("reason", "Reason is required when hiding a review.")`
- `ModelState.AddModelError("reason", "Reason must be omitted when making a review visible.")`
- `ModelState.AddModelError("reason", "Reason must not exceed 1000 characters.")`
- `ModelState.AddModelError("reviewId", "ReviewId is required.")`
- `ModelState.AddModelError("status", "Status must be Visible, Hidden, or PendingModeration.")`
- `ModelState.AddModelError(key, allowPendingModeration ? "ExpectedStatus must be Visible, Hidden, or PendingModeration." : "Status must be Visible or Hidden.")`
- `ModelState.AddModelError(key, message)`

### PromotedListingsController

- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("placement", "Placement is required and must not exceed 100 characters.")`

### PromotionPackagesController

- `ModelState.AddModelError("packageId", "PackageId is required.")`

### PublicCompanyReviewsController

- `ModelState.AddModelError("companySlug", "CompanySlug is required and must not exceed 200 characters.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("rating", "Rating must be between 1 and 5.")`

### PublicViewingSlotsController

- `ModelState.AddModelError("listingSlug", "ListingSlug is required and must not exceed 200 characters.")`
- `ModelState.AddModelError("pageNumber", "PageNumber must be between 1 and 10000.")`
- `ModelState.AddModelError("pageSize", "PageSize must be between 1 and 50.")`
- `ModelState.AddModelError("to", "To must be later than From.")`

### SubscriptionPlansController

- `ModelState.AddModelError("planId", "PlanId is required.")`

## Complete API transport-contract catalog

JSON uses the configured web camel-case naming policy. Nullable annotations are significant; collections serialize as arrays. The entries below enumerate every current public request/response type under `EstateHub.Api/Contracts`; endpoint-specific value rules remain in the Master Reference.

### Authentication

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `AuthenticationTokenResponse` | `accessToken` (`string`); `accessTokenExpiresAt` (`DateTimeOffset`); `refreshToken` (`string`); `refreshTokenExpiresAt` (`DateTimeOffset`) | [`AuthenticationTokenResponse.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthenticationTokenResponse.cs) |
| `RegisterCustomerRequest` | `fullName` (`string?`); `email` (`string?`); `phoneNumber` (`string?`); `password` (`string?`); `confirmPassword` (`string?`); `persona` (`string?`); `acceptTerms` (`bool`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |
| `ConfirmEmailRequest` | `userId` (`Guid`); `code` (`string?`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |
| `ResendConfirmationRequest` | `email` (`string?`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |
| `LoginRequest` | `email` (`string?`); `password` (`string?`); `rememberMe` (`bool`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |
| `RefreshTokenRequest` | `refreshToken` (`string?`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |
| `ForgotPasswordRequest` | `email` (`string?`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |
| `ResetPasswordRequest` | `userId` (`Guid`); `code` (`string?`); `newPassword` (`string?`); `confirmPassword` (`string?`) | [`AuthRequests.cs`](../src/EstateHub.Api/Contracts/Authentication/AuthRequests.cs) |

### Billing

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `InvoiceDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `status` (`string?`); `lineType` (`string?`); `currencyCode` (`string?`); `createdFrom` (`DateTimeOffset?`); `createdTo` (`DateTimeOffset?`); `search` (`string?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BookingChargeDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `status` (`string?`); `currencyCode` (`string?`); `createdFrom` (`DateTimeOffset?`); `createdTo` (`DateTimeOffset?`); `search` (`string?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `PaymentTransactionDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `status` (`string?`); `provider` (`string?`); `currencyCode` (`string?`); `createdFrom` (`DateTimeOffset?`); `createdTo` (`DateTimeOffset?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BookingChargeSelectionRequest` | `bookingChargeIds` (`IReadOnlyList<Guid>?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingInvoiceSummaryResponse` | `id` (`Guid`); `invoiceNumber` (`string`); `status` (`string`); `currencyCode` (`string`); `periodStart` (`DateTimeOffset?`); `periodEnd` (`DateTimeOffset?`); `subtotal` (`decimal`); `total` (`decimal`); `issuedAt` (`DateTimeOffset?`); `dueAt` (`DateTimeOffset?`); `paidAt` (`DateTimeOffset?`); `createdAt` (`DateTimeOffset`); `lineCount` (`int`); `paymentCount` (`int`); `rowVersion` (`string`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingInvoiceDirectoryResponse` | `items` (`IReadOnlyList<BillingInvoiceSummaryResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingSubscriptionSourceResponse` | `companySubscriptionId` (`Guid`); `subscriptionPlanId` (`Guid`); `planName` (`string`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingBookingChargeSourceResponse` | `bookingChargeId` (`Guid`); `viewingBookingId` (`Guid`); `bookingCode` (`string`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingPromotionListingResponse` | `id` (`Guid`); `slug` (`string`); `title` (`string`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingPromotionSourceResponse` | `listingPromotionId` (`Guid`); `listing` (`BillingPromotionListingResponse`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingInvoiceLineResponse` | `id` (`Guid`); `lineType` (`string`); `descriptionSnapshot` (`string`); `quantity` (`decimal`); `unitAmount` (`decimal`); `lineTotal` (`decimal`); `companySubscriptionId` (`Guid?`); `bookingChargeId` (`Guid?`); `listingPromotionId` (`Guid?`); `subscription` (`BillingSubscriptionSourceResponse?`); `bookingCharge` (`BillingBookingChargeSourceResponse?`); `promotion` (`BillingPromotionSourceResponse?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingPaymentResponse` | `id` (`Guid`); `billingInvoiceId` (`Guid`); `invoiceNumber` (`string`); `provider` (`string`); `providerReference` (`string`); `status` (`string`); `amount` (`decimal`); `currencyCode` (`string`); `createdAt` (`DateTimeOffset`); `completedAt` (`DateTimeOffset?`); `failureReason` (`string?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BillingInvoiceDetailsResponse` | `invoice` (`BillingInvoiceSummaryResponse`); `lines` (`IReadOnlyList<BillingInvoiceLineResponse>`); `paymentTransactions` (`IReadOnlyList<BillingPaymentResponse>`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BookingChargeResponse` | `id` (`Guid`); `viewingBookingId` (`Guid`); `bookingCode` (`string`); `listingId` (`Guid`); `listingSlug` (`string`); `listingTitle` (`string`); `customerDisplayName` (`string`); `amountSnapshot` (`decimal`); `currencyCode` (`string`); `status` (`string`); `createdAt` (`DateTimeOffset`); `voidedAt` (`DateTimeOffset?`); `voidReason` (`string?`); `invoiceId` (`Guid?`); `invoiceNumber` (`string?`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BookingChargeDirectoryResponse` | `items` (`IReadOnlyList<BookingChargeResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `PaymentTransactionDirectoryResponse` | `items` (`IReadOnlyList<BillingPaymentResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BookingChargePreviewItemResponse` | `id` (`Guid`); `viewingBookingId` (`Guid`); `bookingCode` (`string`); `amountSnapshot` (`decimal`); `currencyCode` (`string`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |
| `BookingChargePreviewResponse` | `currencyCode` (`string`); `chargeCount` (`int`); `subtotal` (`decimal`); `total` (`decimal`); `requiresCheckoutRevalidation` (`bool`); `items` (`IReadOnlyList<BookingChargePreviewItemResponse>`) | [`BillingContracts.cs`](../src/EstateHub.Api/Contracts/Billing/BillingContracts.cs) |

### CatalogLookups

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `LocationLookupResponse` | `id` (`Guid`); `parentLocationId` (`Guid?`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`); `hasChildren` (`bool`) | [`CatalogLookupResponses.cs`](../src/EstateHub.Api/Contracts/CatalogLookups/CatalogLookupResponses.cs) |
| `UnitTypeLookupResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`) | [`CatalogLookupResponses.cs`](../src/EstateHub.Api/Contracts/CatalogLookups/CatalogLookupResponses.cs) |
| `CurrencyLookupResponse` | `code` (`string`); `name` (`string`); `symbol` (`string`); `decimalPlaces` (`int`) | [`CatalogLookupResponses.cs`](../src/EstateHub.Api/Contracts/CatalogLookups/CatalogLookupResponses.cs) |
| `AmenityLookupResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`); `scope` (`string`) | [`CatalogLookupResponses.cs`](../src/EstateHub.Api/Contracts/CatalogLookups/CatalogLookupResponses.cs) |

### Companies

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `companyType` (`string?`); `locationId` (`Guid?`) | [`CompanyDirectoryRequest.cs`](../src/EstateHub.Api/Contracts/Companies/CompanyDirectoryRequest.cs) |
| `CompanyLocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`CompanyResponses.cs`](../src/EstateHub.Api/Contracts/Companies/CompanyResponses.cs) |
| `CompanyDirectoryItemResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `companyType` (`string`); `logoFileAssetId` (`Guid?`); `location` (`CompanyLocationResponse`); `publishedProjectCount` (`int`); `publishedListingCount` (`int`) | [`CompanyResponses.cs`](../src/EstateHub.Api/Contracts/Companies/CompanyResponses.cs) |
| `CompanyDirectoryResponse` | `items` (`IReadOnlyList<CompanyDirectoryItemResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyResponses.cs`](../src/EstateHub.Api/Contracts/Companies/CompanyResponses.cs) |
| `CompanyAddressResponse` | `addressLine1` (`string`); `addressLine2` (`string?`); `postalCode` (`string?`); `latitude` (`decimal?`); `longitude` (`decimal?`); `location` (`CompanyLocationResponse`) | [`CompanyResponses.cs`](../src/EstateHub.Api/Contracts/Companies/CompanyResponses.cs) |
| `CompanyDetailsResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `legalName` (`string`); `companyType` (`string`); `businessEmail` (`string`); `supportPhone` (`string`); `website` (`string?`); `logoFileAssetId` (`Guid?`); `coverFileAssetId` (`Guid?`); `timeZoneId` (`string`); `baseCurrencyCode` (`string`); `verifiedAt` (`DateTimeOffset`); `address` (`CompanyAddressResponse`); `publishedProjectCount` (`int`); `publishedListingCount` (`int`) | [`CompanyResponses.cs`](../src/EstateHub.Api/Contracts/Companies/CompanyResponses.cs) |

### CompanyAccess

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyAccessCompanyResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `companyType` (`string`); `logoFileAssetId` (`Guid?`); `timeZoneId` (`string`); `baseCurrencyCode` (`string`); `verifiedAt` (`DateTimeOffset`) | [`CompanyAccessResponses.cs`](../src/EstateHub.Api/Contracts/CompanyAccess/CompanyAccessResponses.cs) |
| `CompanyAccessEmployeeResponse` | `id` (`Guid`); `fullName` (`string`); `jobTitle` (`string?`); `isPrimaryContact` (`bool`); `joinedAt` (`DateTimeOffset`) | [`CompanyAccessResponses.cs`](../src/EstateHub.Api/Contracts/CompanyAccess/CompanyAccessResponses.cs) |
| `CompanyAccessRoleResponse` | `id` (`Guid`); `name` (`string`); `isBuiltIn` (`bool`) | [`CompanyAccessResponses.cs`](../src/EstateHub.Api/Contracts/CompanyAccess/CompanyAccessResponses.cs) |
| `CompanyAccessContextResponse` | `company` (`CompanyAccessCompanyResponse`); `employee` (`CompanyAccessEmployeeResponse`); `roles` (`IReadOnlyList<CompanyAccessRoleResponse>`); `permissionCodes` (`IReadOnlyList<string>`) | [`CompanyAccessResponses.cs`](../src/EstateHub.Api/Contracts/CompanyAccess/CompanyAccessResponses.cs) |

### CompanyApplications

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyApplicationFieldsRequest` | `legalName` (`string?`); `businessEmail` (`string?`); `phoneNumber` (`string?`); `registrationNumber` (`string?`); `taxId` (`string?`); `website` (`string?`); `companyType` (`string?`); `officeAddress` (`string?`); `locationText` (`string?`); `estimatedPropertyRange` (`string?`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CreateCompanyApplicationRequest` | Inherits all fields from `CompanyApplicationFieldsRequest`; no additional fields. | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `UpdateCompanyApplicationRequest` | Inherits all fields from `CompanyApplicationFieldsRequest`; adds `rowVersion` (`string?`). | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `ReplaceCompanyApplicationDocumentsRequest` | `rowVersion` (`string?`); `documents` (`IReadOnlyList<CompanyApplicationDocumentRequest>?`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationDocumentRequest` | `fileAssetId` (`Guid`); `documentType` (`string?`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `SubmitCompanyApplicationRequest` | `rowVersion` (`string?`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationSummaryResponse` | `id` (`Guid`); `legalName` (`string`); `companyType` (`string`); `status` (`string`); `submittedAt` (`DateTimeOffset?`); `reviewedAt` (`DateTimeOffset?`); `decisionReason` (`string?`); `documentCount` (`int`); `approvedCompanyId` (`Guid?`); `rowVersion` (`string`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationDirectoryResponse` | `items` (`IReadOnlyList<CompanyApplicationSummaryResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationApprovedCompanyResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationDocumentResponse` | `id` (`Guid`); `documentType` (`string`); `fileAssetId` (`Guid`); `originalFileName` (`string?`); `contentType` (`string`); `sizeBytes` (`long`); `verificationStatus` (`string`); `reviewedAt` (`DateTimeOffset?`); `rejectionReason` (`string?`); `contentPath` (`string`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationStatusHistoryResponse` | `id` (`Guid`); `fromStatus` (`string?`); `toStatus` (`string`); `changedAt` (`DateTimeOffset`); `reason` (`string?`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationDetailsResponse` | `id` (`Guid`); `legalName` (`string`); `businessEmail` (`string`); `phoneNumber` (`string`); `registrationNumber` (`string`); `taxId` (`string?`); `website` (`string?`); `companyType` (`string`); `officeAddress` (`string`); `locationText` (`string`); `estimatedPropertyRange` (`string?`); `status` (`string`); `submittedAt` (`DateTimeOffset?`); `reviewedAt` (`DateTimeOffset?`); `decisionReason` (`string?`); `approvedCompany` (`CompanyApplicationApprovedCompanyResponse?`); `rowVersion` (`string`); `documents` (`IReadOnlyList<CompanyApplicationDocumentResponse>`); `statusHistory` (`IReadOnlyList<CompanyApplicationStatusHistoryResponse>`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |
| `CompanyApplicationDocumentsResponse` | `rowVersion` (`string`); `documents` (`IReadOnlyList<CompanyApplicationDocumentResponse>`) | [`CompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/CompanyApplications/CompanyApplicationContracts.cs) |

### CompanyLeads

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyLeadDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `stage` (`string?`); `priority` (`string?`); `ownerEmployeeId` (`Guid?`); `unassignedOnly` (`bool?`); `source` (`string?`); `createdFrom` (`DateTimeOffset?`); `createdTo` (`DateTimeOffset?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadActivityDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `activityType` (`string?`); `from` (`DateTimeOffset?`); `to` (`DateTimeOffset?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CreateCompanyLeadRequest` | `contactName` (`string?`); `contactPhone` (`string?`); `contactEmail` (`string?`); `priority` (`string?`); `ownerEmployeeId` (`Guid?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `UpdateCompanyLeadRequest` | `contactName` (`string?`); `contactPhone` (`string?`); `contactEmail` (`string?`); `priority` (`string?`); `ownerEmployeeId` (`Guid?`); `rowVersion` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `ChangeCompanyLeadStageRequest` | `stage` (`string?`); `rowVersion` (`string?`); `reason` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadRequirementRequest` | `intent` (`string?`); `originalQuery` (`string?`); `structuredCriteria` (`JsonElement`); `criteriaSchemaVersion` (`int`); `minBudget` (`decimal?`); `maxBudget` (`decimal?`); `currencyCode` (`string?`); `minBedrooms` (`int?`); `minBathrooms` (`int?`); `minArea` (`decimal?`); `maxArea` (`decimal?`); `paymentPlanMonths` (`int?`); `notes` (`string?`); `extractedByAI` (`bool`); `leadRowVersion` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `ConfirmCompanyLeadRequirementRequest` | `leadRowVersion` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadInterestRequest` | `listingId` (`Guid`); `interestType` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `ReplaceCompanyLeadInterestsRequest` | `leadRowVersion` (`string?`); `items` (`IReadOnlyList<CompanyLeadInterestRequest>?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CreateCompanyLeadActivityRequest` | `activityType` (`string?`); `occurredAt` (`DateTimeOffset?`); `notes` (`string?`); `metadata` (`JsonElement?`); `leadRowVersion` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadEmployeeResponse` | `id` (`Guid`); `fullName` (`string`); `jobTitle` (`string?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadCustomerResponse` | `id` (`Guid`); `fullName` (`string`); `persona` (`string`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadBookingResponse` | `id` (`Guid`); `bookingCode` (`string`); `status` (`string`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadResponse` | `id` (`Guid`); `stage` (`string`); `priority` (`string`); `contactName` (`string?`); `contactPhone` (`string?`); `contactEmail` (`string?`); `source` (`string`); `createdAt` (`DateTimeOffset`); `lastActivityAt` (`DateTimeOffset?`); `closedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `requirementCount` (`int`); `interestCount` (`int`); `activityCount` (`int`); `customer` (`CompanyLeadCustomerResponse?`); `sourceBooking` (`CompanyLeadBookingResponse?`); `owner` (`CompanyLeadEmployeeResponse?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadsResponse` | `items` (`IReadOnlyList<CompanyLeadResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadRequirementResponse` | `id` (`Guid`); `intent` (`string`); `originalQuery` (`string?`); `structuredCriteria` (`JsonElement`); `criteriaSchemaVersion` (`int`); `minBudget` (`decimal?`); `maxBudget` (`decimal?`); `currencyCode` (`string?`); `minBedrooms` (`int?`); `minBathrooms` (`int?`); `minArea` (`decimal?`); `maxArea` (`decimal?`); `paymentPlanMonths` (`int?`); `notes` (`string?`); `extractedByAI` (`bool`); `confirmedByEmployee` (`CompanyLeadEmployeeResponse?`); `confirmedAt` (`DateTimeOffset?`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadInterestResponse` | `id` (`Guid`); `listingId` (`Guid`); `listingCode` (`string`); `listingSlug` (`string`); `listingTitle` (`string`); `interestType` (`string`); `createdAt` (`DateTimeOffset`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadDetailsResponse` | `id` (`Guid`); `stage` (`string`); `priority` (`string`); `contactName` (`string?`); `contactPhone` (`string?`); `contactEmail` (`string?`); `source` (`string`); `createdAt` (`DateTimeOffset`); `lastActivityAt` (`DateTimeOffset?`); `closedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `requirementCount` (`int`); `interestCount` (`int`); `activityCount` (`int`); `customer` (`CompanyLeadCustomerResponse?`); `sourceBooking` (`CompanyLeadBookingResponse?`); `owner` (`CompanyLeadEmployeeResponse?`); `requirements` (`IReadOnlyList<CompanyLeadRequirementResponse>`); `interests` (`IReadOnlyList<CompanyLeadInterestResponse>`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadRequirementMutationResponse` | `requirement` (`CompanyLeadRequirementResponse`); `leadRowVersion` (`string`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadInterestsMutationResponse` | `interests` (`IReadOnlyList<CompanyLeadInterestResponse>`); `leadRowVersion` (`string`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadActivityResponse` | `id` (`Guid`); `activityType` (`string`); `occurredAt` (`DateTimeOffset`); `notes` (`string?`); `metadata` (`JsonElement?`); `performer` (`CompanyLeadEmployeeResponse?`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadActivitiesResponse` | `items` (`IReadOnlyList<CompanyLeadActivityResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |
| `CompanyLeadActivityMutationResponse` | `activity` (`CompanyLeadActivityResponse`); `leadRowVersion` (`string`) | [`CompanyLeadContracts.cs`](../src/EstateHub.Api/Contracts/CompanyLeads/CompanyLeadContracts.cs) |

### CompanyListings

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyListingDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `publicationStatus` (`string?`); `listingType` (`string?`); `unitId` (`Guid?`); `projectId` (`Guid?`); `currencyCode` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CreateCompanyListingRequest` | `unitId` (`Guid`); `currencyCode` (`string?`); `listingCode` (`string?`); `slug` (`string?`); `title` (`string?`); `description` (`string?`); `listingType` (`string?`); `askingPrice` (`decimal`); `rentPeriod` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `UpdateCompanyListingRequest` | `unitId` (`Guid`); `currencyCode` (`string?`); `listingCode` (`string?`); `slug` (`string?`); `title` (`string?`); `description` (`string?`); `listingType` (`string?`); `askingPrice` (`decimal`); `rentPeriod` (`string?`); `rowVersion` (`string?`); `priceChangeReason` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingMediaRequest` | `fileAssetId` (`Guid`); `sortOrder` (`int`); `isCover` (`bool`); `caption` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `ReplaceCompanyListingMediaRequest` | `rowVersion` (`string?`); `items` (`IReadOnlyList<CompanyListingMediaRequest>?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CreateCompanyListingPaymentPlanRequest` | `name` (`string?`); `totalPrice` (`decimal`); `currencyCode` (`string?`); `downPaymentPercentage` (`decimal`); `durationMonths` (`int`); `installmentFrequency` (`string?`); `cashDiscountPercentage` (`decimal?`); `isActive` (`bool?`); `rowVersion` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `UpdateCompanyListingPaymentPlanRequest` | `name` (`string?`); `totalPrice` (`decimal`); `currencyCode` (`string?`); `downPaymentPercentage` (`decimal`); `durationMonths` (`int`); `installmentFrequency` (`string?`); `cashDiscountPercentage` (`decimal?`); `isActive` (`bool?`); `rowVersion` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingLifecycleRequest` | `rowVersion` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingCurrencyResponse` | `code` (`string`); `name` (`string`); `symbol` (`string`); `decimalPlaces` (`int`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingLocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingUnitTypeResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingProjectResponse` | `id` (`Guid`); `slug` (`string`); `name` (`string`); `status` (`string`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingUnitResponse` | `id` (`Guid`); `unitCode` (`string`); `status` (`string`); `unitType` (`CompanyListingUnitTypeResponse`); `location` (`CompanyListingLocationResponse`); `project` (`CompanyListingProjectResponse?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingListItemResponse` | `id` (`Guid`); `unitId` (`Guid`); `listingCode` (`string`); `slug` (`string`); `title` (`string`); `listingType` (`string`); `askingPrice` (`decimal`); `currency` (`CompanyListingCurrencyResponse`); `rentPeriod` (`string?`); `publicationStatus` (`string`); `publishedAt` (`DateTimeOffset?`); `archivedAt` (`DateTimeOffset?`); `coverFileAssetId` (`Guid?`); `unit` (`CompanyListingUnitResponse`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingsResponse` | `items` (`IReadOnlyList<CompanyListingListItemResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingCreatedByEmployeeResponse` | `id` (`Guid`); `fullName` (`string`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingPriceHistoryResponse` | `id` (`Guid`); `price` (`decimal`); `currency` (`CompanyListingCurrencyResponse`); `effectiveFrom` (`DateTimeOffset`); `effectiveTo` (`DateTimeOffset?`); `changeReason` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingMediaResponse` | `fileAssetId` (`Guid`); `sortOrder` (`int`); `isCover` (`bool`); `caption` (`string?`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `ReplaceCompanyListingMediaResponse` | `rowVersion` (`string`); `items` (`IReadOnlyList<CompanyListingMediaResponse>`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingPaymentPlanResponse` | `id` (`Guid`); `name` (`string`); `totalPrice` (`decimal`); `currency` (`CompanyListingCurrencyResponse`); `downPaymentPercentage` (`decimal`); `durationMonths` (`int`); `installmentFrequency` (`string`); `cashDiscountPercentage` (`decimal?`); `isActive` (`bool`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingPaymentPlanMutationResponse` | `rowVersion` (`string`); `paymentPlan` (`CompanyListingPaymentPlanResponse`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |
| `CompanyListingDetailsResponse` | `id` (`Guid`); `unitId` (`Guid`); `listingCode` (`string`); `slug` (`string`); `title` (`string`); `description` (`string`); `listingType` (`string`); `askingPrice` (`decimal`); `currency` (`CompanyListingCurrencyResponse`); `rentPeriod` (`string?`); `publicationStatus` (`string`); `publishedAt` (`DateTimeOffset?`); `archivedAt` (`DateTimeOffset?`); `coverFileAssetId` (`Guid?`); `unit` (`CompanyListingUnitResponse`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`); `createdByEmployee` (`CompanyListingCreatedByEmployeeResponse`); `priceHistory` (`IReadOnlyList<CompanyListingPriceHistoryResponse>`); `media` (`IReadOnlyList<CompanyListingMediaResponse>`); `paymentPlans` (`IReadOnlyList<CompanyListingPaymentPlanResponse>`) | [`CompanyListingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyListings/CompanyListingContracts.cs) |

### CompanyManagement

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `UpdateCompanyProfileRequest` | `displayName` (`string?`); `businessEmail` (`string?`); `supportPhone` (`string?`); `website` (`string?`); `timeZoneId` (`string?`); `baseCurrencyCode` (`string?`); `addressLocationId` (`Guid`); `addressLine1` (`string?`); `addressLine2` (`string?`); `postalCode` (`string?`); `latitude` (`decimal?`); `longitude` (`decimal?`); `rowVersion` (`string?`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `EmployeeDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `status` (`string?`); `includeEnded` (`bool`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `AddCompanyEmployeeRequest` | `email` (`string?`); `fullName` (`string?`); `jobTitle` (`string?`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `UpdateCompanyEmployeeRequest` | `fullName` (`string?`); `jobTitle` (`string?`); `rowVersion` (`string?`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `EmployeeRowVersionRequest` | `rowVersion` (`string?`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `ReplaceCompanyEmployeeRolesRequest` | `roleIds` (`IReadOnlyList<Guid>?`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `TransferPrimaryContactRequest` | `targetEmployeeRowVersion` (`string?`) | [`CompanyManagementRequests.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementRequests.cs) |
| `CompanyLocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`CompanyManagementResponses.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementResponses.cs) |
| `CompanyAddressResponse` | `id` (`Guid`); `location` (`CompanyLocationResponse`); `addressLine1` (`string`); `addressLine2` (`string?`); `postalCode` (`string?`); `latitude` (`decimal?`); `longitude` (`decimal?`) | [`CompanyManagementResponses.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementResponses.cs) |
| `CompanyProfileResponse` | `id` (`Guid`); `slug` (`string`); `legalName` (`string`); `displayName` (`string`); `registrationNumber` (`string`); `taxId` (`string?`); `companyType` (`string`); `businessEmail` (`string`); `supportPhone` (`string`); `website` (`string?`); `logoFileAssetId` (`Guid?`); `coverFileAssetId` (`Guid?`); `status` (`string`); `verifiedAt` (`DateTimeOffset?`); `timeZoneId` (`string`); `baseCurrencyCode` (`string`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`); `address` (`CompanyAddressResponse`) | [`CompanyManagementResponses.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementResponses.cs) |
| `CompanyEmployeeRoleResponse` | `id` (`Guid`); `name` (`string`); `isBuiltIn` (`bool`) | [`CompanyManagementResponses.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementResponses.cs) |
| `CompanyEmployeeResponse` | `id` (`Guid`); `email` (`string`); `fullName` (`string`); `jobTitle` (`string?`); `isPrimaryContact` (`bool`); `status` (`string`); `joinedAt` (`DateTimeOffset`); `endedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `activeRoles` (`IReadOnlyList<CompanyEmployeeRoleResponse>`) | [`CompanyManagementResponses.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementResponses.cs) |
| `CompanyEmployeesResponse` | `items` (`IReadOnlyList<CompanyEmployeeResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyManagementResponses.cs`](../src/EstateHub.Api/Contracts/CompanyManagement/CompanyManagementResponses.cs) |

### CompanyProjects

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyProjectDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `status` (`string?`); `deliveryStatus` (`string?`); `locationId` (`Guid?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `CompanyProjectRequest` | `name` (`string?`); `slug` (`string?`); `description` (`string?`); `locationId` (`Guid`); `deliveryStatus` (`string?`); `expectedDeliveryDate` (`DateOnly?`); `rowVersion` (`string?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `RowVersionRequest` | `rowVersion` (`string?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `LocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `CompanyProjectResponse` | `id` (`Guid`); `name` (`string`); `slug` (`string`); `description` (`string?`); `deliveryStatus` (`string`); `expectedDeliveryDate` (`DateOnly?`); `projectStatus` (`string`); `location` (`LocationResponse`); `coverFileAssetId` (`Guid?`); `unitCount` (`int`); `publishedListingCount` (`int`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `CompanyProjectsResponse` | `items` (`IReadOnlyList<CompanyProjectResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `CompanyProjectManagementAmenityResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`); `scope` (`string`); `isActive` (`bool`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `CompanyProjectManagementDetailsResponse` | `id` (`Guid`); `name` (`string`); `slug` (`string`); `description` (`string?`); `deliveryStatus` (`string`); `expectedDeliveryDate` (`DateOnly?`); `projectStatus` (`string`); `location` (`LocationResponse`); `coverFileAssetId` (`Guid?`); `unitCount` (`int`); `publishedListingCount` (`int`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`); `amenities` (`IReadOnlyList<CompanyProjectManagementAmenityResponse>`); `media` (`IReadOnlyList<ProjectMediaReplacementItemResponse>`); `nearbyPlaces` (`IReadOnlyList<ProjectNearbyPlaceResponse>`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ReplaceProjectAmenitiesRequest` | `rowVersion` (`string?`); `amenityIds` (`IReadOnlyList<Guid>?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ProjectMediaRequest` | `fileAssetId` (`Guid`); `sortOrder` (`int`); `isCover` (`bool`); `caption` (`string?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ReplaceProjectMediaRequest` | `rowVersion` (`string?`); `items` (`IReadOnlyList<ProjectMediaRequest>?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ProjectAmenityResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`); `scope` (`string`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ProjectMediaReplacementItemResponse` | `fileAssetId` (`Guid`); `sortOrder` (`int`); `isCover` (`bool`); `caption` (`string?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ReplaceProjectAmenitiesResponse` | `rowVersion` (`string`); `amenities` (`IReadOnlyList<ProjectAmenityResponse>`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ReplaceProjectMediaResponse` | `rowVersion` (`string`); `items` (`IReadOnlyList<ProjectMediaReplacementItemResponse>`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ProjectNearbyPlaceRequest` | `rowVersion` (`string?`); `category` (`string?`); `name` (`string?`); `distanceMeters` (`decimal?`); `travelMinutes` (`int?`); `latitude` (`decimal?`); `longitude` (`decimal?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ProjectNearbyPlaceResponse` | `id` (`Guid`); `category` (`string`); `name` (`string`); `distanceMeters` (`decimal?`); `travelMinutes` (`int?`); `latitude` (`decimal?`); `longitude` (`decimal?`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |
| `ProjectNearbyPlaceMutationResponse` | `rowVersion` (`string`); `nearbyPlace` (`ProjectNearbyPlaceResponse`) | [`CompanyProjectContracts.cs`](../src/EstateHub.Api/Contracts/CompanyProjects/CompanyProjectContracts.cs) |

### CompanyRoles

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CreateCompanyRoleRequest` | `name` (`string?`); `permissionCodes` (`IReadOnlyList<string?>?`) | [`CompanyRoleRequests.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleRequests.cs) |
| `UpdateCompanyRoleRequest` | `name` (`string?`) | [`CompanyRoleRequests.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleRequests.cs) |
| `ReplaceCompanyRolePermissionsRequest` | `permissionCodes` (`IReadOnlyList<string?>?`) | [`CompanyRoleRequests.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleRequests.cs) |
| `CompanyPermissionResponse` | `id` (`Guid`); `code` (`string`); `name` (`string`); `description` (`string?`) | [`CompanyRoleResponses.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleResponses.cs) |
| `CompanyPermissionGroupResponse` | `id` (`Guid`); `name` (`string`); `description` (`string?`); `sortOrder` (`int`); `permissions` (`IReadOnlyList<CompanyPermissionResponse>`) | [`CompanyRoleResponses.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleResponses.cs) |
| `CompanyRoleSummaryResponse` | `id` (`Guid`); `name` (`string`); `isBuiltIn` (`bool`); `isActive` (`bool`); `activePermissionCodes` (`IReadOnlyList<string>`); `activeEmployeeAssignmentCount` (`int`) | [`CompanyRoleResponses.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleResponses.cs) |
| `CompanyRoleDetailsResponse` | `id` (`Guid`); `name` (`string`); `isBuiltIn` (`bool`); `isActive` (`bool`); `activePermissionCodes` (`IReadOnlyList<string>`); `activeEmployeeAssignmentCount` (`int`); `activePermissions` (`IReadOnlyList<CompanyPermissionResponse>`) | [`CompanyRoleResponses.cs`](../src/EstateHub.Api/Contracts/CompanyRoles/CompanyRoleResponses.cs) |

### CompanyUnits

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyUnitDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `status` (`string?`); `projectId` (`Guid?`); `unitTypeId` (`Guid?`); `locationId` (`Guid?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CreateCompanyUnitRequest` | `projectId` (`Guid?`); `locationId` (`Guid`); `unitTypeId` (`Guid`); `unitCode` (`string?`); `finishingType` (`string?`); `bedrooms` (`int`); `bathrooms` (`int`); `floorNumber` (`int?`); `totalFloors` (`int?`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `furnishedStatus` (`string?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `UpdateCompanyUnitRequest` | `projectId` (`Guid?`); `locationId` (`Guid`); `unitTypeId` (`Guid`); `unitCode` (`string?`); `finishingType` (`string?`); `bedrooms` (`int`); `bathrooms` (`int`); `floorNumber` (`int?`); `totalFloors` (`int?`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `furnishedStatus` (`string?`); `rowVersion` (`string?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `UpdateCompanyUnitStatusRequest` | `status` (`string?`); `rowVersion` (`string?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitLocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitTypeResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitProjectResponse` | `id` (`Guid`); `slug` (`string`); `name` (`string`); `deliveryStatus` (`string`); `status` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitListItemResponse` | `id` (`Guid`); `projectId` (`Guid?`); `unitCode` (`string`); `finishingType` (`string?`); `bedrooms` (`int`); `bathrooms` (`int`); `floorNumber` (`int?`); `totalFloors` (`int?`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `furnishedStatus` (`string`); `status` (`string`); `location` (`CompanyUnitLocationResponse`); `unitType` (`CompanyUnitTypeResponse`); `project` (`CompanyUnitProjectResponse?`); `listingCount` (`int`); `publishedListingCount` (`int`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitDetailsResponse` | `id` (`Guid`); `projectId` (`Guid?`); `unitCode` (`string`); `finishingType` (`string?`); `bedrooms` (`int`); `bathrooms` (`int`); `floorNumber` (`int?`); `totalFloors` (`int?`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `furnishedStatus` (`string`); `status` (`string`); `location` (`CompanyUnitLocationResponse`); `unitType` (`CompanyUnitTypeResponse`); `project` (`CompanyUnitProjectResponse?`); `listingCount` (`int`); `publishedListingCount` (`int`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitsResponse` | `items` (`IReadOnlyList<CompanyUnitListItemResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitStatusResponse` | `status` (`string`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `ReplaceCompanyUnitAmenitiesRequest` | `rowVersion` (`string?`); `amenityIds` (`IReadOnlyList<Guid>?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitAmenityResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`); `scope` (`string`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `ReplaceCompanyUnitAmenitiesResponse` | `rowVersion` (`string`); `amenities` (`IReadOnlyList<CompanyUnitAmenityResponse>`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitNearbyPlaceRequest` | `category` (`string?`); `name` (`string?`); `distanceMeters` (`decimal?`); `travelMinutes` (`int?`); `latitude` (`decimal?`); `longitude` (`decimal?`); `rowVersion` (`string?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitNearbyPlaceResponse` | `id` (`Guid`); `category` (`string`); `name` (`string`); `distanceMeters` (`decimal?`); `travelMinutes` (`int?`); `latitude` (`decimal?`); `longitude` (`decimal?`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitNearbyPlaceMutationResponse` | `rowVersion` (`string`); `nearbyPlace` (`CompanyUnitNearbyPlaceResponse`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitManagementAmenityResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`); `scope` (`string`); `isActive` (`bool`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |
| `CompanyUnitManagementDetailsResponse` | `id` (`Guid`); `projectId` (`Guid?`); `unitCode` (`string`); `finishingType` (`string?`); `bedrooms` (`int`); `bathrooms` (`int`); `floorNumber` (`int?`); `totalFloors` (`int?`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `furnishedStatus` (`string`); `status` (`string`); `location` (`CompanyUnitLocationResponse`); `unitType` (`CompanyUnitTypeResponse`); `project` (`CompanyUnitProjectResponse?`); `listingCount` (`int`); `publishedListingCount` (`int`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `rowVersion` (`string`); `amenities` (`IReadOnlyList<CompanyUnitManagementAmenityResponse>`); `nearbyPlaces` (`IReadOnlyList<CompanyUnitNearbyPlaceResponse>`) | [`CompanyUnitContracts.cs`](../src/EstateHub.Api/Contracts/CompanyUnits/CompanyUnitContracts.cs) |

### CompanyViewingBookings

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyViewingBookingDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `status` (`string?`); `listingId` (`Guid?`); `viewingSlotId` (`Guid?`); `assignedEmployeeId` (`Guid?`); `from` (`DateTimeOffset?`); `to` (`DateTimeOffset?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingAssignmentRequest` | `employeeId` (`Guid?`); `rowVersion` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingLifecycleRequest` | `rowVersion` (`string?`); `reason` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingRescheduleRequest` | `toViewingSlotId` (`Guid`); `rowVersion` (`string?`); `reason` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingCustomerResponse` | `id` (`Guid`); `fullName` (`string`); `persona` (`string`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingSlotResponse` | `id` (`Guid`); `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `meetingPoint` (`string`); `status` (`string`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingListingResponse` | `id` (`Guid`); `listingCode` (`string`); `slug` (`string`); `title` (`string`); `listingType` (`string`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingEmployeeResponse` | `id` (`Guid`); `fullName` (`string`); `jobTitle` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingChargeResponse` | `id` (`Guid`); `amountSnapshot` (`decimal`); `currencyCode` (`string`); `status` (`string`); `createdAt` (`DateTimeOffset`); `voidedAt` (`DateTimeOffset?`); `voidReason` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingResponse` | `id` (`Guid`); `bookingCode` (`string`); `status` (`string`); `visitorCount` (`int`); `contactPhone` (`string`); `createdAt` (`DateTimeOffset`); `confirmedAt` (`DateTimeOffset?`); `checkedInAt` (`DateTimeOffset?`); `cancelledAt` (`DateTimeOffset?`); `completedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `customer` (`CompanyViewingBookingCustomerResponse`); `slot` (`CompanyViewingBookingSlotResponse`); `listing` (`CompanyViewingBookingListingResponse`); `assignedEmployee` (`CompanyViewingBookingEmployeeResponse?`); `charge` (`CompanyViewingBookingChargeResponse?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingsResponse` | `items` (`IReadOnlyList<CompanyViewingBookingResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingStatusHistoryResponse` | `id` (`Guid`); `fromStatus` (`string?`); `toStatus` (`string`); `actorType` (`string`); `changedAt` (`DateTimeOffset`); `reason` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingRescheduleHistoryResponse` | `id` (`Guid`); `fromViewingSlotId` (`Guid`); `fromStartsAt` (`DateTimeOffset`); `fromEndsAt` (`DateTimeOffset`); `toViewingSlotId` (`Guid`); `toStartsAt` (`DateTimeOffset`); `toEndsAt` (`DateTimeOffset`); `changedAt` (`DateTimeOffset`); `reason` (`string?`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |
| `CompanyViewingBookingDetailsResponse` | `id` (`Guid`); `bookingCode` (`string`); `status` (`string`); `visitorCount` (`int`); `contactPhone` (`string`); `specialRequests` (`string?`); `cancellationSource` (`string?`); `createdAt` (`DateTimeOffset`); `confirmedAt` (`DateTimeOffset?`); `checkedInAt` (`DateTimeOffset?`); `cancelledAt` (`DateTimeOffset?`); `completedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `customer` (`CompanyViewingBookingCustomerResponse`); `slot` (`CompanyViewingBookingSlotResponse`); `listing` (`CompanyViewingBookingListingResponse`); `assignedEmployee` (`CompanyViewingBookingEmployeeResponse?`); `charge` (`CompanyViewingBookingChargeResponse?`); `statusHistory` (`IReadOnlyList<CompanyViewingBookingStatusHistoryResponse>`); `rescheduleHistory` (`IReadOnlyList<CompanyViewingBookingRescheduleHistoryResponse>`) | [`CompanyViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingBookings/CompanyViewingBookingContracts.cs) |

### CompanyViewingSlots

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CompanyViewingSlotDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `listingId` (`Guid?`); `status` (`string?`); `from` (`DateTimeOffset?`); `to` (`DateTimeOffset?`) | [`CompanyViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingSlots/CompanyViewingSlotContracts.cs) |
| `CreateCompanyViewingSlotRequest` | `listingId` (`Guid`); `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `capacity` (`int`); `meetingPoint` (`string?`) | [`CompanyViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingSlots/CompanyViewingSlotContracts.cs) |
| `UpdateCompanyViewingSlotRequest` | `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `capacity` (`int`); `meetingPoint` (`string?`); `rowVersion` (`string?`) | [`CompanyViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingSlots/CompanyViewingSlotContracts.cs) |
| `CompanyViewingSlotLifecycleRequest` | `rowVersion` (`string?`) | [`CompanyViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingSlots/CompanyViewingSlotContracts.cs) |
| `CompanyViewingSlotResponse` | `id` (`Guid`); `listingId` (`Guid`); `listingCode` (`string`); `listingSlug` (`string`); `listingTitle` (`string`); `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `capacity` (`int`); `meetingPoint` (`string`); `status` (`string`); `activeBookingCount` (`int`); `reservedVisitorCount` (`int`); `rowVersion` (`string`) | [`CompanyViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingSlots/CompanyViewingSlotContracts.cs) |
| `CompanyViewingSlotsResponse` | `items` (`IReadOnlyList<CompanyViewingSlotResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CompanyViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/CompanyViewingSlots/CompanyViewingSlotContracts.cs) |

### Customers

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `UpdateCustomerProfileRequest` | `fullName` (`string?`); `persona` (`string?`); `phoneNumber` (`string?`) | [`CustomerRequests.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerRequests.cs) |
| `SaveSearchRequest` | `name` (`string?`); `filters` (`JsonElement?`); `alertsEnabled` (`bool`) | [`CustomerRequests.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerRequests.cs) |
| `CustomerProfileResponse` | `id` (`Guid`); `fullName` (`string`); `persona` (`string`); `email` (`string`); `phoneNumber` (`string?`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`) | [`CustomerResponses.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerResponses.cs) |
| `CustomerFavoriteResponse` | `favoriteId` (`Guid`); `createdAt` (`DateTimeOffset`); `listing` (`ListingDirectoryItemResponse`) | [`CustomerResponses.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerResponses.cs) |
| `CustomerFavoritesResponse` | `items` (`IReadOnlyList<CustomerFavoriteResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CustomerResponses.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerResponses.cs) |
| `SavedSearchResponse` | `id` (`Guid`); `name` (`string`); `filters` (`JsonElement`); `filterSchemaVersion` (`int`); `alertsEnabled` (`bool`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`) | [`CustomerResponses.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerResponses.cs) |
| `SavedSearchesResponse` | `items` (`IReadOnlyList<SavedSearchResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CustomerResponses.cs`](../src/EstateHub.Api/Contracts/Customers/CustomerResponses.cs) |

### Files

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `UploadFileAssetRequest` | `file` (`IFormFile?`); `fileType` (`string?`) | [`FileAssetContracts.cs`](../src/EstateHub.Api/Contracts/Files/FileAssetContracts.cs) |
| `FileAssetMetadataResponse` | `id` (`Guid`); `fileType` (`string`); `originalFileName` (`string?`); `contentType` (`string`); `sizeBytes` (`long`); `width` (`int?`); `height` (`int?`); `createdAt` (`DateTimeOffset`) | [`FileAssetContracts.cs`](../src/EstateHub.Api/Contracts/Files/FileAssetContracts.cs) |
| `FileAssetDirectoryResponse` | `items` (`IReadOnlyList<FileAssetMetadataResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`FileAssetContracts.cs`](../src/EstateHub.Api/Contracts/Files/FileAssetContracts.cs) |

### Listings

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `ListingDetailsCompanyResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `logoFileAssetId` (`Guid?`); `businessEmail` (`string`); `supportPhone` (`string`); `website` (`string?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingDetailsProjectDeveloperResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `logoFileAssetId` (`Guid?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingDetailsProjectResponse` | `id` (`Guid`); `slug` (`string`); `name` (`string`); `deliveryStatus` (`string`); `expectedDeliveryDate` (`DateOnly?`); `developerCompany` (`ListingDetailsProjectDeveloperResponse`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingDetailsUnitResponse` | `id` (`Guid`); `bedrooms` (`int`); `bathrooms` (`int`); `floorNumber` (`int?`); `totalFloors` (`int?`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `finishingType` (`string?`); `furnishedStatus` (`string`); `status` (`string`); `unitType` (`ListingUnitTypeResponse`); `location` (`ListingLocationResponse`); `project` (`ListingDetailsProjectResponse?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingMediaResponse` | `fileAssetId` (`Guid`); `sortOrder` (`int`); `isCover` (`bool`); `caption` (`string?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingPaymentPlanResponse` | `id` (`Guid`); `name` (`string`); `totalPrice` (`decimal`); `currency` (`ListingCurrencyResponse`); `downPaymentPercentage` (`decimal`); `durationMonths` (`int`); `installmentFrequency` (`string`); `cashDiscountPercentage` (`decimal?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingAmenityResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingNearbyPlaceResponse` | `id` (`Guid`); `category` (`string`); `name` (`string`); `distanceMeters` (`decimal?`); `travelMinutes` (`int?`); `latitude` (`decimal?`); `longitude` (`decimal?`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingDetailsResponse` | `id` (`Guid`); `slug` (`string`); `title` (`string`); `description` (`string`); `listingType` (`string`); `askingPrice` (`decimal`); `rentPeriod` (`string?`); `publishedAt` (`DateTimeOffset?`); `coverFileAssetId` (`Guid?`); `currency` (`ListingCurrencyResponse`); `company` (`ListingDetailsCompanyResponse`); `unit` (`ListingDetailsUnitResponse`); `media` (`IReadOnlyList<ListingMediaResponse>`); `paymentPlans` (`IReadOnlyList<ListingPaymentPlanResponse>`); `unitAmenities` (`IReadOnlyList<ListingAmenityResponse>`); `projectAmenities` (`IReadOnlyList<ListingAmenityResponse>`); `unitNearbyPlaces` (`IReadOnlyList<ListingNearbyPlaceResponse>`); `projectNearbyPlaces` (`IReadOnlyList<ListingNearbyPlaceResponse>`) | [`ListingDetailsResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDetailsResponses.cs) |
| `ListingDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `listingType` (`string?`); `locationId` (`Guid?`); `unitTypeId` (`Guid?`); `companyId` (`Guid?`); `projectId` (`Guid?`); `currencyCode` (`string?`); `minPrice` (`decimal?`); `maxPrice` (`decimal?`); `minBedrooms` (`int?`); `maxBedrooms` (`int?`); `minBathrooms` (`int?`); `maxBathrooms` (`int?`); `minArea` (`decimal?`); `maxArea` (`decimal?`); `sort` (`string?`) | [`ListingDirectoryRequest.cs`](../src/EstateHub.Api/Contracts/Listings/ListingDirectoryRequest.cs) |
| `ListingCurrencyResponse` | `code` (`string`); `name` (`string`); `symbol` (`string`); `decimalPlaces` (`int`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingCompanyResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `logoFileAssetId` (`Guid?`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingUnitTypeResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingLocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingProjectResponse` | `id` (`Guid`); `slug` (`string`); `name` (`string`); `developerCompanySlug` (`string`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingUnitResponse` | `id` (`Guid`); `bedrooms` (`int`); `bathrooms` (`int`); `builtUpArea` (`decimal`); `landArea` (`decimal?`); `finishingType` (`string?`); `furnishedStatus` (`string`); `status` (`string`); `unitType` (`ListingUnitTypeResponse`); `location` (`ListingLocationResponse`); `project` (`ListingProjectResponse?`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingDirectoryItemResponse` | `id` (`Guid`); `slug` (`string`); `title` (`string`); `listingType` (`string`); `askingPrice` (`decimal`); `rentPeriod` (`string?`); `publishedAt` (`DateTimeOffset?`); `coverFileAssetId` (`Guid?`); `hasActivePaymentPlan` (`bool`); `currency` (`ListingCurrencyResponse`); `company` (`ListingCompanyResponse`); `unit` (`ListingUnitResponse`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |
| `ListingDirectoryResponse` | `items` (`IReadOnlyList<ListingDirectoryItemResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`ListingResponses.cs`](../src/EstateHub.Api/Contracts/Listings/ListingResponses.cs) |

### Notifications

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `NotificationDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `state` (`string?`) | [`NotificationContracts.cs`](../src/EstateHub.Api/Contracts/Notifications/NotificationContracts.cs) |
| `NotificationResponse` | `id` (`Guid`); `type` (`string`); `title` (`string`); `body` (`string`); `payload` (`JsonElement?`); `createdAt` (`DateTimeOffset`); `readAt` (`DateTimeOffset?`); `archivedAt` (`DateTimeOffset?`); `isRead` (`bool`); `isArchived` (`bool`) | [`NotificationContracts.cs`](../src/EstateHub.Api/Contracts/Notifications/NotificationContracts.cs) |
| `NotificationDirectoryResponse` | `items` (`IReadOnlyList<NotificationResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`NotificationContracts.cs`](../src/EstateHub.Api/Contracts/Notifications/NotificationContracts.cs) |
| `NotificationUnreadCountResponse` | `unreadCount` (`int`) | [`NotificationContracts.cs`](../src/EstateHub.Api/Contracts/Notifications/NotificationContracts.cs) |

### PlatformCompanyApplications

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `PlatformRowVersionRequest` | `rowVersion` (`string?`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformDocumentVerificationRequest` | `applicationRowVersion` (`string?`); `verificationStatus` (`string?`); `rejectionReason` (`string?`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformDecisionRequest` | `rowVersion` (`string?`); `reason` (`string?`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformCompanyApprovalRequest` | `rowVersion` (`string?`); `slug` (`string?`); `displayName` (`string?`); `locationId` (`Guid`); `addressLine1` (`string?`); `addressLine2` (`string?`); `postalCode` (`string?`); `latitude` (`decimal?`); `longitude` (`decimal?`); `timeZoneId` (`string?`); `baseCurrencyCode` (`string?`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformCompanyApplicationSummaryResponse` | `id` (`Guid`); `applicantApplicationUserId` (`Guid`); `applicantName` (`string?`); `applicantEmail` (`string?`); `legalName` (`string`); `registrationNumber` (`string`); `businessEmail` (`string`); `companyType` (`string`); `status` (`string`); `submittedAt` (`DateTimeOffset?`); `reviewedAt` (`DateTimeOffset?`); `reviewedByApplicationUserId` (`Guid?`); `decisionReason` (`string?`); `documentCount` (`int`); `pendingDocumentCount` (`int`); `approvedDocumentCount` (`int`); `rejectedDocumentCount` (`int`); `approvedCompanyId` (`Guid?`); `rowVersion` (`string`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformCompanyApplicationDirectoryResponse` | `items` (`IReadOnlyList<PlatformCompanyApplicationSummaryResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformApprovedCompanySummaryResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformCompanyApplicationDocumentResponse` | `id` (`Guid`); `documentType` (`string`); `fileAssetId` (`Guid`); `originalFileName` (`string?`); `contentType` (`string`); `sizeBytes` (`long`); `verificationStatus` (`string`); `reviewedAt` (`DateTimeOffset?`); `reviewedByApplicationUserId` (`Guid?`); `rejectionReason` (`string?`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformCompanyApplicationHistoryResponse` | `id` (`Guid`); `fromStatus` (`string?`); `toStatus` (`string`); `changedByApplicationUserId` (`Guid?`); `changedAt` (`DateTimeOffset`); `reason` (`string?`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformCompanyApplicationDetailsResponse` | `id` (`Guid`); `applicantApplicationUserId` (`Guid`); `applicantName` (`string?`); `applicantEmail` (`string?`); `legalName` (`string`); `businessEmail` (`string`); `phoneNumber` (`string`); `registrationNumber` (`string`); `taxId` (`string?`); `website` (`string?`); `companyType` (`string`); `officeAddress` (`string`); `locationText` (`string`); `estimatedPropertyRange` (`string?`); `status` (`string`); `submittedAt` (`DateTimeOffset?`); `reviewedAt` (`DateTimeOffset?`); `reviewedByApplicationUserId` (`Guid?`); `decisionReason` (`string?`); `approvedCompany` (`PlatformApprovedCompanySummaryResponse?`); `rowVersion` (`string`); `documents` (`IReadOnlyList<PlatformCompanyApplicationDocumentResponse>`); `statusHistory` (`IReadOnlyList<PlatformCompanyApplicationHistoryResponse>`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformDocumentDecisionResponse` | `applicationRowVersion` (`string`); `document` (`PlatformCompanyApplicationDocumentResponse`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |
| `PlatformApprovedCompanyResponse` | `companyId` (`Guid`); `slug` (`string`); `displayName` (`string`); `companyEmployeeId` (`Guid`); `primaryContactFullName` (`string`); `ownerBootstrapRequired` (`bool`) | [`PlatformCompanyApplicationContracts.cs`](../src/EstateHub.Api/Contracts/PlatformCompanyApplications/PlatformCompanyApplicationContracts.cs) |

### PlatformReviews

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `PlatformReviewDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `status` (`string?`); `rating` (`int?`); `companyId` (`Guid?`); `createdFrom` (`DateTimeOffset?`); `createdTo` (`DateTimeOffset?`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewModerationRequest` | `expectedStatus` (`string?`); `status` (`string?`); `reason` (`string?`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewCompanyResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewListingResponse` | `id` (`Guid`); `slug` (`string`); `title` (`string`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewReviewerResponse` | `id` (`Guid`); `fullName` (`string`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewSummaryResponse` | `id` (`Guid`); `viewingBookingId` (`Guid`); `rating` (`int`); `comment` (`string?`); `status` (`string`); `createdAt` (`DateTimeOffset`); `moderatedByApplicationUserId` (`Guid?`); `moderatedAt` (`DateTimeOffset?`); `moderationReason` (`string?`); `company` (`PlatformReviewCompanyResponse`); `listing` (`PlatformReviewListingResponse`); `reviewer` (`PlatformReviewReviewerResponse`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewDirectoryResponse` | `items` (`IReadOnlyList<PlatformReviewSummaryResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |
| `PlatformReviewDetailsResponse` | `id` (`Guid`); `viewingBookingId` (`Guid`); `rating` (`int`); `comment` (`string?`); `status` (`string`); `createdAt` (`DateTimeOffset`); `moderatedByApplicationUserId` (`Guid?`); `moderatedAt` (`DateTimeOffset?`); `moderationReason` (`string?`); `company` (`PlatformReviewCompanyResponse`); `listing` (`PlatformReviewListingResponse`); `reviewer` (`PlatformReviewReviewerResponse`); `bookingCode` (`string`); `bookingStatus` (`string`); `completedAt` (`DateTimeOffset?`); `startsAt` (`DateTimeOffset`) | [`PlatformReviewContracts.cs`](../src/EstateHub.Api/Contracts/PlatformReviews/PlatformReviewContracts.cs) |

### Projects

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `ProjectDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `search` (`string?`); `deliveryStatus` (`string?`); `locationId` (`Guid?`) | [`ProjectDirectoryRequest.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectDirectoryRequest.cs) |
| `ProjectLocationResponse` | `id` (`Guid`); `type` (`string`); `nameEn` (`string`); `nameAr` (`string`); `slug` (`string`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectDeveloperResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `logoFileAssetId` (`Guid?`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectDirectoryItemResponse` | `id` (`Guid`); `slug` (`string`); `name` (`string`); `deliveryStatus` (`string`); `expectedDeliveryDate` (`DateOnly?`); `coverFileAssetId` (`Guid?`); `location` (`ProjectLocationResponse`); `publishedListingCount` (`int`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectDirectoryResponse` | `items` (`IReadOnlyList<ProjectDirectoryItemResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectMediaResponse` | `fileAssetId` (`Guid`); `sortOrder` (`int`); `isCover` (`bool`); `caption` (`string?`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectAmenityResponse` | `id` (`Guid`); `code` (`string`); `nameEn` (`string`); `nameAr` (`string`); `iconKey` (`string?`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectNearbyPlaceResponse` | `id` (`Guid`); `category` (`string`); `name` (`string`); `distanceMeters` (`decimal?`); `travelMinutes` (`int?`); `latitude` (`decimal?`); `longitude` (`decimal?`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |
| `ProjectDetailsResponse` | `id` (`Guid`); `slug` (`string`); `name` (`string`); `description` (`string?`); `deliveryStatus` (`string`); `expectedDeliveryDate` (`DateOnly?`); `coverFileAssetId` (`Guid?`); `publishedListingCount` (`int`); `developerCompany` (`ProjectDeveloperResponse`); `location` (`ProjectLocationResponse`); `media` (`IReadOnlyList<ProjectMediaResponse>`); `amenities` (`IReadOnlyList<ProjectAmenityResponse>`); `nearbyPlaces` (`IReadOnlyList<ProjectNearbyPlaceResponse>`) | [`ProjectResponses.cs`](../src/EstateHub.Api/Contracts/Projects/ProjectResponses.cs) |

### Promotions

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `PromotionPackageCurrencyResponse` | `code` (`string`); `name` (`string`); `symbol` (`string`); `decimalPlaces` (`int`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `PromotionPackageResponse` | `id` (`Guid`); `name` (`string`); `placement` (`string`); `durationDays` (`int`); `price` (`decimal`); `currency` (`PromotionPackageCurrencyResponse`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `PromotedListingResponse` | `placement` (`string`); `promotionStartsAt` (`DateTimeOffset`); `promotionEndsAt` (`DateTimeOffset`); `listing` (`ListingDirectoryItemResponse`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `PromotedListingsResponse` | `items` (`IReadOnlyList<PromotedListingResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `CompanyPromotionListingResponse` | `id` (`Guid`); `listingCode` (`string`); `slug` (`string`); `title` (`string`); `listingType` (`string`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `CompanyPromotionInvoiceResponse` | `id` (`Guid`); `invoiceNumber` (`string`); `status` (`string`); `total` (`decimal`); `paidAt` (`DateTimeOffset?`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `CompanyListingPromotionResponse` | `id` (`Guid`); `listing` (`CompanyPromotionListingResponse`); `promotionPackageId` (`Guid`); `packageName` (`string`); `amountSnapshot` (`decimal`); `currencyCode` (`string`); `placementSnapshot` (`string`); `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `effectiveStatus` (`string`); `isCurrentlyActive` (`bool`); `createdAt` (`DateTimeOffset`); `rowVersion` (`string`); `invoice` (`CompanyPromotionInvoiceResponse`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `CompanyListingPromotionsResponse` | `items` (`IReadOnlyList<CompanyListingPromotionResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `CheckoutPromotionRequest` | `listingId` (`Guid`); `promotionPackageId` (`Guid`); `startsAt` (`DateTimeOffset?`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `PromotionCancellationRequest` | `rowVersion` (`string?`); `reason` (`string?`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `PromotionPaymentTransactionResponse` | `id` (`Guid`); `amount` (`decimal`); `currencyCode` (`string`); `provider` (`string`); `providerReference` (`string`); `status` (`string`); `createdAt` (`DateTimeOffset`); `completedAt` (`DateTimeOffset?`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `PromotionCheckoutResponse` | `promotion` (`CompanyListingPromotionResponse`); `invoice` (`CompanyPromotionInvoiceResponse`); `paymentTransaction` (`PromotionPaymentTransactionResponse?`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |
| `CompanyListingPromotionDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `listingId` (`Guid?`); `status` (`string?`); `placement` (`string?`); `from` (`DateTimeOffset?`); `to` (`DateTimeOffset?`) | [`PromotionContracts.cs`](../src/EstateHub.Api/Contracts/Promotions/PromotionContracts.cs) |

### Reviews

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `PublicCompanyReviewRequest` | `pageNumber` (`int`); `pageSize` (`int`); `rating` (`int?`) | [`ReviewContracts.cs`](../src/EstateHub.Api/Contracts/Reviews/ReviewContracts.cs) |
| `ReviewRequest` | `rating` (`int`); `comment` (`string?`) | [`ReviewContracts.cs`](../src/EstateHub.Api/Contracts/Reviews/ReviewContracts.cs) |
| `PublicCompanyReviewResponse` | `id` (`Guid`); `rating` (`int`); `comment` (`string?`); `createdAt` (`DateTimeOffset`); `reviewerName` (`string`) | [`ReviewContracts.cs`](../src/EstateHub.Api/Contracts/Reviews/ReviewContracts.cs) |
| `PublicCompanyReviewsResponse` | `companyId` (`Guid`); `companySlug` (`string`); `averageRating` (`double?`); `reviewCount` (`int`); `filteredCount` (`int`); `items` (`IReadOnlyList<PublicCompanyReviewResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`ReviewContracts.cs`](../src/EstateHub.Api/Contracts/Reviews/ReviewContracts.cs) |
| `CustomerReviewResponse` | `id` (`Guid`); `viewingBookingId` (`Guid`); `rating` (`int`); `comment` (`string?`); `status` (`string`); `createdAt` (`DateTimeOffset`) | [`ReviewContracts.cs`](../src/EstateHub.Api/Contracts/Reviews/ReviewContracts.cs) |

### Subscriptions

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `SubscriptionPlanCurrencyResponse` | `code` (`string`); `name` (`string`); `symbol` (`string`); `decimalPlaces` (`int`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `SubscriptionPlanResponse` | `id` (`Guid`); `name` (`string`); `monthlyPrice` (`decimal`); `maxPublishedListings` (`int?`); `bookingFeeAmount` (`decimal`); `currency` (`SubscriptionPlanCurrencyResponse`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `CompanySubscriptionResponse` | `id` (`Guid`); `subscriptionPlanId` (`Guid`); `planName` (`string`); `status` (`string`); `currentPeriodStart` (`DateTimeOffset`); `currentPeriodEnd` (`DateTimeOffset`); `monthlyPriceSnapshot` (`decimal`); `currencyCode` (`string`); `maxPublishedListingsSnapshot` (`int?`); `bookingFeeSnapshot` (`decimal`); `startedAt` (`DateTimeOffset`); `cancellationRequestedAt` (`DateTimeOffset?`); `endedAt` (`DateTimeOffset?`); `createdAt` (`DateTimeOffset`); `updatedAt` (`DateTimeOffset`); `isCurrentlyEffective` (`bool`); `isCancellationScheduled` (`bool`); `rowVersion` (`string`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `CompanySubscriptionHistoryResponse` | `items` (`IReadOnlyList<CompanySubscriptionResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `CheckoutSubscriptionRequest` | `subscriptionPlanId` (`Guid`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `SubscriptionInvoiceResponse` | `id` (`Guid`); `invoiceNumber` (`string`); `status` (`string`); `currencyCode` (`string`); `subtotal` (`decimal`); `total` (`decimal`); `periodStart` (`DateTimeOffset`); `periodEnd` (`DateTimeOffset`); `issuedAt` (`DateTimeOffset`); `paidAt` (`DateTimeOffset`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `SubscriptionPaymentTransactionResponse` | `id` (`Guid`); `amount` (`decimal`); `currencyCode` (`string`); `provider` (`string`); `providerReference` (`string`); `status` (`string`); `createdAt` (`DateTimeOffset`); `completedAt` (`DateTimeOffset?`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `CheckoutSubscriptionResponse` | `subscription` (`CompanySubscriptionResponse`); `invoice` (`SubscriptionInvoiceResponse`); `paymentTransaction` (`SubscriptionPaymentTransactionResponse?`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `SubscriptionLifecycleRequest` | `rowVersion` (`string?`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |
| `CompanySubscriptionHistoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `status` (`string?`) | [`SubscriptionContracts.cs`](../src/EstateHub.Api/Contracts/Subscriptions/SubscriptionContracts.cs) |

### ViewingBookings

| Contract | Fields (JSON name and CLR type) | Source |
|---|---|---|
| `CustomerViewingBookingDirectoryRequest` | `pageNumber` (`int`); `pageSize` (`int`); `status` (`string?`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CreateCustomerViewingBookingRequest` | `viewingSlotId` (`Guid`); `visitorCount` (`int`); `contactPhone` (`string?`); `specialRequests` (`string?`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CancelCustomerViewingBookingRequest` | `rowVersion` (`string?`); `reason` (`string?`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerBookingSlotResponse` | `id` (`Guid`); `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `meetingPoint` (`string`); `status` (`string`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerBookingListingResponse` | `id` (`Guid`); `slug` (`string`); `title` (`string`); `listingType` (`string`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerBookingCompanyResponse` | `id` (`Guid`); `slug` (`string`); `displayName` (`string`); `logoFileAssetId` (`Guid?`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerViewingBookingResponse` | `id` (`Guid`); `bookingCode` (`string`); `status` (`string`); `visitorCount` (`int`); `contactPhone` (`string`); `createdAt` (`DateTimeOffset`); `confirmedAt` (`DateTimeOffset?`); `checkedInAt` (`DateTimeOffset?`); `cancelledAt` (`DateTimeOffset?`); `completedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `slot` (`CustomerBookingSlotResponse`); `listing` (`CustomerBookingListingResponse`); `company` (`CustomerBookingCompanyResponse`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerViewingBookingsResponse` | `items` (`IReadOnlyList<CustomerViewingBookingResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerBookingStatusHistoryResponse` | `id` (`Guid`); `fromStatus` (`string?`); `toStatus` (`string`); `actorType` (`string`); `changedAt` (`DateTimeOffset`); `reason` (`string?`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerBookingRescheduleHistoryResponse` | `id` (`Guid`); `fromViewingSlotId` (`Guid`); `fromStartsAt` (`DateTimeOffset`); `fromEndsAt` (`DateTimeOffset`); `toViewingSlotId` (`Guid`); `toStartsAt` (`DateTimeOffset`); `toEndsAt` (`DateTimeOffset`); `changedAt` (`DateTimeOffset`); `reason` (`string?`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `CustomerViewingBookingDetailsResponse` | `id` (`Guid`); `bookingCode` (`string`); `status` (`string`); `visitorCount` (`int`); `contactPhone` (`string`); `specialRequests` (`string?`); `cancellationSource` (`string?`); `createdAt` (`DateTimeOffset`); `confirmedAt` (`DateTimeOffset?`); `checkedInAt` (`DateTimeOffset?`); `cancelledAt` (`DateTimeOffset?`); `completedAt` (`DateTimeOffset?`); `rowVersion` (`string`); `slot` (`CustomerBookingSlotResponse`); `listing` (`CustomerBookingListingResponse`); `company` (`CustomerBookingCompanyResponse`); `statusHistory` (`IReadOnlyList<CustomerBookingStatusHistoryResponse>`); `rescheduleHistory` (`IReadOnlyList<CustomerBookingRescheduleHistoryResponse>`) | [`CustomerViewingBookingContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/CustomerViewingBookingContracts.cs) |
| `PublicViewingSlotRequest` | `pageNumber` (`int`); `pageSize` (`int`); `from` (`DateTimeOffset?`); `to` (`DateTimeOffset?`) | [`PublicViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/PublicViewingSlotContracts.cs) |
| `PublicViewingSlotResponse` | `id` (`Guid`); `startsAt` (`DateTimeOffset`); `endsAt` (`DateTimeOffset`); `capacity` (`int`); `reservedVisitorCount` (`int`); `availableVisitorCapacity` (`int`); `meetingPoint` (`string`) | [`PublicViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/PublicViewingSlotContracts.cs) |
| `PublicViewingSlotsResponse` | `items` (`IReadOnlyList<PublicViewingSlotResponse>`); `pageNumber` (`int`); `pageSize` (`int`); `totalCount` (`int`); `totalPages` (`int`); `hasPreviousPage` (`bool`); `hasNextPage` (`bool`) | [`PublicViewingSlotContracts.cs`](../src/EstateHub.Api/Contracts/ViewingBookings/PublicViewingSlotContracts.cs) |


## Complete Domain enum catalog

Application/API mappings intentionally emit explicit text rather than a global enum converter. Input acceptance is endpoint-specific; the list below is the complete current Domain enum inventory, not blanket authorization to send every value to every endpoint.

| Enum | Declared values | Source |
|---|---|---|
| `AmenityScope` | Project, Unit, Both | [`AmenityScope.cs`](../src/EstateHub.Domain/Enums/AmenityScope.cs) |
| `BillingInvoiceStatus` | Draft, Open, Paid, Void | [`BillingInvoiceStatus.cs`](../src/EstateHub.Domain/Enums/BillingInvoiceStatus.cs) |
| `BillingLineType` | Subscription, BookingCharge, Promotion | [`BillingLineType.cs`](../src/EstateHub.Domain/Enums/BillingLineType.cs) |
| `BookingActorType` | Customer, Employee, Platform, System | [`BookingActorType.cs`](../src/EstateHub.Domain/Enums/BookingActorType.cs) |
| `BookingCancellationSource` | Customer, Company, System | [`BookingCancellationSource.cs`](../src/EstateHub.Domain/Enums/BookingCancellationSource.cs) |
| `BookingChargeStatus` | Billable, Invoiced, Void | [`BookingChargeStatus.cs`](../src/EstateHub.Domain/Enums/BookingChargeStatus.cs) |
| `CompanyApplicationStatus` | Draft, Submitted, UnderReview, NeedsChanges, Approved, Rejected | [`CompanyApplicationStatus.cs`](../src/EstateHub.Domain/Enums/CompanyApplicationStatus.cs) |
| `CompanyEmployeeStatus` | Active, Suspended, Ended | [`CompanyEmployeeStatus.cs`](../src/EstateHub.Domain/Enums/CompanyEmployeeStatus.cs) |
| `CompanyReviewStatus` | Visible, Hidden, PendingModeration | [`CompanyReviewStatus.cs`](../src/EstateHub.Domain/Enums/CompanyReviewStatus.cs) |
| `CompanyStatus` | Active, Suspended, Deactivated | [`CompanyStatus.cs`](../src/EstateHub.Domain/Enums/CompanyStatus.cs) |
| `CompanySubscriptionStatus` | Active, Expired, Cancelled | [`CompanySubscriptionStatus.cs`](../src/EstateHub.Domain/Enums/CompanySubscriptionStatus.cs) |
| `CompanyType` | Developer, BrokerAgency | [`CompanyType.cs`](../src/EstateHub.Domain/Enums/CompanyType.cs) |
| `CustomerIntent` | Buy, Rent, Invest | [`CustomerIntent.cs`](../src/EstateHub.Domain/Enums/CustomerIntent.cs) |
| `CustomerPersona` | Buyer, Renter, Agent | [`CustomerPersona.cs`](../src/EstateHub.Domain/Enums/CustomerPersona.cs) |
| `DocumentVerificationStatus` | Pending, Approved, Rejected | [`DocumentVerificationStatus.cs`](../src/EstateHub.Domain/Enums/DocumentVerificationStatus.cs) |
| `FileType` | Image, Document | [`FileType.cs`](../src/EstateHub.Domain/Enums/FileType.cs) |
| `FinishingType` | Unfinished, SemiFinished, Finished | [`FinishingType.cs`](../src/EstateHub.Domain/Enums/FinishingType.cs) |
| `FurnishedStatus` | Unfurnished, SemiFurnished, Furnished | [`FurnishedStatus.cs`](../src/EstateHub.Domain/Enums/FurnishedStatus.cs) |
| `InstallmentFrequency` | Monthly, Quarterly, SemiAnnual, Annual | [`InstallmentFrequency.cs`](../src/EstateHub.Domain/Enums/InstallmentFrequency.cs) |
| `LeadPriority` | Low, Medium, High | [`LeadPriority.cs`](../src/EstateHub.Domain/Enums/LeadPriority.cs) |
| `LeadStage` | New, Contacted, Qualified, ViewingScheduled, Negotiation, Won, Lost | [`LeadStage.cs`](../src/EstateHub.Domain/Enums/LeadStage.cs) |
| `ListingPromotionStatus` | Scheduled, Active, Completed, Cancelled | [`ListingPromotionStatus.cs`](../src/EstateHub.Domain/Enums/ListingPromotionStatus.cs) |
| `ListingPublicationStatus` | Draft, Pending, Published, Archived | [`ListingPublicationStatus.cs`](../src/EstateHub.Domain/Enums/ListingPublicationStatus.cs) |
| `ListingType` | Sale, Rent | [`ListingType.cs`](../src/EstateHub.Domain/Enums/ListingType.cs) |
| `LocationType` | Country, Governorate, City, District | [`LocationType.cs`](../src/EstateHub.Domain/Enums/LocationType.cs) |
| `PaymentProvider` | Fake | [`PaymentProvider.cs`](../src/EstateHub.Domain/Enums/PaymentProvider.cs) |
| `PaymentTransactionStatus` | Pending, Succeeded, Failed | [`PaymentTransactionStatus.cs`](../src/EstateHub.Domain/Enums/PaymentTransactionStatus.cs) |
| `ProjectDeliveryStatus` | Planned, UnderConstruction, ReadyToMove, Delivered | [`ProjectDeliveryStatus.cs`](../src/EstateHub.Domain/Enums/ProjectDeliveryStatus.cs) |
| `ProjectStatus` | Draft, Published, Archived | [`ProjectStatus.cs`](../src/EstateHub.Domain/Enums/ProjectStatus.cs) |
| `RentPeriod` | Monthly, Yearly | [`RentPeriod.cs`](../src/EstateHub.Domain/Enums/RentPeriod.cs) |
| `UnitStatus` | Available, Reserved, Sold, Rented, Withdrawn | [`UnitStatus.cs`](../src/EstateHub.Domain/Enums/UnitStatus.cs) |
| `ViewingBookingStatus` | Pending, Confirmed, CheckedIn, Completed, Rejected, Cancelled, NoShow | [`ViewingBookingStatus.cs`](../src/EstateHub.Domain/Enums/ViewingBookingStatus.cs) |
| `ViewingSlotStatus` | Open, Closed, Cancelled | [`ViewingSlotStatus.cs`](../src/EstateHub.Domain/Enums/ViewingSlotStatus.cs) |
