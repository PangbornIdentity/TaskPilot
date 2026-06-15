---
id: NFR-TASKS-003
type: NFR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Showing a chevron only on the active sort header makes the current sort unambiguous and avoids implying multiple columns are sorted.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
Inactive sortable column headers on the Tasks list render no chevron icon; a sort chevron appears only on the single currently-active sort header.
