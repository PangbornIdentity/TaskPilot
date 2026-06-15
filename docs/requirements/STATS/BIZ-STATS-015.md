---
id: BIZ-STATS-015
type: BIZ
area: STATS
provenance: code
status: ratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsWeekOrderingTests.cs"]
why: Average time-to-complete per week shows the user whether tasks are turning around faster or lingering longer over time.
---
AvgCompletion reports, per week over the last 12 weeks, the average time-to-complete in days, computed as the mean of (CompletedDate − CreatedDate).TotalDays across that week's completed tasks.
