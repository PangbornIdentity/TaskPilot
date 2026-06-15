---
id: BIZ-TASKS-001
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Appending new tasks at the end of the user's ordering keeps manual sort positions stable so existing tasks don't shift when one is added.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
A newly created task receives a sort order of one greater than the user's current maximum task sort order.
