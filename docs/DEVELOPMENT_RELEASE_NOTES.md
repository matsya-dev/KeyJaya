# KeyJaya 1.9.10 release notes

Changes since **1.9.9**, prepared on **1 November 2026**.
Windows x64 with .NET Framework 4.8. The installer is unsigned.

This GitHub release also includes the 1.9.8 and 1.9.9 changes documented below;
the previous published GitHub release was 1.9.7.

- Word comment replies can be found in the verified Word frame when the native
  editing host omits them. Exact editor/runtime identity, native host, live focus
  and ownership remain required. Recovery gives startup and in-flight discovery
  a bounded grace instead of retiring the helper before a valid lookup finishes;
  the caller still waits at most 150 ms. Cached stale providers can be rediscovered.
- Browser clipboard backup retries ambiguous empty OLE views only while the
  original sequence remains unchanged. Genuine format-free restoration uses an
  ownership-checked native clear. Unsupported data, newer clipboard items and
  crashed/hung providers retain their refusal boundaries. Metadata diagnostics
  distinguish refusal causes without recording clipboard contents.
- User-led checks confirm repeated replies in two Word threads, new comments,
  existing-comment edits, Word body and Zen conversions. Focused regressions and
  compiled-app checks pass. The final frozen run passes all 44 isolated suites /
  26,999,576 assertions with zero failures. These
  checks are not a long-term stability or every-editor guarantee.
- The approved 1 November automated session passes two complete comment runs
  without restarting their resident, private helper recovery and all nine body
  fixtures with tracking off/on: **66 comment actions, 144 body samples and
  8,238 recorded native checks**. Formatting, undo, continuation and clipboard/
  installed-settings preservation pass. Same-source timings do not establish
  a speed improvement; physical-hook evidence remains separate.

The versioned build passes all **44 isolated suites / 26,999,576 assertions**,
the strict source/data audit, compiled-app checks and engine budgets. Disposable
clean-install, edited-trigger upgrade, exact 1.9.9 upgrade and complete rollback
checks pass, preserving configuration, custom triggers and learned spelling
preferences. All 38 packaged file hashes and the embedded installer payload are
verified. The compiled normal registration path assigns Windows' version label
to 1.9.10; isolated test mode deliberately leaves live registration untouched.

The user's local installation of 1.9.10 passed its smoke check, with existing
configuration preserved. Detailed results and limits are recorded in
[Verification](VERIFICATION.md) and
[Development verification](DEVELOPMENT_VERIFICATION.md). There is no new general
Word speed claim or extended normal-use stability guarantee.

# KeyJaya 1.9.9 release notes

Changes since **1.9.8**, prepared on **30 October 2026**.
KeyJaya supports Windows x64 with .NET Framework 4.8. The installer is prepared
locally; publication and updating the installed app are separate actions.

## Word editing and search

- Added conversion in Word's docked Ctrl+F Navigation search box: caret and
  selected-word conversion, cycling, backtick letters, Backspace restoration
  and selected-query Ctrl+Z. Native query coordinates and live field identity
  keep these edits separate from the document body. Auto encoding is Unicode;
  explicit legacy overrides remain available. Control characters and Unicode
  line/paragraph separators are refused; normal Unicode spacing stays supported.
- Added recognition of previously posted modern-comment editors. New comments,
  existing-comment edits and replies retain owned, live, writable-editor checks.
  Supervised helper refresh/startup catch-up can rediscover the current comment
  from its verified native host even when Word's global focus still reports Body.
  The simulated silent-helper case recovers without restarting KeyJaya or
  refocusing the comment; this is not a claim about every possible provider fault.
- Reduced repeated Word context/revision calls. Native revision batches retain
  the count, type/deletion filtering, partial/error checks and 10,000-item limit;
  managed-only providers retain their established scan. Consecutive changed
  Latin letters are grouped only after a fresh plain-run formatting proof before
  the undo record starts. Styled, mixed-font and unsupported ranges keep the
  existing per-letter path. Source fonts are restored for one-letter changes too.
- Retained Track Changes, original fonts/styles, automatic italics, selection/
  caret, conversion undo and earlier undo history. Timing improvements observed
  in controlled adapter comparisons are not guarantees for every Word operation.

## Reliability

- Foreign OLE clipboard backups are read in a bounded disposable STA child and
  transported as explicitly typed data, without CLR-object deserialization.
  Snapshot acceptance requires successful child exit and the original clipboard
  sequence. A failed or hung provider is not repeatedly retried for the same
  unchanged sequence. Unsupported data safely declines the capture.
