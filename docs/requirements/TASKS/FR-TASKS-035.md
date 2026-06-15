---
id: FR-TASKS-035
type: FR
area: TASKS
provenance: code
status: unratified
verification: automated
why: Rejecting out-of-range enum values prevents corrupt enum state that downstream logic and storage cannot interpret.
tests: []
---
Creating or fully updating a task is rejected when the priority, status, area, or target-date-type value is not a defined enum value.
