---
id: FR-TASKTYPES-002
type: FR
area: TASKTYPES
provenance: doc
status: unratified
why: TaskType is a fixed lookup for iteration 1; seeding the six standard types at startup and exposing only a read endpoint avoids the cost of full CRUD management UI that the product does not yet need.
verification: automated
tests: []
---
TaskType records are read-only in iteration 1: the API exposes only `GET /api/v1/task-types` (list active types) and there are no create, update, or delete endpoints for task types. The six standard types are provided as seed data applied at startup rather than managed through the application.
