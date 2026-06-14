---
id: BIZ-STATS-010
type: BIZ
area: STATS
provenance: test
status: unratified
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/StatsServiceTests.cs"]
why: Capping the tag chart at the top five keeps it readable and surfaces only the user's dominant categories.
---
TopTags returns at most the five most-used tags for the user, ordered by descending task count, excluding tags below the cutoff when more than five tags exist.
