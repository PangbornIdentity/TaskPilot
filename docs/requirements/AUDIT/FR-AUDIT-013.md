---
id: FR-AUDIT-013
type: FR
area: AUDIT
provenance: doc
status: ratified
why: Click-to-filter and an API-key dropdown plus status-code range let an owner drill straight into one key's activity or isolate error responses without hand-editing filters.
verification: automated
tests: []
---
On the LLM Audit Dashboard (API Access tab), clicking an API key name filters the audit-log table to that key's activity, and the available filters include an API-key dropdown and a status-code range in addition to date range and HTTP method.

Status: aspirational — not yet implemented. REQUIREMENTS.md §4.5 (lines 246, 249) specifies the API-key dropdown, status-code-range filter, and click-to-filter on key name, but src/Pages/Audit/Index.cshtml renders the key name as plain text (not a link) and exposes only an HTTP-method text input and a date range (no API-key dropdown, no status-code-range filter).
