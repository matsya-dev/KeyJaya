# Ctrl scripture insertion, ranges, and cleaner cycles

This is the development-stage record. The completed 1.9.5 release checks are
recorded in [Release verification](VERIFICATION.md).

Verified locally on 23 October 2026 on `codex/ctrl-verse-insertion`.
This section records logic-only feature verification before the subsequent
[Excel investigation](EXCEL_DEBUGGING.md). It is not a packaged release. No
installer was built. The Excel report separately records its isolated resident
and disposable-workbook tests.

## Automated results

- **43 isolated suites passed; 25,161,990 assertions; zero failures.** Results
  combine the full regression run with the new range suite and targeted reruns
  after the ACBSP and SSR updates. Logs are in `out/tests/combined-feature-verification.json`.
- Checked **36,684 adjacent verse ranges across BG, SB, CC, CB, ISO, BS, and NOI**
  in Unicode, Balarama, and plain Roman output.
- Checked 1,131 distributed formatting cases, including both forced verse italics
  and prose translation boundaries. Word span merging, rich-editor HTML, and
  Markdown output preserve the text and verse/translation order.
- Checked 4,011 shared-translation cases across the encodings. Adjacent verses
  sharing a translation include it once; nonadjacent repetitions stay in place.
- Bare references and ranges remain verse-only; `T`, `VT`, and `R` retain their
  explicit meanings. Ctrl, selected-reference conversion, and symbol endings
  produce consistent content. Punctuation, surrounding text, and abbreviation
  priority are covered.
- Synthetic data verifies missing first/middle/last records, translation-only
  records, the exact 200-verse limit, and the exact 200,000-character output limit.
  Missing or oversized passages return no partial insertion.
- Cleaner cycles retain validated alternatives and the original plain spelling,
  remove tested partial/case duplicates, and loop continuously. The original undo
  spelling survives 100 complete loops, including through the plain option.
  English protection and preferred-spelling tests also pass.
- ACBSP defaults now use `A.C. Bhaktivedanta Swami Prabhupāda`, with corresponding
  Balarama and plain forms. The upgrade correction preserves custom and exact
  replacements, disabled/deleted variants, comments, encoding, and unrelated YAML.
- All 30 runtime-data checksums and local documentation links were checked.
  Only the requested short-trigger TSV changed in the runtime data pack.
- SSR expands to `The Science of Self-Realization` with Ctrl or `>`, `<`, and `?`.
  Its independent upgrade marker allows installation after the earlier SBB
  addition, preserving custom/disabled SSR entries and subsequent deletions.

## Remaining release checks

