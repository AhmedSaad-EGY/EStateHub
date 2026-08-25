# EstateHub API Master Reference

Documentation date: 2026-08-23  
Source of truth: the complete current EstateHub working-tree source audited on the documentation date.  
Stack: .NET 10, ASP.NET Core controllers, EF Core 10, ASP.NET Core Identity, SQL Server.  
Canonical endpoint count: **164 distinct HTTP verb/route pairs**.

> Current source is authoritative. Historical handoffs and generated Swagger are secondary and may omit business validation, tenant resolution, transactions, concurrency, and side effects.

## Shared API conventions

- Authentication uses Bearer JWTs. Authenticated controllers parse the validated `sub` claim as a non-empty `Guid`; malformed or absent subjects cannot act as a current user.
- Company-scoped endpoints never accept the effective company from the client. `ICompanyAccessService` resolves an active employee, active/verified company, active role chain, and exact ordinal permission code from the database.
- Platform administration uses the distinct `PlatformAdmin` Identity role policy; company permissions never imply platform access.
- Validation errors use `ValidationProblemDetails`. Generic resource, conflict, and temporary-failure responses use `ProblemDetails` with controller-defined titles.
- Directory endpoints use SQL-side filtering, deterministic ordering, and pagination when shown by the service implementation. Exact defaults and bounds are stated per endpoint.
- API enum strings are produced by explicit mappings. Input parsing behavior is endpoint-specific; numeric enum strings are rejected where controllers implement strict textual parsing.
- Rowversions are represented as Base64 and, where controller validation enforces it, decode to exactly eight bytes. Stale values normally return `409 Conflict`.
- Production timestamps are obtained through injected `TimeProvider` where implemented and are represented as ISO-8601 UTC `DateTimeOffset` values.
- Explicit transactions are documented where services create multiple related rows or notifications atomically. Otherwise normal EF `SaveChanges` atomicity applies only to that save operation.
- Authenticated personal, company, and platform controllers commonly set `Cache-Control: no-store`; the exact controller/action inventory is in the Integration and Testing Guide.
- CORS uses the named `EstateHubFrontend` policy with explicit `Cors:AllowedOrigins`, any required header/method, and no credentialed-cookie support.

See [API Contracts Reference](API_CONTRACTS_REFERENCE.md) for reusable field-level contract definitions and [Integration and Testing Guide](API_INTEGRATION_AND_TESTING_GUIDE.md) for cross-cutting flows.


## Batch 1 — Public authentication and discovery

Batch reconciliation: **6 controllers, 19 routes, cumulative 19, remaining 145**.

### 1. POST /api/auth/register

1. **Identity and source:** `AuthController.RegisterCustomer` ([`AuthController.RegisterCustomer`](../src/EstateHub.Api/Controllers/AuthController.cs)). Creates a customer account and starts email confirmation.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `Registration`.
3. **Route parameters:** None.
4. **Query/body contract:** `RegisterCustomerRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `Password is required and must contain between 8 and 128 characters.`; `ConfirmPassword must match Password.`; `Persona must be Buyer, Renter, or Agent.`; `AcceptTerms must be true.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `accountService.RegisterCustomerAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** SMTP delivery can be requested; endpoint-specific enumeration-safe behavior is preserved.
14. **Persistence:** Delegates to `accountService.RegisterCustomerAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 202 Accepted.
16. **Errors:** 400 validation; 503 status branch; 400 status branch; ProblemDetails: “Verification email could not be delivered.”; ProblemDetails: “Registration is temporarily unavailable.”; ProblemDetails: “Registration could not be completed.”
17. **Request example:** `POST /api/auth/register` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** JSON serialized from `Task<IActionResult>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 2. POST /api/auth/confirm-email

1. **Identity and source:** `AuthController.ConfirmEmail` ([`AuthController.ConfirmEmail`](../src/EstateHub.Api/Controllers/AuthController.cs)). Confirms a customer email with the Identity confirmation token.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `Verification`.
3. **Route parameters:** None.
4. **Query/body contract:** `ConfirmEmailRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `UserId is required.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `accountService.ConfirmEmailAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `accountService.ConfirmEmailAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 204 No Content.
16. **Errors:** 400 validation; 400 status branch; ProblemDetails: “Email confirmation failed.”
17. **Request example:** `POST /api/auth/confirm-email` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** Empty response body.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 3. POST /api/auth/resend-confirmation

1. **Identity and source:** `AuthController.ResendConfirmation` ([`AuthController.ResendConfirmation`](../src/EstateHub.Api/Controllers/AuthController.cs)). Requests a replacement confirmation message without disclosing account existence.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `EmailDelivery`.
3. **Route parameters:** None.
4. **Query/body contract:** `ResendConfirmationRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `accountService.ResendConfirmationEmailAsync`.
11. **Idempotency:** Designed to return a stable generic outcome for repeated/unknown inputs; it does not disclose account existence.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** SMTP delivery can be requested; endpoint-specific enumeration-safe behavior is preserved.
14. **Persistence:** Delegates to `accountService.ResendConfirmationEmailAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 202 Accepted.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `POST /api/auth/resend-confirmation` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** JSON serialized from `Task<IActionResult>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 4. POST /api/auth/login

1. **Identity and source:** `AuthController.Login` ([`AuthController.Login`](../src/EstateHub.Api/Controllers/AuthController.cs)). Authenticates a confirmed active user and issues access/refresh tokens.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `Login`.
3. **Route parameters:** None.
4. **Query/body contract:** `LoginRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `Password is required and must not exceed 128 characters.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `accountService.LoginAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `accountService.LoginAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 400 validation; 401 status branch; ProblemDetails: “Authentication failed.”
17. **Request example:** `POST /api/auth/login` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** JSON serialized from `Task<IActionResult>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 5. POST /api/auth/refresh

1. **Identity and source:** `AuthController.Refresh` ([`AuthController.Refresh`](../src/EstateHub.Api/Controllers/AuthController.cs)). Rotates a valid refresh token and issues a replacement token pair.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `TokenLifecycle`.
3. **Route parameters:** None.
4. **Query/body contract:** `RefreshTokenRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `authenticationTokenService.RotateRefreshTokenAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `authenticationTokenService.RotateRefreshTokenAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `POST /api/auth/refresh` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** JSON serialized from `Task<IActionResult>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 6. POST /api/auth/logout

1. **Identity and source:** `AuthController.Logout` ([`AuthController.Logout`](../src/EstateHub.Api/Controllers/AuthController.cs)). Revokes the supplied refresh token when it exists.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `TokenLifecycle`.
3. **Route parameters:** None.
4. **Query/body contract:** `RefreshTokenRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `authenticationTokenService.RevokeRefreshTokenAsync`.
11. **Idempotency:** Designed to return a stable generic outcome for repeated/unknown inputs; it does not disclose account existence.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `authenticationTokenService.RevokeRefreshTokenAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 204 No Content.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `POST /api/auth/logout` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** Empty response body.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 7. POST /api/auth/forgot-password

1. **Identity and source:** `AuthController.ForgotPassword` ([`AuthController.ForgotPassword`](../src/EstateHub.Api/Controllers/AuthController.cs)). Starts password recovery without exposing whether the email is registered.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `EmailDelivery`.
3. **Route parameters:** None.
4. **Query/body contract:** `ForgotPasswordRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `accountService.RequestPasswordResetAsync`.
11. **Idempotency:** Designed to return a stable generic outcome for repeated/unknown inputs; it does not disclose account existence.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** SMTP delivery can be requested; endpoint-specific enumeration-safe behavior is preserved.
14. **Persistence:** Delegates to `accountService.RequestPasswordResetAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 202 Accepted.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `POST /api/auth/forgot-password` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** JSON serialized from `Task<IActionResult>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 8. POST /api/auth/reset-password

1. **Identity and source:** `AuthController.ResetPassword` ([`AuthController.ResetPassword`](../src/EstateHub.Api/Controllers/AuthController.cs)). Resets a password with the supplied Identity reset token.
2. **Access:** Anonymous; no bearer token required. Rate-limited by `Verification`.
3. **Route parameters:** None.
4. **Query/body contract:** `ResetPasswordRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `UserId is required.`; `NewPassword is required and must contain between 8 and 128 characters.`; `ConfirmPassword must match NewPassword.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `accountService.ResetPasswordAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** SMTP delivery can be requested; endpoint-specific enumeration-safe behavior is preserved.
14. **Persistence:** Delegates to `accountService.ResetPasswordAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 204 No Content.
16. **Errors:** 400 validation; 503 status branch; 400 status branch; ProblemDetails: “Password reset is temporarily unavailable.”; ProblemDetails: “Password reset could not be completed.”
17. **Request example:** `POST /api/auth/reset-password` with `Content-Type: application/json` and the request shape in the Contracts Reference.
18. **Response example:** Empty response body.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 9. GET /api/catalog/locations

1. **Identity and source:** `CatalogLookupsController.GetLocations` ([`CatalogLookupsController.GetLocations`](../src/EstateHub.Api/Controllers/CatalogLookupsController.cs)). Lists active public locations, optionally under a parent.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `[FromQuery] Guid? parentId`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicCatalogLookupService.GetLocationsAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicCatalogLookupService.GetLocationsAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `GET /api/catalog/locations`
18. **Response example:** JSON serialized from `Task<ActionResult<IReadOnlyList<LocationLookupResponse>>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 10. GET /api/catalog/unit-types

1. **Identity and source:** `CatalogLookupsController.GetUnitTypes` ([`CatalogLookupsController.GetUnitTypes`](../src/EstateHub.Api/Controllers/CatalogLookupsController.cs)). Lists active public unit types.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `None.`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicCatalogLookupService.GetUnitTypesAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicCatalogLookupService.GetUnitTypesAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `GET /api/catalog/unit-types`
18. **Response example:** JSON serialized from `Task<ActionResult<IReadOnlyList<UnitTypeLookupResponse>>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 11. GET /api/catalog/currencies

