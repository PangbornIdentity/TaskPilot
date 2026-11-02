---
id: FR-TASKS-042
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Recurrence can only be set through the API or MCP, so a web edit that does not show those fields must not silently reset them.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
Saving the Task Detail edit form keeps the task's stored IsRecurring and RecurrencePattern values, because the form does not include them; only the fields on the form are updated and logged.
