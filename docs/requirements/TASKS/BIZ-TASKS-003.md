---
id: BIZ-TASKS-003
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: A create entry gives the activity log a defined starting point so the task's full change history is auditable from inception.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
Creating a task writes a single activity-log entry whose field is "Created" and whose new value is the task title.
