---
id: FR-HEALTH-007
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Health/HealthPageTests.cs"]
why: A human-readable /health page lets anyone inspect a deployed instance's status and version at a glance without calling the API directly.
---
The application exposes a public "/health" page (loadable anonymously, never redirecting to login) that renders the overall health status badge, the build version, and the short git commit consistent with the /api/v1/health endpoints, lists at least seven per-check rows each showing name/status/duration, and provides a "raw JSON" link that navigates to /api/v1/health/full.
