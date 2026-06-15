---
id: FR-TASKS-024
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Preserving other filters when sorting prevents losing the user's current view context on a header click.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
Sorting via a column header preserves the other active filters (such as area) in the URL.