These tests exercise conversion logic, caret targeting, formatting output,
ownership checks, and undo state. They do not prove physical input timing or an
editor's native undo stack. Before packaging, use disposable Word documents to
check Ctrl+Z after range insertion, Backspace after cleaned word cycles, original
font/style restoration, Track Changes, and queued typing. The examples are in
[Testing KeyJaya](TESTING.md#physical-release-checks).

The accepted source baseline and existing release verification remain for v1.9.4;
they have not been refreshed to label these unbuilt changes as a release.

## Consolidation — 24 October 2026

Unreleased work on `codex/consolidate-definitions`, based on the published 1.9.5
source. The accepted 1.9.5 baseline, installed app and existing installer are
unchanged. No installer was built, no resident was activated, and no live editor
or open document was controlled during this pass.

- `assets/short-trigger-defaults.yml` is the sole maintained trigger definition.
  Fresh editable YAML is copied from it; restoration, requested upgrade groups
  and fallback lookup use it too. Its original bytes are unchanged. Separate
  default, upgrade-addition and TSV fallback lists were removed. This also makes
  `BB` available in fresh defaults. Existing edited/deleted/disabled canonical
  triggers remain authoritative. Legacy personal-YAML fallback behavior is tested.
- Seven comment-only customization templates were replaced by one sectioned
  `customizations.ini`. It covers aliases, preferred/protected words, script words,
  script phrases, italics and ordered replacements. Legacy files are read with
  their existing precedence, without rewriting them. Unified entries override
  legacy entries of the same kind; word preferences still win over general aliases.
- Caret and selection commands share word preparation and cycle choice; copied
  and selected commands share source-encoding dispatch. Editor-specific selection,
  native replacement, clipboard ownership, fonts, undo and input queues remain
  separate. Large dictionary/index files were not consolidated.
- Future builds use a fresh output tree, retaining prior development output.
  Future upgrades archive the two obsolete factory-only files in the transaction
  backup. User customization files are retained. An isolated transaction probe
  verified cleanup, settings preservation and complete rollback.

Verification: all **44 suites passed, 26,936,540 assertions, zero failures**.
After a final parser fix for literal bracketed replacement rules, the affected
`StandaloneTests` rerun passed **41,722 assertions**; the latest result for each
of the 44 suites totals **26,936,542 assertions**. These additions test all seven
output modes, legacy/unified equivalence, precedence, invalid/duplicate sections,
failed reloads, protected words, exact triggers, continuous cycles and no writes
while loading configuration.

A separate probe compiled both the released and refactored engines and compared
**35,088 behavior records** with identical results: word/copied/selected conversion,
spelling choices, formatting decisions and verse expansion. Preservation checks
confirmed all **27 installed/temporary configuration files** and all **29 retained
runtime-data files** unchanged. Only the redundant trigger TSV was removed from
the data inventory. Development audit, documentation links, script syntax and
setup-source library compilation passed.

Detailed local evidence is in `out/consolidation/verification.json`,
`full44-results.json`, `final-customization-rerun.json`, `combined44-results.json`,
`differential-verification.json` and `preservation.json`. The strict release audit
still detects intentional source drift; `--development` reports it without
refreshing the accepted baseline. This pass verifies automated logic and
configuration compatibility, not new physical editor timing or native undo tests.

### BB upgrade follow-up

The automatic BB repair described below was superseded by the 26 October review
fixes at the end of this report. An absent BB group is no longer restored during
upgrade without an explicit user action.

The installed editable trigger file lacked `BB`, although the factory list had
it. The upgrade-addition groups included SBB and SSR but omitted BB. BB now has
its own one-time addition marker, with tests for missing, custom, disabled,
partial and empty configurations, idempotency and subsequent user deletion.
`CtrlShortTests` passed 5,231 assertions after this change.

The four missing BB forms were also appended to the installed configuration,
backing up and preserving every original YAML byte. All 14 other configuration
files stayed unchanged. Loading the actual installed engine in a nonresident
probe verified Ctrl and explicit-symbol outputs in all three Roman encodings.
The running resident needs Reload configuration to see the added entries; no
installer or replacement resident was built. Evidence is in
`out/bb-trigger/verification.json`.

## Code-quality pass — 24 October 2026

This pass starts from the unreleased consolidation and BB repair above, rather
than reverting to the published source. It changes organization, formatting and
documentation while preserving executable C# bodies. The accepted 1.9.5 baseline
and existing installer remain unchanged.

- Formatted 85 C# files with a C# 5 parser and executable-token comparison. Added
  purpose comments to modules and suites, and ownership/ordering contracts at
  edit, queue, configuration and indexed-data boundaries.
- Moved existing types into `Program.cs`, `SafeEdit.cs` and `IndexedStore.cs`;
  updated explicit compiler input lists. All 146 type groups retain identical
  executable tokens, literals, member order and partial-declaration contents.
- Made Python tools import-safe, separated their functions from command-line
  entry points, and kept integrity/path guards active under optimized Python.
  Optional image dependencies load only when rendering is requested. PowerShell
  scripts now document their purpose and expose readable compiler argument lists.
- Added formatting conventions, pinned optional developer tools, a contributor
  guide and a code map. The new standard-library Python checks are wired into CI.
  Factory configuration changes are comments only; parsed rows are unchanged.

| Verification | Result |
| --- | --- |
| Complete C# registry | 44 suites; 26,936,571 assertions; zero failures |
| Python developer-tool boundaries | 20 tests passed; network/installer mocked |
| Pre-pass/current conversion comparison | 35,088 behavior records identical |
| PowerShell compatibility | All 8 scripts parse in 5.1 and 7; focused suite runs in 5.1 |
| Single-suite results | JSON remains an array; full 44-suite evidence retained |
| Setup/uninstaller source | Both compile as libraries; neither entry point executed |
| Documentation renderer | Old/new GIF, PNGs and timeline byte-identical in disposable folders |
| Preservation | 52 installed-config, data, image and existing-installer hashes unchanged |
| Repository checks | Development audit, local links, image headers and pinned Hindi sources pass |

Detailed local evidence is retained under `out/code-quality`, including the
pre-pass source snapshot, `verification.json`, `full44-results.json`,
`csharp-body-proof.json`, `differential-verification.json`, `preservation.json`,
`python-results.txt` and `render-parity.json`.

No installer was built, resident replaced, installed setting changed, or native
editor/document controlled. These checks establish source and automated behavior
preservation; they do not measure fresh physical typing latency or native editor
integration. The strict release audit intentionally still rejects source drift;
development mode reports it without refreshing the accepted baseline.

## Responsiveness and shared contracts — 25 October 2026

Implemented the follow-up recommendations without replacing the installed
resident or building an installer.

- Added `tools/benchmark.ps1`, a noninteractive runner, and a checked-in 150 ms
  warm engine p95 budget. Seventeen cases cover the principal conversion paths.
  Startup and first requests are reported separately from warm percentiles. CI
  runs the gate and retains JSON reports.
- Added one shared nine-scenario matrix for six editor-capability profiles to
  the existing `SelectionTests` suite. Both Unicode and Balarama are covered,
  including target boundaries, English/numbers, ignored and delayed selection,
  focus loss, lost selections, continuous cycles and original-spelling retention.
  Production Word revision mapping, InDesign GREP protection, Excel formula
  exclusion and browser paragraph normalization also run in that shared check.
  These are deterministic models, not native editor automation.
- Extracted 14 input lifecycle and replay-acknowledgment methods into
  `src/ResidentInput.cs`. This is a partial of the existing resident, with no new
  state owner, worker, timer or hook. All 152 resident member token sequences
  match the pre-pass snapshot. The 72 field declarations remain in their original
  order in `Runtime.cs`; other production-source files are unchanged.

Verification: all **44 C# suites passed, 26,940,440 assertions, zero failures**.
The shared matrix contributes **3,869 assertions**. The 20 Python developer-tool
tests also pass. A separate before/after comparison produced **35,088 identical
behavior records**. Setup sources compile as a library without executing setup.

The full benchmark measured **301 warm samples per case**, with no budget misses.
The largest observed p95 was **7.5263 ms**, for the longer selected passage.
Engine initialization was **357.6626 ms**, reported separately. This does not
measure complete native-editor latency. A deliberately impossible budget was
rejected with the expected nonzero status; a PowerShell 5.1 benchmark run also
passed. Nine PowerShell scripts parse in both 5.1 and 7, and all 88 C# files pass
the formatter check.

All **52 preservation hashes** and the factory asset/default bytes remain
unchanged. The accepted source baseline is not refreshed. Development audit,
documentation links and image checks pass. No open document was controlled.

Evidence is retained under `out/responsiveness`: `verification.json`,
`full44-results.json`, `benchmark-results.json`, `negative-budget-results.json`,
`queue-proof.json`, `differential-verification.json`, `preservation.json`,
`python-results.txt`, and the pre-pass source snapshot. The separate native smoke
test protocol and measurement limits are in
[Responsiveness verification](RESPONSIVENESS.md).

## Word instance binding fix — 25 October 2026

Ctrl word/short-trigger/scripture conversion and explicit `>` / `<` / `?`
triggers could briefly select their target in Word without replacing it. The
installed resident used a global Word application lookup for all six automation
paths. With multiple Word processes running, that lookup failed to expose the
active document window while the actual editor's native window interface worked.

Added `WordApplication.cs` for window-specific binding through Word's native
object model. The helper verifies the application window and process, uses global
registration only as a verified compatibility fallback, and transfers/releases
each acquired COM reference explicitly. Selection, document identity, expected
text, formatting, replay and undo checks remain in `WordSelection`. Font lookup,
replay anchors, caret anchors and replacement all use the same resolver.

Thirteen targeted suites passed **3,227,798 assertions**, including **182 new
binding assertions**. The new cases cover competing instances, wrong window or
process, missing native interfaces, disconnected objects, safe fallback and
reference ownership. Live read-only metadata probes verified the correct Word
window from both STA and MTA threads. A background window was correctly rejected.
The probes did not read document text, selection or document names.

A temporary patched executable was activated using the existing installed
configuration. The installed executable and all **52 configuration/data/asset/
installer preservation hashes** remain unchanged. Neither Word process was
closed, and no document was controlled through automation. Physical typing
confirmation was subsequently received: `K`, `SP`, `prasada` and `BG1.1` with
Left Ctrl, plus `prasada>`, `SP<` and `SP?`, all converted without leaving their
targets highlighted. No installer was built.

Evidence is retained under `out/word-instance-fix`: `verification.json`,
`combined-results.json`, `ownership-before.json`, `ownership-fixed-active.json`,
`preservation.json`, `activation.json`, and the pre-fix source snapshot.

## Final pre-installer checks — 25 October 2026

The complete 44-suite registry was rerun on the Word-fixed source and passed.
After the requested distribution change, the affected `StandaloneTests` suite
was rerun with 51 new policy/transaction assertions and passed 41,773 assertions.
The latest result for each suite totals **26,940,673 assertions, zero failures**.
All **23 Python tooling tests** also pass.

The engine benchmark passed all **17 cases at 301 warm samples each**, with a
largest observed p95 of **5.0234 ms** against the 150 ms budget. These are engine
measurements, not complete native-editor timings. The user confirmed live
conversion, cycling, Backspace undo and continued typing in scratch text in
**Balarama Word, a browser, InDesign and Excel cell editing**.

The owner explicitly confirmed separate permissions to omit the third-party
notices from the application. KeyJaya's original copyright/MIT notice remains
byte-identical. Original upstream records remain in development sources.
`assets/distribution-exclusions.txt` is shared by staging, ZIP packaging and
backed-up upgrade retirement. Its validators reject user/configuration paths,
runtime dictionaries, traversal, duplicate entries and Windows filename aliases.
Rollback tests restore every original fixture file.

The staged app and review payload contain **38 files**, including all **19
runtime data files** with source-matching hashes; **12 source-only files** are
omitted. The compiled app passed its 19-check nonresident probe. Setup compiles
from its declared 19 sources as a library, without producing or executing an
installer. A separate snapshot exercise of actual configuration migration
preserved all **15 user configuration files**, including the canonical trigger
YAML, byte for byte. All **52 protected input hashes** remain unchanged.

Review also found broken relative links in the installed user guide. The staged
app now includes the first-run, privacy and security help plus the existing tray
image, with all six local help links valid. Repository documentation is unchanged
by this relocation. All 92 C# files pass formatting checks; nine PowerShell
scripts parse in both 5.1 and 7.

Evidence is in `out/pre-release-check-20261025`: `verification.json`,
`full44-results.json`, `latest44-results.json`, `distribution-csharp-results.json`,
`python-results.txt`, `benchmark-results.json`, `package-review.json` and
`profile-preservation.json`. The review ZIP is an internal payload check, not an
installer. The installed executable, live configuration, working documents and
accepted release baseline were not changed, and nothing was published to GitHub.

## Code-review fixes — 26 October 2026

Both P2 findings were reproduced with failing regressions before the fixes. No
finding was rejected. Installer rebuilding and GitHub publication remain held.
The installer produced before this review is superseded and is not evidence for
the revised source.

- Removed BB from automatic upgrade additions. An absent group, including an
  intentionally empty YAML file without `bb-trigger-added.flag`, is left absent.
  Existing custom, disabled and partial groups remain byte-identical. Fresh
  defaults and an explicit trigger-editor restoration still provide all four BB
  forms. The established SBB/SSR addition behavior is unchanged.
- Unified `[replacements]` entries now override identical case-sensitive legacy
  FROM keys in their first legacy position, removing stale duplicate rules. New
  keys append afterward; unrelated rules retain their order. Tests cover the
  reported conflict, cascades/order, empty output, case sensitivity, copied
  conversion and byte-identical input files.
- Added real locked-file failures at every distribution retirement position to
  `StandaloneTests`. Backup reads succeed while deletion fails. Rollback restores
  the exact configuration, existing and newly introduced migration markers, and
  retired files; the recovery backup remains complete.
- Added a setup integration test that compiles the exact production setup
  sources as a test library and invokes `Install(test=true)`. Early, middle and
  final retirement failures, plus failure after all retirements, restore the
  complete original installation inventory. This produced no installer and did
  not register startup, launch a resident or use an installed profile. Both CI
  shell jobs now include this check.
- Added a separate opt-in native Word test, excluded from CI and the 44-suite
  registry. It verifies a new, exclusively owned Word process and scratch
  document before fault injection. It exercises delayed native COM binding,
  actual italic/font preservation, one-step native undo, focus-loss rollback
  after a real patch, disconnected COM cleanup and queue recovery after the
  actual Word window closes. Existing documents are not read or edited.

Verification: the full **44 suites passed, 26,940,779 assertions**, then the final
focused `CtrlShortTests` rerun added five explicit editor-restoration checks.
The latest result per suite totals **26,940,784 assertions; zero failures**.
All **23 Python tooling tests**, **25 setup integration checks** and **32 native
Word checks** passed. All **17 engine benchmarks**, with 301 warm samples each,
passed the 150 ms budget; the largest p95 was **9.348 ms**. Benchmarks measure the
engine, not complete editor latency. All 94 C# files pass formatting checks.

Native coverage is bounded: the Word server delay is 350 ms and uses an independent
resume thread. The elapsed lookup measurement excludes watchdog cleanup. Queued
events are constructed and inspected for FIFO order and paired modifier releases,
not injected into Windows. A permanently hung COM server and end-to-end physical
keystroke replay during a hang remain outside these automated checks. Earlier
physical-key results do not establish those missing cases.

Reproduction commands and safeguards are in [Testing](TESTING.md). Local evidence
is under `out/review-20261026`, `out/setup-transaction` and `out/native-word`.
