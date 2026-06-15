---
id: FR-AUDIT-008
type: FR
area: AUDIT
provenance: test
status: unratified
verification: automated
why: The summary endpoint backs the dashboard cards, so it must return the same per-user totals and zero them for a user with no activity.
tests: ["tests/TaskPilot.Tests.Integration/Audit/AuditApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/AuditServiceTests.cs"]
---
GET /api/v1/audit/summary returns the requesting user's API usage summary comprising totalRequests, getsToday, writesToday, and activeApiKeys, all zero for a user with no API activity.
