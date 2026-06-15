---
id: BIZ-TASKS-044
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Exposing a human-readable area name lets clients display the area without re-implementing the enum-to-label mapping.
tests: []
---
A task response exposes a human-readable area name derived from the area value: "Work" for Work and "Personal" for any other area value.