1. **Identity and source:** `CatalogLookupsController.GetCurrencies` ([`CatalogLookupsController.GetCurrencies`](../src/EstateHub.Api/Controllers/CatalogLookupsController.cs)). Lists active public currencies.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `None.`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicCatalogLookupService.GetCurrenciesAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicCatalogLookupService.GetCurrenciesAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** Framework authentication/authorization, model binding, cancellation, and unhandled service/database failures propagate normally.
17. **Request example:** `GET /api/catalog/currencies`
18. **Response example:** JSON serialized from `Task<ActionResult<IReadOnlyList<CurrencyLookupResponse>>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 12. GET /api/catalog/amenities

1. **Identity and source:** `CatalogLookupsController.GetAmenities` ([`CatalogLookupsController.GetAmenities`](../src/EstateHub.Api/Controllers/CatalogLookupsController.cs)). Lists active public amenities, optionally filtered by Project or Unit applicability.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `[FromQuery] string? appliesTo`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `AppliesTo must be Project or Unit.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicCatalogLookupService.GetAmenitiesAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicCatalogLookupService.GetAmenitiesAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 400 validation
17. **Request example:** `GET /api/catalog/amenities`
18. **Response example:** JSON serialized from `Task<ActionResult<IReadOnlyList<AmenityLookupResponse>>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 13. GET /api/companies

1. **Identity and source:** `CompaniesController.GetCompanies` ([`CompaniesController.GetCompanies`](../src/EstateHub.Api/Controllers/CompaniesController.cs)). Searches and pages eligible public companies.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `[FromQuery] CompanyDirectoryRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `CompanyType must be Developer or BrokerAgency.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicCompanyQueryService.GetCompaniesAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicCompanyQueryService.GetCompaniesAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 400 validation
17. **Request example:** `GET /api/companies`
18. **Response example:** JSON serialized from `Task<ActionResult<CompanyDirectoryResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 14. GET /api/companies/{slug}

1. **Identity and source:** `CompaniesController.GetCompanyBySlug` ([`CompaniesController.GetCompanyBySlug`](../src/EstateHub.Api/Controllers/CompaniesController.cs)). Returns one eligible public company by slug.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** Tokens shown in the route must bind to their declared types and are normalized only where the controller explicitly does so.
4. **Query/body contract:** `string? slug`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicCompanyQueryService.GetCompanyBySlugAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicCompanyQueryService.GetCompanyBySlugAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 404 status branch; ProblemDetails: “Company not found.”
17. **Request example:** `GET /api/companies/{slug}`
18. **Response example:** JSON serialized from `Task<ActionResult<CompanyDetailsResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 15. GET /api/companies/{companySlug}/projects

1. **Identity and source:** `CompanyProjectsController.GetProjects` ([`CompanyProjectsController.GetProjects`](../src/EstateHub.Api/Controllers/CompanyProjectsController.cs)). Searches and pages eligible public projects for one company slug.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** Tokens shown in the route must bind to their declared types and are normalized only where the controller explicitly does so.
4. **Query/body contract:** `string? companySlug, [FromQuery] ProjectDirectoryRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `DeliveryStatus must be Planned, UnderConstruction, ReadyToMove, or Delivered.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicProjectQueryService.GetProjectsAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicProjectQueryService.GetProjectsAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 400 validation; 404 status branch; ProblemDetails: “Company not found.”
17. **Request example:** `GET /api/companies/{companySlug}/projects`
18. **Response example:** JSON serialized from `Task<ActionResult<ProjectDirectoryResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 16. GET /api/companies/{companySlug}/projects/{projectSlug}

1. **Identity and source:** `CompanyProjectsController.GetProjectBySlug` ([`CompanyProjectsController.GetProjectBySlug`](../src/EstateHub.Api/Controllers/CompanyProjectsController.cs)). Returns an eligible public project by company and project slugs.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** Tokens shown in the route must bind to their declared types and are normalized only where the controller explicitly does so.
4. **Query/body contract:** `string? companySlug, string? projectSlug`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Only records satisfying the service's public/active eligibility projection are returned.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicProjectQueryService.GetProjectBySlugAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicProjectQueryService.GetProjectBySlugAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 404 status branch; ProblemDetails: “Project not found.”
17. **Request example:** `GET /api/companies/{companySlug}/projects/{projectSlug}`
18. **Response example:** JSON serialized from `Task<ActionResult<ProjectDetailsResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 17. GET /api/listings

1. **Identity and source:** `ListingsController.GetListings` ([`ListingsController.GetListings`](../src/EstateHub.Api/Controllers/ListingsController.cs)). Searches and pages listings through the shared public-eligibility predicate.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `[FromQuery] ListingDirectoryRequest request`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `ListingType must be Sale or Rent.`; `CurrencyCode must contain exactly three ASCII letters.`; `Sort must be Newest, PriceLowToHigh, PriceHighToLow, AreaLowToHigh, or AreaHighToLow.`; `CurrencyCode is required when filtering by price.`; `CurrencyCode is required when sorting by price.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Uses the shared public-listing eligibility predicate: published listing, active verified owner company, active company subscription, and—when project-backed—a published project owned by an active verified Developer.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicListingQueryService.GetListingsAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicListingQueryService.GetListingsAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 400 validation
17. **Request example:** `GET /api/listings`
18. **Response example:** JSON serialized from `Task<ActionResult<ListingDirectoryResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 18. GET /api/listings/{slug}

1. **Identity and source:** `ListingsController.GetListingBySlug` ([`ListingsController.GetListingBySlug`](../src/EstateHub.Api/Controllers/ListingsController.cs)). Returns database-backed details for one eligible public listing.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** Tokens shown in the route must bind to their declared types and are normalized only where the controller explicitly does so.
4. **Query/body contract:** `string? slug`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** No action-specific `ModelState` rule is added beyond binding/contract checks shown in source.
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Uses the shared public-listing eligibility predicate: published listing, active verified owner company, active company subscription, and—when project-backed—a published project owned by an active verified Developer.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `publicListingQueryService.GetListingBySlugAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `publicListingQueryService.GetListingBySlugAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 404 status branch; ProblemDetails: “Listing not found.”
17. **Request example:** `GET /api/listings/{slug}`
18. **Response example:** JSON serialized from `Task<ActionResult<ListingDetailsResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

### 19. GET /api/promoted-listings

