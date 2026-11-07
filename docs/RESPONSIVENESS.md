# Responsiveness verification

There are two different measurements: conversion-engine time and the complete
keypress-to-editor result. Measure them separately so a fast dictionary lookup
does not hide slow selection discovery or clipboard delivery.

## Automated engine benchmark

```powershell
./tools/benchmark.ps1 -Check
```

The command compiles a noninteractive runner and copies factory configuration and
data into a new directory beneath `out/benchmarks`. It never starts resident
hooks, opens an editor, touches the clipboard, reads installed settings or builds
an installer. Python is not required.

The 17 cases cover words, English protection, compounds, spelling choices, letters,
short triggers, single verses, paired verse ranges, selections, italic decisions
and plain/ITRANS/Devanagari/Bengali copied conversion. Each case records its first
request separately, warms up, then measures 301 calls by default. The report gives
median, p95, p99 and maximum in milliseconds; p95 uses the nearest-rank method.
Engine initialization and approximate managed-memory growth are separate fields.
Initialization occurs in a fresh process, but the OS file cache is not cleared.

`tests/benchmarks/budgets.json` sets the warm engine p95 budget to 150 ms. `-Check`
fails if any case exceeds it. Without `-Check`, budget misses are reported for
investigation; compilation and correctness errors always fail. CI uses 101 warm
samples per case to limit duration. Reports are retained as CI artifacts.

This is a ceiling, not a claim that every conversion takes 150 ms or that a native
editor meets that target. Compare reports from similar machines and workloads;
inspect first-call and maximum values as well as the percentile. Keep a previous
report if you need to investigate a performance regression. The command creates
a fresh run directory and never overwrites an earlier report.

## Shared editor contracts

```powershell
./test.ps1 -Suite SelectionTests
```

The existing selection suite additionally runs the same nine scenarios through
six capability profiles: Word, browser plain text, browser rich text, InDesign
text, InDesign search fields and Excel cell editing. Profiles vary direct,
delayed, ignored and keyboard-first selection behavior. Every profile receives
the same spelling, preceding-text, focus-loss, lost-selection, timeout, repeated
cycle and original-spelling checks. Both Unicode and Balarama output are covered.

The matrix deliberately uses unambiguous spellings for exact-output assertions.
The separate `raja` cycling checks allow the dictionary's ranked choices instead
of assuming which valid spelling must rank first. No preferred spelling is
written to a user's configuration.

These fixtures exercise production guards and conversion logic with simulated
editor surfaces. They do not launch native applications or prove a particular
version of an application implements its accessibility interface correctly.

## Native smoke test protocol

Use a separate disposable document or field in each application. Preserve the
currently installed settings for a realistic test; record the app, font, encoding,
caret location, paste method and whether Track Changes is enabled when a failure
occurs. Never run these checks in a document that contains working material.

1. Type `Earlier untouched prasada`, then tap the whole-word key once. Only the
   final word should convert. Repeat as the first word and in a later paragraph.
2. Type `raja` and cycle at least six times. Check that cycling continues and
   Backspace restores the original word. Repeat inside existing and pasted text.
3. Select `Earlier untouched prasada and Krsna. 24` and convert. English and the
   number must remain unchanged. Check that the selection stays selected, undo
   restores it once, and subsequent English typing does not inherit italics.
4. Type a supported letter, press the letter key immediately, then rapidly cycle.
   Change to a different letter and repeat. Check for literal backticks, spaces,
   missing input and stuck modifiers.
5. Convert, then immediately type ` next`. Check that every queued keystroke is
   preserved in order. Change focus during another attempt: no input may be
   replayed into a different field or leave a partial conversion.
6. In Word, also test ScaGoudy, mixed Unicode fallback, Paste Options, later
   paragraphs and Track Changes. In InDesign, cover text frames and Text/GREP
   search fields without executing Find/Change. In Excel, use a visible cell-edit
   caret and keep formulas untouched. In browsers, test both plain and rich text.

Record timing separately for first use and subsequent conversions. A full native
latency test must include target discovery, verification, formatting, clipboard
ownership and queued delivery. Do not shorten verification waits solely to make
an engine-only benchmark faster.

## Modern Word comments

The comment focus path fetches UIA properties in batches and tries the live
focus-event editor before the global focused-element provider. It checks current
ancestry and native ownership on every call. The Word snapshot suite includes a
provider model for new comments and replies: 30 successful resolutions each must
make zero global-focus queries, while still checking ancestry each time. It also
covers focus loss, mixed-process ancestry, changed host ownership, body fallback,
missing providers and the 18-node traversal bound.

