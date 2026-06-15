---
id: FR-AUDIT-005
type: FR
area: AUDIT
provenance: test
status: unratified
verification: automated
why: An empty state confirms there has been no API activity rather than leaving the user staring at a blank or seemingly broken log.
tests: ["tests/TaskPilot.Tests.E2E/Audit/AuditTests.cs"]
---
When the user has no API access logs, the API Access tab shows an empty-state message instead of a populated log list.
