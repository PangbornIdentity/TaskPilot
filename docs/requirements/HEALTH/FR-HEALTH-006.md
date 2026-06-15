---
id: FR-HEALTH-006
type: FR
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Integration/Health/HealthEndpointTests.cs"]
why: Health endpoints must stay anonymous so external probes and uptime monitors can reach them without credentials.
---
All /api/v1/health/* endpoints (live, ready, full, version, assets) are accessible anonymously (never returning 401 or 403).
