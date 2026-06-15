---
id: BIZ-TASKS-036
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Differentiating full-replace from partial tag semantics lets a patch leave tags untouched while a full update authoritatively sets them, matching user expectations of each operation.
tests: []
---
A full update replaces the task's entire tag set with the supplied tag ids (an empty or omitted list clears all tags), whereas a partial update modifies the tag set only when a tag-id list is supplied: a supplied empty list clears all tags, and an omitted (null) list leaves the existing tags unchanged.
