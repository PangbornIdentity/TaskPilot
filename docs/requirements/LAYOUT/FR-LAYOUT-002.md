---
id: FR-LAYOUT-002
type: FR
area: LAYOUT
provenance: code
status: unratified
verification: automated
tests: []
why: SUGGESTED: Querying only incomplete tasks for the default "active" scope keeps the common view fast, while broader scopes fetch more and filter in the page layer.
---
For the "active" show scope the Tasks page requests only incomplete tasks from the repository; for the "completed" scope it fetches all tasks and then post-filters at the page layer to only Completed and Cancelled tasks; "all" returns every status.
