#!/usr/bin/env node
// P6: execute a human-reviewed RECONCILIATION (conflict) ledger — the Appendix B artifact P2 produces.
//
// Columns (Appendix B): ID, Area, Sources, Statement_A, Statement_B, Conflict_Type, Conflict_Detail,
//   Recommendation, Rationale, Confidence, Decision, Edited_Statement, Ratified.
// The human edits Decision (Accept|Reject|Edit|Defer), Edited_Statement, and Ratified (Y|N).
//
// This script automates ONLY the mechanical, unambiguous subset of a conflict ledger:
//   • Edit / Accept with an Edited_Statement  → rewrite that record's body statement.
//   • Ratified = Y                            → set the record's status: ratified.
//   • Reject / Defer                          → skip + log.
// Recommendations that require judgment or code changes (fix-the-code, fix-the-doc, create-a-NEW-record
// for a PRD-stated-but-unbuilt requirement) are NOT auto-applied — they're listed as MANUAL FOLLOW-UP so
// you do them deliberately (see the P6 prompt: a record must never keep describing a bug after it's fixed).
//
// Ledger location (first that exists, or an explicit path arg):
//   1) <arg>                              e.g. `cvs apply-ledger docs/cvs/my-ledger.csv`
//   2) docs/cvs/reconciliation-ledger.csv (the >~25-conflict CSV form)
//   3) docs/cvs/RECONCILIATION-LEDGER.md  (the small markdown-table form scaffolded by `cvs init`)
//
// Re-runnable: editing a statement to the same text or re-ratifying a ratified record is a no-op.
// Exit: 0 applied/clean · 2 bad invocation / ledger not found / header mismatch.

import { readFileSync, writeFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, relative, sep, isAbsolute } from 'node:path';

const ROOT = process.cwd(); // kit CLI: root at the consumer repo's working dir
const REQDIR = join(ROOT, 'docs', 'requirements');

// ── locate the ledger ─────────────────────────────────────────────────────────
const argPath = process.argv.slice(2).find((a) => !a.startsWith('-'));
const CANDIDATES = [
  argPath && (isAbsolute(argPath) ? argPath : join(ROOT, argPath)),
  join(ROOT, 'docs', 'cvs', 'reconciliation-ledger.csv'),
  join(ROOT, 'docs', 'cvs', 'RECONCILIATION-LEDGER.md'),
].filter(Boolean);
const LEDGER = CANDIDATES.find((p) => existsSync(p));
if (!LEDGER) {
  console.error(`No reconciliation ledger found. Looked for:\n  ${CANDIDATES.map((p) => relative(ROOT, p)).join('\n  ')}`);
  console.error('Pass a path explicitly: cvs apply-ledger <path-to-ledger.csv|.md>');
  process.exit(2);
}

// ── parse rows (CSV or markdown table) into header + row objects ───────────────
function parseCsv(text) {
  const rows = [];
  let row = [], field = '', inQ = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQ) {
      if (c === '"' && text[i + 1] === '"') { field += '"'; i++; }
      else if (c === '"') inQ = false;
      else field += c;
    } else if (c === '"') inQ = true;
    else if (c === ',') { row.push(field); field = ''; }
    else if (c === '\n' || c === '\r') {
      if (c === '\r' && text[i + 1] === '\n') i++;
      if (field !== '' || row.length) { row.push(field); rows.push(row); row = []; field = ''; }
    } else field += c;
  }
  if (field !== '' || row.length) { row.push(field); rows.push(row); }
  return rows;
}
// Markdown table: lines that start with `|`. Drop the `|---|---|` separator row.
function parseMdTable(text) {
  const rows = [];
  for (const line of text.split('\n')) {
    const t = line.trim();
    if (!t.startsWith('|')) continue;
    const cells = t.replace(/^\|/, '').replace(/\|$/, '').split('|').map((s) => s.trim());
    if (cells.every((c) => /^:?-{2,}:?$/.test(c) || c === '')) continue; // separator
    rows.push(cells);
  }
  return rows;
}

const raw = readFileSync(LEDGER, 'utf8').replace(/\r\n/g, '\n');
const grid = LEDGER.endsWith('.md') ? parseMdTable(raw) : parseCsv(raw);
if (grid.length < 1) { console.error(`Empty ledger: ${relative(ROOT, LEDGER)}`); process.exit(2); }

// ── header guard (R9): error on a column mismatch instead of silently skipping every row ───────────────
const norm = (s) => String(s).trim().toLowerCase().replace(/[\s-]+/g, '_');
const header = grid[0].map(norm);
const col = (...names) => { for (const n of names) { const i = header.indexOf(norm(n)); if (i >= 0) return i; } return -1; };
const cID = col('ID'), cDecision = col('Decision'), cEdited = col('Edited_Statement'), cRatified = col('Ratified');
const REQUIRED = { ID: cID, Decision: cDecision, Edited_Statement: cEdited, Ratified: cRatified };
const missing = Object.entries(REQUIRED).filter(([, i]) => i < 0).map(([n]) => n);
if (missing.length) {
  console.error(`Ledger header mismatch in ${relative(ROOT, LEDGER)}.`);
  console.error(`  Missing required column(s): ${missing.join(', ')}`);
  console.error(`  Found: ${grid[0].join(', ')}`);
  console.error('  Expected an Appendix B reconciliation ledger: ID, Area, Sources, Statement_A, Statement_B,');
  console.error('  Conflict_Type, Conflict_Detail, Recommendation, Rationale, Confidence, Decision, Edited_Statement, Ratified.');
  process.exit(2);
}

