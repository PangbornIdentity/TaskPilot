# Requirements Constitution

This repository is maintained toward a strict Computable Verifiable State (CVS). During bootstrap,
`docs/traceability-baseline.json` (accepted gaps) and `docs/untagged-test-baseline.json` may exist
as shrink-only transition scaffolding; strict CVS requires both empty.

Invariants enforced by CI (`cvs index --check`) on every PR and the default branch:
1. Every requirement record has a stable ID and is bound to a test (via `tests:`) OR marked `Verification: Manual`.
2. Every requirement is covered per `.traceability.config.json` policy (its `tests:` binding resolves to a
   real test of the required kind), unless baselined. A non-empty binding that resolves to nothing fails.
3. Every UI/integration test cites its requirement ID(s) (untagged ones are listed in the untagged
   baseline, shrink-only). Unit tests are exempt — coverage comes from the record's `tests:` binding.
4. No orphans (a test cites a non-existent ID).
5. Both baselines may only SHRINK; strict CVS requires them empty. Growing either needs a
   `constitution-change` PR.
6. IDs use the org-standard FR / BIZ / NFR taxonomy; globally unique; area prefix matches the path.
7. The README carries a valid `cvs-repo` block (slug/name/description + areas glossary) — the CVS hub's
   single source of repo identity. A missing README, missing block, missing required field, or invalid
   slug is a `REPO-META` hard-fail (`cvs init` scaffolds the block; replace its placeholders).

Advisory — NOT gated, by design: a record may carry an optional `source:` field listing the source
files/globs it implements. `cvs index --affected` maps changed files -> requirements (via `source:`) ->
their bound `tests:` and reports blind spots (changed source under no requirement) and reverse-drift
(a requirement whose source changed but whose bound tests did not). These inform a local / pre-push gate
but DO NOT block merge, and `source:` is fully backward-compatible (records without it are unaffected; an
unresolved glob is an advisory only). Promoting `--affected` or reverse-drift to a hard CI gate is itself
a `constitution-change` PR.

Governance: area CODEOWNERS ratify (`unratified -> ratified`); amendments need a `constitution-change`
PR approved by <NAME/ROLE>. Ratification is tracked but NOT gated. The requirement records are the
source of truth; tests verify them — when a test and a ratified requirement disagree, the requirement
wins and the test/code is fixed.
