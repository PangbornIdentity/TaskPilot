---
id: BIZ-HEALTH-003
type: BIZ
area: HEALTH
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Diagnostics/HealthCheckTests.cs"]
why: Classifying each check's required-ness correctly is what makes the aggregate healthy/degraded/unhealthy verdict trustworthy for routing decisions.
---
Each health check component reports the correct name, required-ness, and status: the database check (required) is healthy when it can connect and unhealthy on connection failure; the migrations check (required, exposing a "pendingMigrations" count) is unhealthy when migrations are pending; the config check (required) is unhealthy when a required key such as "Hmac:SecretKey" is missing and healthy when all required keys are present; the auth-handlers check (required) is unhealthy when the ApiKey scheme is not registered; the mcp check (optional) is unhealthy when the MCP endpoint is not registered; the temp-writable check (optional) is healthy when it can write and delete a temp file, leaving no temp file behind; the assembly-metadata check (optional) is unhealthy when the git commit is "unknown".
