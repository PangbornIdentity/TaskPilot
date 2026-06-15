---
id: NFR-SEC-008
type: NFR
area: SEC
provenance: doc
status: unratified
verification: automated
why: SUGGESTED: Hardening headers mitigate MIME-sniffing, clickjacking, and referrer-leak classes of browser attacks.
tests: []
---
Every HTTP response carries hardening headers `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and `Referrer-Policy: strict-origin-when-cross-origin` (per ARCHITECTURE.md §4.5).

Status: aspirational — not yet implemented. No security-headers middleware is registered in `src/Program.cs` or `src/Extensions/ApplicationBuilderExtensions.cs`; the documented `Strict-Transport-Security` and `Content-Security-Policy` headers are explicitly deferred to iteration 2.
