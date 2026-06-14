---
id: NFR-HEALTH-001
type: NFR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs"]
why: Readiness probes run on a tight interval; a sub-500ms response keeps the check well inside typical platform probe timeouts.
---
GET /api/v1/health/ready responds within 500ms.
