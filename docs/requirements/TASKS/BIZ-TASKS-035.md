---
id: BIZ-TASKS-035
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Distinguishing explicit completion from an update-driven status change preserves an existing completion timestamp during edits while still stamping a true completion event.
tests: []
---
Completing a task sets its completion timestamp to the current time unconditionally, even if a completion timestamp was already present, whereas changing a task to Completed via a full or partial update sets the completion timestamp only when it is currently null (an already-set completion timestamp is preserved).
