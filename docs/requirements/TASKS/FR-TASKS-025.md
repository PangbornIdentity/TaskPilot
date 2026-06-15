---
id: FR-TASKS-025
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: The Active/Completed/All control lets users focus on current work by default while still reaching finished or all tasks; URL state makes it shareable.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksShowParamTests.cs", "tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
The Tasks page has an Active/Completed/All segmented control: Active (the default) shows only NotStarted/InProgress/Blocked tasks, Completed shows only Completed/Cancelled tasks, and All shows every status; the selected segment is reflected in the URL and its pressed state.
