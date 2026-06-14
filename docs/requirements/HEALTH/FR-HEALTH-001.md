---
id: FR-HEALTH-001
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs"]
why: Post-deploy verification needs an unambiguous, uncacheable way to confirm exactly which build and commit is live on an instance.
---
GET /api/v1/health/version returns 200 with a payload exposing the build version, full git commit, and short git commit, matching the values stamped into the assembly; the response carries no-cache headers (Cache-Control no-store, Pragma no-cache, Expires 0) and custom X-TaskPilot-Version and X-TaskPilot-Commit headers.
