---
id: BIZ-STATS-011
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: Per-user scoping prevents one user's stats from leaking another's data, and zeroed metrics on an empty DB give new users a clean, non-erroring dashboard.
---
All task statistics are scoped to the requesting user, and an empty database yields zero counts for every metric.
