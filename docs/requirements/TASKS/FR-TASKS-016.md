---
id: FR-TASKS-016
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Inline tag creation lets users categorize a task without breaking flow to a separate tag-management screen.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskAreaTypeTagTests.cs"]
---
A user can create a new tag inline from within the task-create modal, and the new tag immediately appears in the modal's tag list and can be assigned to the task being created.
