---
id: BIZ-TASKTYPES-002
type: BIZ
area: TASKTYPES
provenance: doc
status: unratified
why: Deactivating rather than deleting a type, plus a restrict-on-delete relationship, preserves referential integrity so historical tasks keep a valid type instead of pointing at a removed record.
verification: automated
tests: []
---
An inactive task type is hidden from selection dropdowns but is retained for any existing tasks that already reference it: a task type cannot be deleted while tasks reference it (the task-to-task-type relationship is restricted on delete), so historical tasks keep a valid type even after that type is deactivated.
