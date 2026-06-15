---
id: BIZ-HEALTH-004
type: BIZ
area: HEALTH
provenance: code
status: unratified
verification: automated
tests: []
why: A per-run timeout prevents a single hanging check from stalling the readiness response and tripping the platform's own probe timeout.
---
The health service bounds every check run (readiness and full) with a 5-second timeout enforced via a cancellation token linked to the caller's token, so an individual check that hangs is cancelled rather than blocking the response indefinitely.
