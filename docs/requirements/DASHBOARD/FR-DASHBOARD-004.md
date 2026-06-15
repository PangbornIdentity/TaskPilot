---
id: FR-DASHBOARD-004
type: FR
area: DASHBOARD
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.E2E/Dashboard/DashboardTests.cs"]
why: Making cards and tiles click-through to the matching filtered list turns each metric into a direct path to act on it.
---
Dashboard summary cards and the incomplete-status sub-tiles link to the filtered Tasks view: the not-started tile navigates to the tasks list filtered by status=NotStarted (with active tasks as the default scope), and the overdue card navigates to the tasks list filtered by overdue=true (with active tasks as the default scope).
