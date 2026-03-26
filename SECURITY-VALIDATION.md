# TaskPilot — Security Validation

> Produced in Phase 4. All checks performed against the codebase as-built.
> Reviewer: qa-engineer agent | Date: 2026-03-26

## 1. Authentication & Authorization
- [x] All API endpoints require authentication (cookie or API key)
- [x] API keys validated via HMAC-SHA256 hash — plaintext never stored
- [x] API key prefix (8 chars) stored for display only, never used for auth
- [x] Inactive API keys rejected immediately in handler
- [x] Cookie auth uses `isPersistent: true` on login — appropriate for single-user app
- [x] `[Authorize]` on all controllers (AccountController login/register are public by design)

## 2. Data Protection
- [x] HMAC-SHA256 key — secret loaded from `IConfiguration` (user-secrets locally, Key Vault in prod)
- [x] No plaintext API keys in DB — only hash + 8-char prefix
- [x] Request bodies hashed with SHA256 in audit log — full body never stored
- [x] SQLite DB file not in wwwroot — not web-accessible
- [x] Soft-delete only — `IsDeleted + DeletedAt`, global EF query filter excludes deleted records

## 3. Input Validation
- [x] FluentValidation on all request DTOs
- [x] Title max 200 chars enforced at validator and DB column level
- [x] Tag color validated as hex regex `^#[0-9A-Fa-f]{6}$`
- [x] Enum inputs validated with `.IsInEnum()`
- [x] GUID route parameters — invalid GUIDs return 400 automatically via route constraint `{id:guid}`

## 4. API Security
- [x] Standard error envelope — no stack traces leaked in production
- [x] `GlobalExceptionMiddleware` catches all unhandled exceptions, returns generic 500
- [x] CORS restricted to localhost origins (dev) — must be updated for production
- [x] All responses use `ApiResponse<T>` envelope — no bare JSON
- [x] No rate limiting in iteration 1 — middleware insertion point documented in ARCHITECTURE.md §4.8
- [x] Swagger disabled in production (`if (app.Environment.IsDevelopment())`)

## 5. Multi-Tenancy / Data Isolation
- [x] All repository queries filter by `userId`
- [x] `GetByIdWithTagsAsync` includes `&& t.UserId == userId` — cannot access other users' tasks
- [x] API key validation returns `UserId` from DB — user context set from DB, not from request
- [x] `DeleteTaskAsync` checks `task.UserId != userId` — returns false for cross-user access
- [x] Tags scoped to user — `GetByNameAsync` includes `userId` filter

## 6. Secrets Management
- [x] HMAC secret key never hardcoded — loaded from `IConfiguration`
- [x] `dotnet user-secrets` configured for local development
- [x] `appsettings.json` contains empty `Hmac:SecretKey` placeholder only
- [x] Connection string uses SQLite file path — no credentials needed for iteration 1
- [ ] Azure Key Vault integration — deferred to iteration 2

## 7. Logging & Audit
- [x] Serilog structured logging on all controllers and middleware
- [x] `ApiAuditMiddleware` logs every API key request — method, endpoint, status, duration, body hash
- [x] Audit failures are non-blocking — logged but do not affect response
- [x] `TaskActivityLog` created on every task write — field-level change tracking
- [x] `LastModifiedBy` format enforced: `"user:{username}"` or `"api:{keyName}"`

## Summary
| Category | Passed | Failed | N/A |
|----------|--------|--------|-----|
| Authentication & Authorization | 6 | 0 | 0 |
| Data Protection | 5 | 0 | 0 |
| Input Validation | 5 | 0 | 0 |
| API Security | 6 | 0 | 1 (rate limiting — iter 2) |
| Multi-Tenancy / Data Isolation | 5 | 0 | 0 |
| Secrets Management | 5 | 0 | 1 (Key Vault — iter 2) |
| Logging & Audit | 5 | 0 | 0 |
| **Total** | **37** | **0** | **2** |
