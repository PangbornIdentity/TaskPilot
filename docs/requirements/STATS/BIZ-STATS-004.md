---
id: BIZ-STATS-004
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: Separate InProgress and Blocked counts let the user see active work versus work that is stuck and needs unblocking.
---
InProgress counts only the user's tasks with status InProgress, and Blocked counts only the user's tasks with status Blocked.
