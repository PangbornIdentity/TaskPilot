---
id: BIZ-TASKS-028
type: BIZ
area: TASKS
provenance: test
status: unratified
verification: automated
why: Never overwriting with null protects existing data from being wiped by partial inputs that simply omit a field.
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskServiceTests.cs"]
---
A full or partial update never overwrites a field with a null/omitted input value; the existing stored value is retained.