- Kept provider/OLE work and callback-origin file logging off the keyboard hook's
  state gate/thread. Own-process dialogs now bypass caption/class readers before
  they can wait for KeyJaya's UI thread. External cheat-sheet detection retains
  its caption/class proof, and ordinary typing in resident dialogs stays literal.
- Focus notices remain ordered by helper session and event revision. Retired or
  late observations cannot authorize an edit. Clipboard cycles still require
  a stable sequence, and queued input keeps its target/generation checks.

## Recognized spellings

- Added only `devanagari` → `devanāgarī`, preserving capitalization, legacy output,
  cycling and personal overrides under the existing formatting policy. Bengali,
  Tamil and other language/script names were not added or changed.

## Verification and limits

The approved native body matrix passed all nine fixtures with tracking off/on:
144 samples and 6,384 checks. All 33 comment actions across six editors passed
886 checks. The final silent-helper recovery, conversion and native undo check
passed 83 checks. Scratch windows closed and installed files/settings/startup
were verified unchanged.

The automated fixture now separates its setup typing from conversion undo with
a guarded caret boundary, which recovery deliberately omits. Recovery log reads
allow concurrent append, use complete bounded records and one helper identity.
Full isolated regression, compatibility, engine-budget and audit results are
recorded in [Verification](VERIFICATION.md). Detailed investigation history and
failed/interrupted attempts remain in [Development verification](DEVELOPMENT_VERIFICATION.md).
These checks do not establish a new general Word keyboard-latency improvement.

The 1.9.9 installer passes all **44 isolated suites / 26,999,534 assertions**,
the strict source/data audit, compiled-app and engine-budget checks. All 38
packaged files and the embedded payload are verified by SHA-256. Disposable
clean-install, edited-trigger upgrade, 1.9.8 upgrade and complete failure rollback
checks pass, preserving settings, custom triggers and learned spelling preferences.
The installer is unsigned. Installed 1.9.8 remains unchanged; publication and
updating that installation are separate actions.

# KeyJaya 1.9.8 release notes

Changes since **1.9.7**, prepared on **28 October 2026**.
KeyJaya supports Windows x64 with .NET Framework 4.8.

## Acrobat search fields

- Ctrl-word conversion works at the caret in both **Ctrl+F Find** and
  **Advanced Search**, including repeated cycling and continued typing.
- Verified native Edit coordinates handle Acrobat's empty accessibility ranges.
  Selection, letter and restoration discovery use the same guarded path. PDF
  page text and Acrobat comments retain their existing editor paths.

## Reliability

- Word focus events run in an isolated helper, protecting the resident from the
  diagnosed Framework accessibility-tracker crash. A failed helper loses its
  cached focus evidence and restarts with bounded retries.
- Keyboard/mouse hooks use a dedicated message thread. Unexpected message-loop
  exits recover, slow provider/clipboard work leaves the input state lock, and
  failed startup cleans up acquired hooks and its helper.
- Helper readiness precedes focus notices; older event completions cannot
  replace newer evidence. A healthy minute resets its retry streak. Clipboard
  cycling requires a stable read and current sequence.

## Word conversions

- Fixed Ctrl conversion of exactly one highlighted word.
- Reduced repeated body/comment/menu checks, native letter bindings and styling
  context reads for word, passage and backtick conversion.
- Word comments/replies, formatting, native undo, input replay and the reviewed
  1.9.7 formatting/undo safeguards retain their established contracts. No factory
  trigger, spelling default or runtime dictionary changes are included.

## Verification

All **44 isolated suites passed: 26,998,668 assertions, zero failures**. The
compiled app, C# 5 compatibility, strict source/data audit and engine benchmarks
passed. All 38 packaged files and the embedded installer payload were verified
by SHA-256. Disposable installation tests covered clean installation, edited
triggers, upgrading from the exact published 1.9.7 installer and complete failure
rollback, including preservation of settings and learned spelling preferences.

An approximately 89-minute user-led temporary trial of the reviewed behavior
reported everything working fine, with no helper or hook failure/retry in its log.
The original installed 1.9.7 remains unchanged; installer tests did not stop it.

Installer, upgrade and rollback evidence is recorded in
[Release verification](VERIFICATION.md) and
[Development verification](DEVELOPMENT_VERIFICATION.md). The installer is unsigned.
Publishing and updating the live installation remain separate actions.

# KeyJaya 1.9.7 release notes

