---
id: BIZ-STATS-017
type: BIZ
area: STATS
provenance: code
status: unratified
verification: automated
tests: []
why: Bucketing untyped tasks as "Unknown" ensures the by-type totals add up to all incomplete tasks, so the chart never silently drops work.
---
ByType labels tasks whose task-type association is absent as "Unknown" so every incomplete task contributes to exactly one type bucket.
