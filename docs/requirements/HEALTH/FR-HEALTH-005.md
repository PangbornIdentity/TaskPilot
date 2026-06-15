---
id: FR-HEALTH-005
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs", "tests/TaskPilot.Tests.Integration/Smoke/DeploymentSmokeTests.cs"]
why: A SHA-256 asset manifest lets operators detect a stale or CDN-cached bundle that no longer matches the deployed build.
---
GET /api/v1/health/assets returns 200 with an asset manifest mapping each asset path to its SHA-256 integrity hash (prefixed "sha256-"); the hashes are stable across repeated calls and match the SHA-256 of the actually served asset bytes.
