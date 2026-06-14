---
id: BIZ-HEALTH-002
type: BIZ
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Diagnostics/HealthCheckTests.cs"]
why: Distinguishing degraded (optional check failed) from unhealthy (required check failed) lets orchestrators keep serving when only non-critical components are down.
---
The health service aggregates individual check results into an overall status: "healthy" when all checks pass, "unhealthy" when any required check fails, and "degraded" when only optional (non-required) checks fail.
