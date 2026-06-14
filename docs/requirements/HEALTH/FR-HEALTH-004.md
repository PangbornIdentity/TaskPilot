---
id: FR-HEALTH-004
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs"]
why: The deep per-check breakdown lets an operator diagnose which component degraded an instance without shelling into the host.
---
GET /api/v1/health/full returns the aggregate status plus a per-check array including the checks database, migrations, config, auth-handlers, mcp, temp-writable, and assembly-metadata, where each check carries a non-null duration; it returns 200 (status "healthy" or "degraded") when only optional checks fail and 503 (status "unhealthy") when a required check such as migrations fails.
