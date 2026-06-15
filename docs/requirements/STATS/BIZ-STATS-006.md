---
id: BIZ-STATS-006
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: The breakdown-by-type donut shows the user what kinds of open work dominate their plate; completed tasks would distort the current mix.
---
ByType groups the user's incomplete tasks by task-type name and reports a count per type (completed tasks excluded).
