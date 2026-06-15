---
id: FR-TASKS-035
type: FR
area: TASKS
provenance: human
status: ratified
verification: automated
why: Rejecting out-of-range enum values prevents corrupt enum state that downstream logic and storage cannot interpret.
tests: ["tests/TaskPilot.Tests.Unit/Validators/PatchTaskRequestValidatorTests.cs", "tests/TaskPilot.Tests.Integration/Tasks/PatchTaskValidationTests.cs"]
---
Create, full-update (PUT), AND partial-update (PATCH) reject invalid input (empty/>200-char title, TaskTypeId ≤ 0, non-enum priority/status/area/date-type, IsRecurring without a valid RecurrencePattern, SpecificDay without a date) with HTTP 400 and the standard validation-error envelope.
