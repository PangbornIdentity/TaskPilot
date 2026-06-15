---
id: BIZ-STATS-001
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: TotalActive is the headline "how much is on my plate" number; excluding Completed and Cancelled keeps it to work that still needs doing.
---
TotalActive counts the user's tasks whose status is neither Completed nor Cancelled.