// ── map record id -> file path (records may live anywhere under docs/requirements/<AREA>/) ─────────────
function recordPaths() {
  const map = new Map();
  (function walk(d) {
    for (const n of (existsSync(d) ? readdirSync(d) : [])) {
      const f = join(d, n);
      let s; try { s = statSync(f); } catch { continue; }
      if (s.isDirectory()) walk(f);
      else if (n.endsWith('.md')) map.set(n.replace(/\.md$/, ''), f);
    }
  })(REQDIR);
  return map;
}
const recById = recordPaths();

// Frontmatter-aware editors. Records keep the statement in the BODY (kit convention), so an Edit rewrites
// the first non-empty body line; a ratification flips `status:` in the frontmatter.
function splitDoc(text) {
  const m = text.match(/^---\n([\s\S]*?)\n---\n?([\s\S]*)$/);
  if (!m) return null;
  return { fm: m[1].split('\n'), body: m[2] };
}
function setStatusRatified(fmLines) {
  let found = false;
  const out = fmLines.map((l) => {
    if (/^status\s*:/.test(l)) { found = true; return 'status: ratified'; }
    return l;
  });
  if (!found) out.push('status: ratified');
  return out;
}
function replaceStatement(body, statement) {
  // Replace the ENTIRE first paragraph (the statement), not just its first line — a statement that wraps
  // across several lines (no blank line) would otherwise leave its trailing lines behind and corrupt the
  // record. Find the first non-empty line, then the first empty line after it, and splice that span out.
  const lines = body.split('\n');
  const start = lines.findIndex((l) => l.trim() !== '');
  if (start === -1) return statement + '\n';
  let end = lines.slice(start).findIndex((l) => l.trim() === '');
  end = end === -1 ? lines.length : end + start;
  lines.splice(start, end - start, statement);
  return lines.join('\n');
}

// ── execute ─────────────────────────────────────────────────────────────────────
const edited = [], ratified = [], skipped = [], manual = [], notFound = [];
for (const r of grid.slice(1)) {
  const id = (r[cID] || '').trim();
  if (!id) continue;
  const decision = (r[cDecision] || '').trim().toUpperCase();
  const editedStmt = (r[cEdited] || '').trim();
  const doRatify = /^(y|yes)$/i.test((r[cRatified] || '').trim());

  if (decision === 'REJECT' || decision === 'DEFER') { skipped.push(`${id} (${decision})`); continue; }

  const path = recById.get(id);
  if (!path) {
    // No on-disk record for this id: the recommendation is almost certainly create-new / fix-code /
    // fix-doc — not a mechanical edit. Surface it as manual follow-up rather than fabricating a record.
    notFound.push(`${id} (${decision || 'no decision'}${editedStmt ? ', has Edited_Statement' : ''})`);
    continue;
  }

  let text = readFileSync(path, 'utf8').replace(/\r\n/g, '\n');
  const doc = splitDoc(text);
  if (!doc) { manual.push(`${id} — record has no parseable frontmatter; edit by hand`); continue; }

  let changed = false;
  if ((decision === 'EDIT' || decision === 'ACCEPT') && editedStmt) {
    doc.body = replaceStatement(doc.body, editedStmt);
    edited.push(id);
    changed = true;
  } else if (decision === 'EDIT' && !editedStmt) {
    // EDIT means "rewrite the statement" but no Edited_Statement was supplied — don't silently no-op;
    // surface it so the reviewer fills it in.
    manual.push(`${id} — EDIT decision but Edited_Statement is empty; supply the rewritten statement.`);
  } else if (decision === 'ACCEPT' && !editedStmt && !doRatify) {
    // Accept with no Edited_Statement and no ratify flag: the accepted recommendation needs human/code
    // action (fix code, fix the doc, add a test). Not mechanical — leave it for the P6 manual pass.
    manual.push(`${id} — ACCEPT with no Edited_Statement: apply the recommendation by hand (fix code/doc, bind a test).`);
  }
  if (doRatify) {
    doc.fm = setStatusRatified(doc.fm);
    ratified.push(id);
    changed = true;
  }
  if (changed) writeFileSync(path, `---\n${doc.fm.join('\n')}\n---\n${doc.body.startsWith('\n') ? '' : '\n'}${doc.body}`);
}

// ── report ────────────────────────────────────────────────────────────────────
console.log(`apply-ledger: ${relative(ROOT, LEDGER)}`);
console.log(`  Edited statement on ${edited.length} record(s)${edited.length ? ': ' + edited.join(', ') : ''}`);
console.log(`  Ratified ${ratified.length} record(s)${ratified.length ? ': ' + ratified.join(', ') : ''}`);
console.log(`  Skipped ${skipped.length} (Reject/Defer)${skipped.length ? ': ' + skipped.join(', ') : ''}`);
if (notFound.length) {
  console.log(`\n  MANUAL FOLLOW-UP — ${notFound.length} row(s) reference no existing record (create-new / fix-code / fix-doc):`);
  for (const n of notFound) console.log(`    - ${n}`);
}
if (manual.length) {
  console.log(`\n  MANUAL FOLLOW-UP — ${manual.length} accepted row(s) need a non-mechanical change:`);
  for (const m of manual) console.log(`    - ${m}`);
}
console.log('\nRe-run `cvs index` to regenerate the traceability index and verify the gate.');
