---
id: FR-HEALTH-002
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs"]
why: A dependency-free liveness probe lets orchestrators (Azure Always On, uptime monitors) confirm the process is up without coupling to DB health.
---
GET /api/v1/health/live always returns 200 with status "alive".
