#!/usr/bin/env node
// cvs — dispatcher for the Computable Verifiable Requirements kit.
//   cvs init                 scaffold the kit into the current repo
//   cvs index [--check]      regenerate / verify the traceability index (reqindex)
//   cvs index --affected [--base/--since/--files] [--json|--list-tests] [--run]
//                            change-aware: map changed files → requirements (source:) → bound tests (advisory)
//   cvs index --seed-gaps    seed the accepted-gap baseline with today's uncovered requirements
//   cvs index --seed-untagged  seed the untagged-test baseline with today's untagged UI/integration tests
//   cvs next-id --area A [--type FR]  print the next free requirement ID for an area
//   cvs ratify [...]         flip requirement records unratified->ratified
//   cvs migrate [--prose [path]]  one-time: legacy REQUIREMENTS.md table -> records (or --prose: a rich
//                            PRD with **ID**: statements -> records + an old->new ID crosswalk)
//   cvs apply-ledger [path]  apply a reviewed Appendix B reconciliation ledger (.csv or .md)
// All subcommands run against the current working directory (the consumer repo).
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const MAP = {
  init: 'cvs-init.mjs',
  index: 'reqindex.mjs', reqindex: 'reqindex.mjs',
  'next-id': 'reqindex.mjs',          // alias: runs reqindex with --next-id prepended
  ratify: 'ratify.mjs',
  migrate: 'migrate-requirements-to-records.mjs',
  'apply-ledger': 'apply-ledger.mjs',
};
const [cmd, ...rest] = process.argv.slice(2);
if (!cmd || cmd === '--help' || cmd === '-h' || !MAP[cmd]) {
  const known = Object.keys(MAP).join(' | ');
  console.log(`Usage: cvs <${known}> [args]`);
  process.exit(cmd && cmd !== '--help' && cmd !== '-h' ? 2 : 0);
}
const args = cmd === 'next-id' ? ['--next-id', ...rest] : rest;
const res = spawnSync(process.execPath, [join(HERE, MAP[cmd]), ...args], { stdio: 'inherit' });
process.exit(res.status ?? 1);
