---
id: BIZ-AUDIT-003
type: BIZ
area: AUDIT
provenance: code
status: unratified
why: SUGGESTED: Clamping the empty case to one page keeps the pager well-formed (never zero pages) when there is no activity.
verification: automated
tests: []
---
A paged audit-log result whose total record count is zero reports a total page count of 1 (the ceiling computation is bypassed and clamped to 1 for the empty case).