Changes since **1.9.6**, prepared for release on **27 October 2026**.
KeyJaya supports Windows x64 with .NET Framework 4.8.

## Word comments and navigation

- Word conversions work in the Comments panel and contextual comment/reply
  bubbles. Comment text keeps its current formatting without automatic italics.
- Ctrl+Shift+Left navigation no longer waits for Word font-reference cleanup
  inside KeyJaya's keyboard hook.
- Word conversion avoids duplicate paste-UI preparation, application binding and
  revision scans. Already-correct italic/regular formatting avoids another write.
- Ctrl immediately after pasting retains selection checks and paste-popup
  recovery. Native formatting and conversion remain grouped for one-step undo.

## Ctrl-word italics

- With **Auto-italicize conversions** enabled, the first Ctrl tap italicizes an
  already-correct recognized term unchanged, including `bhakti`, `nitya`, `dvija`
  and plural forms such as `dvijas`.
- Individual words, including exactly one selected word, can italicize loanwords
  such as `dharma`, `guru`, `yoga` and `karma`, including their plurals.
- Multiword passages make those loanword exceptions regular even if they were
  already italic. Other eligible Sanskrit/Bengali terms become italic; proper
  names retain their regular-type policy and personal style rules remain supported.
- Complete Sanskrit compounds such as `bhakti-yoga`, `nitya-dharma` and
  `guru-sevā` are treated as one term, including in passages.
- Further Ctrl taps cycle spelling alternatives and eventually restore the
  original text and formatting. Terms without alternatives cycle between italic
  and their original formatting. English protection remains.
- Supported rich-text and Markdown paths apply these rules. Word comments,
  text-only Excel editing, plain-text fields and script output retain their
  existing formatting limits.

## Excel editing

- Word and letter conversion work on later lines of a multiline cell. Selected
  passages can span LF/CRLF line breaks, remain selected after conversion and
  retain one-step undo.
- Discovery uses fewer accessibility-provider reads. Relative selection starts
  from the nearest verified edge, reducing navigation in long cells.
- Emoji or combining accents elsewhere in a cell can remain untouched while a
  nearby word converts when the navigation path can avoid those characters.
- The Formula Bar must remain visible. English Edit/Enter mode labels, the
  4,096-character bound and formula-like-buffer exclusions still apply. Unverified
  cursor paths through complex character clusters can be skipped.

## Interface and configuration

- **Check Word conflicts…** is available inside **Customize shortcuts**. Its
  bounded, read-only helper reports assignments and navigation overlaps from
  already-open Word instances without changing Word or its documents.
- The standalone shortcut-overlap tray action and development spelling/style
  editor were removed as requested. Existing settings and spelling/style
  configuration remain supported; resident settings windows keep ordinary input.

## Review fixes — 27 October 2026

- Markdown conversion preserves literal identifiers such as `note_dharma_id`
  and validates emphasis boundaries before removing existing italics. Escaped
  markers and punctuation remain text rather than becoming stray formatting.
- Original Word formatting is captured for one-word selections with surrounding
  whitespace. An active Ctrl cycle retains its formatting evidence through a
  pause; Backspace keeps its separate immediate-undo deadline.
- Formatting-only conversions arm Backspace undo in supported rich editors and
  InDesign. Restoration uses the saved original italic state or native font
  snapshot instead of treating an unchanged spelling as an untouched word.
- URLs, email addresses, file paths and protected literal comments are excluded
  from both italic and regular-type spans. Eligible terms around them retain
  the individual-word versus passage rules.

## Verification

All 44 functional regression suites passed, followed by five focused suites for
the final boundary cases. The compiled 1.9.7 application, strict source/data audit,
developer-tool tests and engine benchmarks passed. The 38 packaged files and
embedded payload were verified by SHA-256.

Disposable installation tests covered a clean install, edited-trigger upgrade,
upgrade from the exact published 1.9.6 installer and complete failure rollback.
Existing configuration and state were preserved, and the upgraded converter
honored synthetic custom triggers, spellings, style rules and shortcuts. No live
installation or working document was used for these tests.

Earlier user-led scratch checks confirmed Word comments, Ctrl italics, faster
conversion, native undo and multiline Excel editing. Review-specific padded Word
and rich-editor Backspace cases have automated helper/model coverage. These checks
do not establish every editor or Office version. The installer is unsigned.
Details are in [Release verification](VERIFICATION.md) and
[Development verification](DEVELOPMENT_VERIFICATION.md); usage is in the
[User guide](USER_GUIDE.md).
