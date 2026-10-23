# Excel cell-editing investigation

Development checks on 23 October 2026. No installer was built or release
published. Windows input was confined to a newly created, disposable `Book4`;
existing workbooks were not edited or closed. Formula Bar was enabled for testing.

## Findings and changes

- Excel's native in-cell editor does not expose the text interface used by most
  applications. Its visible Formula Bar provides a mirror of the active edit
  buffer and selection.
- Descendant discovery could walk Excel's virtual worksheet grid. Formula Bar
  discovery now examines immediate children only and accepts the owned edit
  control without depending on one Office-version-specific class name.
- Provider reads have deadlines and allow at most one outstanding read-only
  request. A late result cannot authorize a later conversion. Discovery and
  Excel selected-text replacement run away from the resident's keyboard-hook
  thread.
- Excel's Enter and Edit modes treat navigation differently. KeyJaya checks the
  owned status bar, switches Enter to Edit with F2 only when necessary, then
  verifies the exact buffer and selection again before replacement. It avoids
  spreadsheet Ctrl+Home/End commands.
- Duplicate focus notifications for the same owned editor no longer invalidate
  the conversion. Real focus changes and physical edits still invalidate it.
- Failed, untouched discovery can release queued typing without requiring the
  unavailable provider again. An empty queue can be released after focus moves.
- Cancellation leaves an already-restored caret alone. Formula-like buffers,
  multiline/control-character buffers, and buffers above 4,096 characters are
  conservatively excluded. Replacement cannot turn a text buffer into a formula.

## Automated evidence

Eight relevant suites passed, with **1,527,537 assertions and zero failures**:

| Suite | Assertions |
| --- | ---: |
| ExcelCellTests | 209 |
| CorrectionRaceTests | 53,283 |
| NativeKeyTests | 816,515 |
| SelectionTests | 594,402 |
| ShortcutSettingsTests | 18 |
| ReplayTests | 43,026 |
| ResidentReplacementTests | 9 |
| SelectionRefinementTests | 20,075 |

Coverage includes stalled/failing/concurrent providers, discarding late results,
typing and modifier replay, exact mode-label recognition, formula rejection,
every navigation interval in buffers of length 0–80, and 10,000 longer random
intervals with and without Ctrl navigation. Native selection fixtures include
ignored/asynchronous selection, stale focus, and changed surrounding text.

Combined evidence is recorded in `out/tests/excel-debug-verification.json`.
These are targeted development results, not a claim that all 44 registered
suites or the release baseline were rerun for this investigation.

## Live scratch-workbook evidence

The normal runtime actions were exercised through temporary F8/F9 mappings,
because KeyJaya deliberately ignores software-injected typing and Ctrl taps.
Observed results included:

- Whole-word conversion and repeated spelling cycles in an existing cell.
- Single-letter conversion and cycling in both existing and freshly entered
  cells, including `a → ā → a` without committing the cell.
- Selected passage conversion with English unchanged and the converted passage
  remaining selected.
- No recurrence of the permanent input freeze during these checks.

The normal backtick and Left Ctrl mappings were restored. The user confirmed
letter cycling, repeated whole-word conversion, selected `Ctrl+.` conversion,
and normal subsequent typing/Shift behavior in the scrap workbook. Native undo
and fast queued typing remain separate live checks.

Ctrl+Backspace was subsequently checked with KeyJaya stopped: Excel itself left
the test text unchanged, including when a word was selected. Ctrl+Shift+Left
followed by ordinary Backspace did delete the selected test word. The user
requested leaving Ctrl+Backspace alone, so no compatibility remapping is included.

Warm letter-action timings during diagnosis were approximately 235–290 ms;
initial provider discovery was slower. The under-150-ms target is not yet met.
Formula Bar must currently remain visible. Unavailable or ambiguous targets are
skipped rather than edited using guessed ranges.

## Subsequent regression round

A full run of all 44 registered suites passed with 25,978,238 assertions and
zero failures. Results are retained in `out/tests/round2-full-verification.json`.
The caret-position changes received targeted reruns afterward; Formula Bar
routing was subsequently checked with five affected suites (622,292 assertions,
zero failures). These are development checks, not a refreshed release baseline.

Live testing used a new disposable `Book5`, because Book4 was no longer open.
It verified native one-step undo/redo, continued typing after a word's final
period, clipboard preservation, letter cycles, and selected round trips through
Unicode, Balarama, Devanagari, Bengali, ITRANS, and plain Roman text. The selected
text stayed selected. A merely selected formula cell remained unchanged.

Two additional defects were corrected:

- A converted word could leave the caret before its trailing punctuation.
  Caret placement now includes only the exact verified punctuation tail and
  cannot advance into a following word.
- Excel's Formula Bar used the generic editor path, allowing script shortcuts
  to change digits inside a formula. Its observed `EXCEL<` native editor now
  uses the same bounded adapter and formula guard as `EXCEL6`. Both selected
  script conversion and whole-word conversion then left `=1+2` unchanged.

