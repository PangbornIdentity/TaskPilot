---
id: BIZ-STATS-018
type: BIZ
area: STATS
provenance: code
status: unratified
verification: automated
tests: []
why: Excluding soft-deleted tasks keeps the top-tags ranking reflective of live work rather than tags inflated by deleted tasks.
---
TopTags counts only tags attached to the user's non-deleted tasks, so tags on soft-deleted tasks do not contribute to the top-tags ranking.
