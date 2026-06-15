---
id: BIZ-HEALTH-006
type: BIZ
area: HEALTH
provenance: code
status: unratified
verification: automated
tests: []
why: The /health page must stay reachable precisely when things are broken, so a failing health check cannot be allowed to crash the page itself.
---
The public "/health" page renders even when running the full health check throws: the exception is swallowed, the health result is treated as null, and the page still returns successfully with no-cache headers.