A fresh Word document also passed selected Balarama conversion, selective
italics, one-step undo, and regular native typing/Shift after an italicized word.
Browser live checks were interrupted and remain pending. Warm Excel letter
actions in this round took approximately 200–240 ms; the 150-ms target is still
unmet, and Word formatting calls were substantially slower.

## Input-queue recovery follow-up

Subsequent normal use exposed a global conversion stall. Diagnostics showed
repeated attempts awaiting an owned caret, followed by unavailable Ctrl-tap
input holds and an automatic queue reserved for an older editor.

- Replayed keyboard and mouse events now acknowledge their in-flight delivery
  in the hook's completion path. A replayed shortcut can start the next
  conversion without leaving its own delivered event permanently outstanding.
  Deferred events retain their order and are not acknowledged as delivered.
- Closing a queue's original target now resets both manual and automatic
  transaction metadata before another editor acquires the queue. Automatic
  correction cannot inherit an abandoned manual target or exhausted retry state.
- An empty manual queue is released even when a Word continuation record exists;
  that record is finished without requiring another caret-provider read.

Five relevant suites passed after these changes: CorrectionRaceTests (103,312),
ReplayTests (43,026), ExcelCellTests (1,205), SelectionTests (594,402), and
ResidentReplacementTests (9): **741,954 assertions, zero failures**. The new cases
include 10,000 replay-to-next-conversion transitions and the resident's actual
manual/automatic closed-target recovery paths, without starting application
hooks or injecting input into user applications. Results are retained in
`out/tests/input-queue-recovery-verification.json`.

The corrected isolated resident was activated with normal shortcuts. No installer
was built. Physical confirmation of this final queue fix remains a user check.

## Browser rich-text follow-up

The user confirmed first-tap conversion after queue recovery. Subsequent rich-text
cycling exposed a separate lookup failure: after the first italicized paste,
UIA character-unit movement produced a range whose text length differed from
the independently located word. Later checks confirmed other editors continued
working while that rich-text box failed.

The bounded fallback now reads a contiguous context, preserving implicit editor
separators that separate left/right text reads can omit. It searches for exact
text and verifies the text on both sides of the actual collapsed caret instead
of trusting character-unit offsets. Candidate checks exclude neighboring words,
headings, changed punctuation, and repeated occurrences at another location.

Because recovering a generic range alone did not resolve the live cycling case,
repeated browser cycles now first re-find the prior committed spelling at its
original verified field and prefix. The current caret must lie within that exact
spelling. This uses the previous location only to find and prove the current
range; it does not authorize an edit from cached text alone. A dispatched rich
paste is also tracked during verification so recovery does not try to restore
the original, already-replaced spelling.

The final targeted run passed CompoundCaretTests (2,637), SelectionAnchorTests
(20), FontCyclingTests (486,654), CorrectionRaceTests (103,312), and SelectionTests
(594,402): **1,187,025 assertions, zero failures**. Evidence is retained in
`out/tests/rich-cycle-anchor-verification.json`. These logic checks do not prove
the browser provider's behavior. The isolated candidate is running with normal
shortcuts. The user confirmed that freshly typed `raja` continued cycling through
four Ctrl taps in the rich-text test box with the final anchored lookup. This
confirms that reported scenario; it is not a claim of universal editor coverage.

## Ctrl scripture transport and repeated-tap recovery

The user subsequently reported that `BG2.1-3` failed in ChatGPT, browsers, and
WhatsApp, while Word continued working. A focused diagnostic confirmed that the
reference was recognized and its complete output prepared. The generic caret
replacement path nevertheless sent unformatted output through `Native.TypeText`,
which accepts only short single-line text. Even a single verse contains a line
break; ranges can also exceed the direct-input length limit. Word's native
replacement path did not have this restriction.

Generic caret replacement now uses a shared transport decision: short plain
words keep direct input, while multiline, long, or formatted replacements use
the clipboard-preserving paste path. Clipboard restoration runs in a `finally`
block after paste, including when verification is interrupted or throws.

The stall log also showed an input hold reserved after repeated failed attempts.
An unchanged failed discovery can now retire complete buffered bare-Ctrl taps.
This is limited to balanced undelivered Ctrl down/up pairs: typing, other
shortcuts, mouse events, unmatched releases, and in-flight events remain
protected. The actual resident recovery path is covered without starting hooks
or typing into user applications.

Eight relevant suites passed across the targeted runs, including the 36,684
corpus-range checks. Combined evidence is in
`out/tests/verse-ctrl-verification.json`. The user confirmed that the final
temporary candidate inserted both `BG2.1` and `BG2.1-3` in ChatGPT with Left Ctrl,
and that subsequent `Krsna` conversion still worked. Browser and WhatsApp share
the corrected transport, but this round's physical confirmation was in ChatGPT.
No installer was built, and existing documents were not automated or edited.

Immediate typing after a range exposed a further verification delay. Browser
providers can decline a multiline `FindText`, expose paragraph separators
differently, and give logically equivalent caret endpoints different UIA
coordinates. The first endpoint-based follow-up still waited several seconds;
it was not accepted as a successful responsiveness fix.

