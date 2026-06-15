---
id: FR-TASKS-005
type: FR
area: TASKS
provenance: test
status: unratified
verification: automated
why: Auto-creating a successor on completing a recurring task removes the manual work of re-entering routine tasks.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
Completing a recurring task creates a successor task in addition to completing the current one.
