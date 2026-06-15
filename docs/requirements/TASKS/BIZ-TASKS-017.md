---
id: BIZ-TASKS-017
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: The copy suffix gives a clone a clearly distinguishable default name so users do not confuse it with the original.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
When no title override is supplied (including null, empty, or whitespace-only values), a cloned task's title is the source title with " (copy)" appended.
