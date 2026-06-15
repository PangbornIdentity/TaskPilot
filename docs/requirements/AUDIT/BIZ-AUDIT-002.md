---
id: BIZ-AUDIT-002
type: BIZ
area: AUDIT
provenance: code
status: unratified
why: SUGGESTED: These definitions back the dashboard summary cards — separating all-time totals from today's reads vs writes and counting only live keys gives the owner a meaningful activity snapshot.
verification: automated
tests: []
---
In the audit usage summary, totalRequests counts all of the user's API access logs across all time, whereas getsToday and writesToday count only logs whose Timestamp falls on or after the current UTC calendar date (DateTime.UtcNow.Date); getsToday counts requests with HttpMethod equal to "GET" and writesToday counts requests with any other HttpMethod, and activeApiKeys counts the user's keys whose IsActive is true.
