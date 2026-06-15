---
id: BIZ-STATS-009
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: Splitting completions into Personal vs Work shows the user how their finished effort divides across life areas.
---
CompletionsByArea counts the user's completed tasks separately per area (Personal and Work); non-completed tasks are excluded.
