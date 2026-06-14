---
id: BIZ-HEALTH-005
type: BIZ
area: HEALTH
provenance: code
status: unratified
verification: automated
tests: []
why: The assets endpoint exists to detect stale bundles; a single missing file must not break that diagnostic by failing the whole response.
---
The asset fingerprint manifest omits (rather than errors on) any tracked asset whose physical file is missing, logging a warning and continuing, so a missing asset never fails the /api/v1/health/assets response.
