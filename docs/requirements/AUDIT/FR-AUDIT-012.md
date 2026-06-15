---
id: FR-AUDIT-012
type: FR
area: AUDIT
provenance: doc
status: unratified
why: A per-key bar chart over 30 days lets an owner visually spot which LLM client drives traffic and detect unexpected spikes.
verification: automated
tests: []
---
The LLM Audit Dashboard (API Access tab) presents a per-API-key bar chart showing request count per API key over the last 30 days.

Status: aspirational — not yet implemented. REQUIREMENTS.md §4.5 (line 248) specifies this chart, but src/Pages/Audit/Index.cshtml renders no chart (no ApexCharts canvas/series, summary cards + table only).
