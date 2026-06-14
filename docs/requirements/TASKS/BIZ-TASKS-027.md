---
id: BIZ-TASKS-027
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Accepting all target-date option combinations keeps the clone API flexible for any due-date intent without surprising rejections.
tests: ["tests/TaskPilot.Tests.Unit/Validators/CloneTaskRequestValidatorTests.cs"]
---
A clone request's target-date and clear-target-date options are accepted in any combination (clear true, clear false, target date with clear true or false).
