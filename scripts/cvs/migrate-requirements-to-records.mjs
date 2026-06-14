#!/usr/bin/env node
// One-time (re-runnable) migration of a legacy requirements doc -> federated frontmatter records at
// docs/requirements/<AREA>/<ID>.md (statement prose in the BODY so YAML never escapes backticks/#/colons).
//
//   cvs migrate                      table mode: parse docs/REQUIREMENTS.md markdown table rows
//                                    (| ID | text |); reverse-maps tests: from IDs already tagged in tests;
//                                    replaces REQUIREMENTS.md with a stub pointer.
//   cvs migrate --prose [path]       prose mode (R2): parse a rich PRD with embedded **ID**: statement
//                                    patterns (default docs/REQUIREMENTS.md), remap non-CVS IDs (e.g.
//                                    REQ-AUTH-1) to FR/BIZ/NFR-<AREA>-NNN, write an old->new crosswalk, and
//                                    LEAVE the PRD in place (it stays a high-authority P1/P2 doc).
//
// Re-runnable: never overwrites an existing record (logs [skip]).

import { readFileSync, writeFileSync, mkdirSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const ROOT = process.cwd(); // kit CLI: root at the consumer repo's working dir
const OUTDIR = join(ROOT, 'docs', 'requirements');
const args = process.argv.slice(2);
const PROSE = args.includes('--prose');
const argPath = args.find((a) => !a.startsWith('-'));

const ID_ROW = /^\|\s*((?:FR|BIZ|NFR)-[A-Z]+-\d+)\s*\|(.*)$/;
const ID_G = /\b(?:FR|BIZ|NFR)-[A-Z]+-\d+\b/g;
const RANGE_RE = /\b(?:FR|BIZ|NFR)-[A-Z]+-\d+\s+through\s+(?:FR|BIZ|NFR)-[A-Z]+-\d+\b/g;
const MANUAL = 'Verification: Manual';

const familyOf = (id) => id.split('-')[0];
const areaOf = (id) => (id.match(/^[A-Z]+-([A-Z]+)-\d+$/) || [, 'MISC'])[1];

function listFiles(dir, exts) {
  const out = [];
  let entries; try { entries = readdirSync(dir); } catch { return out; }
  for (const n of entries) {
    if (['node_modules', 'bin', 'obj', '.git'].includes(n)) continue;
    const f = join(dir, n);
    let s; try { s = statSync(f); } catch { continue; }
    if (s.isDirectory()) out.push(...listFiles(f, exts));
    else if (exts.some((x) => n.endsWith(x))) out.push(f);
  }
  return out;
}

// Existing record IDs, so a re-run (or a mixed table+prose repo) continues numbering instead of colliding.
function existingIds() {
  const ids = new Set();
  if (!existsSync(OUTDIR)) return ids; // first run: docs/requirements/ may not exist yet
  for (const f of listFiles(OUTDIR, ['.md'])) ids.add(f.split(sep).pop().replace(/\.md$/, ''));
  return ids;
}
function makeNextId(existing) {
  return (type, area) => {
    const prefix = `${type}-${area}`;
    let max = 0;
    for (const id of existing) { const m = id.match(new RegExp(`^${prefix}-(\\d+)$`)); if (m) max = Math.max(max, +m[1]); }
    const id = `${prefix}-${String(max + 1).padStart(3, '0')}`;
    existing.add(id);
    return id;
  };
}

function writeRecord({ id, type, area, provenance, manual, tests, statement, sourceNote }) {
  const dir = join(OUTDIR, area);
  if (existsSync(join(dir, `${id}.md`))) return false;
  mkdirSync(dir, { recursive: true });
  const body = [statement, sourceNote ? `\n\n${sourceNote}` : '', manual ? `\n\nVerification: Manual — confirm the procedure.` : ''].join('');
  writeFileSync(join(dir, `${id}.md`),
    ['---', `id: ${id}`, `type: ${type}`, `area: ${area}`,
      `provenance: ${provenance}`, `status: unratified`,
      `verification: ${manual ? 'manual' : 'automated'}`,
      `tests: [${(tests || []).map((t) => `"${t}"`).join(', ')}]`, '---', '', body.trim(), ''].join('\n'));
  return true;
}

// ── prose mode (R2) ──────────────────────────────────────────────────────────
function classify(stmt) {
  const s = stmt.toLowerCase();
  if (/\b(latency|throughput|availab|uptime|scal|performance|response time|encrypt|audit|rate limit|retention|gdpr|pci|secur|concurren|p9[59]|sla|backup|logging)\b/.test(s)) return 'NFR';
  if (/\b(calculat|formula|commission|fee\b|rate\b|percent|tax\b|discount|round|prorat|must equal|threshold|markup|margin)\b/.test(s)) return 'BIZ';
  return 'FR';
}
function proseMode() {
  const SRC = argPath ? join(ROOT, argPath) : join(ROOT, 'docs', 'REQUIREMENTS.md');
  if (!existsSync(SRC)) { console.error(`Prose source not found: ${relative(ROOT, SRC)}. Pass a path: cvs migrate --prose <file>`); process.exit(2); }
  const text = readFileSync(SRC, 'utf8').replace(/\r\n/g, '\n');

  // Find every **ID** marker; the statement is the text from the marker to the next blank line.
  const MARK = /\*\*\s*([A-Za-z][A-Za-z0-9]*(?:-[A-Za-z]+)*-\d+)\s*\*\*\s*[:：.\-—]?\s*/g;
  const hits = [];
  let m;
  while ((m = MARK.exec(text))) hits.push({ oldId: m[1], start: m.index, textStart: MARK.lastIndex });
  if (!hits.length) {
    console.error(`No **ID**: statement patterns found in ${relative(ROOT, SRC)}.`);
    console.error('Prose mode expects bold IDs like `**REQ-AUTH-1**: <statement>`. For a markdown TABLE, run `cvs migrate` (no --prose).');
    process.exit(1);
  }
  const existing = existingIds();
  const nextId = makeNextId(existing);
  const crosswalk = [], created = [], skipped = [];
  for (let i = 0; i < hits.length; i++) {
    const h = hits[i];
    const end = i + 1 < hits.length ? hits[i + 1].start : text.length;
    const chunk = text.slice(h.textStart, end);
    const statement = chunk.split(/\n\s*\n/)[0].replace(/\s+/g, ' ').trim(); // first paragraph only
    if (!statement) continue;
    const om = h.oldId.match(/^[A-Za-z]+-([A-Za-z]+)-\d+$/);
    const area = (om ? om[1] : 'MISC').toUpperCase();
    const type = classify(statement);
    const manual = statement.includes(MANUAL);
    const id = nextId(type, area);
    const ok = writeRecord({
      id, type, area, provenance: 'doc', manual, tests: [], statement,
      sourceNote: `Source: ${h.oldId} in ${relative(ROOT, SRC).split(sep).join('/')} (migrated — verify intent before ratifying).`,
    });
    (ok ? created : skipped).push(id);
    crosswalk.push({ oldId: h.oldId, id, type, area, statement });
  }

  // Crosswalk so traceability to the historical doc isn't lost.
  const cwPath = join(ROOT, 'docs', 'cvs', 'ID-CROSSWALK.md');
  mkdirSync(join(ROOT, 'docs', 'cvs'), { recursive: true });
  const cw = ['# ID Crosswalk — legacy PRD IDs → CVS records', '',
    `Generated by \`cvs migrate --prose\` from \`${relative(ROOT, SRC).split(sep).join('/')}\`. The classification`,
    '(FR/BIZ/NFR) is heuristic — review it (and the statements) in P2/P3 before ratifying.', '',
    '| Legacy ID | CVS ID | Type | Area | Statement (first line) |',
    '|-----------|--------|------|------|------------------------|',
    ...crosswalk.map((c) => `| ${c.oldId} | ${c.id} | ${c.type} | ${c.area} | ${c.statement.slice(0, 80).replace(/\|/g, '\\|')} |`),
    ''].join('\n');
  writeFileSync(cwPath, cw);

  console.log(`Prose-migrated ${created.length} record(s) into docs/requirements/ (${skipped.length} skipped — already exist).`);
  console.log(`Wrote crosswalk: ${relative(ROOT, cwPath).split(sep).join('/')} (${crosswalk.length} mappings).`);
  console.log(`Left ${relative(ROOT, SRC).split(sep).join('/')} in place — fold it into P1/P2 as a high-authority doc. Review heuristic FR/BIZ/NFR types.`);
}

// ── table mode (default) ───────────────────────────────────────────────────────
function tableMode() {
  // Table mode ALWAYS operates on the canonical docs/REQUIREMENTS.md and replaces it with a stub (below).
  // A positional path is only meaningful for `--prose`; honoring it here would let `cvs migrate some.md`
  // silently stub-overwrite an arbitrary file. Ignore it (and warn) rather than risk that data loss.
  if (argPath) console.error(`Note: positional path "${argPath}" is ignored in table mode (it is only used by --prose). Operating on docs/REQUIREMENTS.md.`);
  const SRC = join(ROOT, 'docs', 'REQUIREMENTS.md');
  if (!existsSync(SRC)) { console.error(`Table source not found: ${relative(ROOT, SRC)}.`); process.exit(2); }

  // Reverse map: requirement ID -> test files that reference it (the binding).
  const testFiles = [
    ...listFiles(join(ROOT, 'tests'), ['.cs']),
    ...listFiles(join(ROOT, 'PlaywrightTests', 'tests'), ['.ts']),
  ];
  const idToTests = new Map();
  for (const f of testFiles) {
    const rawf = readFileSync(f, 'utf8').replace(/\r\n/g, '\n').replace(RANGE_RE, ' ');
    const rel = relative(ROOT, f).split(sep).join('/');
    for (const id of rawf.match(ID_G) || []) {
      if (!idToTests.has(id)) idToTests.set(id, new Set());
      idToTests.get(id).add(rel);
    }
  }

  const text = readFileSync(SRC, 'utf8').replace(/\r\n/g, '\n');
  const records = [];
  for (const line of text.split('\n')) {
    const m = line.match(ID_ROW);
    if (!m) continue;
    const id = m[1];
    let rest = m[2];
    if (rest.endsWith('|')) rest = rest.slice(0, -1);
    const cells = rest.split(/(?<!\\)\|/).map((s) => s.replace(/\\\|/g, '|').trim()).filter((s) => s.length);
    records.push({ id, type: familyOf(id), area: areaOf(id), statement: cells.join(' — ') });
  }
  if (records.length === 0) {
    console.error(`No requirement rows in ${relative(ROOT, SRC)}. For a prose PRD with **ID**: patterns, run \`cvs migrate --prose\`.`);
    process.exit(1);
  }

  let written = 0;
  for (const r of records) {
    const manual = r.statement.includes(MANUAL);
    const ok = writeRecord({
      id: r.id, type: r.type, area: r.area, provenance: 'md', manual,
      tests: [...(idToTests.get(r.id) || [])].sort(), statement: r.statement,
    });
    if (ok) written++;
  }

  writeFileSync(SRC,
    `# Requirements — federated\n\n` +
    `This file is superseded. Requirements now live as one record per requirement under\n` +
    `\`docs/requirements/<AREA>/<ID>.md\` (the source of truth). See the generated index\n` +
    `[TRACEABILITY.md](TRACEABILITY.md) and \`docs/requirements-index.json\`.\n\n` +
    `Regenerate with: \`node scripts/cvs/reqindex.mjs\`.\n`);

  console.log(`Migrated ${written} records into docs/requirements/. Replaced ${relative(ROOT, SRC)} with a stub.`);
}

PROSE ? proseMode() : tableMode();
