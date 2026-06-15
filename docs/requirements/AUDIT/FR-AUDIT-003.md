---
id: FR-AUDIT-003
type: FR
area: AUDIT
provenance: test
status: unratified
verification: automated
why: The Task History tab exists to let the owner see what changed on a task and when, so edits must surface as visible history entries.
tests: ["tests/TaskPilot.Tests.E2E/Audit/AuditTests.cs"]
---
After a task is edited, the Task History tab displays the resulting change-history entries for that user.
