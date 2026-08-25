# EstateHub API Documentation Findings

This file records documentation observations only. No production behavior is changed by this documentation step.


## Batch 1 findings

| Severity | Finding | Evidence/impact |
|---|---|---|
| Medium | Swagger middleware is not currently protected by the intended Development-only guard. | Current `Program.cs` contains the environment conditional as commented source, so deployed behavior depends on the active middleware lines. Documentation does not change it. |
| Low | Promoted-listing placement is required but no placement-discovery endpoint exists. | Clients need an out-of-band configured placement value; invalid/unknown placement yields the service-defined empty/result behavior. |
## Final source-backed findings

| ID | Severity | Finding | Current-source evidence | Integration consequence |
|---|---|---|---|---|
| DOC-001 | Medium | Swagger and Swagger UI execute unconditionally. | In Program.cs, the Development environment wrapper is commented while UseSwagger and UseSwaggerUI remain active. | Treat production Swagger exposure as current behavior until the owner restores the guard. No change was made here. |
| DOC-002 | Medium | Remote-IP auth rate limiting has no source-visible forwarded-header setup. | Partitions use HttpContext.Connection.RemoteIpAddress; no UseForwardedHeaders registration is present in the API source. | Behind a reverse proxy, many callers may share the proxy partition unless hosting supplies trusted forwarding outside this source. Hosting behavior is **Not specified by current source.** |
| DOC-003 | Medium | No centralized ProblemDetails exception pipeline is configured. | Controllers create ProblemDetails for known branches, but current Program.cs contains no AddProblemDetails/exception-handler middleware. | Known errors have documented shapes; the body/shape of an unexpected 500 is **Not specified by current source.** Clients must not rely on all failures being ProblemDetails. |
| DOC-004 | Low | The committed CORS origin is a local-development origin. | appsettings.json contains only http://localhost:5173; startup requires at least one valid origin. | Deployment must provide the actual frontend origins through configuration. Credentials are not enabled by the CORS policy. |
| DOC-005 | Low | Promoted-listing placement has no discovery endpoint. | GET /api/promoted-listings requires a placement string; the 164-route registry contains no placement-catalog route. | The frontend needs an agreed placement value from product/configuration; discovery behavior is **Not specified by current source.** |
| DOC-006 | Informational | Enum handling is intentionally local, not globally uniform. | There is no global JsonStringEnumConverter; controllers contain focused parsers/mappers. | Clients must follow each endpoint's documented case/trim/value rules rather than assume one universal enum parser. |
| DOC-007 | Informational | Generic not-found behavior intentionally hides multiple eligibility/ownership states. | Public and tenant-scoped services/controllers project only eligible/owned data and use generic 404 branches. | QA and clients must not interpret 404 as proof that a database row does not exist. |
| DOC-008 | Informational | The API has no endpoint version segment. | All current controller routes begin with /api/...; Swagger document naming uses v1 but route templates do not. | URI-versioning/backward-compatibility policy is **Not specified by current source.** |
| DOC-009 | Informational | Current response contracts do not expose stable machine-readable domain error codes. | ProblemDetails branches provide status/title/detail and validation dictionaries; no common application error-code member is configured. | Clients should branch primarily on HTTP status and field validation, not parse English text as a permanent code. |

## Reconciliation findings

- Current controller action methods with HTTP mappings: **164**.
- HTTP mapping attributes: **164**.
- Effective verb/route pairs: **164**.
- Distinct verb/route pairs: **164**.
- Duplicate verb/route pairs: **0**.
- Actions with multiple HTTP mapping attributes: **0**.
- Public routes: **26**; authenticated routes (personal or company): **127**; PlatformAdmin routes: **11**.
- Company-permission-protected route mappings: **92**, across exactly **18** stable permission codes.
- Current authentication rate-limit policies: **5**, attached across **8** auth routes.
- Public API transport declarations catalogued: **278** (type declarations, including reusable nested/base contracts; same short type names can legitimately occur in different modules).
- Domain enums catalogued: **33**.

## Scope disposition

These are documentation observations, not authorized code changes. No finding was remediated, no endpoint was added or removed, and no runtime/configuration/schema behavior was altered.
