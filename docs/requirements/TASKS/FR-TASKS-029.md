---
id: FR-TASKS-029
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Persisting filters per session restores the user's working view on return without redirect loops, improving continuity.
tests: ["tests/TaskPilot.Tests.E2E/Tasks/TaskLifecycleTests.cs"]
---
Tasks-page filter selections are persisted per browser session and restored when the user navigates back to the Tasks page (via sidebar or address bar), without producing a navigation/redirect loop; a fresh session starts at the default Active view.
