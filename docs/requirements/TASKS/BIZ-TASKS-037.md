---
id: BIZ-TASKS-037
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Silently ignoring foreign tag ids enforces tag ownership without failing the whole request over a stray id.
tests: []
---
When attaching tags on create, update, or patch, only tag ids that resolve to tags owned by the requesting user are associated; supplied tag ids that do not belong to the user are silently ignored rather than rejected.
