---
id: BIZ-TASKS-007
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: SUGGESTED: A full update should record every genuine field change so the activity log stays a complete, trustworthy history.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
A full update writes an activity-log entry for each field whose value differs from the prior stored value.
