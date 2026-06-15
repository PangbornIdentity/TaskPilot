#!/usr/bin/env node
// ratify — batch-flip requirement records from `status: unratified` to `status: ratified`.
//
// Ratification is a HUMAN judgment (per REQUIREMENTS_CONSTITUTION.md: area CODEOWNERS confirm intent).
// This tool only removes the typing; it never decides. Read the checklist, then flip a batch.
//
//   node scripts/ratify.mjs --list-unratified [--area AUTH]      # review checklist (no writes)
//   node scripts/ratify.mjs --area AUTH                          # ratify all unratified AUTH records
//   node scripts/ratify.mjs --ids FR-AUTH-003,FR-AUTH-005        # ratify a specific list
//   node scripts/ratify.mjs --provenance md                     # ratify all migrated (md) records
//   node scripts/ratify.mjs --area AUTH --dry-run               # preview only
//   node scripts/ratify.mjs --all                                # ratify everything (explicit; use with care)
//
// A selector (--area | --ids | --provenance | --all) is REQUIRED for any write. Idempotent: records
// already ratified are skipped. Only the frontmatter `status:` line is changed; nothing else is touched.
//
// Exit: 0 ok · 2 bad invocation.

import { readFileSync, writeFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = process.cwd(); // kit CLI: root at the consumer repo's working dir

// ── args ──────────────────────────────────────────────────────────────────────
const args = process.argv.slice(2);
function flag(name) { return args.includes(`--${name}`); }
function opt(name) { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : undefined; }
const LIST = flag('list-unratified');
const DRY = flag('dry-run');
const area = opt('area');
const ids = opt('ids') ? new Set(opt('ids').split(',').map((s) => s.trim()).filter(Boolean)) : null;
const provenance = opt('provenance');
const all = flag('all');

// ── config → requirement source globs ────────────────────────────────────────
function loadSources() {
  const p = join(ROOT, '.traceability.config.json');
  if (existsSync(p)) {
    try { return JSON.parse(readFileSync(p, 'utf8')).requirementSources || ['docs/requirements/**/*.md']; }
    catch { /* fall through */ }
  }
  return ['docs/requirements/**/*.md'];
}
function globToRe(g) {
  let re = '';
  for (let i = 0; i < g.length; i++) {
    const c = g[i];
    if (c === '*') { if (g[i + 1] === '*') { re += '.*'; i++; if (g[i + 1] === '/') i++; } else re += '[^/]*'; }
    else if ('.+^${}()|[]\\'.includes(c)) re += '\\' + c;
    else if (c === '?') re += '[^/]'; else re += c;
  }
  return new RegExp('^' + re + '$');
}
function listAll(dir) {
  const out = [];
  for (const n of (existsSync(dir) ? readdirSync(dir) : [])) {
    if (['node_modules', 'bin', 'obj', '.git'].includes(n)) continue;
    const f = join(dir, n);
    if (statSync(f).isDirectory()) out.push(...listAll(f));
    else out.push(relative(ROOT, f).split(sep).join('/'));
  }
  return out;
}
const allFiles = listAll(ROOT);
const sources = loadSources();
const recordFiles = [...new Set(sources.flatMap((g) => { const re = globToRe(g); return allFiles.filter((f) => re.test(f)); }))];

// ── parse the bits we need from a record ──────────────────────────────────────
function readRecord(rel) {
  const text = readFileSync(join(ROOT, rel), 'utf8');
  if (!text.startsWith('---')) return null;
  const end = text.indexOf('\n---', 3);
  if (end === -1) return null;
  const fm = text.slice(3, end);
  const get = (k) => (fm.match(new RegExp(`^${k}:\\s*(.*)$`, 'm')) || [, ''])[1].trim();
  const body = (text.slice(text.indexOf('\n', end + 1) + 1) || '').trim();
  return { rel, text, id: get('id'), area: get('area'), provenance: get('provenance'),
    status: get('status') || 'unratified', first: body.split('\n').find((l) => l.trim()) || '' };
}
let records = recordFiles.map(readRecord).filter(Boolean);

// ── select ────────────────────────────────────────────────────────────────────
function selected(r) {
  if (ids) return ids.has(r.id);
  if (area) return r.area === area;
  if (provenance) return r.provenance === provenance;
  if (all) return true;
  return false;
}

// ── list mode (no writes) ─────────────────────────────────────────────────────
if (LIST) {
  const rows = records.filter((r) => r.status !== 'ratified' && (area ? r.area === area : true))
    .sort((a, b) => a.id.localeCompare(b.id));
  console.log(`${rows.length} unratified record(s)${area ? ` in ${area}` : ''}:\n`);
  for (const r of rows) console.log(`  [${r.provenance.padEnd(5)}] ${r.id.padEnd(20)} ${r.first.slice(0, 80)}`);
  console.log(`\nReview, then ratify a batch, e.g.:  node scripts/ratify.mjs --area <AREA>`);
  process.exit(0);
}

// ── write mode ────────────────────────────────────────────────────────────────
if (!ids && !area && !provenance && !all) {
  console.error('Refusing to ratify without a selector. Use --area | --ids | --provenance | --all,');
  console.error('or --list-unratified to review first.');
  process.exit(2);
}

const targets = records.filter(selected);
const toFlip = targets.filter((r) => r.status !== 'ratified');
for (const r of toFlip) {
  if (!DRY) {
    // Replace only the first frontmatter `status:` line.
    const end = r.text.indexOf('\n---', 3);
    const head = r.text.slice(0, end).replace(/^status:.*$/m, 'status: ratified');
    writeFileSync(join(ROOT, r.rel), head + r.text.slice(end));
  }
}
console.log(`${DRY ? '[dry-run] would ratify' : 'Ratified'} ${toFlip.length} record(s)` +
  `${targets.length - toFlip.length ? ` (${targets.length - toFlip.length} already ratified)` : ''}.`);
if (toFlip.length) console.log('  ' + toFlip.map((r) => r.id).sort().join(', '));
console.log(`\nRegenerate the index:  node scripts/reqindex.mjs`);
