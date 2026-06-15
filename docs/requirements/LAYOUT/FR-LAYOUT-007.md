---
id: FR-LAYOUT-007
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: Rejecting blank titles prevents empty tasks, and post-redirect-get with a toast confirms success while avoiding duplicate creates on refresh.
---
Creating a task from the Tasks page rejects a whitespace-only title with an error toast and no creation; on success the new task is created with IsRecurring false and a success toast is shown via post-redirect-get.
