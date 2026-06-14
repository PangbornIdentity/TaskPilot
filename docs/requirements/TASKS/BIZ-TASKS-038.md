---
id: BIZ-TASKS-038
type: BIZ
area: TASKS
provenance: code
status: unratified
verification: automated
why: Returning a correct rounded-up page count lets the UI render accurate pagination controls.
tests: []
---
The total page count returned with a paginated task listing is the total matching task count divided by the requested page size, rounded up to the next whole number.
