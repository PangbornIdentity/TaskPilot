#!/usr/bin/env node
// reqindex — requirements-as-data indexer + traceability CI gate.
//
// Realizes the substrate decision in docs/COMPUTABLE_REQUIREMENTS_PLAYBOOK.md (§1.1):
// requirements live as a federated, granular dataset (frontmatter records, with a legacy
// markdown-table form supported); a deterministic script maintains the index and gates CI.
//
// Zero runtime dependencies (Node 20+). Reads .traceability.config.json.
//
//   node scripts/reqindex.mjs           # regenerate index, exit non-zero on NEW violations
//   node scripts/reqindex.mjs --check   # gate only (still writes index), exit non-zero on NEW violations
//   node scripts/reqindex.mjs --json    # print machine summary to stdout
//   node scripts/reqindex.mjs --affected [--base <ref>|--since <ref>|--files <list>] [--json|--list-tests] [--run]
//                                        # change-aware: map changed files → requirements (source:) → bound tests.
//                                        # ADVISORY (exit 0); only --run propagates the test runner's exit code.
//
// Coverage (per .traceability.config.json): a requirement is covered when its record `tests:` binding
// resolves to a real test file of the required kind (the records are the source of truth):
//   FR-*  → playwright | integration | unit     BIZ-* → unit     NFR-* → any test OR `Verification: Manual`
// A non-empty `tests:` entry that resolves to no configured test source is a BROKEN-BINDING violation.
// Test→requirement: UI/integration test sources (requireTagged:true) must tag every test with an ID
//   (untagged ones are listed in the untagged baseline). Unit sources are exempt from global tagging.
//   The ID-in-test scan is used for orphan detection and the untagged invariant, not for coverage.
//
// Ratchet: docs/traceability-baseline.json (accepted gaps) and the untagged-test baseline are
// shrink-only transition scaffolding. NEW gaps/untagged tests (not in a baseline) and orphans fail.
//
// Exit: 0 clean · 1 violations · 2 bad invocation/IO.

