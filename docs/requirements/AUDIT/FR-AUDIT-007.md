---
id: FR-AUDIT-007
type: FR
area: AUDIT
provenance: test
status: unratified
verification: automated
why: Scoping audit logs to the requesting user and paginating them prevents cross-tenant data exposure and keeps large log volumes navigable.
tests: ["tests/TaskPilot.Tests.Integration/Audit/AuditApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/AuditServiceTests.cs"]
---
GET /api/v1/audit returns the requesting user's API access audit logs as a paginated list, honoring page and pageSize parameters and returning an empty list when the user has no logs.
