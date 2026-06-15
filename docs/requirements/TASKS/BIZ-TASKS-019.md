---
id: BIZ-TASKS-019
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Enforcing the 200-char limit on clones keeps clone titles within the same storage and display constraints as any other task.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceCloneTests.cs"]
---
A cloned task's title is truncated so it never exceeds 200 characters, whether the length comes from the copy-suffix derivation or an over-long title override.