The final verifier locates the first and last lines, proves the original prefix
and complete intervening content, and checks the content on both sides of the
current collapsed caret before requesting any caret movement. Only paragraph
separator representations are normalized for this proof; missing or reordered
lines, changed words, diacritics, punctuation, or ordinary spaces are rejected.
This normalization does not modify inserted text. SelectionAnchorTests now has
78 checks covering these cases, including caret positions that must be refused.

The final live `BG2.1-3` → Left Ctrl → immediately type ` next` check passed.
Diagnostics confirmed complete paste verification, clipboard restoration, and
queued typing release without the repeated caret-recovery timeout. The user
confirmed the long delay was gone. This is not evidence that the separate
under-150-ms performance target has been met.

## Full automated run and browser field boundaries

All 44 registered suites passed on 23 October 2026: **26,029,762 assertions,
zero failures**. The source hashes and complete results are retained in
`out/tests/full-44-verification-2026-10-23.json`. Configuration validation passed
for the temporary and installed app, including eight shortcut actions, trigger
YAML, encoding, script/style/word overrides, and font mappings. All 39 configuration
files snapshotted before/after the checks remained byte-identical. Documentation
checks passed for 21 local links and four image headers. No installer was built.

During the run, the user reported a distinct positional browser defect: the
first `raja` in either local test field did not convert; the second rich-text
word converted once without cycling; later words worked. Diagnostics showed
the field's word being combined with surrounding page labels. UIA caret context
is now bounded by the provider's paragraph structure before word discovery.
Five affected suites passed after that change, recorded in
`out/tests/browser-boundary-targeted-2026-10-23.json`. The user confirmed that
first-word conversion/cycling now worked in both local fields and that the
second rich-text word also cycled. The 44-suite result predates this last change;
it must not be represented as an exact final-source rerun.

## Deeper data audit finding: scripture record identifiers

All 30 bundled-file hashes and source/temporary-pack inventories passed. All
seven index stores were then inspected record by record for sorted hashes,
record bounds, UTF-8, key separators and matching key hashes; lookup hash tables
were checked for size/order. Six stores passed. Scripture contained **276
identifier/hash mismatches**, including duplicate identifiers for different
payloads. The original complete data audit failed despite passing manifests
and automated conversion suites. The original failure is retained in
`out/tests/pre-repair-data-audit-2026-10-23.json`.

A read-only repair preview recovers one matching identifier per affected index
hash, within the same book/chapter/encoding and with unchanged identifier byte
length. Proposed repairs yield 226,032 unique record keys and preserve every
verse/translation payload byte and all index bytes. Independent samples agree
with [SB 1.4.7](https://vedabase.io/en/library/sb/1/4/7/) and
[CC Adi 4.79](https://vedabase.io/en/library/cc/adi/4/79/). Full proposed changes
are in `out/tests/scripture-header-repair-preview-2026-10-23.json`.

Automatic approval review initially rejected applying the data repair because
that request authorized auditing rather than rewriting bundled book data. No
runtime data or manifest was changed until the user subsequently gave explicit
authorization for the 276 identifier repairs.

## Approved scripture identifier repair

The user explicitly approved the repair on 23 October 2026, requiring all verse
and translation text to stay unchanged and no installer to be built. Exactly
276 identifier bytes were changed in `scripture.dat`, restoring the approved
record keys to the existing index hashes. No index bytes, record offsets,
lengths, or file size changed. Only the scripture-data hash was updated in the
manifest, preserving the manifest's other bytes.

An independent disk-to-backup comparison checked all 226,032 text payloads:
**34,825,664 verse/translation bytes unchanged**. The other 29 runtime files also
remain unchanged. Evidence is retained in
`out/tests/scripture-header-repair-2026-10-23.json` and
`out/tests/scripture-payload-preservation-2026-10-23.json`; the original data and
manifest are backed up outside the shipped tree under `out/diagnostics/`.

The data audit now passes every record in all seven stores with zero identifier
hash mismatches. The scripture suite now rejects identifier/hash disagreement,
duplicate record keys, unsorted indexes, and invalid bounds. Its fixture for all
276 repaired identifiers verifies actual indexed lookups, original-payload
SHA-256 hashes, and Ctrl expansion in their respective encodings.

The initial affected rerun passed ScriptureRangeTests (1,974,586 assertions) and
CtrlShortTests (5,193), including 36,731 corpus ranges, 1,134 formatting cases,
and 4,023 shared-translation cases. The fresh full run after the repair and the
browser boundary fix passed **all 44 suites: 26,935,872 assertions, zero
failures**. Results and source hashes are in
`out/tests/scripture-repair-full44-2026-10-23.json` and
`out/tests/scripture-repair-full44-summary-2026-10-23.json`.

The existing temporary resident was restarted with only the repaired scripture
data and updated manifest copied into its data folder. Final verification found
the source and temporary packs identical across all 30 runtime files, zero
mismatches in all 2,734,965 data records, and all 39 configuration snapshots
unchanged after the restart. The installed release was not refreshed. No
installer was built, and no open user document or workbook was automated.