Modern comment word shortcuts use their own caret/selection discovery directly;
they do not repeat native-body Paste Options preparation. This removes two fixed
40 ms waits and duplicate selection/menu probes. Existing diagnostic timestamps
can distinguish `shortcut comment editor ready`, caret discovery, resolution and
held-input release. Provider-call reductions and removed waits do not establish
a measured end-to-end speedup; record live Word timings separately.

## Word body transaction reuse

### Automated comparison fixture

`tools/test_word_performance.ps1` prepares two implementations of the same
known-output Word adapter workload. An optional saved `WordSelection.cs` replaces
only that source in the baseline lane; everything else is shared. The live
opt-in run uses one exclusively owned scratch document, alternating lane order
for each workload, tracking state and repetition. Setup and correctness/font/undo
verification are outside the timer. Word discovery/selection, native replacement
and owned continuation finalization are timed; the conversion engine/mode
selection, resident dispatch, physical Ctrl, OS input replay, paste UI and modern
comments are not. It is not an end-to-end keyboard-latency benchmark.

The 28 October quiet calibration passed all **112 repetitions / 4,848 native
assertions**, including seven workloads with Track Changes off/on. Both lanes
used identical adapter source. Their small three-sample warm medians still
varied appreciably, so timing ratios from that control run are not improvements.
The report labels identical-source comparisons as calibration. Changes need
repeatable gains on the fixed scenarios with correctness still passing.

On 29 October the full nine-case matrix passed **144 samples / 6,384 assertions**.
A final five-case follow-up passed **80 samples / 3,520 assertions** after a cheap
formatting rejection removed redundant XML exports for styled/mixed-font words.
Word/passage warm medians improved about **16–35%** against a reference sharing
the source-font preservation fix. Each lane has three measured repetitions;
fallbacks still pay cheap metadata reads and are not claimed as universally
faster. These are native editing measurements, not installed-app Ctrl latency.
Full tables, source hashes and exclusions are in
[Development verification](DEVELOPMENT_VERIFICATION.md).

