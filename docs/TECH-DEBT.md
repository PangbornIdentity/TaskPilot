# Tech Debt

Tracked engineering follow-ups for TaskPilot.

---

## TD-001 — Reintroduce a small self-owned requirements-traceability gate

**Context.** Requirements are documented as structured records under
`docs/requirements/<AREA>/<ID>.md` (one atomic, falsifiable statement each, with
`id / type / area / provenance / status / verification / tests / why` frontmatter), plus the
generated snapshots `docs/requirements-index.json` and `docs/TRACEABILITY.md`.

A traceability **CI gate** was prototyped — it bound every requirement to a test and blocked
untagged UI/integration tests, orphan IDs, broken bindings, and uncovered requirements — but the
engine was external/vendored and not suitable to ship in this public repo, so it was removed. The
records + index/matrix were kept as documentation.

**Ask.** Build a **small, self-owned** replacement: a lightweight script (own OSS license, zero
proprietary deps) that
- parses the records under `docs/requirements/` and their `tests:` bindings,
- regenerates `requirements-index.json` + `TRACEABILITY.md`, and
- fails CI on a requirement with no resolving test, a `tests:` path that resolves to nothing, or an
  orphan requirement ID cited by a test.

Keep it minimal and tailored to this repo (skip baselines/governance machinery unless needed). Wire it
as a GitHub Actions check once it's clean-room and license-clear.

**Done when.** A self-contained, explicitly-OSS-licensed script lives in this repo and a CI workflow
runs it on PRs, blocking on the invariants above.
