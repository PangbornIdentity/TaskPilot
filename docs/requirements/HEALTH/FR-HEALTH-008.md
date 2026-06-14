---
id: FR-HEALTH-008
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Health/HealthPageTests.cs"]
why: The version pill gives every user an always-visible, authoritative indicator of the running build that links straight to diagnostics.
---
The sidebar shows a version pill displaying the current build version and short git commit (matching /api/v1/health/version) that links to the "/health" page.
