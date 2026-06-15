---
id: FR-HEALTH-009
type: FR
area: HEALTH
provenance: code
status: unratified
verification: automated
tests: []
why: Uptime, environment, and machine name help operators spot recent restarts and confirm they are inspecting the intended instance.
---
The /api/v1/health/version and /api/v1/health/full|ready responses expose an uptime computed as the difference between current UTC time and the process start time, plus the runtime environment name and machine name.
