---
id: BIZ-STATS-016
type: BIZ
area: STATS
provenance: code
status: unratified
verification: automated
tests: []
why: Deriving NotStarted arithmetically avoids an extra DB query, and clamping at zero guards against a nonsensical negative count from any timing skew.
---
The NotStarted count within IncompleteByStatus is derived arithmetically as TotalActive minus InProgress minus Blocked (avoiding an extra query) and is clamped to a minimum of zero so it can never be negative.