Warmups are reported separately. Failed, missing, duplicated or invalid samples
invalidate the comparison rather than disappearing from the percentiles. Pure
contracts independently check scheduling, statistics, selected-case filtering
and the text oracle. The oracle uses [Word's range XML](https://learn.microsoft.com/en-us/office/vba/api/word.range.wordopenxml)
to check visible fixture text independently of production revision mapping.

The harness finalizes the same owned continuation used by the resident before
examining pending typing style: inspecting revisions can reset that style. Its
edit callback uses the resident's native focus identity; initial scripted setup
must first publish stable document focus. These are test sequencing requirements,
not changes to the production edit safeguards. Desktop input/focus interruptions
abort the live test; isolated preparation can run while the computer is in use.

### Follow-up after installed 1.9.8 — 28 October 2026

The user reported persistent body delays for Ctrl-word, one selected word and
passages, while backtick and new comments remained satisfactory. In an authorized
private diagnostic copy, stage timings showed substantial native Word revision
and formatting work. Nine committed initial native transactions ranged from
141.2 to 302.2 ms; individual revision-count calls were approximately 27–47 ms.
These are transaction-stage samples, excluding discovery and input delivery,
from user-selected operations of different sizes, not a controlled benchmark.

An experiment removed the preliminary revision count while preserving bounded
enumeration and revision-type checks. It moved the delay into enumeration;
nine committed samples ranged from 123.6 to 516.1 ms, with different passage
sizes/formatting and Track Changes state. The user reported no perceived gain.
The experiment was reverted. The native revision projection, paste settling
wait, formatting, Track Changes and undo safeguards remain.

The retained focus change reads each body/classic-comment node's process, type,
ID and native handle together rather than through separate provider requests.
This reduces provider calls with fresh evidence on each check. In the final
temporary copy the user confirmed word, selected-word, passage and immediate
after-paste conversion all worked, but again reported speed about the same.
Four committed native transactions in that final phase ranged from 241.7 to
940.8 ms. Track Changes had been exercised during the trial, operations differed
in size/mode and no controlled timing comparison can be made from these samples.
There is no demonstrated end-to-end speedup from this follow-up. Existing posted
comment editing was separately repaired and confirmed; that is a correctness fix.

`WordNativeTiming` emits bounded diagnostic stage/call summaries through the
existing opt-in logger, without field values, call arguments or document names.
Native IEnumVARIANT calls and Word binding have stage wall time but are not
individual reflection-call samples. Normal startup does not enable tracing.
This instrumentation does not replace user-observed latency or change the
engine-only benchmark's scope.

A Ctrl-word request reuses its own completed paste-UI preparation and selection
observation when it enters word discovery. The evidence is scoped to the same
foreground owner and operation generation; selected text is verified before
handoff. This removes one repeated selection read, one 40 ms settling wait and
the duplicate paste-menu preparation. The final focus, modifiers, expected text,
native range and document checks still run. Comments retain their separate path.

Word body word and passage conversions also capture replay ownership from the
native transaction's already-verified document and visible range. They no longer
bind to Word and enumerate the same selection's revisions in a separate preflight.
An existing owned font snapshot supplies the original continuation style; other
selections read only their final character's style. Font restoration reuses the
transaction's verified result projection while preserving each character's font
and skipping tracked deletions. Italic writes are omitted when the live native
font already has the requested style; mixed or different states are still set.
No document or range proof is cached across edits.

`WordSnapshotTests` runs these production helpers against a fake range whose
unexpected text/revision reads fail. It checks stale ownership, tracked native
coordinates, provider failures, cancellation and a single native undo record.
These checks prove reduced provider work, not a measured native speedup. Compare
user-led scratch checks and accepted-action-to-release diagnostics separately.

## Word reliability and focus profiling — 28 October 2026

The unreleased path rejects a body provider as a modern-comment candidate after
one property batch, without walking its ancestry. Paste-menu traversal stops at
the document but retains the independent native popup scan. Native Word letter
selection performs its existing document/story/text/caret/selection proof in one
binding, then keeps the final live commit check. Its request reuses the discovered
target; Ctrl-word styling reuses one context read. No proof crosses requests.

With permission, `tools/profile_word_focus.ps1` compared 12 metadata-only
`DocumentFocused` calls against the retained 1.9.7 library in a disposable body:

| Focus check | First | Median | Maximum |
| --- | ---: | ---: | ---: |
| 1.9.7 baseline | 24.829 ms | 8.663 ms | 24.829 ms |
| Corrected development path | 19.087 ms | 3.508 ms | 61.123 ms |

These small samples include an outlier and are not end-to-end conversion timings
or a latency guarantee. The user subsequently reported all body conversion types
working with acceptable speed, and both comments and replies working. The final
request-local styling-context reuse was checked automatically after that trial.

Keyboard hooks now run on a dedicated message thread. Word focus/paste queries
and clipboard/OLE reads run outside its state gate. This addresses hook starvation
as well as visible waits: Windows may silently remove a timed-out low-level hook.
See [Microsoft's hook guidance](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc).
The isolated queue fixture proves dispatch while a caller waits; user-led checks
confirm input works in the tested copy, without establishing indefinite uptime.

## Excel provider work

The Excel mirror's current identity and TextPattern are fetched together with a
per-call CacheRequest. Cached metadata is refreshed on each read; it does not
authorize a later edit. For a collapsed caret, the adapter proves endpoint
equality and reads the prefix once, avoiding a second end-prefix read and a
selected-text query. Selected ranges still prove both offsets and exact text.
`ExcelCellTests` exercises the production read decision with callbacks that fail
if a collapsed caret requests those unnecessary reads. The provider deadlines,
single outstanding request, epoch, current editor and formula guards remain.
This is a provider-call reduction, not a newly measured live Excel latency.

Excel word discovery also resolves a selection or caret from one capture;
clipboard discovery resolves a selection or insertion from one capture. Those
operations previously captured the same field twice. Replacement reuses the
full-buffer/range proof already performed by EnsureEdit rather than repeating
it immediately beforehand, and keeps the final pre-input check.

Relative selection now chooses the nearest verified selection edge for
single-line cells too. A nearby-word fixture in a 3,606-character cell uses 10
navigation events instead of the previous Home/End plan's 3,614. Complex
characters elsewhere in the cell are not traversed or counted. Status-mode
identity, labels and ValuePattern data also use a fresh batched UIA request.
These changes reduce generated input and provider calls; the mixed manual
selection/undo checks do not establish a controlled native speed comparison.

The 26 October full verification ran all 17 engine cases with 301 warm samples
each and no budget failures. The highest warm p95 was 11.293 ms for the long
selected passage; engine initialization was 209.905 ms. These remain engine
measurements, separate from Excel's provider and native editing time.

In diagnostic mode, `manual input hold armed editor=XLMAIN` identifies an Excel
manual action. Pairing it with `manual conversion released empty input hold`
measures accepted-action to empty-hold release. This also occurs on skipped
conversions, so provider/selection failures and the observed result must be
checked before treating it as a successful-conversion sample. Samples with
queued typing, cancellation, focus loss or another manual action are reported
separately. The existing 15-minute/2,000-entry diagnostic limit still applies;
an absent record is not a zero-millisecond conversion. No text is needed to
summarize these timing messages.
