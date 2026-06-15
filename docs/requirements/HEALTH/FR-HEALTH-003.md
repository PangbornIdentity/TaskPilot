---
id: FR-HEALTH-003
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs"]
why: Readiness must return 503 when a required dependency is down so the platform stops routing traffic to an instance that cannot serve requests.
---
GET /api/v1/health/ready returns 200 with status "healthy" or "degraded" when all required checks pass, and 503 with status "unhealthy" when a required check (e.g. database) is unhealthy.
