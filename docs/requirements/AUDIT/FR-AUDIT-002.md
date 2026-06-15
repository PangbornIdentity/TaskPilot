---
id: FR-AUDIT-002
type: FR
area: AUDIT
provenance: test
status: unratified
verification: automated
why: An explicit empty state reassures the user that there is genuinely no history rather than a load failure or empty table glitch.
tests: ["tests/TaskPilot.Tests.E2E/Audit/AuditTests.cs"]
---
When the user has no task change history, the Task History tab shows an empty-state message instead of a populated history table.