import { readFileSync, writeFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process'; // built-in: git + the --affected test runner (no new deps)

const ROOT = process.cwd(); // kit CLI: root at the consumer repo's working dir
const ARGV = process.argv.slice(2);
const argv = new Set(ARGV);
const CHECK = argv.has('--check');
const JSON_OUT = argv.has('--json');
const AFFECTED = argv.has('--affected');
// Value of a `--flag <value>` option from the raw argv (first occurrence); undefined if the flag is absent.
function argValue(name) { const i = ARGV.indexOf(name); return i >= 0 ? ARGV[i + 1] : undefined; }

// ── Config ──────────────────────────────────────────────────────────────────
const DEFAULT_CONFIG = {
  requirementSources: ['**/requirements/*.md', 'docs/REQUIREMENTS.md'],
  idPattern: '(FR|BIZ|NFR)-[A-Z]+-\\d+',
  enforceGlobalIdUniqueness: true,
  enforceAreaPrefixMatchesPath: true,
  testSources: [
    { glob: 'tests/**/*.cs', kind: 'unit', requireTagged: false },
    { glob: 'PlaywrightTests/tests/**/*.ts', kind: 'playwright', requireTagged: true },
  ],
  policy: {
    FR: { requires: 'playwright|integration|unit' },
    BIZ: { requires: 'unit' },
    NFR: { requires: 'any|manual' },
  },
  manualMarker: 'Verification: Manual',
  // A requirement whose only proof is a test that standard PR CI can't execute (needs sandbox creds,
  // external infra, etc.) is still "covered" for traceability, but its proof isn't *continuously*
  // verified. Mark such a record's body with this string to surface it as infra-debt (advisory, not a
  // gate failure) so a "fully covered" index doesn't overstate continuous verification.
  infraMarker: 'Verification: Infra',
  ignoreRangeHeaders: true,
  baseline: 'docs/traceability-baseline.json',
  untaggedTestBaseline: 'docs/untagged-test-baseline.json',
  index: { json: 'docs/requirements-index.json', traceability: 'docs/TRACEABILITY.md' },
  // Repo README carrying the required `cvs-repo` metadata block (see parseCvsRepoBlock).
  // The CVS hub (Alexander) reads this block on every sync and hard-fails without it,
  // so the gate validates it here too — a repo can't reach baseline missing the block.
  repoReadme: 'README.md',
};

function loadConfig() {
  const p = join(ROOT, '.traceability.config.json');
  if (!existsSync(p)) return DEFAULT_CONFIG;
  try {
    return { ...DEFAULT_CONFIG, ...JSON.parse(readFileSync(p, 'utf8')) };
  } catch (e) {
    console.error(`Bad .traceability.config.json: ${e.message}`);
    process.exit(2);
  }
}
const CFG = loadConfig();
const ID_RE = new RegExp(`\\b${CFG.idPattern}\\b`, 'g');
const RANGE_RE = new RegExp(
  `\\b${CFG.idPattern}\\s+through\\s+${CFG.idPattern}\\b`, 'g');
const SLUG_RE = /^[a-z0-9][a-z0-9-]*$/;       // repo slug shape
const AREA_KEY_RE = /^[A-Z][A-Z0-9]*$/;       // cvs-repo areas glossary key shape

// ── Tiny glob (supports ** and * ; no braces) ─────────────────────────────────
function globToRe(glob) {
  let re = '';
  for (let i = 0; i < glob.length; i++) {
    const c = glob[i];
    if (c === '*') {
      if (glob[i + 1] === '*') { re += '.*'; i++; if (glob[i + 1] === '/') i++; }
      else re += '[^/]*';
    } else if ('.+^${}()|[]\\'.includes(c)) re += '\\' + c;
    else if (c === '?') re += '[^/]';
    else re += c;
  }
  return new RegExp('^' + re + '$');
}
function listFiles(dir) {
  const out = [];
  let entries;
  try { entries = readdirSync(dir); } catch { return out; }
  for (const name of entries) {
    if (name === 'node_modules' || name === 'bin' || name === 'obj' || name === '.git') continue;
    const full = join(dir, name);
    let st; try { st = statSync(full); } catch { continue; }
    if (st.isDirectory()) out.push(...listFiles(full));
    else out.push(full);
  }
  return out;
}
let _allFiles = null;
function glob(pattern) {
  if (!_allFiles) _allFiles = listFiles(ROOT).map((f) => relative(ROOT, f).split(sep).join('/'));
  const re = globToRe(pattern);
  return _allFiles.filter((f) => re.test(f));
}
// CRLF tolerance: on a Windows / `core.autocrlf=true` checkout the working tree carries `\r\n`. The
// frontmatter key regex and the `\n`-joined index would otherwise both break (every key fails to parse →
// "No requirements found"; committed-CRLF vs generated-LF → false STALE-INDEX). Normalize on every read.
function readText(abs) { return readFileSync(abs, 'utf8').replace(/\r\n/g, '\n'); }

// ── Minimal frontmatter YAML (handles the record schema only) ─────────────────
// Returns { data, body }. Frontmatter carries only safe short scalars (id/type/area/…); the messy
// statement prose lives in the BODY, so the parser never has to YAML-escape backticks/quotes/#/colons.
function parseFrontmatter(text) {
  if (!text.startsWith('---')) return null;
  const end = text.indexOf('\n---', 3);
  if (end === -1) return null;
  const block = text.slice(3, end).trim();
  const afterFence = text.indexOf('\n', end + 1);
  const body = afterFence === -1 ? '' : text.slice(afterFence + 1).trim();
  const data = {};
  for (const raw of block.split('\n')) {
    const m = raw.match(/^([A-Za-z_][\w-]*):\s*(.*)$/);
    if (!m) continue;
    const key = m[1];
    const rest = m[2];
    // Quoted scalars are taken verbatim, so prose (e.g. the `why` rationale) can safely contain
    // #, :, or embedded quotes without being split or truncated. A trailing ` #…` comment AFTER the
    // closing quote is still honored; the `#` is only a comment outside the quotes. Lazy match so the
    // first closing quote whose tail is blank-or-comment wins (a `#` inside the quotes is preserved).
    const quoted = rest.match(/^(['"])([\s\S]*?)\1(?:\s+#.*)?\s*$/);
    if (quoted) { data[key] = quoted[2]; continue; }
    const val = rest.replace(/\s+#.*$/, '').trim(); // unquoted: a trailing ` #…` is a comment
    if (val === '' || val === 'null' || val === '~') { data[key] = null; continue; }
    if (val.startsWith('[') && val.endsWith(']')) {
      data[key] = val.slice(1, -1).split(',').map((s) => unquote(s.trim())).filter(Boolean);
    } else {
      data[key] = unquote(val);
    }
  }
  return { data, body };
}
function firstLine(body) {
  for (const l of body.split('\n')) { const t = l.trim(); if (t) return t; }
  return '';
}
function unquote(s) {
  if ((s.startsWith('"') && s.endsWith('"')) || (s.startsWith("'") && s.endsWith("'")))
    return s.slice(1, -1);
  return s;
}

// ── README `cvs-repo` block ───────────────────────────────────────────────────
// Repo metadata for the CVS hub (Alexander): `slug`, `name`, `description`, inline-array
// `aliases`/`owners`, and a multi-line `areas:` glossary. The README is the single source
// of truth for repo identity; the hub overwrites it from the README on every sync and
// hard-fails without a valid block, so the gate validates it here to catch the gap in CI.
function parseFlatBlock(block) {
  const data = {};
  for (const raw of block.split('\n')) {
    const line = raw.replace(/\s+#.*$/, ''); // safe: values are short scalars only
    const m = line.match(/^([A-Za-z_][\w-]*):\s*(.*)$/);
    if (!m) continue;
    const key = m[1];
    const val = m[2].trim();
    if (val === '' || val === 'null' || val === '~') { data[key] = null; continue; }
    if (val.startsWith('[') && val.endsWith(']')) {
      data[key] = val.slice(1, -1).split(',').map((s) => unquote(s.trim())).filter(Boolean);
    } else {
      data[key] = unquote(val);
    }
  }
  return data;
}
// `areas:` is a multi-line sub-block of `KEY: description` lines; everything after the
// first colon is the description verbatim (so `=` and `;` in descriptions survive).
function parseAreasSubBlock(areaLines) {
  const areas = {};
  for (const raw of areaLines) {
    const line = raw.trim();
    if (!line) continue;
    const ci = line.indexOf(':');
    if (ci === -1) continue;
    const key = line.slice(0, ci).trim();
    const desc = line.slice(ci + 1).trim();
    if (AREA_KEY_RE.test(key)) areas[key] = desc;
  }
  return areas;
}
// Extract the fenced ```cvs-repo … ``` block and parse it. Returns null when absent.
function parseCvsRepoBlock(readmeText) {
  const m = readmeText.match(/```cvs-repo[ \t]*\n([\s\S]*?)\n```/);
  if (!m) return null;
  const flatLines = [];
  const areaLines = [];
  let inAreas = false;
  for (const raw of m[1].split('\n')) {
    if (!inAreas) {
      if (/^areas:\s*$/.test(raw.replace(/\s+#.*$/, '').trimEnd())) { inAreas = true; continue; }
      flatLines.push(raw);
    } else if (raw.trim() === '' || /^\s/.test(raw)) {
      areaLines.push(raw); // blank or indented → still inside the areas sub-block
    } else {
      inAreas = false; flatLines.push(raw); // dedented scalar → areas ended
    }
  }
  const data = parseFlatBlock(flatLines.join('\n'));
  data.areas = parseAreasSubBlock(areaLines);
  return data;
}
// Validate the repo README + cvs-repo block. Hard-fails (returned as violations) on a
// missing README, missing block, missing required field, or invalid slug. Advises
// (non-fatal) when a requirements area has no glossary entry.
function validateRepoMeta(reqAreas) {
  const violations = [];
  const advisories = [];
  const readmePath = CFG.repoReadme || 'README.md';
  const abs = join(ROOT, readmePath);
  if (!existsSync(abs)) {
    violations.push(`REPO-META ${readmePath} missing — a repo README with a \`cvs-repo\` block is required`);
    return { repo: null, violations, advisories };
  }
  const repo = parseCvsRepoBlock(readText(abs));
  if (!repo) {
    violations.push(`REPO-META ${readmePath} has no \`\`\`cvs-repo\`\`\` block — add one (slug/name/description/aliases/owners/areas)`);
    return { repo: null, violations, advisories };
  }
  for (const field of ['slug', 'name', 'description']) {
    if (!repo[field] || (typeof repo[field] === 'string' && !repo[field].trim()))
      violations.push(`REPO-META \`cvs-repo\` is missing required field \`${field}\``);
  }
  if (repo.slug && !SLUG_RE.test(repo.slug))
    violations.push(`REPO-META \`cvs-repo\` slug "${repo.slug}" is invalid — use ${SLUG_RE}`);
  for (const area of [...reqAreas].sort()) {
    if (!(area in (repo.areas || {})))
      advisories.push(`REPO-META area \`${area}\` has requirements but no \`cvs-repo\` areas glossary entry`);
  }
  return { repo, violations, advisories };
}

// ── Per-record content hash ───────────────────────────────────────────────────
// Deterministic sha256 over a CANONICAL ordered array of the semantically significant
// fields — not raw file bytes — so frontmatter key ordering and whitespace never churn
// the hash. Statement/why are CRLF→LF normalized and trimmed. Mirrored by the CVS hub's
// sync lib so a row's hash is stable across the indexer and the hub.
// NOTE: `source:` is intentionally NOT hashed (like `provenance`/`sourceFile`) — it is advisory
// change-mapping metadata, and hashing it would churn every existing hash and broaden FR-REQINDEX-002.
function requirementContentHash(rec) {
  const canonical = JSON.stringify([
    ['id', rec.id],
    ['type', rec.type],
    ['area', rec.area],
    ['status', rec.status],
    ['verification', rec.manual ? 'manual' : (rec.verification || 'automated')],
    ['tests', [...(rec.tests || [])].sort()],
    ['statement', String(rec.statement || '').replace(/\r\n/g, '\n').trim()],
    ['why', String(rec.why || '').replace(/\r\n/g, '\n').trim()],
  ]);
  return 'sha256:' + createHash('sha256').update(canonical).digest('hex');
}

// Advisory-only lint for self-contained statements (constitution §"Self-contained
// requirement statements"). Statements are mirrored into the hub and read by an LLM
// without repo context, so they must stand alone. Returns an advisory string or null —
// never gates.
const DEICTIC_RE = /^(this|that|these|those|the above|the below|see code|it)\b/i;
const MIN_STATEMENT_CHARS = 25;
function lintStatement(rec) {
  const s = String(rec.statement || '').trim();
  if (!s) return `STATEMENT ${rec.id} — empty statement (must name subject + observable behavior)`;
  if (s.length < MIN_STATEMENT_CHARS) return `STATEMENT ${rec.id} — very short statement ("${s}"); make it self-contained`;
  if (DEICTIC_RE.test(s)) return `STATEMENT ${rec.id} — leading deictic ("${s.slice(0, 24)}…"); name the subject explicitly`;
  return null;
}

// ── Parse requirement records (frontmatter) + legacy MD tables ────────────────
function familyOf(id) { return id.replace(/-\d+$/, '').split('-')[0]; }   // FR / BIZ / NFR
function areaOf(id) { const m = id.match(/^[A-Z]+-([A-Z]+)-\d+$/); return m ? m[1] : '?'; }

function parseRequirements() {
  const reqs = new Map();
  const dupes = [];
  const areaPathViolations = [];
  const files = [...new Set(CFG.requirementSources.flatMap(glob))];

  for (const rel of files) {
    const text = readText(join(ROOT, rel));
    const parsed = parseFrontmatter(text);
    if (parsed && parsed.data.id) {
      const fm = parsed.data;
      const id = fm.id;
      const rec = {
        id,
        type: fm.type || familyOf(id),
        area: fm.area || areaOf(id),
        statement: fm.statement || firstLine(parsed.body),
        provenance: fm.provenance || 'md',
        status: fm.status || 'unratified',
        verification: fm.verification || 'automated',
        tests: Array.isArray(fm.tests) ? fm.tests : (fm.tests ? [fm.tests] : []),
        // Optional `source:` — source-file paths/globs that IMPLEMENT this requirement. Enables the
        // change-aware `--affected` mode: a changed file matching a source glob pulls in this requirement
        // and its bound `tests:`. OPTIONAL + backward-compatible — records without it are simply never
        // change-mapped (no existing behavior changes). Like `tests:`, a scalar is wrapped to one element.
        source: Array.isArray(fm.source) ? fm.source : (fm.source ? [fm.source] : []),
        // Optional, less-strict "why" (rationale) — context for LLM clients and human
        // reviewers; surfaced in the index but never gated. A scalar; arrays are joined for safety.
        why: Array.isArray(fm.why) ? fm.why.join(', ') : (typeof fm.why === 'string' ? fm.why : null),
        manual: (fm.verification === 'manual') ||
          !!(CFG.manualMarker && ((fm.statement || '') + ' ' + (parsed.body || '')).includes(CFG.manualMarker)),
        infra: !!(CFG.infraMarker && ((fm.statement || '') + ' ' + (parsed.body || '')).includes(CFG.infraMarker)),
        sourceFile: rel,
      };
      if (reqs.has(id)) dupes.push(`${id} (${reqs.get(id).sourceFile} & ${rel})`);
      reqs.set(id, rec);
      // area-prefix-matches-path: records under .../requirements/ must sit in/under their area
      if (CFG.enforceAreaPrefixMatchesPath && rel.includes('/requirements/')) {
        if (!rel.toUpperCase().includes('/' + rec.area + '/') && !rel.toUpperCase().includes('/' + rec.area.toUpperCase() + '/'))
          areaPathViolations.push(`${id} in ${rel} (area ${rec.area} not in path)`);
      }
      continue;
    }
    // Legacy markdown table rows: | ID | text ... |
    for (const line of text.split('\n')) {
      const m = line.match(new RegExp(`^\\|\\s*(${CFG.idPattern})\\s*\\|(.*)$`));
      if (!m) continue;
      const id = m[1];
      const rest = m[2];
      const rec = {
        id, type: familyOf(id), area: areaOf(id),
        statement: rest.split('|')[0].trim(),
        why: null,
        provenance: 'md', status: 'unratified', verification: 'automated', tests: [], source: [],
        manual: CFG.manualMarker ? rest.includes(CFG.manualMarker) : false,
        infra: CFG.infraMarker ? rest.includes(CFG.infraMarker) : false,
        sourceFile: rel,
      };
      if (reqs.has(id)) dupes.push(`${id} (${reqs.get(id).sourceFile} & ${rel})`);
      reqs.set(id, rec);
    }
  }
  return { reqs, dupes, areaPathViolations };
}

// ── Scan tests for ID refs + untagged detection ───────────────────────────────
function stripRanges(s) { return CFG.ignoreRangeHeaders ? s.replace(RANGE_RE, ' ') : s; }

function scanTests() {
  const refsByKind = {};      // id -> { kind -> Set(files) }  (orphan + untagged detection only)
  const untagged = [];        // { id:'file:label', file, label } for requireTagged sources
  const testFileKind = {};    // rel -> kind  (first matching source wins) — used for binding coverage
  const declWarnings = [];    // requireTagged sources that match files but detect 0 test declarations
  for (const src of CFG.testSources) {
    let srcFiles = 0, srcDecls = 0;
    for (const rel of glob(src.glob)) {
      if (!(rel in testFileKind)) testFileKind[rel] = src.kind;
      const raw = readText(join(ROOT, rel));
      const scan = stripRanges(raw);
      for (const id of scan.match(ID_RE) || []) {
        (refsByKind[id] ||= {});
        (refsByKind[id][src.kind] ||= new Set()).add(rel);
      }
      if (src.requireTagged) {
        srcFiles++;
        const { untagged: u, decls } = findUntagged(rel, raw, src);
        srcDecls += decls;
        untagged.push(...u);
      }
    }
    // Silent-green guard (R1): a requireTagged source that matches files but contains ZERO detected test
    // declarations almost always means the decl pattern doesn't fit the framework — e.g. the default
    // JavaScript `test('…')` regex applied to a C# MSTest source. Without this warning the gate passes
    // green while enforcing nothing. Surface it; the fix is an explicit per-source "declRegex" (or the
    // right `kind`/stack).
    if (src.requireTagged && srcFiles > 0 && srcDecls === 0)
      declWarnings.push(`${src.glob} (kind: ${src.kind}): matched ${srcFiles} file(s) but found 0 test declarations — the decl pattern likely doesn't match this framework. Set a per-source "declRegex" in .traceability.config.json.`);
  }
  return { refsByKind, untagged, testFileKind, declWarnings };
}

// Per-stack test-declaration patterns (capture group 1 = the test name/title when available).
// Override per source with "declRegex" in .traceability.config.json; add stacks here as needed.
const DECL_DEFAULTS = {
  playwright:  String.raw`\b(?:test|it)(?:\.(?!describe\b)\w+)?\(\s*['"\x60]([^'"\x60]+)['"\x60]`,
  integration: String.raw`\b(?:test|it)(?:\.(?!describe\b)\w+)?\(\s*['"\x60]([^'"\x60]+)['"\x60]`,
  jest:        String.raw`\b(?:test|it)(?:\.(?!describe\b)\w+)?\(\s*['"\x60]([^'"\x60]+)['"\x60]`,
  dotnet:      String.raw`\[(?:Fact|Theory|TestMethod|DataTestMethod|TestCase|Test)\b[^\]]*\]`,
  pytest:      String.raw`\bdef\s+(test_\w+)`,
  go:          String.raw`\bfunc\s+(Test\w+)`,
  junit:       String.raw`@Test\b`,
  rspec:       String.raw`\b(?:it|specify)\s+['"]([^'"]+)['"]`,
};
const CONTEXT = 3; // lines around a declaration to scan for a requirement-ID tag
// A requireTagged test is "tagged" if an ID appears in/near its declaration (comment above, or — for
// attribute styles like [Fact]/@Test — the method name below). Otherwise it's reported as untagged.
// Method-signature matcher for attribute styles: the first declared identifier before `(` below the
// attribute (skipping any extra decorators like [DataRow]/[Trait]). Used so attribute-style untagged ids
// are per-TEST, not per-file.
const SIG_RE = /\b(?:public|private|protected|internal|static|async|void|Task|[A-Z]\w*)\b[^\n(]*?\b([A-Za-z_]\w*)\s*\(/;
function findUntagged(rel, raw, src) {
  const re = new RegExp(src.declRegex || DECL_DEFAULTS[src.kind] || DECL_DEFAULTS.playwright);
  const idInline = new RegExp(CFG.idPattern);
  const lines = raw.split('\n');
  const out = [];
  let decls = 0;
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(re);
    if (!m) continue;
    decls++;
    const hasTitle = m[1] != null;
    // Title styles (test('…')): the ID must be in the title or a comment ABOVE — never below (that
    // would borrow the next test's tag). Attribute styles ([Fact]/@Test): also scan BELOW for the
    // method name / its tag.
    const above = lines.slice(Math.max(0, i - CONTEXT), i + 1).join('\n');
    const below = hasTitle ? '' : lines.slice(i + 1, i + 1 + CONTEXT).join('\n');
    if (idInline.test(above) || (below && idInline.test(below))) continue; // tagged
    // Attribute styles have no captured title (R8): derive the method name from the signature below so
    // the untagged id is per-test (the baseline then shrinks one test at a time). Scan a few lines past
    // any extra attribute decorators. Title styles use the captured title as before.
    let label = m[1];
    if (label == null) {
      const sig = lines.slice(i + 1, i + 1 + 6).join('\n').match(SIG_RE);
      label = sig ? sig[1] : lines[i].trim();
    }
    label = label.slice(0, 80);
    out.push({ id: `${rel}:${label.slice(0, 60)}`, file: rel, label });
  }
  return { untagged: out, decls };
}

// ── Baselines ─────────────────────────────────────────────────────────────────
function loadList(relPath, key) {
  const p = join(ROOT, relPath);
  if (!existsSync(p)) return new Set();
  try { return new Set(JSON.parse(readFileSync(p, 'utf8'))[key] ?? []); }
  catch { return new Set(); }
}

// ── Main ────────────────────────────────────────────────────────────────────
const { reqs, dupes, areaPathViolations } = parseRequirements();

// `--next-id --area AUTH [--type FR]`: print the next free ID for an area (helps parallel authors avoid
// collisions). Runs before the empty-repo guard so a fresh repo returns <TYPE>-<AREA>-001.
if (argv.has('--next-id')) {
  const a = process.argv.slice(2);
  const optv = (n) => { const i = a.indexOf(`--${n}`); return i >= 0 ? a[i + 1] : undefined; };
  const nArea = (optv('area') || '').toUpperCase();
  const nType = (optv('type') || 'FR').toUpperCase();
  if (!nArea) { console.error('Usage: reqindex --next-id --area <AREA> [--type FR|BIZ|NFR]'); process.exit(2); }
  let max = 0;
  for (const r of reqs.values()) if (r.type === nType && r.area === nArea) {
    const m = String(r.id).match(/-(\d+)$/); if (m) max = Math.max(max, parseInt(m[1], 10));
  }
  console.log(`${nType}-${nArea}-${String(max + 1).padStart(3, '0')}`);
  process.exit(0);
}

// ── Change-aware mode (--affected) ────────────────────────────────────────────
// Map changed files → requirements (whose `source:` globs match) → those requirements' bound `tests:`.
// Reports the graph, a blind-spot list (changed source under no requirement), and a reverse-drift
// warning (source changed but its bound tests did not — the "edited impl, not its test" gap). ADVISORY:
// exit 0 in every output mode; only `--run` propagates the configured test runner's exit code. It never
// scans tests, computes coverage, or writes the index — so `--check` behavior is completely unchanged.
// Promoting `--affected`/reverse-drift to a merge-blocking gate is a governance change (a
// `constitution-change` PR), deliberately NOT done here. See FR-REQINDEX-005 / FR-REQINDEX-006.
if (AFFECTED) process.exit(runAffected());

function gitOut(cmdArgs) {
  const r = spawnSync('git', cmdArgs, { cwd: ROOT, encoding: 'utf8' });
  return r.status === 0 ? (r.stdout || '') : null;
}
function mergeBase(ref) { const o = gitOut(['merge-base', ref, 'HEAD']); return o ? o.trim() : null; }
function normFiles(list) { return [...new Set(list.map((f) => f.split(sep).join('/')))].sort(); }
// Resolve the repo's default branch ref (origin/HEAD → origin/main|master → main|master) for the
// default merge-base comparison. Returns null when none resolve (no remote / detached / bare).
function defaultBranchRef() {
  const sym = gitOut(['symbolic-ref', '--quiet', 'refs/remotes/origin/HEAD']);
  if (sym && sym.trim()) return sym.trim().replace(/^refs\/remotes\//, '');
  for (const ref of ['origin/main', 'origin/master', 'main', 'master'])
    if (gitOut(['rev-parse', '--verify', '--quiet', ref]) != null) return ref;
  return null;
}
// The changed-file set (repo-relative, forward-slash, sorted). From --files if given (no git at all —
// ideal for piping `git diff --name-only | …` and for deterministic tests); else from git:
//   --since <ref>  → files changed from <ref> to the working tree (direct: everything since that ref)
//   --base  <ref>  → files changed from merge-base(<ref>, HEAD) to the working tree (the branch-point diff)
//   (neither)      → merge-base(defaultBranch, HEAD) → working tree
// `git diff <base>` (no `..HEAD`) compares base to the WORKING TREE, so committed-since-base AND
// uncommitted local edits are both captured; untracked new files are unioned in (a new file is a real
// blind-spot / drift candidate).
function resolveChangedFiles() {
  const filesArg = argValue('--files');
  if (filesArg !== undefined)
    return { base: '(--files)', files: normFiles(filesArg.split(/[\s,]+/).map((s) => s.trim()).filter(Boolean)) };
  const since = argValue('--since');
  const baseOpt = argValue('--base');
  let base, label;
  if (since !== undefined) { base = since; label = since; }
  else if (baseOpt !== undefined) { base = mergeBase(baseOpt) || baseOpt; label = baseOpt; }
  else {
    const def = defaultBranchRef();
    base = def ? (mergeBase(def) || def) : 'HEAD';
    label = def ? `merge-base(${def}, HEAD)` : 'HEAD (no default branch found — uncommitted changes only)';
  }
  const diff = gitOut(['diff', '--name-only', base]);
  if (diff == null) {
    console.error(`reqindex --affected: \`git diff --name-only ${base}\` failed — not a git repo, or a bad ref. ` +
      `Pass an explicit \`--files <list>\` to map a changed-file set without git.`);
    process.exit(2);
  }
  const tracked = diff.split('\n').map((s) => s.trim()).filter(Boolean);
  const untracked = (gitOut(['ls-files', '--others', '--exclude-standard']) || '')
    .split('\n').map((s) => s.trim()).filter(Boolean);
  return { base: label, files: normFiles([...tracked, ...untracked]) };
}

function testPaths(rec) { // bound test files, `#ID` fragment stripped, deduped + sorted
  return [...new Set((rec.tests || []).map((t) => String(t).split('#')[0].trim()).filter(Boolean))].sort();
}
function isReqRecord(f) { return CFG.requirementSources.some((g) => globToRe(g).test(f)); }
function isTestSource(f) { return (CFG.testSources || []).some((s) => globToRe(s.glob).test(f)); }
function isCvsArtifact(f) {
  return [CFG.index?.json, CFG.index?.traceability, CFG.baseline, CFG.untaggedTestBaseline, '.traceability.config.json']
    .filter(Boolean).includes(f);
}

function runAffected() {
  const { base, files: changedFiles } = resolveChangedFiles();
  const reqList = [...reqs.values()];
  const compiled = reqList.map((r) => ({ rec: r, res: (r.source || []).map((g) => ({ g, re: globToRe(g) })) }));

  // changed file → requirement (source-glob match) → bound tests
  const matched = new Map(); // id -> { id, source, tests, changedSource:Set }
  for (const f of changedFiles)
    for (const { rec, res } of compiled) {
      if (!res.some(({ re }) => re.test(f))) continue;
      let m = matched.get(rec.id);
      if (!m) { m = { id: rec.id, source: rec.source, tests: testPaths(rec), changedSource: new Set() }; matched.set(rec.id, m); }
      m.changedSource.add(f);
    }
  const requirements = [...matched.values()].sort((a, b) => a.id.localeCompare(b.id))
    .map((m) => ({ id: m.id, source: m.source, tests: m.tests, changedSource: [...m.changedSource].sort() }));
  const affectedTests = [...new Set(requirements.flatMap((r) => r.tests))].sort();

  // Reverse-drift: source changed but NONE of its (non-empty) bound tests are in the diff.
  const changedSet = new Set(changedFiles);
  const reverseDrift = requirements
    .filter((r) => r.tests.length && !r.tests.some((t) => changedSet.has(t)))
    .map((r) => ({ id: r.id, changedSource: r.changedSource, tests: r.tests }));

  // Blind spots: changed files matching no requirement source, excluding requirement records, CVS
  // artifacts, and test sources. If `affected.sourceGlobs` is set, restrict to those (focuses the
  // report on real source files); otherwise report every such file (advisory; set sourceGlobs to focus).
  const matchedFiles = new Set(requirements.flatMap((r) => r.changedSource));
  const sourceGlobs = (CFG.affected?.sourceGlobs || []).map(globToRe);
  const blindSpots = changedFiles.filter((f) =>
    !matchedFiles.has(f) && !isReqRecord(f) && !isCvsArtifact(f) && !isTestSource(f) &&
    (sourceGlobs.length === 0 || sourceGlobs.some((re) => re.test(f))));

  const withoutSource = reqList.filter((r) => !(r.source && r.source.length)).length;
  const summary = {
    changedFiles: changedFiles.length, requirements: requirements.length, affectedTests: affectedTests.length,
    blindSpots: blindSpots.length, reverseDrift: reverseDrift.length,
    requirementsWithoutSource: withoutSource, totalRequirements: reqList.length,
  };
  const result = { base, changedFiles, requirements, affectedTests, blindSpots, reverseDrift, summary };

  if (JSON_OUT) console.log(JSON.stringify(result, null, 2));
  else if (argv.has('--list-tests')) { if (affectedTests.length) console.log(affectedTests.join('\n')); }
  else printAffectedHuman(result);

  return argv.has('--run') ? runAffectedTests(affectedTests) : 0;
}

function printAffectedHuman(res) {
  console.log(`reqindex --affected: ${res.summary.changedFiles} changed file(s) → ` +
    `${res.summary.requirements} requirement(s) → ${res.summary.affectedTests} affected test(s)  (base: ${res.base})`);
  if (!res.requirements.length) console.log('  (no changed file matches any requirement `source:` mapping)');
  else for (const r of res.requirements)
    for (const f of r.changedSource)
      for (const t of (r.tests.length ? r.tests : ['(no bound test)']))
        console.log(`  changed ${f} → ${r.id} → ${t}`);
  console.log(`\nReverse-drift (source changed, bound test(s) did NOT) — advisory:`);
  if (res.reverseDrift.length) for (const d of res.reverseDrift)
    console.log(`  ${d.id} — changed [${d.changedSource.join(', ')}] but test(s) [${d.tests.join(', ')}] unchanged`);
  else console.log('  none');
  console.log(`\nBlind spots (changed source matching no requirement \`source:\`) — advisory:`);
  if (res.blindSpots.length) for (const f of res.blindSpots) console.log(`  ${f}`);
  else console.log('  none');
  console.log(`\nRequirements without a \`source:\` mapping: ` +
    `${res.summary.requirementsWithoutSource}/${res.summary.totalRequirements} (backfill to grow change-mapping coverage).`);
  console.log('Advisory only — `--affected` does not gate. Promoting it to a merge-blocking gate is a constitution-change.');
}

// `--run`: shell out to the project's test runner for the affected set. Configurable via
// `.traceability.config.json` → `affected.runCmd` (a template; `{tests}` is replaced with the
// space-joined affected test paths, else they are appended). Unset → no-op + printed command, exit 0.
function runAffectedTests(tests) {
  if (!tests.length) { console.log('reqindex --affected --run: no affected tests; nothing to run.'); return 0; }
  const tmpl = CFG.affected?.runCmd;
  const testsArg = tests.join(' ');
  if (!tmpl) {
    console.log('reqindex --affected --run: no `affected.runCmd` configured in .traceability.config.json (no-op).');
    console.log(`  would run the affected tests, e.g.:  npx vitest run ${testsArg}`);
    return 0;
  }
  const cmd = tmpl.includes('{tests}') ? tmpl.split('{tests}').join(testsArg) : `${tmpl} ${testsArg}`;
  console.log(`reqindex --affected --run: ${cmd}`);
  const r = spawnSync(cmd, { cwd: ROOT, stdio: 'inherit', shell: true });
  return r.status ?? 1;
}

if (reqs.size === 0) { console.error('No requirements found.'); process.exit(2); }
const { refsByKind, untagged, testFileKind, declWarnings } = scanTests();

// One-time bootstrap seed: accept today's untagged UI/integration tests as shrink-only debt.
if (argv.has('--seed-untagged')) {
  const ids = [...new Set(untagged.map((u) => u.id))].sort();
  writeFileSync(join(ROOT, CFG.untaggedTestBaseline),
    JSON.stringify({ $comment: 'Shrink-only bootstrap debt: UI/integration tests not yet tagged with a requirement ID. Tag a test and remove its entry. Do not add new entries to mask a regression.', acceptedUntagged: ids }, null, 2) + '\n');
  console.log(`Seeded ${ids.length} untagged UI/integration test(s) into ${CFG.untaggedTestBaseline}.`);
  process.exit(0);
}

const gapBaseline = loadList(CFG.baseline, 'acceptedGaps');
const untaggedBaseline = loadList(CFG.untaggedTestBaseline, 'acceptedUntagged');

const violations = [];
const debt = [];
const advisories = [];

// Repo metadata: validate the required README `cvs-repo` block (hard-fail on a missing
// README/block/field/invalid slug; advise on a requirements area with no glossary entry).
const reqAreas = new Set([...reqs.values()].map((r) => r.area));
const { repo: repoMeta, violations: repoViolations, advisories: repoAdvisories } = validateRepoMeta(reqAreas);
violations.push(...repoViolations);
advisories.push(...repoAdvisories);

// Orphans (always hard)
for (const id of Object.keys(refsByKind)) {
  if (!reqs.has(id)) violations.push(`ORPHAN ${id} — referenced in tests but not defined`);
}
if (CFG.enforceGlobalIdUniqueness) for (const d of dupes) violations.push(`DUPLICATE-ID ${d}`);
for (const a of areaPathViolations) violations.push(`AREA-PATH ${a}`);

// Coverage comes from the record's `tests:` binding (records are the source of truth): each bound path
// (a trailing `#ID` fragment is ignored) must resolve to a configured test source; its kind counts.
function bindingKinds(rec) {
  const kinds = new Set();
  const broken = [];
  for (const t of rec.tests) {
    const p = String(t).split('#')[0].trim();
    if (!p) continue;
    if (p in testFileKind) kinds.add(testFileKind[p]);
    else broken.push(p);
  }
  return { kinds: [...kinds], broken };
}
function satisfies(requires, kinds, manual) {
  return requires.split('|').some((tok) =>
    tok === 'any' ? kinds.length > 0 :
    tok === 'manual' ? manual :
    kinds.includes(tok));
}

const rows = [];
for (const rec of [...reqs.values()].sort((a, b) => a.id.localeCompare(b.id))) {
  const { kinds, broken } = bindingKinds(rec);
  for (const p of broken) violations.push(`BROKEN-BINDING ${rec.id} — tests: "${p}" matches no configured test source`);
  const policy = CFG.policy[rec.type]?.requires || 'any|manual';
  const ok = satisfies(policy, kinds, rec.manual);
  let coverage;                                  // covered | manual | gap (distinct from rec.status, the ratification state)
  if (ok) coverage = rec.manual && kinds.length === 0 ? 'manual' : 'covered';
  else {
    coverage = 'gap';
    const msg = `UNCOVERED ${rec.id} — needs [${policy}]; found [${kinds.join(',') || 'none'}]` +
      (kinds.length ? ` (kind mismatch — bind a [${policy}] test, or re-type the requirement, e.g. a BIZ rule proven only by a UI test should be FR)` : '');
    if (gapBaseline.has(rec.id)) debt.push(msg); else violations.push(msg);
  }
  if ((coverage === 'covered' || coverage === 'manual') && gapBaseline.has(rec.id))
    advisories.push(`RESOLVED ${rec.id} — now covered; remove from ${CFG.baseline}`);
  const lint = lintStatement(rec);
  if (lint) advisories.push(lint);
  // Source-glob validation (advisory, NEVER gating — unlike a broken `tests:` binding): each `source:`
  // glob should resolve to ≥1 real file, else the change-mapping silently can't fire for it.
  for (const s of rec.source)
    if (glob(s).length === 0)
      advisories.push(`SOURCE ${rec.id} — source: "${s}" matches no file (fix the path/glob or remove it; advisory — not gated)`);
  rows.push({ ...rec, kinds, coverage, debt: coverage === 'gap' && gapBaseline.has(rec.id) });
}

// Infra-debt (R4): a covered requirement whose only proof is marked not-runnable-in-standard-CI. Counts
// as covered (traceability holds), but its proof isn't continuously exercised — advisory, never a gate fail.
const infraDebt = rows.filter((r) => r.infra && (r.coverage === 'covered' || r.coverage === 'manual'));

// One-time bootstrap seed: accept today's uncovered requirements as shrink-only gap debt.
if (argv.has('--seed-gaps')) {
  const ids = rows.filter((r) => r.coverage === 'gap').map((r) => r.id).sort();
  writeFileSync(join(ROOT, CFG.baseline),
    JSON.stringify({ $comment: 'Shrink-only bootstrap debt: requirements not yet covered per policy. Add a test + binding and remove the ID. Do not add new entries to mask a regression.', acceptedGaps: ids }, null, 2) + '\n');
  console.log(`Seeded ${ids.length} uncovered requirement(s) into ${CFG.baseline}.`);
  process.exit(0);
}

// Untagged UI/integration tests
let untaggedNew = 0, untaggedDebt = 0;
for (const u of untagged) {
  if (untaggedBaseline.has(u.id)) { untaggedDebt++; }
  else { untaggedNew++; violations.push(`UNTAGGED ${u.file} :: "${u.label}" — UI/integration test cites no requirement ID`); }
}

// ── Emit index ────────────────────────────────────────────────────────────────
const summary = {
  total: rows.length,
  covered: rows.filter((r) => r.coverage === 'covered').length,
  manual: rows.filter((r) => r.coverage === 'manual').length,
  debt: rows.filter((r) => r.debt).length,
  gapsNew: rows.filter((r) => r.coverage === 'gap' && !r.debt).length,
  ratified: rows.filter((r) => r.status === 'ratified').length,
  unratified: rows.filter((r) => r.status !== 'ratified').length,
  withWhy: rows.filter((r) => r.why && String(r.why).trim()).length,
  withSource: rows.filter((r) => r.source && r.source.length).length,
  infraDebt: infraDebt.length,
  untaggedDebt, untaggedNew, orphans: violations.filter((v) => v.startsWith('ORPHAN')).length,
};

// Build the generated index. In --check mode we VERIFY the committed artifact is current (read-only)
// instead of writing — so a stale committed index fails CI. In default mode we (re)write it.
function syncOrCheck(relPath, content) {
  const abs = join(ROOT, relPath);
  if (CHECK) {
    // CRLF-tolerant compare (R10): normalize the committed artifact so a CRLF working tree doesn't read
    // as STALE against the always-LF generated `content`.
    const cur = existsSync(abs) ? readFileSync(abs, 'utf8').replace(/\r\n/g, '\n') : '';
    if (cur !== content) violations.push(`STALE-INDEX ${relPath} — regenerate with \`node scripts/reqindex.mjs\` and commit`);
  } else {
    writeFileSync(abs, content);
  }
}
if (CFG.index?.json) {
  const jsonContent = JSON.stringify({
    generatedBy: 'reqindex', summary,
    repo: repoMeta,
    requirements: rows.map((r) => ({
      id: r.id, type: r.type, area: r.area,
      coverage: r.coverage, debt: r.debt, infraDebt: !!r.infra,
      ratification: r.status, provenance: r.provenance,
      verification: r.manual ? 'manual' : 'automated',
      coveredBy: r.kinds, tests: r.tests, source: r.source, sourceFile: r.sourceFile,
      statement: r.statement,
      why: r.why ?? null,
      contentHash: requirementContentHash(r),
    })),
  }, null, 2) + '\n';
  syncOrCheck(CFG.index.json, jsonContent);
}
if (CFG.index?.traceability) {
  const byArea = new Map();
  for (const r of rows) { if (!byArea.has(r.area)) byArea.set(r.area, []); byArea.get(r.area).push(r); }
  let md = '# Requirement → Test Traceability Matrix\n\n';
  md += `> Generated by \`scripts/reqindex.mjs\` — do not hand-edit. Source of truth: the requirement records.\n\n`;
  md += `**Coverage: ${summary.covered + summary.manual}/${summary.total}** — `;
  md += `${summary.covered} tested, ${summary.manual} manual, ${summary.debt} accepted debt, `;
  md += `${summary.gapsNew} unbaselined gaps. Ratified: ${summary.ratified}/${summary.total}. `;
  md += `Source-mapped: ${summary.withSource}/${summary.total}. `;
  md += `Untagged UI/integration tests: ${untaggedDebt} debt / ${untaggedNew} new.`;
  md += summary.infraDebt ? ` Infra-debt (proof not run in standard CI): ${summary.infraDebt}.\n\n` : '\n\n';
  md += 'Coverage badge: ✅ covered · 🖐 manual · 🔌 infra (covered, but proof not run in standard CI) · 🧱 debt (baseline) · ❌ gap.\n\n';
  for (const area of [...byArea.keys()].sort()) {
    md += `## ${area}\n\n| ID | Coverage | Provenance | Ratification | Covered by | Source |\n|----|----------|-----------|--------------|------------|--------|\n`;
    for (const r of byArea.get(area)) {
      const badge = r.debt ? '🧱 debt'
        : r.coverage === 'covered' ? (r.infra ? '🔌 infra' : '✅ covered')
        : r.coverage === 'manual' ? (r.infra ? '🔌 infra' : '🖐 manual') : '❌ gap';
      const src = r.source && r.source.length ? r.source.join('<br>') : '—';
      md += `| ${r.id} | ${badge} | ${r.provenance} | ${r.status} | ${r.kinds.join(', ') || '—'} | ${src} |\n`;
    }
    md += '\n';
  }
  syncOrCheck(CFG.index.traceability, md);
}

// ── Report & exit ──────────────────────────────────────────────────────────────
if (!JSON_OUT) {
  console.log(`reqindex: ${summary.covered + summary.manual}/${summary.total} covered ` +
    `(${summary.debt} debt, ${summary.gapsNew} new gaps; untagged ${untaggedDebt} debt/${untaggedNew} new).`);
  console.log(`reqindex: ${summary.withWhy}/${summary.total} have a \`why\` rationale (advisory — not gated).`);
  console.log(`reqindex: ${summary.withSource}/${summary.total} have a \`source\` mapping (advisory — enables \`--affected\`).`);
} else {
  console.log(JSON.stringify(summary));
}
if (debt.length) console.warn(`::warning::${debt.length} accepted requirement gap(s) in ${CFG.baseline} — burn down.`);
if (untaggedDebt) console.warn(`::warning::${untaggedDebt} accepted untagged UI/integration test(s) — burn down.`);
if (infraDebt.length) console.warn(`::warning::${infraDebt.length} requirement(s) are infra-debt (proof exists but isn't run in standard CI): ${infraDebt.map((r) => r.id).join(', ')} — "covered" here is traceability, not continuous verification.`);
for (const w of declWarnings) console.warn(`::warning::${w}`);
for (const a of advisories) console.warn(`::warning::${a}`);

if (violations.length) {
  console.error(`\n${violations.length} NEW traceability violation(s):`);
  for (const v of violations) console.error(`  ${v}`);
  console.error('\nFix: tag the test, add a test, correct the requirement record, or (only if genuinely accepted) add to a baseline.');
  process.exit(1);
}
console.log(`Traceability OK — no new violations.`);
