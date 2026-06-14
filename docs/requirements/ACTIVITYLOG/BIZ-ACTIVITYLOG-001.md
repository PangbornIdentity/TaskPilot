---
id: BIZ-ACTIVITYLOG-001
type: BIZ
area: ACTIVITYLOG
provenance: test
status: unratified
verification: automated
why: SUGGESTED: Clamping the page count to a minimum of 1 keeps the pager well-formed even when a user has no activity.
tests: ["tests/TaskPilot.Tests.Unit/Services/ActivityLogServiceTests.cs"]
---
The total page count for a paged activity-log result is the ceiling of total record count divided by page size, clamped to a minimum of 1 (an empty result reports 1 total page).
