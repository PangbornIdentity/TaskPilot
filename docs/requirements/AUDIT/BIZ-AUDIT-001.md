---
id: BIZ-AUDIT-001
type: BIZ
area: AUDIT
provenance: test
status: unratified
verification: automated
why: SUGGESTED: A correct page count lets the audit-log pager show the right number of pages so the owner can reach every record.
tests: ["tests/TaskPilot.Tests.Unit/Services/AuditServiceTests.cs"]
---
The total page count for a paged audit-log result is the ceiling of total record count divided by page size (e.g. 5 records at page size 3 yields 2 pages).