1. **Identity and source:** `PromotedListingsController.GetPromotedListings` ([`PromotedListingsController.GetPromotedListings`](../src/EstateHub.Api/Controllers/PromotedListingsController.cs)). Returns currently eligible promoted listings for a required placement.
2. **Access:** Anonymous; no bearer token required.
3. **Route parameters:** None.
4. **Query/body contract:** `[FromQuery] string? placement, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20`. See [Contracts Reference](API_CONTRACTS_REFERENCE.md#batch-1-contracts).
5. **Validation:** `Placement is required and must not exceed 100 characters.`; `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`
6. **Enum/text rules:** Explicit parsing/mapping in the controller is authoritative; numeric enum strings are not a substitute for documented textual values.
7. **Eligibility/tenant scope:** Uses the shared public-listing eligibility predicate: published listing, active verified owner company, active company subscription, and—when project-backed—a published project owned by an active verified Developer.
8. **Business/state rules:** Read-only operation; filtering, deterministic ordering, paging and availability rules are applied before projection.
9. **Concurrency:** No rowversion is accepted by this endpoint.
10. **Transaction:** The controller opens no transaction. The service owns any required atomic boundary; this action invokes `service.GetPromotedListingsAsync`.
11. **Idempotency:** Safe and read-only; repeating it can observe newer database state.
12. **Side effects:** None; database queries use the read service path.
13. **Notifications:** No in-app notification is created by the controller.
14. **Persistence:** Delegates to `service.GetPromotedListingsAsync` with the request cancellation token; service exceptions are not rewritten as not-found unless an explicit controller result branch does so.
15. **Success:** 200 OK.
16. **Errors:** 400 validation
17. **Request example:** `GET /api/promoted-listings`
18. **Response example:** JSON serialized from `Task<ActionResult<PromotedListingsResponse>>`; exact reusable fields are catalogued in the Contracts Reference.
19. **Invalid examples:** Malformed identifiers, out-of-range paging, unsupported textual enums, or missing required fields produce the documented generic or validation response without leaking hidden state.
20. **Frontend guidance:** Preserve returned identifiers/slugs and token/rowversion strings exactly; debounce directory searches and honor paging metadata.
21. **QA:** Assert the success branch, each controller validation message, anonymous/auth boundary, cancellation, generic not-found/credential behavior, deterministic order, and no secret/internal-field exposure.
22. **AI use:** Treat returned facts as authoritative database data. Do not infer eligibility failures, credentials, investment metrics, or fields absent from the response.

## Batch 2 — Authenticated customer self-service

Batch reconciliation: **5 controllers, 27 routes, cumulative 46, remaining 118**.

### 20. GET /api/me/profile

1. **Identity and purpose:** `MeController.GetProfile` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Reads profile from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.GetProfileAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `customerSelfService.GetProfileAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.GetProfileAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerProfileResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/profile` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 21. PUT /api/me/profile

1. **Identity and purpose:** `MeController.UpdateProfile` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Updates profile after validation and concurrency checks.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `UpdateCustomerProfileRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `Persona must be Buyer, Renter, or Agent.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.UpdateProfileAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `customerSelfService.UpdateProfileAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.UpdateProfileAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerProfileResponse>>`.
16. **Errors:** 400 validation; 503 status branch; ProblemDetails “Profile update is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/profile`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 22. GET /api/me/favorites

1. **Identity and purpose:** `MeController.GetFavorites` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Reads favorites from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.GetFavoritesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `customerSelfService.GetFavoritesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.GetFavoritesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerFavoritesResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/favorites` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 23. PUT /api/me/favorites/{listingId}

1. **Identity and purpose:** `MeController.AddFavorite` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Executes the add favorite operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `ListingId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.AddFavoriteAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `customerSelfService.AddFavoriteAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.AddFavoriteAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/favorites/{listingId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 24. DELETE /api/me/favorites/{listingId}

1. **Identity and purpose:** `MeController.RemoveFavorite` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Executes the remove favorite operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `ListingId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.RemoveFavoriteAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `customerSelfService.RemoveFavoriteAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.RemoveFavoriteAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/me/favorites/{listingId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 25. GET /api/me/saved-searches

1. **Identity and purpose:** `MeController.GetSavedSearches` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Reads saved searches from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.GetSavedSearchesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `customerSelfService.GetSavedSearchesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.GetSavedSearchesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<SavedSearchesResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/saved-searches` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 26. POST /api/me/saved-searches

1. **Identity and purpose:** `MeController.CreateSavedSearch` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Creates saved search after validation and authorization.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `SaveSearchRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.CreateSavedSearchAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `customerSelfService.CreateSavedSearchAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.CreateSavedSearchAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<SavedSearchResponse>>`.
16. **Errors:** 201 status branch. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/saved-searches`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 27. PUT /api/me/saved-searches/{savedSearchId}

1. **Identity and purpose:** `MeController.UpdateSavedSearch` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Updates saved search after validation and concurrency checks.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid savedSearchId, SaveSearchRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `SavedSearchId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.UpdateSavedSearchAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `customerSelfService.UpdateSavedSearchAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.UpdateSavedSearchAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<SavedSearchResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/saved-searches/{savedSearchId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 28. DELETE /api/me/saved-searches/{savedSearchId}

1. **Identity and purpose:** `MeController.RemoveSavedSearch` ([source](../src/EstateHub.Api/Controllers/MeController.cs)). Executes the remove saved search operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid savedSearchId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `SavedSearchId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `customerSelfService.RemoveSavedSearchAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `customerSelfService.RemoveSavedSearchAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `customerSelfService.RemoveSavedSearchAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/me/saved-searches/{savedSearchId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 29. POST /api/me/files

1. **Identity and purpose:** `MyFilesController.Upload` ([source](../src/EstateHub.Api/Controllers/MyFilesController.cs)). Executes the upload operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromForm] UploadFileAssetRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `Exactly one non-empty file is required.`; `FileType must be Image or Document without surrounding whitespace.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UploadAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UploadAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UploadAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<FileAssetMetadataResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/files`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 30. GET /api/me/files

1. **Identity and purpose:** `MyFilesController.GetFiles` ([source](../src/EstateHub.Api/Controllers/MyFilesController.cs)). Reads files from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? fileType = null, [FromQuery] string? search = null`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `FileType must be Image or Document without surrounding whitespace.`; `Search must not exceed 100 characters.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetOwnedFilesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetOwnedFilesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetOwnedFilesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<FileAssetDirectoryResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/files` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 31. GET /api/me/files/{fileAssetId}

1. **Identity and purpose:** `MyFilesController.GetMetadata` ([source](../src/EstateHub.Api/Controllers/MyFilesController.cs)). Reads metadata from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid fileAssetId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `FileAssetId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetOwnedFileAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetOwnedFileAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetOwnedFileAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<FileAssetMetadataResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/files/{fileAssetId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 32. GET /api/me/files/{fileAssetId}/content

1. **Identity and purpose:** `MyFilesController.GetContent` ([source](../src/EstateHub.Api/Controllers/MyFilesController.cs)). Reads content from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid fileAssetId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `FileAssetId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetOwnedContentAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetOwnedContentAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetOwnedContentAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 file response. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/files/{fileAssetId}/content` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 33. DELETE /api/me/files/{fileAssetId}

1. **Identity and purpose:** `MyFilesController.Delete` ([source](../src/EstateHub.Api/Controllers/MyFilesController.cs)). Executes the delete operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid fileAssetId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `FileAssetId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.DeleteOwnedFileAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.DeleteOwnedFileAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.DeleteOwnedFileAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/me/files/{fileAssetId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 34. GET /api/me/notifications

1. **Identity and purpose:** `MyNotificationsController.GetNotifications` ([source](../src/EstateHub.Api/Controllers/MyNotificationsController.cs)). Reads notifications from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] NotificationDirectoryRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `State must be Unread, Read, or Archived.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `notificationService.GetNotificationsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `notificationService.GetNotificationsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `notificationService.GetNotificationsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<NotificationDirectoryResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/notifications` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 35. GET /api/me/notifications/unread-count

1. **Identity and purpose:** `MyNotificationsController.GetUnreadCount` ([source](../src/EstateHub.Api/Controllers/MyNotificationsController.cs)). Reads unread count from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `notificationService.GetUnreadCountAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `notificationService.GetUnreadCountAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `notificationService.GetUnreadCountAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<NotificationUnreadCountResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/notifications/unread-count` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 36. PUT /api/me/notifications/{notificationId}/read

1. **Identity and purpose:** `MyNotificationsController.MarkRead` ([source](../src/EstateHub.Api/Controllers/MyNotificationsController.cs)). Executes the mark read operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid notificationId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `notificationService.MarkAllReadAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `notificationService.MarkAllReadAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `notificationService.MarkAllReadAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/notifications/{notificationId}/read`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 37. PUT /api/me/notifications/read-all

1. **Identity and purpose:** `MyNotificationsController.MarkAllRead` ([source](../src/EstateHub.Api/Controllers/MyNotificationsController.cs)). Executes the mark all read operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `notificationService.MarkAllReadAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `notificationService.MarkAllReadAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `notificationService.MarkAllReadAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/notifications/read-all`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 38. PUT /api/me/notifications/{notificationId}/archive

1. **Identity and purpose:** `MyNotificationsController.Archive` ([source](../src/EstateHub.Api/Controllers/MyNotificationsController.cs)). Executes the archive operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid notificationId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `NotificationId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/notifications/{notificationId}/archive`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 39. GET /api/me/viewing-bookings/{bookingId}/review

1. **Identity and purpose:** `MyReviewsController.GetBookingReview` ([source](../src/EstateHub.Api/Controllers/MyReviewsController.cs)). Reads booking review from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.GetCustomerReviewAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `reviewService.GetCustomerReviewAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.GetCustomerReviewAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerReviewResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/viewing-bookings/{bookingId}/review` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 40. POST /api/me/viewing-bookings/{bookingId}/review

1. **Identity and purpose:** `MyReviewsController.CreateReview` ([source](../src/EstateHub.Api/Controllers/MyReviewsController.cs)). Creates review after validation and authorization.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, ReviewRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `BookingId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.CreateReviewAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `reviewService.CreateReviewAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.CreateReviewAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CustomerReviewResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/viewing-bookings/{bookingId}/review`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 41. PUT /api/me/reviews/{reviewId}

1. **Identity and purpose:** `MyReviewsController.UpdateReview` ([source](../src/EstateHub.Api/Controllers/MyReviewsController.cs)). Updates review after validation and concurrency checks.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid reviewId, ReviewRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `ReviewId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.UpdateReviewAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `reviewService.UpdateReviewAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.UpdateReviewAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerReviewResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/reviews/{reviewId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 42. DELETE /api/me/reviews/{reviewId}

1. **Identity and purpose:** `MyReviewsController.DeleteReview` ([source](../src/EstateHub.Api/Controllers/MyReviewsController.cs)). Deletes review only when the service safety rules permit it.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid reviewId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.DeleteReviewAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `reviewService.DeleteReviewAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.DeleteReviewAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/me/reviews/{reviewId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 43. GET /api/me/viewing-bookings

1. **Identity and purpose:** `MyViewingBookingsController.GetBookings` ([source](../src/EstateHub.Api/Controllers/MyViewingBookingsController.cs)). Reads bookings from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CustomerViewingBookingDirectoryRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetBookingsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetBookingsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetBookingsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerViewingBookingsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/viewing-bookings` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 44. GET /api/me/viewing-bookings/{bookingId}

1. **Identity and purpose:** `MyViewingBookingsController.GetBooking` ([source](../src/EstateHub.Api/Controllers/MyViewingBookingsController.cs)). Reads booking from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetBookingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetBookingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetBookingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CustomerViewingBookingDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/viewing-bookings/{bookingId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 45. POST /api/me/viewing-bookings

1. **Identity and purpose:** `MyViewingBookingsController.CreateBooking` ([source](../src/EstateHub.Api/Controllers/MyViewingBookingsController.cs)). Creates booking after validation and authorization.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCustomerViewingBookingRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `ViewingSlotId is required.`; `VisitorCount must be positive.`; `ContactPhone is required and must not exceed 32 characters.`; `SpecialRequests must not exceed 4000 characters.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateBookingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateBookingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateBookingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CustomerViewingBookingDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/viewing-bookings`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 46. POST /api/me/viewing-bookings/{bookingId}/cancel

1. **Identity and purpose:** `MyViewingBookingsController.CancelBooking` ([source](../src/EstateHub.Api/Controllers/MyViewingBookingsController.cs)). Cancels booking through its approved lifecycle transition.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CancelCustomerViewingBookingRequest request`. Reusable wire fields are in [Batch 2 contracts](API_CONTRACTS_REFERENCE.md#batch-2-contracts).
5. **Validation:** `BookingId is required.`; `Reason must not exceed 1000 characters.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CancelBookingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CancelBookingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CancelBookingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/viewing-bookings/{bookingId}/cancel`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

## Batch 3 — Current company context, profile, roles, and employees

Batch reconciliation: **4 controllers, 20 routes, cumulative 66, remaining 98**.

### 47. GET /api/me/company-context

1. **Identity and purpose:** `CompanyContextController.GetCurrentContext` ([source](../src/EstateHub.Api/Controllers/CompanyContextController.cs)). Reads current context from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyAccessService.GetCurrentContextAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyAccessService.GetCurrentContextAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyAccessService.GetCurrentContextAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyAccessContextResponse>>`.
16. **Errors:** 404 status branch; ProblemDetails “Company context not found.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/company-context` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 48. POST /api/me/company-context/bootstrap-owner

1. **Identity and purpose:** `CompanyContextController.BootstrapOwner` ([source](../src/EstateHub.Api/Controllers/CompanyContextController.cs)). Executes the bootstrap owner operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.BootstrapOwnerAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyRoleManagementService.BootstrapOwnerAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.BootstrapOwnerAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 404 status branch; 409 status branch; 503 status branch; ProblemDetails “Company context not found.”; ProblemDetails “Company owner bootstrap conflict.”; ProblemDetails “Company owner bootstrap is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/company-context/bootstrap-owner`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 49. GET /api/company/profile

1. **Identity and purpose:** `CompanyProfileController.GetProfile` ([source](../src/EstateHub.Api/Controllers/CompanyProfileController.cs)). Reads profile from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.profile.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.GetProfileAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyManagementService.GetProfileAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.GetProfileAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyProfileResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/profile` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 50. PUT /api/company/profile

1. **Identity and purpose:** `CompanyProfileController.UpdateProfile` ([source](../src/EstateHub.Api/Controllers/CompanyProfileController.cs)). Updates profile after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.profile.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `UpdateCompanyProfileRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.UpdateProfileAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyManagementService.UpdateProfileAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.UpdateProfileAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyProfileResponse>>`.
16. **Errors:** 409 status branch; 503 status branch; ProblemDetails “Company profile update conflict.”; ProblemDetails “Company profile update is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/profile`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 51. GET /api/company/permissions

1. **Identity and purpose:** `CompanyRolesController.GetPermissions` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Reads permissions from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.GetPermissionsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyRoleManagementService.GetPermissionsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.GetPermissionsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<IReadOnlyList<CompanyPermissionGroupResponse>>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/permissions` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 52. GET /api/company/roles

1. **Identity and purpose:** `CompanyRolesController.GetRoles` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Reads roles from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] bool includeInactive = false`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.GetRolesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyRoleManagementService.GetRolesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.GetRolesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<IReadOnlyList<CompanyRoleSummaryResponse>>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/roles` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 53. GET /api/company/roles/{roleId}

1. **Identity and purpose:** `CompanyRolesController.GetRole` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Reads role from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid roleId`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** `RoleId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.GetRoleAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyRoleManagementService.GetRoleAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.GetRoleAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyRoleDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/roles/{roleId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 54. POST /api/company/roles

1. **Identity and purpose:** `CompanyRolesController.CreateRole` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Creates role after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCompanyRoleRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.CreateRoleAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyRoleManagementService.CreateRoleAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.CreateRoleAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyRoleDetailsResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/roles`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 55. PUT /api/company/roles/{roleId}

1. **Identity and purpose:** `CompanyRolesController.UpdateRole` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Updates role after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid roleId, UpdateCompanyRoleRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** `RoleId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.UpdateRoleAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyRoleManagementService.UpdateRoleAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.UpdateRoleAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyRoleDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/roles/{roleId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 56. PUT /api/company/roles/{roleId}/permissions

1. **Identity and purpose:** `CompanyRolesController.ReplacePermissions` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Executes the replace permissions operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid roleId, ReplaceCompanyRolePermissionsRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** `RoleId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.ReplaceRolePermissionsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyRoleManagementService.ReplaceRolePermissionsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.ReplaceRolePermissionsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyRoleDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/roles/{roleId}/permissions`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 57. DELETE /api/company/roles/{roleId}

1. **Identity and purpose:** `CompanyRolesController.DeactivateRole` ([source](../src/EstateHub.Api/Controllers/CompanyRolesController.cs)). Executes the deactivate role operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.roles.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid roleId`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** `RoleId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyRoleManagementService.DeactivateRoleAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyRoleManagementService.DeactivateRoleAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyRoleManagementService.DeactivateRoleAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 409 status branch; 503 status branch; ProblemDetails “Company role conflict.”; ProblemDetails “Company role operation is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/company/roles/{roleId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 58. GET /api/company/employees

1. **Identity and purpose:** `CompanyEmployeesController.GetEmployees` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Reads employees from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] EmployeeDirectoryRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.GetEmployeesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyManagementService.GetEmployeesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.GetEmployeesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyEmployeesResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/employees` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 59. GET /api/company/employees/{employeeId}

1. **Identity and purpose:** `CompanyEmployeesController.GetEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Reads employee from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.GetEmployeeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `companyManagementService.GetEmployeeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.GetEmployeeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyEmployeeResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/employees/{employeeId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 60. POST /api/company/employees

1. **Identity and purpose:** `CompanyEmployeesController.AddEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Executes the add employee operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `AddCompanyEmployeeRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.AddEmployeeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyManagementService.AddEmployeeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.AddEmployeeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyEmployeeResponse>>`.
16. **Errors:** 409 status branch; 503 status branch; ProblemDetails “Employee could not be added.”; ProblemDetails “Employee operation is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/employees`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 61. PUT /api/company/employees/{employeeId}

1. **Identity and purpose:** `CompanyEmployeesController.UpdateEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Updates employee after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId, UpdateCompanyEmployeeRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.UpdateEmployeeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyManagementService.UpdateEmployeeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.UpdateEmployeeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyEmployeeResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/employees/{employeeId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 62. POST /api/company/employees/{employeeId}/suspend

1. **Identity and purpose:** `CompanyEmployeesController.SuspendEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Executes the suspend employee operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId, EmployeeRowVersionRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/employees/{employeeId}/suspend`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 63. POST /api/company/employees/{employeeId}/activate

1. **Identity and purpose:** `CompanyEmployeesController.ActivateEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Executes the activate employee operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId, EmployeeRowVersionRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/employees/{employeeId}/activate`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 64. POST /api/company/employees/{employeeId}/end

1. **Identity and purpose:** `CompanyEmployeesController.EndEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Executes the end employee operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId, EmployeeRowVersionRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.EndEmployeeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyManagementService.EndEmployeeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.EndEmployeeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/employees/{employeeId}/end`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 65. PUT /api/company/employees/{employeeId}/roles

1. **Identity and purpose:** `CompanyEmployeesController.ReplaceRoles` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Executes the replace roles operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId, ReplaceCompanyEmployeeRolesRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.ReplaceEmployeeRolesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyManagementService.ReplaceEmployeeRolesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.ReplaceEmployeeRolesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyEmployeeResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/employees/{employeeId}/roles`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 66. PUT /api/company/employees/{employeeId}/primary-contact

1. **Identity and purpose:** `CompanyEmployeesController.TransferPrimaryContact` ([source](../src/EstateHub.Api/Controllers/CompanyEmployeesController.cs)). Executes the transfer primary contact operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.employees.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid employeeId, TransferPrimaryContactRequest request`. Reusable wire fields are in [Batch 3 contracts](API_CONTRACTS_REFERENCE.md#batch-3-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `companyManagementService.TransferPrimaryContactAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `companyManagementService.TransferPrimaryContactAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `companyManagementService.TransferPrimaryContactAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/employees/{employeeId}/primary-contact`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

## Batch 4 — Company projects, units, listings, and viewing slots

Batch reconciliation: **4 controllers, 37 routes, cumulative 103, remaining 61**.

### 67. GET /api/company/projects

1. **Identity and purpose:** `CompanyProjectManagementController.Get` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the get operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery]CompanyProjectDirectoryRequest r`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.GetProjectsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `svc.GetProjectsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.GetProjectsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyProjectsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/projects` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 68. GET /api/company/projects/{projectId}

1. **Identity and purpose:** `CompanyProjectManagementController.Get` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the get operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.GetProjectAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `svc.GetProjectAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.GetProjectAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyProjectManagementDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/projects/{projectId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 69. POST /api/company/projects

1. **Identity and purpose:** `CompanyProjectManagementController.Post` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the post operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CompanyProjectRequest r`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyProjectResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/projects`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 70. PUT /api/company/projects/{projectId}

1. **Identity and purpose:** `CompanyProjectManagementController.Put` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the put operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,CompanyProjectRequest r`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyProjectResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/projects/{projectId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 71. POST /api/company/projects/{projectId}/publish

1. **Identity and purpose:** `CompanyProjectManagementController.Publish` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the publish operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,RowVersionRequest r`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/projects/{projectId}/publish`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 72. POST /api/company/projects/{projectId}/archive

1. **Identity and purpose:** `CompanyProjectManagementController.Archive` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the archive operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,RowVersionRequest r`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.CreateProjectAsync`, `svc.UpdateProjectAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `svc.CreateProjectAsync`, `svc.UpdateProjectAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.CreateProjectAsync`, `svc.UpdateProjectAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK, 201 Created. The declared return type is `Task<IActionResult>`.
16. **Errors:** ProblemDetails “Project operation is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/projects/{projectId}/archive`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 73. PUT /api/company/projects/{projectId}/amenities

1. **Identity and purpose:** `CompanyProjectManagementController.ReplaceAmenities` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the replace amenities operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,ReplaceProjectAmenitiesRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `AmenityIds must contain unique non-empty IDs.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.ReplaceAmenitiesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `svc.ReplaceAmenitiesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.ReplaceAmenitiesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<ReplaceProjectAmenitiesResponse>>`.
16. **Errors:** 400 validation; ProblemDetails “Project operation is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/projects/{projectId}/amenities`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 74. PUT /api/company/projects/{projectId}/media

1. **Identity and purpose:** `CompanyProjectManagementController.ReplaceMedia` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Executes the replace media operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,ReplaceProjectMediaRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `Items are invalid.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.ReplaceMediaAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `svc.ReplaceMediaAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.ReplaceMediaAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<ReplaceProjectMediaResponse>>`.
16. **Errors:** 400 validation; ProblemDetails “Project operation is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/projects/{projectId}/media`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 75. POST /api/company/projects/{projectId}/nearby-places

1. **Identity and purpose:** `CompanyProjectManagementController.CreateNearbyPlace` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Creates nearby place after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,ProjectNearbyPlaceRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `Invalid project ID.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.CreateNearbyPlaceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `svc.CreateNearbyPlaceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.CreateNearbyPlaceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<ProjectNearbyPlaceMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/projects/{projectId}/nearby-places`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 76. PUT /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}

1. **Identity and purpose:** `CompanyProjectManagementController.UpdateNearbyPlace` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Updates nearby place after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,Guid nearbyPlaceId,ProjectNearbyPlaceRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `Invalid nearby-place ID.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.UpdateNearbyPlaceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `svc.UpdateNearbyPlaceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.UpdateNearbyPlaceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<ProjectNearbyPlaceMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 77. DELETE /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}

1. **Identity and purpose:** `CompanyProjectManagementController.DeleteNearbyPlace` ([source](../src/EstateHub.Api/Controllers/CompanyProjectManagementController.cs)). Deletes nearby place only when the service safety rules permit it.
2. **Access:** Bearer JWT plus dynamic company permission `company.projects.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid projectId,Guid nearbyPlaceId,[FromQuery]string? rowVersion`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `Invalid nearby-place ID.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `svc.DeleteNearbyPlaceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `svc.DeleteNearbyPlaceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `svc.DeleteNearbyPlaceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; ProblemDetails “Project operation is temporarily unavailable.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/company/projects/{projectId}/nearby-places/{nearbyPlaceId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 78. GET /api/company/units

1. **Identity and purpose:** `CompanyUnitsController.GetUnits` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Reads units from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanyUnitDirectoryRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetUnitsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetUnitsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetUnitsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyUnitsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/units` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 79. GET /api/company/units/{unitId}

1. **Identity and purpose:** `CompanyUnitsController.GetUnit` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Reads unit from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetUnitAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetUnitAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetUnitAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyUnitManagementDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/units/{unitId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 80. POST /api/company/units

1. **Identity and purpose:** `CompanyUnitsController.CreateUnit` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Creates unit after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCompanyUnitRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateUnitAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateUnitAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateUnitAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyUnitDetailsResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/units`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 81. PUT /api/company/units/{unitId}

1. **Identity and purpose:** `CompanyUnitsController.UpdateUnit` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Updates unit after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId, UpdateCompanyUnitRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `UnitId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateUnitAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateUnitAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateUnitAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyUnitDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/units/{unitId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 82. PUT /api/company/units/{unitId}/status

1. **Identity and purpose:** `CompanyUnitsController.UpdateStatus` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Updates status after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId, UpdateCompanyUnitStatusRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `UnitId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateUnitStatusAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateUnitStatusAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateUnitStatusAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyUnitStatusResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/units/{unitId}/status`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 83. PUT /api/company/units/{unitId}/amenities

1. **Identity and purpose:** `CompanyUnitsController.ReplaceAmenities` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Executes the replace amenities operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId, ReplaceCompanyUnitAmenitiesRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `UnitId is required.`; `AmenityIds must contain unique non-empty IDs.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.ReplaceAmenitiesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.ReplaceAmenitiesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.ReplaceAmenitiesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<ReplaceCompanyUnitAmenitiesResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/units/{unitId}/amenities`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 84. POST /api/company/units/{unitId}/nearby-places

1. **Identity and purpose:** `CompanyUnitsController.CreateNearbyPlace` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Creates nearby place after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId, CompanyUnitNearbyPlaceRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `UnitId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateNearbyPlaceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateNearbyPlaceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateNearbyPlaceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyUnitNearbyPlaceMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/units/{unitId}/nearby-places`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 85. PUT /api/company/units/{unitId}/nearby-places/{nearbyPlaceId}

1. **Identity and purpose:** `CompanyUnitsController.UpdateNearbyPlace` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Updates nearby place after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId, Guid nearbyPlaceId, CompanyUnitNearbyPlaceRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `UnitId is required.`; `NearbyPlaceId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateNearbyPlaceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateNearbyPlaceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateNearbyPlaceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyUnitNearbyPlaceMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/units/{unitId}/nearby-places/{nearbyPlaceId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 86. DELETE /api/company/units/{unitId}/nearby-places/{nearbyPlaceId}

1. **Identity and purpose:** `CompanyUnitsController.DeleteNearbyPlace` ([source](../src/EstateHub.Api/Controllers/CompanyUnitsController.cs)). Deletes nearby place only when the service safety rules permit it.
2. **Access:** Bearer JWT plus dynamic company permission `company.units.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid unitId, Guid nearbyPlaceId, [FromQuery] string? rowVersion`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `UnitId is required.`; `NearbyPlaceId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.DeleteNearbyPlaceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.DeleteNearbyPlaceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.DeleteNearbyPlaceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/company/units/{unitId}/nearby-places/{nearbyPlaceId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 87. GET /api/company/listings

1. **Identity and purpose:** `CompanyListingsController.GetListings` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Reads listings from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanyListingDirectoryRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetListingsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetListingsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetListingsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyListingsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/listings` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 88. GET /api/company/listings/{listingId}

1. **Identity and purpose:** `CompanyListingsController.GetListing` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Reads listing from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetListingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetListingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetListingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyListingDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/listings/{listingId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 89. POST /api/company/listings

1. **Identity and purpose:** `CompanyListingsController.CreateListing` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Creates listing after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCompanyListingRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateListingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateListingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateListingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyListingDetailsResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/listings`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 90. PUT /api/company/listings/{listingId}

1. **Identity and purpose:** `CompanyListingsController.UpdateListing` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Updates listing after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, UpdateCompanyListingRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `ListingId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateListingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateListingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateListingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyListingDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/listings/{listingId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 91. PUT /api/company/listings/{listingId}/media

1. **Identity and purpose:** `CompanyListingsController.ReplaceMedia` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Executes the replace media operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, ReplaceCompanyListingMediaRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `ListingId is required.`; `Items are invalid.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.ReplaceMediaAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.ReplaceMediaAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.ReplaceMediaAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<ReplaceCompanyListingMediaResponse>>`.
16. **Errors:** 400 validation; 400 status branch. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/listings/{listingId}/media`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 92. POST /api/company/listings/{listingId}/payment-plans

1. **Identity and purpose:** `CompanyListingsController.CreatePaymentPlan` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Creates payment plan after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, CreateCompanyListingPaymentPlanRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `ListingId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreatePaymentPlanAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreatePaymentPlanAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreatePaymentPlanAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyListingPaymentPlanMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/listings/{listingId}/payment-plans`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 93. PUT /api/company/listings/{listingId}/payment-plans/{paymentPlanId}

1. **Identity and purpose:** `CompanyListingsController.UpdatePaymentPlan` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Updates payment plan after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, Guid paymentPlanId, UpdateCompanyListingPaymentPlanRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `ListingId is required.`; `PaymentPlanId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdatePaymentPlanAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdatePaymentPlanAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdatePaymentPlanAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyListingPaymentPlanMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/listings/{listingId}/payment-plans/{paymentPlanId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 94. DELETE /api/company/listings/{listingId}/payment-plans/{paymentPlanId}

1. **Identity and purpose:** `CompanyListingsController.DeletePaymentPlan` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Deletes payment plan only when the service safety rules permit it.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, Guid paymentPlanId, [FromQuery] string? rowVersion`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `ListingId is required.`; `PaymentPlanId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.DeletePaymentPlanAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.DeletePaymentPlanAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.DeletePaymentPlanAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/company/listings/{listingId}/payment-plans/{paymentPlanId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 95. POST /api/company/listings/{listingId}/publish

1. **Identity and purpose:** `CompanyListingsController.PublishListing` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Publishes listing through its approved lifecycle transition.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, CompanyListingLifecycleRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/listings/{listingId}/publish`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 96. POST /api/company/listings/{listingId}/archive

1. **Identity and purpose:** `CompanyListingsController.ArchiveListing` ([source](../src/EstateHub.Api/Controllers/CompanyListingsController.cs)). Archives listing through its approved lifecycle transition.
2. **Access:** Bearer JWT plus dynamic company permission `company.listings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid listingId, CompanyListingLifecycleRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK, 201 Created. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 status branch. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/listings/{listingId}/archive`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 97. GET /api/company/viewing-slots

1. **Identity and purpose:** `CompanyViewingSlotsController.GetSlots` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Reads slots from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanyViewingSlotDirectoryRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetSlotsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetSlotsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetSlotsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingSlotsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/viewing-slots` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 98. GET /api/company/viewing-slots/{slotId}

1. **Identity and purpose:** `CompanyViewingSlotsController.GetSlot` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Reads slot from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid slotId`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetSlotAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetSlotAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetSlotAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingSlotResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/viewing-slots/{slotId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 99. POST /api/company/viewing-slots

1. **Identity and purpose:** `CompanyViewingSlotsController.CreateSlot` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Creates slot after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCompanyViewingSlotRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateSlotAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateSlotAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateSlotAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyViewingSlotResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-slots`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 100. PUT /api/company/viewing-slots/{slotId}

1. **Identity and purpose:** `CompanyViewingSlotsController.UpdateSlot` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Updates slot after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid slotId, UpdateCompanyViewingSlotRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `SlotId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateSlotAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateSlotAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateSlotAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingSlotResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/viewing-slots/{slotId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 101. POST /api/company/viewing-slots/{slotId}/close

1. **Identity and purpose:** `CompanyViewingSlotsController.CloseSlot` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Executes the close slot operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid slotId, CompanyViewingSlotLifecycleRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-slots/{slotId}/close`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 102. POST /api/company/viewing-slots/{slotId}/reopen

1. **Identity and purpose:** `CompanyViewingSlotsController.ReopenSlot` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Executes the reopen slot operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid slotId, CompanyViewingSlotLifecycleRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-slots/{slotId}/reopen`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 103. POST /api/company/viewing-slots/{slotId}/cancel

1. **Identity and purpose:** `CompanyViewingSlotsController.CancelSlot` ([source](../src/EstateHub.Api/Controllers/CompanyViewingSlotsController.cs)). Cancels slot through its approved lifecycle transition.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid slotId, CompanyViewingSlotLifecycleRequest request`. Reusable wire fields are in [Batch 4 contracts](API_CONTRACTS_REFERENCE.md#batch-4-contracts).
5. **Validation:** `SlotId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CloseSlotAsync`, `service.ReopenSlotAsync`, `service.CancelSlotAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CloseSlotAsync`, `service.ReopenSlotAsync`, `service.CancelSlotAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CloseSlotAsync`, `service.ReopenSlotAsync`, `service.CancelSlotAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-slots/{slotId}/cancel`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

## Batch 5 — Company viewing bookings and leads with public slots and reviews

Batch reconciliation: **4 controllers, 24 routes, cumulative 127, remaining 37**.

### 104. GET /api/company/viewing-bookings

1. **Identity and purpose:** `CompanyViewingBookingsController.GetBookings` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Reads bookings from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanyViewingBookingDirectoryRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetBookingsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetBookingsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetBookingsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingBookingsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/viewing-bookings` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 105. GET /api/company/viewing-bookings/{bookingId}

1. **Identity and purpose:** `CompanyViewingBookingsController.GetBooking` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Reads booking from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetBookingAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetBookingAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetBookingAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingBookingDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/viewing-bookings/{bookingId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 106. PUT /api/company/viewing-bookings/{bookingId}/assignment

1. **Identity and purpose:** `CompanyViewingBookingsController.AssignEmployee` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the assign employee operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingAssignmentRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `BookingId is required.`; `EmployeeId cannot be empty when supplied.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.AssignEmployeeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.AssignEmployeeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.AssignEmployeeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingBookingDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/viewing-bookings/{bookingId}/assignment`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 107. POST /api/company/viewing-bookings/{bookingId}/confirm

1. **Identity and purpose:** `CompanyViewingBookingsController.Confirm` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the confirm operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingLifecycleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/confirm`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 108. POST /api/company/viewing-bookings/{bookingId}/reject

1. **Identity and purpose:** `CompanyViewingBookingsController.Reject` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the reject operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingLifecycleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/reject`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 109. POST /api/company/viewing-bookings/{bookingId}/check-in

1. **Identity and purpose:** `CompanyViewingBookingsController.CheckIn` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the check in operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingLifecycleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/check-in`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 110. POST /api/company/viewing-bookings/{bookingId}/complete

1. **Identity and purpose:** `CompanyViewingBookingsController.Complete` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the complete operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingLifecycleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/complete`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 111. POST /api/company/viewing-bookings/{bookingId}/no-show

1. **Identity and purpose:** `CompanyViewingBookingsController.MarkNoShow` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the mark no show operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingLifecycleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/no-show`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 112. POST /api/company/viewing-bookings/{bookingId}/cancel

1. **Identity and purpose:** `CompanyViewingBookingsController.Cancel` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the cancel operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingLifecycleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/cancel`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 113. POST /api/company/viewing-bookings/{bookingId}/reschedule

1. **Identity and purpose:** `CompanyViewingBookingsController.Reschedule` ([source](../src/EstateHub.Api/Controllers/CompanyViewingBookingsController.cs)). Executes the reschedule operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.bookings.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid bookingId, CompanyViewingBookingRescheduleRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `BookingId is required.`; `ToViewingSlotId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.RescheduleAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.RescheduleAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.RescheduleAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyViewingBookingDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/viewing-bookings/{bookingId}/reschedule`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 114. GET /api/company/leads

1. **Identity and purpose:** `CompanyLeadsController.GetLeads` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Reads leads from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanyLeadDirectoryRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `OwnerEmployeeId cannot be empty.`; `OwnerEmployeeId and UnassignedOnly cannot be combined.`; `CreatedTo must be later than CreatedFrom.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetLeadsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetLeadsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetLeadsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyLeadsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/leads` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 115. GET /api/company/leads/{leadId}

1. **Identity and purpose:** `CompanyLeadsController.GetLead` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Reads lead from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetLeadAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetLeadAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetLeadAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyLeadDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/leads/{leadId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 116. POST /api/company/leads

1. **Identity and purpose:** `CompanyLeadsController.CreateLead` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Creates lead after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCompanyLeadRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `OwnerEmployeeId cannot be empty.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateLeadAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateLeadAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateLeadAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyLeadDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/leads`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 117. PUT /api/company/leads/{leadId}

1. **Identity and purpose:** `CompanyLeadsController.UpdateLead` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Updates lead after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, UpdateCompanyLeadRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `OwnerEmployeeId cannot be empty.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateLeadAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateLeadAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateLeadAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyLeadDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/leads/{leadId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 118. PUT /api/company/leads/{leadId}/stage

1. **Identity and purpose:** `CompanyLeadsController.ChangeStage` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Executes the change stage operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, ChangeCompanyLeadStageRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.ChangeStageAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.ChangeStageAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.ChangeStageAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/leads/{leadId}/stage`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 119. POST /api/company/leads/{leadId}/requirements

1. **Identity and purpose:** `CompanyLeadsController.CreateRequirement` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Creates requirement after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, CompanyLeadRequirementRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateRequirementAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateRequirementAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateRequirementAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyLeadRequirementMutationResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/leads/{leadId}/requirements`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 120. PUT /api/company/leads/{leadId}/requirements/{requirementId}

1. **Identity and purpose:** `CompanyLeadsController.UpdateRequirement` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Updates requirement after validation and concurrency checks.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, Guid requirementId, CompanyLeadRequirementRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.UpdateRequirementAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.UpdateRequirementAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.UpdateRequirementAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyLeadRequirementMutationResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/leads/{leadId}/requirements/{requirementId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 121. POST /api/company/leads/{leadId}/requirements/{requirementId}/confirm

1. **Identity and purpose:** `CompanyLeadsController.ConfirmRequirement` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Executes the confirm requirement operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, Guid requirementId, ConfirmCompanyLeadRequirementRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.ConfirmRequirementAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.ConfirmRequirementAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.ConfirmRequirementAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/leads/{leadId}/requirements/{requirementId}/confirm`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 122. DELETE /api/company/leads/{leadId}/requirements/{requirementId}

1. **Identity and purpose:** `CompanyLeadsController.DeleteRequirement` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Deletes requirement only when the service safety rules permit it.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, Guid requirementId, [FromQuery] string? leadRowVersion`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.DeleteRequirementAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Repeat behavior follows the service not-found/conflict policy; no idempotency key exists.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.DeleteRequirementAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.DeleteRequirementAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `DELETE /api/company/leads/{leadId}/requirements/{requirementId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 123. PUT /api/company/leads/{leadId}/interests

1. **Identity and purpose:** `CompanyLeadsController.ReplaceInterests` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Executes the replace interests operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, ReplaceCompanyLeadInterestsRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `Items is required.`; `Items must not exceed 100 entries.`; `ListingId is required.`; `Duplicate listing and interest type pairs are not allowed.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.ReplaceInterestsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.ReplaceInterestsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.ReplaceInterestsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyLeadInterestsMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/company/leads/{leadId}/interests`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 124. GET /api/company/leads/{leadId}/activities

1. **Identity and purpose:** `CompanyLeadsController.GetActivities` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Reads activities from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, [FromQuery] CompanyLeadActivityDirectoryRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `To must be later than From.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetActivitiesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetActivitiesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetActivitiesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyLeadActivitiesResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/leads/{leadId}/activities` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 125. POST /api/company/leads/{leadId}/activities

1. **Identity and purpose:** `CompanyLeadsController.CreateActivity` ([source](../src/EstateHub.Api/Controllers/CompanyLeadsController.cs)). Creates activity after validation and authorization.
2. **Access:** Bearer JWT plus dynamic company permission `company.leads.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid leadId, CreateCompanyLeadActivityRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `Metadata must be a JSON object.`; `Canonical metadata must not exceed 16000 characters.`; `OccurredAt must not be later than the current UTC time.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CreateActivityAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CreateActivityAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CreateActivityAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyLeadActivityMutationResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/leads/{leadId}/activities`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 126. GET /api/listings/{listingSlug}/viewing-slots

1. **Identity and purpose:** `PublicViewingSlotsController.GetSlots` ([source](../src/EstateHub.Api/Controllers/PublicViewingSlotsController.cs)). Reads slots from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `string? listingSlug, [FromQuery] PublicViewingSlotRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `ListingSlug is required and must not exceed 200 characters.`; `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `To must be later than From.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetSlotsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetSlotsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetSlotsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PublicViewingSlotsResponse>>`.
16. **Errors:** 400 validation; 404 status branch; ProblemDetails “Listing not found.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/listings/{listingSlug}/viewing-slots` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 127. GET /api/companies/{companySlug}/reviews

1. **Identity and purpose:** `PublicCompanyReviewsController.GetReviews` ([source](../src/EstateHub.Api/Controllers/PublicCompanyReviewsController.cs)). Reads reviews from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `string? companySlug, [FromQuery] PublicCompanyReviewRequest request`. Reusable wire fields are in [Batch 5 contracts](API_CONTRACTS_REFERENCE.md#batch-5-contracts).
5. **Validation:** `CompanySlug is required and must not exceed 200 characters.`; `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `Rating must be between 1 and 5.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.GetPublicCompanyReviewsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `reviewService.GetPublicCompanyReviewsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.GetPublicCompanyReviewsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PublicCompanyReviewsResponse>>`.
16. **Errors:** 400 validation; 404 status branch; ProblemDetails “Company not found.”. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/companies/{companySlug}/reviews` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

## Batch 6 — Billing, listing promotions, subscriptions, and public commercial catalogs

Batch reconciliation: **5 controllers, 19 routes, cumulative 146, remaining 18**.

### 128. GET /api/company/billing/invoices

1. **Identity and purpose:** `CompanyBillingController.GetInvoices` ([source](../src/EstateHub.Api/Controllers/CompanyBillingController.cs)). Reads invoices from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] InvoiceDirectoryRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetInvoicesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetInvoicesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetInvoicesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<BillingInvoiceDirectoryResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/billing/invoices` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 129. GET /api/company/billing/invoices/{invoiceId}

1. **Identity and purpose:** `CompanyBillingController.GetInvoice` ([source](../src/EstateHub.Api/Controllers/CompanyBillingController.cs)). Reads invoice from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid invoiceId`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `InvoiceId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetInvoiceAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetInvoiceAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetInvoiceAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<BillingInvoiceDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/billing/invoices/{invoiceId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 130. GET /api/company/billing/booking-charges

1. **Identity and purpose:** `CompanyBillingController.GetBookingCharges` ([source](../src/EstateHub.Api/Controllers/CompanyBillingController.cs)). Reads booking charges from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] BookingChargeDirectoryRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetBookingChargesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetBookingChargesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetBookingChargesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<BookingChargeDirectoryResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/billing/booking-charges` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 131. GET /api/company/billing/payment-transactions

1. **Identity and purpose:** `CompanyBillingController.GetPaymentTransactions` ([source](../src/EstateHub.Api/Controllers/CompanyBillingController.cs)). Reads payment transactions from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] PaymentTransactionDirectoryRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetPaymentTransactionsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetPaymentTransactionsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetPaymentTransactionsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PaymentTransactionDirectoryResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/billing/payment-transactions` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 132. POST /api/company/billing/booking-charges/preview

1. **Identity and purpose:** `CompanyBillingController.PreviewBookingCharges` ([source](../src/EstateHub.Api/Controllers/CompanyBillingController.cs)). Executes the preview booking charges operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `BookingChargeSelectionRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.PreviewBookingChargesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.PreviewBookingChargesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.PreviewBookingChargesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<BookingChargePreviewResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/billing/booking-charges/preview`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 133. POST /api/company/billing/booking-charges/checkout

1. **Identity and purpose:** `CompanyBillingController.CheckoutBookingCharges` ([source](../src/EstateHub.Api/Controllers/CompanyBillingController.cs)). Executes the checkout booking charges operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `BookingChargeSelectionRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CheckoutBookingChargesAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CheckoutBookingChargesAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CheckoutBookingChargesAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<BillingInvoiceDetailsResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/billing/booking-charges/checkout`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 134. GET /api/company/listing-promotions

1. **Identity and purpose:** `CompanyListingPromotionsController.GetPromotions` ([source](../src/EstateHub.Api/Controllers/CompanyListingPromotionsController.cs)). Reads promotions from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanyListingPromotionDirectoryRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetCompanyPromotionsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetCompanyPromotionsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetCompanyPromotionsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyListingPromotionsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/listing-promotions` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 135. GET /api/company/listing-promotions/{promotionId}

1. **Identity and purpose:** `CompanyListingPromotionsController.GetPromotion` ([source](../src/EstateHub.Api/Controllers/CompanyListingPromotionsController.cs)). Reads promotion from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid promotionId`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `PromotionId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetCompanyPromotionAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetCompanyPromotionAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetCompanyPromotionAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyListingPromotionResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/listing-promotions/{promotionId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 136. POST /api/company/listing-promotions/checkout

1. **Identity and purpose:** `CompanyListingPromotionsController.Checkout` ([source](../src/EstateHub.Api/Controllers/CompanyListingPromotionsController.cs)). Executes the checkout operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CheckoutPromotionRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `ListingId is required.`; `PromotionPackageId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CheckoutAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CheckoutAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CheckoutAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<PromotionCheckoutResponse>>`.
16. **Errors:** 400 validation; 400 status branch. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/listing-promotions/checkout`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 137. POST /api/company/listing-promotions/{promotionId}/cancel

1. **Identity and purpose:** `CompanyListingPromotionsController.Cancel` ([source](../src/EstateHub.Api/Controllers/CompanyListingPromotionsController.cs)). Executes the cancel operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid promotionId, PromotionCancellationRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `PromotionId is required.`; `Reason must not exceed 1000 characters.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CancelAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CancelAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CancelAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/listing-promotions/{promotionId}/cancel`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 138. GET /api/company/subscription

1. **Identity and purpose:** `CompanySubscriptionsController.GetCurrent` ([source](../src/EstateHub.Api/Controllers/CompanySubscriptionsController.cs)). Reads current from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetCurrentAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetCurrentAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetCurrentAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanySubscriptionResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/subscription` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 139. GET /api/company/subscriptions

1. **Identity and purpose:** `CompanySubscriptionsController.GetHistory` ([source](../src/EstateHub.Api/Controllers/CompanySubscriptionsController.cs)). Reads history from the current source-backed service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.read`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] CompanySubscriptionHistoryRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetHistoryAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetHistoryAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetHistoryAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanySubscriptionHistoryResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/company/subscriptions` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 140. POST /api/company/subscription/checkout

1. **Identity and purpose:** `CompanySubscriptionsController.Checkout` ([source](../src/EstateHub.Api/Controllers/CompanySubscriptionsController.cs)). Executes the checkout operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CheckoutSubscriptionRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `SubscriptionPlanId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CheckoutAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CheckoutAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CheckoutAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CheckoutSubscriptionResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/subscription/checkout`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 141. POST /api/company/subscription/cancel

1. **Identity and purpose:** `CompanySubscriptionsController.Cancel` ([source](../src/EstateHub.Api/Controllers/CompanySubscriptionsController.cs)). Executes the cancel operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `SubscriptionLifecycleRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CancelAsync`, `service.ResumeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CancelAsync`, `service.ResumeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CancelAsync`, `service.ResumeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/subscription/cancel`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 142. POST /api/company/subscription/resume

1. **Identity and purpose:** `CompanySubscriptionsController.Resume` ([source](../src/EstateHub.Api/Controllers/CompanySubscriptionsController.cs)). Executes the resume operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus dynamic company permission `company.billing.manage`. Company scope comes from the database-backed employee chain, never request input. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `SubscriptionLifecycleRequest request`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The handler re-resolves active employee/company/role/permission state on the request; client tenant identifiers cannot expand scope.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.CancelAsync`, `service.ResumeAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `service.CancelAsync`, `service.ResumeAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.CancelAsync`, `service.ResumeAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/company/subscription/resume`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 143. GET /api/subscription-plans

1. **Identity and purpose:** `SubscriptionPlansController.GetPlans` ([source](../src/EstateHub.Api/Controllers/SubscriptionPlansController.cs)). Reads plans from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/subscription-plans` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 144. GET /api/subscription-plans/{planId}

1. **Identity and purpose:** `SubscriptionPlansController.GetPlan` ([source](../src/EstateHub.Api/Controllers/SubscriptionPlansController.cs)). Reads plan from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid planId`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `PlanId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetPublicPlanAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetPublicPlanAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetPublicPlanAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<SubscriptionPlanResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/subscription-plans/{planId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 145. GET /api/promotion-packages

1. **Identity and purpose:** `PromotionPackagesController.GetPackages` ([source](../src/EstateHub.Api/Controllers/PromotionPackagesController.cs)). Reads packages from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `None`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<IReadOnlyList<PromotionPackageResponse>>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/promotion-packages` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 146. GET /api/promotion-packages/{packageId}

1. **Identity and purpose:** `PromotionPackagesController.GetPackage` ([source](../src/EstateHub.Api/Controllers/PromotionPackagesController.cs)). Reads package from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid packageId`. Reusable wire fields are in [Batch 6 contracts](API_CONTRACTS_REFERENCE.md#batch-6-contracts).
5. **Validation:** `PackageId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetPublicPackageAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetPublicPackageAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetPublicPackageAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PromotionPackageResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/promotion-packages/{packageId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

## Batch 7 — Company applications, platform moderation, and public files

Batch reconciliation: **4 controllers, 18 routes, cumulative 164, remaining 0**.

### 147. GET /api/me/company-applications

1. **Identity and purpose:** `MyCompanyApplicationsController.GetApplications` ([source](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs)). Reads applications from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `Status must be Draft, Submitted, UnderReview, NeedsChanges, Approved, or Rejected.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.GetApplicationsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `applicationService.GetApplicationsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.GetApplicationsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyApplicationDirectoryResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/company-applications` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 148. GET /api/me/company-applications/{applicationId}

1. **Identity and purpose:** `MyCompanyApplicationsController.GetApplication` ([source](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs)). Reads application from the current source-backed service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.GetApplicationAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `applicationService.GetApplicationAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.GetApplicationAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyApplicationDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/me/company-applications/{applicationId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 149. POST /api/me/company-applications

1. **Identity and purpose:** `MyCompanyApplicationsController.CreateApplication` ([source](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs)). Creates application after validation and authorization.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `CreateCompanyApplicationRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.CreateApplicationAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.CreateApplicationAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.CreateApplicationAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 201 Created. The declared return type is `Task<ActionResult<CompanyApplicationDetailsResponse>>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/company-applications`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 150. PUT /api/me/company-applications/{applicationId}

1. **Identity and purpose:** `MyCompanyApplicationsController.UpdateApplication` ([source](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs)). Updates application after validation and concurrency checks.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, UpdateCompanyApplicationRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.UpdateApplicationAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.UpdateApplicationAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.UpdateApplicationAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyApplicationDetailsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/company-applications/{applicationId}`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 151. PUT /api/me/company-applications/{applicationId}/documents

1. **Identity and purpose:** `MyCompanyApplicationsController.ReplaceDocuments` ([source](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs)). Executes the replace documents operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, ReplaceCompanyApplicationDocumentsRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.ReplaceDocumentsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.ReplaceDocumentsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.ReplaceDocumentsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<CompanyApplicationDocumentsResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/me/company-applications/{applicationId}/documents`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 152. POST /api/me/company-applications/{applicationId}/submit

1. **Identity and purpose:** `MyCompanyApplicationsController.SubmitApplication` ([source](../src/EstateHub.Api/Controllers/MyCompanyApplicationsController.cs)). Executes the submit application operation defined by the current controller and application service.
2. **Access:** Bearer JWT required. The validated non-empty GUID `sub` claim identifies the current user. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, SubmitCompanyApplicationRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The current `ApplicationUser` is resolved only from the validated JWT subject; request IDs remain resource IDs, not identity overrides.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.SubmitApplicationAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.SubmitApplicationAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.SubmitApplicationAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/me/company-applications/{applicationId}/submit`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 153. GET /api/platform/company-applications

1. **Identity and purpose:** `PlatformCompanyApplicationsController.GetApplications` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Reads applications from the current source-backed service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? companyType = null, [FromQuery] DateTimeOffset? submittedFrom = null, [FromQuery] DateTimeOffset? submittedTo = null`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `Search must not exceed 100 characters.`; `Status is invalid.`; `CompanyType must be Developer or BrokerAgency.`; `SubmittedFrom must use UTC.`; `SubmittedTo must use UTC.`; `SubmittedFrom must not be later than SubmittedTo.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.GetApplicationsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `applicationService.GetApplicationsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.GetApplicationsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PlatformCompanyApplicationDirectoryResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/platform/company-applications` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 154. GET /api/platform/company-applications/{applicationId}

1. **Identity and purpose:** `PlatformCompanyApplicationsController.GetApplication` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Reads application from the current source-backed service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.GetApplicationAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `applicationService.GetApplicationAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.GetApplicationAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PlatformCompanyApplicationDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/platform/company-applications/{applicationId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 155. GET /api/platform/company-applications/{applicationId}/documents/{documentId}/content

1. **Identity and purpose:** `PlatformCompanyApplicationsController.GetDocumentContent` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Reads document content from the current source-backed service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, Guid documentId`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`; `DocumentId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.GetDocumentContentAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `applicationService.GetDocumentContentAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.GetDocumentContentAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 file response. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/platform/company-applications/{applicationId}/documents/{documentId}/content` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 156. POST /api/platform/company-applications/{applicationId}/start-review

1. **Identity and purpose:** `PlatformCompanyApplicationsController.StartReview` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Executes the start review operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, PlatformRowVersionRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.StartReviewAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.StartReviewAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.StartReviewAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/platform/company-applications/{applicationId}/start-review`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 157. PUT /api/platform/company-applications/{applicationId}/documents/{documentId}/verification

1. **Identity and purpose:** `PlatformCompanyApplicationsController.VerifyDocument` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Executes the verify document operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, Guid documentId, PlatformDocumentVerificationRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`; `DocumentId is required.`; `RejectionReason must be omitted when approving a document.`; `RejectionReason is required when rejecting a document.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** A Base64 rowversion is decoded/validated and the service maps a stale write to the action conflict outcome.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.VerifyDocumentAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.VerifyDocumentAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.VerifyDocumentAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PlatformDocumentDecisionResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/platform/company-applications/{applicationId}/documents/{documentId}/verification`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 158. POST /api/platform/company-applications/{applicationId}/request-changes

1. **Identity and purpose:** `PlatformCompanyApplicationsController.RequestChanges` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Executes the request changes operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, PlatformDecisionRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/platform/company-applications/{applicationId}/request-changes`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 159. POST /api/platform/company-applications/{applicationId}/reject

1. **Identity and purpose:** `PlatformCompanyApplicationsController.Reject` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Executes the reject operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, PlatformDecisionRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as no asynchronous application-service call; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is no asynchronous application-service call. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls no asynchronous application-service call with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation; 401/403 on protected routes; generic 404 for unavailable/not-owned resources, 409 for lifecycle/concurrency/uniqueness conflict, and 503 for translated temporary service failure where those branches are defined; cancellation and otherwise unhandled failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/platform/company-applications/{applicationId}/reject`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 160. POST /api/platform/company-applications/{applicationId}/approve

1. **Identity and purpose:** `PlatformCompanyApplicationsController.Approve` ([source](../src/EstateHub.Api/Controllers/PlatformCompanyApplicationsController.cs)). Executes the approve operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid applicationId, PlatformCompanyApprovalRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ApplicationId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `applicationService.ApproveAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `applicationService.ApproveAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `applicationService.ApproveAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PlatformApprovedCompanyResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `POST /api/platform/company-applications/{applicationId}/approve`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 161. GET /api/platform/reviews

1. **Identity and purpose:** `PlatformReviewsController.GetReviews` ([source](../src/EstateHub.Api/Controllers/PlatformReviewsController.cs)). Reads reviews from the current source-backed service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** No route parameter.
4. **Query/body:** `[FromQuery] PlatformReviewDirectoryRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `PageNumber must be between 1 and 10000.`; `PageSize must be between 1 and 50.`; `Status must be Visible, Hidden, or PendingModeration.`; `Rating must be between 1 and 5.`; `CompanyId must be a non-empty GUID.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.GetReviewsAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `reviewService.GetReviewsAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.GetReviewsAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PlatformReviewDirectoryResponse>>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/platform/reviews` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 162. GET /api/platform/reviews/{reviewId}

1. **Identity and purpose:** `PlatformReviewsController.GetReview` ([source](../src/EstateHub.Api/Controllers/PlatformReviewsController.cs)). Reads review from the current source-backed service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid reviewId`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.GetReviewAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `reviewService.GetReviewAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.GetReviewAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 OK. The declared return type is `Task<ActionResult<PlatformReviewDetailsResponse>>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/platform/reviews/{reviewId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 163. PUT /api/platform/reviews/{reviewId}/moderation

1. **Identity and purpose:** `PlatformReviewsController.ModerateReview` ([source](../src/EstateHub.Api/Controllers/PlatformReviewsController.cs)). Executes the moderate review operation defined by the current controller and application service.
2. **Access:** Bearer JWT plus the `PlatformAdmin` Identity-role policy. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid reviewId, PlatformReviewModerationRequest request`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** `ReviewId is required.`
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** The role policy is distinct from company permissions; no company role implies this access.
8. **Business and lifecycle rules:** The controller validates the transport contract; the service enforces ownership, lifecycle, uniqueness and dependent-data rules before saving.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `reviewService.ModerateAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** No idempotency key is accepted. Repeat requests are constrained by state, uniqueness and concurrency rules.
12. **Side effects:** May persist the named state change and its service-defined related records. No unrelated subsystem should be inferred.
13. **Notifications:** The service call list is `reviewService.ModerateAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `reviewService.ModerateAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 204 No Content. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 validation. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `PUT /api/platform/reviews/{reviewId}/moderation`; bind route/query parameters as declared and send the request DTO as JSON when one is present.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.

### 164. GET /api/files/{fileAssetId}

1. **Identity and purpose:** `FilesController.GetPublicImage` ([source](../src/EstateHub.Api/Controllers/FilesController.cs)). Reads public image from the current source-backed service.
2. **Access:** Anonymous; no bearer token is required. Responses on this controller are marked no-store when indicated by the current controller implementation.
3. **Path:** Route tokens must bind to the declared signature; IDs must be valid non-empty GUIDs where the controller enforces that rule.
4. **Query/body:** `Guid fileAssetId`. Reusable wire fields are in [Batch 7 contracts](API_CONTRACTS_REFERENCE.md#batch-7-contracts).
5. **Validation:** No action-local static message appears in this action body; reusable controller/helper checks are catalogued in the [controller validation message catalog](API_CONTRACTS_REFERENCE.md#controller-validation-message-catalog), and service-level ownership/state/reference rules still apply.
6. **Enums and normalization:** Only the exact textual values and trim/case behavior implemented by this action are accepted; numeric enum strings are not documented as valid.
7. **Identity/tenant resolution:** Public eligibility is applied by the projected query; unavailable and nonexistent records share generic outcomes where implemented.
8. **Business and lifecycle rules:** Read-only projection with service-defined filters, deterministic ordering and paging. No write is performed.
9. **Concurrency:** No rowversion is accepted by this action; any service race is handled by its explicit conflict/uniqueness path.
10. **Transaction:** The controller opens no transaction. Atomicity is owned by the service implementation invoked as `service.GetPublicImageAsync`; a single SaveChanges is atomic, while explicit multi-write transactions are called out in the integration guide.
11. **Idempotency:** Safe/read-only; repeated calls can observe newer state.
12. **Side effects:** None.
13. **Notifications:** The service call list is `service.GetPublicImageAsync`. In-app notification creation occurs only when that implementation explicitly creates it; otherwise none is implied.
14. **Persistence:** Calls `service.GetPublicImageAsync` with the request cancellation token. Reads are projected/no-tracking where implemented; exceptions are not converted to success.
15. **Success:** 200 file response. The declared return type is `Task<IActionResult>`.
16. **Errors:** 400 for binding/query validation where applicable; 401/403 on protected routes; generic 404 on unavailable detail resources; cancellation and unhandled database failures propagate. Authentication failures are 401 and authenticated permission/role denial is 403.
17. **Request example:** `GET /api/files/{fileAssetId}` with any documented query parameters in the URL; no request body.
18. **Response example:** Serialize the declared response contract; collection properties remain arrays and opaque Base64/token values must be preserved exactly.
19. **Invalid examples:** Missing/empty GUIDs, unsupported enum text, invalid paging/ranges, stale rowversion, or forbidden lifecycle changes exercise the action’s validation/problem branch.
20. **Frontend guidance:** Handle 401, 403, 404, 409 and 503 distinctly; preserve no-store data in memory only and refresh projected lists after mutations.
21. **QA:** Cover success, every listed validation message, authorization policy, cross-tenant/not-found equivalence, cancellation, concurrency, deterministic results and absence of forbidden fields/writes.
22. **AI use:** Use only returned fields as facts. Do not infer tenant identity, hidden eligibility reasons, personal data, financial predictions or missing domain values.
