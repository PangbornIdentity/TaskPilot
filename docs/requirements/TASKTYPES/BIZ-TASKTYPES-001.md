---
id: BIZ-TASKTYPES-001
type: BIZ
area: TASKTYPES
provenance: test
status: unratified
why: Hiding inactive types keeps retired options out of selection dropdowns, while SortOrder gives the product control over presentation order independent of id or name.
verification: automated
tests: ["tests/TaskPilot.Tests.Unit/Services/TaskTypeServiceTests.cs", "tests/TaskPilot.Tests.Integration/TaskTypes/TaskTypeApiTests.cs"]
---
The task types returned exclude inactive types and are ordered ascending by their SortOrder; when no active types exist the result is an empty list.
