---
id: BIZ-STATS-014
type: BIZ
area: STATS
provenance: code
status: ratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsWeekOrderingTests.cs"]
why: Pairing created vs completed per week shows whether the user is keeping up with incoming work or falling behind.
---
CompletionRate pairs, per week over the last 12 weeks, the count of tasks created in that week against the count of tasks completed in that week (keyed by year and week index), with completed defaulting to zero when no completions match a created-week bucket.
