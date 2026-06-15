---
id: BIZ-TASKS-026
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Validating the clone title up front prevents invalid clones and gives the user immediate feedback, mirroring create and update rules.
tests: ["tests/TaskPilot.Tests.Unit/Validators/CloneTaskRequestValidatorTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/CloneTaskEndpointTests.cs"]
---
A clone request is valid when its title is absent, null, whitespace-only, or up to 200 characters, and is invalid (rejected) when the supplied title exceeds 200 characters.
