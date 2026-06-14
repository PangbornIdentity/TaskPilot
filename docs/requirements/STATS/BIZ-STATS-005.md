---
id: BIZ-STATS-005
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: The Incomplete-by-Status card needs each open-status count plus a Total that reconciles with TotalActive so the user trusts the breakdown.
---
IncompleteByStatus breaks down the user's active (incomplete) tasks into NotStarted, InProgress, and Blocked counts plus a Total; Completed and Cancelled tasks are excluded, and the Total equals TotalActive.
