---
id: FR-TASKS-023
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Cycling column-header sort with URL and aria-sort state gives users familiar, accessible, shareable sorting.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
Clicking a sortable column header on the Tasks list cycles its sort through ascending, descending, then off, reflecting the state in the URL (sortBy/sortDir) and the header's aria-sort attribute.
