---
id: FR-AUDIT-001
type: FR
area: AUDIT
provenance: test
status: unratified
verification: automated
why: SUGGESTED: Task History is the more frequently consulted view, so it is the sensible default landing tab on the audit page.
tests: ["tests/TaskPilot.Tests.E2E/Audit/AuditTests.cs"]
---
The audit page presents two tabs — "Task History" and "API Access" — and opens on the Task History tab by default when no tab is specified.
