---
id: FR-TASKS-020
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: In-page search lets users narrow a long task list quickly; the empty state confirms a no-match result rather than a broken page.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
On the Tasks page the user can filter the list with a search input, which narrows results and shows an empty state when nothing matches.
