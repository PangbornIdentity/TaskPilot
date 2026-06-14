---
id: BIZ-STATS-008
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: The completed-per-week bar chart shows recent throughput so the user can see their pace trend at a glance.
---
CompletedPerWeek summarizes the user's completed tasks over the last 12 weeks into per-week counts, with a completion within the current week counted in its week's bucket.
