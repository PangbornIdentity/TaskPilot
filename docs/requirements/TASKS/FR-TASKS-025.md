---
id: FR-TASKS-025
type: FR
area: TASKS
provenance: human
status: ratified
verification: automated
why: The Active/Completed/All control lets users focus on current work by default while still reaching finished or all tasks; URL state makes it shareable.
tests: ["tests/TaskPilot.Tests.Integration/Tasks/TasksShowParamTests.cs", "tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/TaskScopeApiTests.cs", "tests/TaskPilot.Tests.Unit/Services/TaskScopeFilterTests.cs"]
---
The Active/Completed/All scope is enforced server-side — the repository filters by scope (Active = NotStarted/InProgress/Blocked; Completed = Completed/Cancelled; All = no restriction) and pagination + total count reflect the scoped set.
