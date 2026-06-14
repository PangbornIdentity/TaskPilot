---
id: BIZ-STATS-003
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: Overdue highlights work that slipped past its target date so the user can triage it; completed-but-late tasks aren't actionable and are excluded.
---
Overdue counts the user's tasks with a target date earlier than today that are not completed; completed tasks past their target date are excluded.
