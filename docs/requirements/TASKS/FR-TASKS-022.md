---
id: FR-TASKS-022
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: The Overdue chip lets users toggle a late-work view, and reflecting it in the URL makes the view shareable and bookmarkable.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
The Tasks page provides an Overdue filter chip that toggles its pressed state and adds or removes overdue=true from the page URL.
