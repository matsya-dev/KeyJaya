# Testing KeyJaya

Build/test runners now use [owned scratch cleanup](OUTPUT_CLEANUP.md). Successful
suites remove their copied dictionaries and neighboring test artifacts; failures
keep logs and a compact verified snapshot before cleanup. Reports are bounded,
and `-KeepArtifacts` explicitly preserves a full fixture. Saved Office documents,
live trial profiles, installed settings and unmarked historical output are not
swept. The new cleanup regressions use synthetic roots in both Windows shells,
including junction refusal, interrupted runs and last-good-build rollback.

The 1 November versioned **1.9.10** candidate passed all **44 isolated suites /
26,999,576 assertions**, zero failures. Compiled-app, strict source/data audit,
C# 5, Python, pinned-source, engine-budget and disposable installer clean/upgrade/
rollback checks pass. Its runtime differs from the native-tested source only in
version identity. The full results and packaging limits are recorded in
[Verification](VERIFICATION.md).

The 7 November helper revision adds nine deterministic parent-pipe checks:
startup preambles and payload do not end the lease, while EOF releases it.
A one-byte-watcher mutation fails. The focus and native Edit fixtures also
exercise UTF-8 BOM command input even on ANSI hosts, and decode that input
without losing the first command. The focused pair passes **818,202 assertions**;
the full tagged-source matrix runs in both Windows PowerShell and PowerShell 7
on GitHub. These tests use in-memory pipes, private processes and hidden owned
controls, with no live Word or clipboard access.

The authorized 1 November native session passes two full comment matrices with
the same resident (**66 actions / 1,774 checks**), the private silent-helper
recovery (**80 checks**) and all nine body fixtures with tracking off/on
(**144 samples / 6,384 checks**). All **8,238 recorded native checks** pass.
Installed files/preferences/registration and the clipboard fingerprint remain
unchanged. See [Verification](VERIFICATION.md) for scope, ownership and keyboard
hook/calibration limits. These live checks use separate scratch content and
require explicit approval; the preparation command alone never starts Word.

The unreleased 31 October reply/browser investigation adds regressions for
transient locator failure, delayed-success recovery cancellation, stale-candidate
invalidation and point lookup independent of an unavailable legacy host root.
Clipboard contracts cover ambiguous empty OLE views with unchanged/changed
sequences, explicit refusal codes and ownership-checked format-free restoration.
These use injected providers and private pipe children, without Word or the
system clipboard. Frame fallback contracts cover an omitted/unavailable native
provider, lazy discovery and lost focus/ownership. The late-success pipe fixture
uses a 1.1-second provider read and requires the same helper to survive; a private
short-delay mutation fails. There are now 163 comment-focus, 92 focus-monitor and
63 clipboard-snapshot contracts. The latest authorized manual check passes
repeated replies without the helper refresh loop. See
[Development verification](DEVELOPMENT_VERIFICATION.md) for full-run evidence and
scope limits. The release results below describe accepted 1.9.9, not the current
development candidate.

The final frozen development candidate passes **44 isolated suites / 26,999,576
assertions**, with no failures and unchanged runtime/test input hashes throughout
the run. The accepted 1.9.9 source baseline is deliberately not refreshed.

The final release-readiness run passes all **44 isolated suites / 26,999,534
assertions** with `/langversion:5` explicitly set. `WordSearchFieldTests` now has
108 checks, including C1 controls, Unicode line/paragraph separators, retained
Unicode spaces and unchanged non-query output. `TriggerEditorTests` uses throwing
property readers to prove own-process bypass never waits for caption/class data;
foreign cheat-sheet classification retains its proof. Both regressions fail
before their respective fixes. No additional live editor test is involved.

The preparation-only `tools/prepare_word_verification.ps1` builds and self-tests
the full existing body, comment and silent-helper recovery matrix without
desktop access, profile reads or a trial restart. It records input/binary hashes
under `out/word-full-verification` and has no live-execution switch. See the
[Word verification plan](WORD_VERIFICATION_PLAN.md) for coverage and the separate
live-test gates. The comment runner now requires all declared view/action cases
exactly once and distinguishes existing-comment edits in the two views.

The Modern Comments tester now discovers supported actions from fresh owned
native hosts, accepts split/toggle controls and Office labels where available,
deduplicates repeated UIA identities and refuses ambiguous/stale candidates.
Panel-only modes first create a posted scratch thread; panel New is found
independently of the previous editor. Posting retains the known parent thread
or identifies one new card from before/after metadata, without guessing server IDs.

Normal seeding types prepared text through the verified scratch editor and waits
for stable focus metadata before dispatch. Recovery seeding deliberately keeps
the value-only path and omitted notifications. The recovery wait requires a
resolved locator after the replacement helper is ready. UTF-8 diagnostics quote
expected/observed text so missing diacritics remain distinguishable.

`tools/test_word_modern.ps1` runs **238 isolated setup/coverage/log assertions**, a scope-info probe
and the default-deny check before any desktop opt-in. `-Scope SetupViews` prepares
new/edit/reply editors in contextual and panel views, with no resident conversion
or settlement probe. It can run against the current trial without restarting it;
`-AllowDesktop` still requires explicit user permission and an idle keyboard/mouse.
Conversion scopes retain the fresh-diagnostic-window requirement. Panel scans
prove their posted-thread anchor before enumerating actions; count and focus
failures are reported separately. Native diagnostics showed that the same posted
thread changes from `cardRoot_4_<thread>` to `cardRoot_1_<thread>` across these
views. The tester compares the exact numeric thread component inside its verified
scratch frame, requiring a unique visible match and rejecting drafts or unknown
ID formats. It never derives a posted ID from an unposted draft.

The approved final `SetupViews` run passes **186 checks and all six editors**,
with unchanged body and no resident conversion/settlement probe. On failure the
tester captures bounded owned card/action metadata before cleanup without trying
another action automatically. A setup pass alone does not establish the complete
resident-conversion/recovery matrix; execution requires a freshly authorized
diagnostic window, without bypassing the active trial's expired probe.

The later approved full run passes all 33 comment actions across six editors,
and the final silent-helper recovery passes 83 native checks. Normal seeding
now adds a verified owned caret Left/Right boundary so fixture typing is outside
the conversion's undo unit. It requires unchanged text and the exact restored
caret before dispatch. Recovery deliberately skips this boundary and its reads.
This tests conversion undo from an established baseline; it does not promise
that Word never coalesces rapid uninterrupted user typing into one undo unit.

Recovery reads the private log with read/write/delete sharing, a one-megacharacter
cap and complete-line snapshots. Helper identity and resolved-locator evidence
come from the same snapshot. The isolated contract holds an append writer open,
checks an incomplete/completed PID append and refuses an oversized log. Temporary
log fixtures are deleted afterward; no installed profile or Word access occurs.

The comment helper's startup catch-up also checks the current owned native host
when legacy focus/caret metadata remains in the body. `WordCommentFocusTests`
adds twelve isolated regressions for this fallback and its refusal boundaries.
The approved native silent-helper recovery case passes without restarting the
resident or refocusing the comment. The comment runner can select
`-Scope ContextualAll` to exclude the separate Comments-panel view switch.
The full `All` run now passes all 17 contextual actions and all 16 panel actions.

The body performance fixture uses final/no-markup display to match its independent
final-text XML oracle, while retaining actual revisions for the native adapter.
It restores and verifies the inherited revision display before closing its own
Word application. A same-source full matrix passes all nine cases, both tracking
states and 144 samples. This does not cover every markup-display mode or establish
a speed improvement.

## Developer-tool checks

The Python tooling has a separate standard-library test set:

```powershell
python -m unittest discover -s tests/python -v
```

These checks exercise archive exclusions and version guards, documentation links
and image headers, pinned dictionary-source verification, import safety, and
isolated-install verification logic. Network and installer calls are mocked;
fixtures live beneath ignored `out/python-tests` and are removed after each test.
The optimized-code check verifies that path guards remain active when assertions
are disabled. This set supplements the 44 C# suites and is included in CI.

`TriggerEditorTests` checks that all resident editors and message boxes bypass conversion by
process ownership, keeping ordinary typing literal even when dialog titles change.
`ShortcutSettingsTests` exercises the Word shortcut reader through a getter-only
fake object model, including modifier mapping, the original navigation conflict,
normal/disabled/unassigned/prefix bindings, current proposed shortcuts, ownership
changes and release after a failed read. Disposable helper processes additionally
prove report decoding, malformed-output rejection and the five-second timeout.
Neither fixture controls Word or sends desktop input. These contracts remain
within the existing 44-suite registry.
The menu fixtures verify that the removed spelling editor and standalone Word
overlap actions stay absent while shortcut-window inspection remains available.

`BookStyleTests` also runs the Ctrl-italics contracts: unchanged spellings first,
plural forms, individual loanword overrides, explicit passage regularization,
proper names, whole compounds, English protection, distinct formatting cycle
positions, HTML and protected Markdown. `FontAutoSelectionTests` includes a
mixed-font passage whose unchanged terms require both italic and regular spans.
These fixtures exercise production policy and cycle code without typing into an
editor. Native undo and visible formatting still require user-led scratch checks.

The Word transaction-reuse fixture in `WordSnapshotTests` counts calls to a fake
range and fails on a duplicate text/revision read. It exercises production replay
capture and font restoration, including matching/stale font tickets, tracked
deletions, paragraph markers, provider failure, cancellation and one undo record.
It never connects to Word or sends desktop input.

The same suite includes 73 focus-supervisor checks using disposable pipe children,
not UIA subscriptions. They cover exit before readiness, later failure/restart,
silent startup, oversized records, session retirement, late stale results and
bounded shutdown of a stuck child, registration-time events, out-of-order lookup
completion and retry-streak reset. Recovery contracts also cover an alive but
silent helper, cancelled refreshes, fresh-event cancellation, coalesced requests,
cooldown, lazy retry after a failed locator and stale in-flight results. They
prove that replacing only the helper can restore metadata delivery. Twenty-six
hook-thread checks use an injected queue
to prove dispatch while the calling UI thread waits, installing-thread affinity,
single cleanup, failed registration, unexpected message-loop return/failure,
failed re-registration, recovery while logging stalls, and resident startup/
shutdown cleanup. They install no desktop hooks.
Callback-origin diagnostics additionally retain their message on another thread,
let later input dispatch while the writer stalls, and contain a failed logger.
Ordinary callers retain synchronous logging. A private mutation that restores
synchronous hook logging fails the stalled-writer regression.
The comment-focus contracts reject ordinary body focus before ancestry, preserve
menu detection and distinguish an early observation from live edit permission.
Follow-up contracts cover posted-comment `postEditor_` ancestry, stale focus and
wrong process/type/card. The body focus model counts one property batch per
node, rechecks changed ancestry on the next call, rejects menus/foreign processes
and retains writable classic-comment checks and the twelve-node bound.
The 139 comment-focus checks include startup catch-up behind a stale global body
provider, a failed global query, missing native-caret evidence, lost live focus,
foreign host ownership and bounded caret-hit ancestry. These are provider models;
native catch-up and perceived recovery still require authorized Word checks.
An explicit STA caller proves that locator providers run on the dedicated MTA
worker, with bounded waits and coalesced metadata instead of unbounded threads.

`NativeKeyTests` also runs 52 clipboard-snapshot regressions. In-memory data
objects cover UTF-16 units, rich bytes/streams, file lists and bitmap pixels/DPI.
Real disposable children cover exit, a stalled provider, malformed output, stale
sequence and retry after a new clipboard sequence. Unknown CLR objects, duplicate
formats, oversized lengths, trailing data and truncated payloads are refused.
Late child crashes and hangs after closing a valid frame still block another
capture of the unchanged clipboard; a new sequence allows recovery. Normal
unsupported-format refusal and child-start failure remain retryable. These
regressions fail against the preceding capture implementation.
Native text-reader models distinguish nontext data from failed reads so cycling
evidence stays guarded. These tests do not read or change the system clipboard.

`WordSnapshotTests` also runs native revision-enumeration contracts with managed
and IEnumVARIANT-style collections: empty ranges, tracked deletion/move-from
coordinates, unsupported revision types, invalid/excessive counts, failed or
unknown native enumeration status and the 10,000-item limit. Provider failure
must not silently become a revision-free range. Timing contracts check values,
exceptions, nested scopes, a failed observer and exclusion of private text.
These fixtures never connect to Word or read a document.
`WordRevisionBatchTests` adds 58 assertions against direct native-style iterators
and the Framework's public adapter bridge. Counts at 1/2/31/32/33/64/65 exercise
partial/full/terminal batches and reduced call counts without another revision
count. Late deletions, move-from/unsupported types, unexpected collection growth,
malformed fetched counts/status, null/extra items, later failures and the 10,000
ceiling retain rejection/mapping behavior. Managed-only providers keep their
full scan. Three deliberate regressions (dropping a partial batch, disabling the
bridge and weakening the native limit) fail these checks. No Word is started.

`SelectionTests` checks that a captured one-word selection needs exact text/range
and current context, without navigation. Unsolicited selections still fail the
default caret contract. Native-selection batching tests cover lost context,
failed selection/restoration and the final live context check. These supplement
the existing formatting, undo, input replay and six-editor contract matrix.

`ReplayTests` also exercises clipboard observation through injected readers:
stable/empty data, a clipboard change during delayed rendering, later sequence
invalidation and an unavailable provider. These tests never open the clipboard.

`WordFormattingCycleTests` exercises actual native font capture/restoration on
fake Word ranges, including surrounding whitespace, punctuation, mixed fonts,
the selection bound and expiry while a Ctrl cycle remains active. It also checks
the production manual-undo decision and original-italic HTML payload. The fixture
never starts the resident or connects to Word. Ctrl-italics contracts additionally
cover literal Markdown identifiers, escaped/bounded emphasis, punctuation and
protected URLs, email addresses, paths and literal comments in all Roman modes.

Formatting and module-boundary guidance is in
[Contributing](../CONTRIBUTING.md) and [the code map](ARCHITECTURE.md).
The selection suite now includes the shared six-profile editor contract matrix.
`WordSnapshotTests` also exercises window-specific Word application binding,
including multiple instances, wrong window/process, stale global registration,
fallback and COM-reference ownership. Those tests use fake applications and do
not open or read native Word documents.
`WordSearchFieldTests` adds 68 assertions for the observed docked Navigation query
path: native control/process/frame identity, missing/changed/cyclic ancestry,
read-only/password/disabled/multiline flags, live editor metadata, refusal of stale
document fallback, unchanged body-focus policy, native text-provider precedence,
plain query encoding and control-output refusal. Synthetic metadata and private
settings are used, without Word or input. Three private mutations (foreign owner,
document routing and control output) fail these checks. Separately approved
read-only probes of the prepared `krsna` query verified word/letter/caret discovery
and Unicode auto encoding without conversion, document access or input. Native
conversion/cycling/undo remain a separate user-authorized trial check.

The same suite runs the resident's real font-ticket cleanup against a deliberately
stalled release backend on a separate STA caller. Navigation must return without
waiting for that backend, while the old ticket loses ownership immediately and
active leases release exactly once. `ReplayTests` also covers 256 Ctrl+Shift
navigation chords: both modifier sides/orders, arrows/Home/End/Page Up/Page Down,
autorepeat and both release orders. It checks FIFO order and extended-key flags
without sending input to Windows.
`ExcelCellTests` includes the real adapter's transaction methods against an
independent field backend. LF/CRLF selection, retention, punctuation, original
caret restoration, Enter/Edit transitions, stale reads, cancellation, ignored
selection and formula-output refusal are exercised without native input. The
one-pass discovery fixture checks that a failed capture stays ambiguous rather
than becoming a usable blank caret. These checks establish code contracts;
they do not establish every Excel provider's native cursor semantics.
The fixture also runs actual highlighted conversion across LF/CRLF passages,
models balanced Alt+Enter insertion, preserves nearby emoji/combining sequences,
and checks short relative travel inside long cells. Single-line Devanagari,
Bengali, ITRANS and plain-output regressions retain their established fallback.
Native retained selection and one-step undo were separately confirmed by the
user in a disposable Excel cell.
`StandaloneTests` checks the source-only distribution policy and reversible
retirement in disposable directories, including own-license retention, unchanged
user configuration and complete rollback. It locks each obsolete file in turn,
allowing backup reads while denying deletion, and verifies exact restoration of
configuration, old/new migration markers and retired files. Python checks enforce the same policy
at payload creation and reject excluded files in the staging tree.
See [Responsiveness verification](RESPONSIVENESS.md) for the automated 150 ms
engine benchmark and the separate native-editor smoke test protocol.

From the repository root:

```powershell
.\test.ps1
```

The default set covers word/component targeting, slash boundaries, numbers,
single letters, contractions and race models, script handling, spelling/style
rules, shortcut configuration, trigger editing, native input structure construction
and feature toggles. Tests create private configurations beneath `out/tests/`.

`RecognitionTests` also covers the requested `devanagari` → `devanāgarī` default
across casing, Unicode/Balarama/plain output, caret/selection/copied conversion,
cycling and explicit suffixes. Private-fixture overrides retain precedence;
Bengali/Tamil output and styling, protected URLs and existing English guards
remain unchanged. These tests do not connect to an editor or use installed
preferences.

Run one or several suites:

```powershell
.\test.ps1 -Suite ItransTests,HindiTests,TriggerEditorTests
```

Run all 44 included noninteractive suites:

```powershell
.\test.ps1 -Full
```

The full set includes large English-guard and dictionary/cycling checks and can
take several minutes and substantial temporary disk space. Test results are in
`out/tests/results.json`; individual logs are beside it. Fixtures are reset from
repository defaults before each suite. Input fixture copies are written under
`out/tests/inputs` so tests cannot overwrite committed test inputs.

Each invocation uses a fresh fixture directory. Its editable trigger YAML is
copied directly from the authoritative factory list. `StandaloneTests` also
checks legacy/unified customization equivalence across all seven output modes,
override precedence, protected words, ordered replacements, invalid sections and
failed reloads. It verifies that loading leaves the original configuration bytes
unchanged. Shared conversion-plan tests cover abbreviations, verses, numbers,
names and continuous word cycles. These remain part of the 44-suite registry.
Conflicting unified replacement rules replace the matching legacy rules in their
first effective position; unrelated rules retain their order. `CtrlShortTests`
also verifies that an absent BB group stays absent on the first upgrade without
a migration marker, while fresh installs and explicit editor restoration retain BB.

These tests do not type into apps, read open documents, replace the clipboard,
register global shortcuts or start the resident. Native-key tests construct and
inspect event structures; they do not send those events. Interactive browser,
Word, InDesign, clipboard and live automation probes from development are excluded.

`CtrlShortTests` covers Ctrl scripture targeting, verse-only defaults, range
modifiers, paired translations, and abbreviation priority. `ScriptureRangeTests`
checks every adjacent pair in the bundled books in all three Roman encodings,
samples verse/prose italic boundaries, exercises continuous cycle undo origins,
and uses synthetic data for missing records and exact size limits. These check
conversion logic and formatting output; actual editor Ctrl+Z and Backspace
behavior still require the physical checks below.

The scripture suite also checks every record identifier against its stored index
hash, rejects duplicate identifiers, and verifies index order and record bounds.
The 276 repaired identifiers in `tests/fixtures/scripture-identifiers.tsv` must
resolve through both indexed lookup and Ctrl expansion, with SHA-256 hashes
matching their unchanged original verse/translation payloads.

## Setup rollback integration

```powershell
./tools/test_setup_transaction.ps1
```

This compiles the exact production setup sources as a **test library**, embeds a
source-data fixture and calls `Install` only with `test=true`. It does not build
or execute an installer, register startup, launch a resident or touch an installed
profile. Real locked-file failures cover early, middle and final retirements;
another failure occurs after all retirements. Every case checks the full original
file inventory and bytes, settings, trigger edits, migration markers, engine and
recovery backup. Fixtures and logs remain under `out/setup-transaction`.

## Repeatable Word body comparisons

```powershell
./tools/test_word_performance.ps1
# With separate desktop permission and a quiet keyboard/mouse:
./tools/test_word_performance.ps1 -AllowDesktop -Iterations 5
# Compare a saved adapter source with the current adapter:
./tools/test_word_performance.ps1 -AllowDesktop -BaselineWordSelection out/word-performance-tests/reviewed-baseline.cs
# Focused debugging; the report marks this as a partial matrix:
./tools/test_word_performance.ps1 -AllowDesktop -Cases mixed-format -Iterations 3
```

The default compiles C# 5 baseline/candidate implementations and runs pure
fixture/report contracts. It never starts Word, inspects desktop focus, reads an
installed profile or starts a resident. A direct native-runner invocation also
requires explicit desktop arguments. Preparation records source/binary/fixture
hashes and both adapter source snapshots. One shared build directory avoids
repeated runtime/data installations; compact run reports remain under
`out/word-performance-tests`. Copy a reviewed `src/WordSelection.cs` there before
changing the adapter when a historical baseline is wanted. Without an explicit
baseline, both lanes use the same source: this is calibration, never a speedup.

With permission, the fixture creates a new Word process with no existing
documents, verifies its native window/process ownership, then uses one scratch
document. It never enumerates/reads existing documents or starts/stops KeyJaya.
It uses COM/UIA selection without keyboard fallback or clipboard operations.
Native foreground/focus changes abort the run; it does not fight for focus.
Immediately before the undo oracle, a null foreground reading may settle for at
most 100 ms; another foreground window aborts immediately and the original native
focus identity must still match. This is verification-only: production edit
guards are unchanged. Windows documents transient null readings during
[activation changes](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow).
A deadline stops only the owned test helper, leaving any remaining Word window
for review. Normal cleanup quits only the exclusively owned single-document
instance; if another document joins it, only the scratch document is closed.

Nine fixed cases cover caret word, selected word, passage, mixed fonts,
formatting-only conversion, a retained tracked deletion, changed text length and
a named character style, plus a mixed-font word that forces grouping to decline.
Setup explicitly resets only the scratch text's paragraph/character styles so
the new-document template cannot silently turn every plain case into a fallback.
The font signature also checks character/paragraph
style identity and proofing language. Both lanes use the same expanded oracle.
Each runs with Track Changes off/on, in alternating baseline/candidate order,
with separate warmups. The independent XML oracle excludes deleted/move-from
text. Native assertions cover exact output/neighbors, font name, size, bold,
italic, color and underline, retained selection/caret, continuation finalization,
one-step undo and restoration of the original revision population. A second undo
must restore a known earlier scratch edit, checking that earlier history survives.
These do not
accept/reject changes in a user document: setup resets only the owned scratch.

Timing includes known-output discovery/selection, native replacement and owned
continuation finalization. Setup and verification are outside that interval.
First per-case, median and nearest-rank p95 are reported. This excludes conversion
engine/mode selection, resident dispatch, physical Ctrl recognition, OS input
replay, Paste Options UI and Modern Comments. Small native samples are not CI
latency guarantees. Failed warmups, failed/duplicate/missing samples and invalid
timings invalidate the comparison; they are never silently discarded.
The pure schedule/statistics/XML contracts also run in `WordSnapshotTests`.

`WordBatchingTests` exercises the production patch loop with counted fake Word
ranges. Uniform adjacent letters use one text write and revision scan instead of
three. Mixed runs, named styles, failed/invalid XML, existing deleted native
positions, other scripts and length changes keep the previous behavior. Tests
also cover cancellation during export, source changes during a declined export,
the 32-letter ceiling, failed inserted revision reads and publication of partial
progress for the existing rollback proof. Only an explicitly defined unmodified
default character style qualifies; named/customized defaults retain letter edits.
An injected font substitution must also be repaired for one-letter patches.
Counted metadata checks reject a character style or mixed font without exporting
XML. Unavailable metadata still requires successful XML evidence; unavailable
metadata combined with failed XML keeps letter edits.
Range XML must be read before the custom undo record starts; native tests exposed
undo interference when it was read inside that record, even on declined groups.
Disabling grouping or source-font restoration in isolated source copies makes
the corresponding regression fail; repository sources stay intact.
Native comparison samples retain metadata-only transaction traces so actual
group acceptance and expensive provider calls can be distinguished from noise.
Diagnostic scopes also count direct revision-enumerator calls and time COM
release separately. These operations retain their original results, exceptions
and cleanup behavior; disabled diagnostics do not allocate their timing delegates.
The extra coverage measures previously unattributed stage time, not a speedup.

### Separate Modern Comments integration

The fixture activates only its owned scratch document's pane, requires stable
body typing focus, and checks/enacts Word's `ReviewNewComment` built-in command.
Selecting a range alone is not treated as proof of focus. Every setup stage is
reported; interrupted focus, disabled commands, missing/read-only editors and
changed body text fail explicitly. The setup policy has isolated regressions
without Word, UIA or input. If its own notification monitor misses the editor,
the fixture can discover a unique, currently focused, writable comment directly
inside its own frame and process. It rechecks ownership and editor eligibility
after enumeration; it never focuses a guessed candidate. This prevents setup
from depending on the notification channel being tested. An approved setup-only
run on 29 October passed with unchanged body text and no conversion dispatch;
ambiguity and lost/stale focus have additional isolated regressions. The full
silent-child recovery case still needs a separate approved run.
The command identifier is from Microsoft's
[Word command list](https://github.com/OfficeDev/office-fluent-ui-command-identifiers/tree/main/Microsoft%20365/Current%20Channel);
the command uses [GetEnabledMso](https://learn.microsoft.com/en-us/office/vba/api/office.commandbars.getenabledmso)
and [ExecuteMso](https://learn.microsoft.com/en-us/office/vba/api/office.commandbars.executemso).

`-Scope SetupOnly` opens and validates the scratch comment without dispatching
any conversion or restarting the trial resident. Use it to validate fixture
setup before a separate silent-helper recovery run. Native execution still
requires fresh desktop permission. Both the wrapper and compiled runner default
to no desktop execution. Compile-only runs also verify refusal without the
runner's explicit authorization argument. `-TimeoutSeconds` defaults to 180;
on timeout only the test runner is stopped, and any scratch Word window is left
for review rather than terminating Word. Normal completion closes its owned
scratch document. No existing document is opened or read.
Conversion scopes also require enough of the resident's 15-minute diagnostic
window to cover their deadline: an expired queue probe must not count as an idle
queue. This guard never restarts a resident. `SetupOnly` needs no queue probe and
can validate setup while a longer trial keeps running.

`tools/test_word_modern.ps1` retains its compile-only default. Its opt-in run now
also posts a fixture comment and opens its Edit action, requiring a focused
`postEditor_` before testing conversion, selection, native undo and English
continuation. English action labels must resolve uniquely inside the owned card
or its just-opened menu; unsupported UI fails instead of guessing a click.
The fixture uses the current supervised focus API rather than a removed private
field and stops on desktop interruption. Native verification of these new UI
selectors remains separate from successful compilation.

`-Scope ContextualNew` stops after the new-draft cases, avoiding a dependency on
posted-card discovery. The additional Ctrl case sends an owned Ctrl down/up pair
before ordinary shortcut dispatch to exercise Word's focus response; it does not
test recognition of a physical Ctrl tap by the resident hook. On 29 October,
two authorized runs passed the preceding six new-draft cases then failed to
discover the posted card. The revised virtual-host selector and Ctrl case compile
but have not been run live. User-led new/edit/reply checks passed after restarting
the unchanged temporary resident; the earlier intermittent failure is unresolved.

`-Scope ContextualRecovery` is a separate opt-in fault test. With fresh permission
and a hash-verified private test copy, it requires a deliberately silent first
focus child, substituted by a development-only entry point outside runtime sources.
It never suspends a real UIA client. It requires helper-only recovery,
the same resident PID, catch-up without refocusing the comment, correct conversion,
plain continuation formatting and native undo. Process ID, parent, executable and
helper command line and private fixture marker are checked before testing.
Cleanup terminates only that owned silent child if recovery has not replaced it. Existing
documents and installed files are outside this fixture. The first recovery
candidate and an ordinary-comment follow-up stopped before conversion because no
writable editor was found. Both scratch windows closed and the previous temporary
copy was restored. These are failed runs, not native recovery proof. The revised
MTA resolver's silent-child run also stopped at editor discovery before dispatch;
native automatic recovery remains unverified. The exact production-source binary
was then started without the test entry point, and the user confirmed all three
new/edit/reply actions. A separate read-only capture probe passed with unchanged
clipboard sequence and no writes/contents reported. No diagnostic hook was added
to the runtime, and no further live tests run without fresh readiness.

For an already authorized private diagnostic resident, pass `-ResidentRoot`
and `-ResidentAssembly` to identify its profile and exact tested executable.
The resident's actual file hash must match; the tool does not prepare a profile,
change installed settings or switch residents. The current posted-comment fix
is development source and is not in the restored installed 1.9.8 executable.

## Optional Word focus timing

`tools/probe_word_focus.ps1 -CompileOnly` prepares a metadata inspector without
inspecting windows. Its `-AllowInspection -WaitSeconds 45` mode needs separate
permission and a user-focused editor in a disposable Word document. It compares
global focus, native caret and pointer-hit metadata, including bounded ancestry
and supported patterns, without reading field/document text, sending input,
using the clipboard or changing focus. This exposed the posted-comment editor
whose global provider still reported the legacy body.

```powershell
./tools/profile_word_focus.ps1              # compile only; no windows inspected
./tools/profile_word_focus.ps1 -Mode Baseline -AllowDesktop -WaitSeconds 30
./tools/profile_word_focus.ps1 -Mode Current -AllowDesktop -WaitSeconds 30
```

The live modes require permission and the user-focused body of a disposable Word
document. They read control metadata only, never document text, clipboard or
input. Baseline requires the retained 1.9.7 library from the Acrobat investigation;
Current compiles development sources. Missing baseline or changed focus prevents
a comparison. Twelve samples measure only `DocumentFocused`, not conversion
latency. See [Responsiveness](RESPONSIVENESS.md) for the measured scope.

`tools/test_word_modern.ps1` remains compile-only by default. Its opt-in desktop
mode excludes the focus helper when identifying the resident and accepts
`-ResidentRoot` for a separately authorized disposable profile. Its own entry point
routes `--watch-word-focus` before any test document creation. Do not use
`-AllowDesktop` without permission; compilation does not authorize live input.

## Optional Acrobat search-field inspection

```powershell
./tools/probe_acrobat_focus.ps1 -CompileOnly     # no windows inspected
./tools/probe_acrobat_focus.ps1 -AllowInspection -WaitSeconds 60
```

The second command requires the user's permission and an empty, user-focused
Ctrl+F Find or Advanced Search field in a disposable PDF. It reports control
classes, types, IDs, native focus and supported interfaces only. It never reads
field values or PDF text, activates windows, sends input, uses the clipboard or
changes preferences. Foreground/focus changes discard the result. Run separately
for the two search fields. Without either switch it exits without inspecting.
This is diagnostic preparation, not a conversion or native undo test.

`tools/probe_acrobat_search.ps1` compiles a nonresident production-code library
by default, without inspecting windows. With separately granted permission,
`-AllowInspection` reads only a prepared synthetic `krsna` query and reports
status/length/caret metadata. `-AllowConversion` additionally selects and attempts
`kṛṣṇa` in that exact query, leaving it for inspection; no search is executed.
Both modes accept `-WaitSeconds 60` so the user can return to the field.
`tools/probe_acrobat_ctrl.ps1` is compile-only unless `-AllowDesktop` is supplied
with permission. Its bounded passive monitor records only physical Left Ctrl
timing and focus notifications in foreground Acrobat, without reading text,
sending/suppressing keys or activating windows. It never starts the resident.

`NativeKeyTests` includes cross-process Edit/Rich Edit fixtures for the native
fallback used by patternless Acrobat fields. These owned hidden controls cover
Unicode queries, multiline coordinates, exact selection/caret, non-edit/invalid
handles and bounded refusal of a stalled provider. No Acrobat process, desktop
focus, clipboard or keyboard input is used by this automated fixture.

## Optional native Word fault tests

```powershell
./tools/test_word_native.ps1                # compile only; Word is not started
./tools/test_word_native.ps1 -AllowDesktop  # requires Word and a quiet keyboard
./tools/test_word_native.ps1 -AllowDesktop -DelayMilliseconds 3000
# Physical typing: use the currently running resident and the NEW scratch doc.
./tools/test_word_native.ps1 -AllowDesktop -DelayMilliseconds 3000 -PhysicalTyping
```

This separate opt-in test creates a new Word application and scratch document,
then verifies that its process was absent before the run. It refuses a shared
process. It never enumerates or reads existing documents, starts KeyJaya hooks,
uses the clipboard or injects keyboard input. Only its own Word process is closed.

The test pauses only that new Word server's UI thread for 350 ms by default,
with a configurable upper bound of ten seconds and an independent resume thread.
It checks real native-object-model acquisition after
the delay, formatting and one-step undo, rollback after focus loss during an
edit, disconnected COM rejection, and the resident's queue recovery after the
actual Word window closes. Queue events are constructed and inspected for FIFO
ordering and paired modifier release; they are **not replayed into Windows**.
This supplements the fake-application binding tests rather than replacing them.

The same native test creates a real comment and threaded reply in its scratch
document. It checks their focus recognition, word and letter conversion,
abbreviation expansion, Unicode and ScaGoudy/Balarama output, cycling and original
font restoration, one-step undo, deferred undo/typing continuation and rejection
of expired or neighboring targets. Body text and the other comment/reply are
checked throughout. Draft and Print Layout fixtures explicitly activate their
owned comment editor; selecting a COM range in a hidden pane alone is not a
valid typing-focus fixture. UIA discovery is given the same bounded settling
window used by the resident. No keys are injected.

The optional physical mode waits up to ten minutes for a real Left Ctrl tap in
its own scratch document, then pauses that Word server while the person immediately
types ` next Word`. The existing running resident performs conversion; the helper
does not edit its settings. The test verifies exact suffix ordering, capitalization,
English formatting, released modifiers and undo, then closes only its own document.
It does not inject typing. Do not use another document for this check.

It does not prove behavior when a COM server never returns. Physical mode covers
real typing during a bounded stall only. Desktop Word is not installed on the normal CI runners, so this test
is deliberately excluded from the noninteractive 44-suite registry and CI.

## Physical release checks

### Optional Modern Comments integration

```powershell
./tools/test_word_modern.ps1                # compile only
./tools/test_word_modern.ps1 -AllowDesktop
```

This separate opt-in fixture requires a current, hash-matching development
resident running with `--diagnostic`, Modern Comments enabled, and Word's English
Comments button label. It creates a new exclusively owned Word process and local
scratch document. It checks contextual and panel views, new comments and replies,
word/letter/abbreviation/selected conversion, one-step undo, unchanged body text
and ordinary English continuation with the editor's existing formatting.

Only its own verified comment editor receives fixture input. The resident's
normal Windows shortcut dispatch is used; injected-key filtering is not relaxed.
The diagnostic queue probe waits for selection retention and held input to settle
before assertions. This verifies the live editing path, not physical Ctrl-tap
recognition or hardware delivery timing. It does not open existing documents,
change the installed configuration, build an installer or publish anything.

### Physical editor checks

Use separate disposable documents and a scratch browser editor. Check:

1. In Unicode and Balarama Word text: backtick letter cycles; Left Ctrl word,
   compound and selection conversion; punctuation and slash boundaries; undo.
2. Later-paragraph and first-document triggers, contractions and scripture insertion.
3. Pasted Word text while Paste Options is visible; rapid typing after contractions.
4. Track Changes, style/font preservation, mixed-font repair, English protection,
   correct italics and ordinary formatting after a converted word.
5. Selected/copied script conversions, clipboard preservation, Hindi/ITRANS choice.
6. Cheat sheet, trigger editor and shortcut editor on each monitor; light/dark/high
   contrast; repeated open/close and save/cancel.
7. Pause/startup options and ordinary Ctrl shortcuts.
8. InDesign text frames and Text/GREP Find/Change fields; Obsidian Live Preview;
   Excel cell editing; Acrobat comments; browser/ChatGPT/WhatsApp scratch inputs.
9. `BG2.13–14` + Left Ctrl inserts verses only; `BG2.13–14VT` pairs each verse
   with its translation. Both verses are italic; translations follow prose rules.
   Ctrl+Z should restore the full reference and original formatting in one step.
   Repeat with Track Changes and each encoding. Missing ranges must stay unchanged.
10. Cycle `Krishna` and `Rama` through multiple complete loops, then Backspace:
    restore the original spelling and formatting, preserving neighboring text.
11. In a new Word comment and a reply, check letter/word cycles, abbreviation
    expansion, selected conversion, one-step undo and immediate English typing.
    Repeat with Unicode and ScaGoudy/Balarama in the comment pane and Print Layout.
    Verify the document body and neighboring comments/replies remain unchanged.

Never use an existing working manuscript as a disposable fixture. Sleep/wake and
Windows sign-in tests are separate from the noninteractive suite.

## Continuous integration

The **Windows checks** GitHub Actions workflow runs on pushes to `main`, pull
requests, or a manual dispatch. Both PowerShell 7 and Windows PowerShell 5.1 jobs
use fresh Windows Server 2022 runners. Each audits the source and documentation,
checks bundled dictionary hashes, builds a verification installer, runs the
compiled-app probe and all included suites, then checks package integrity and isolated
installation/upgrade and post-retirement rollback behavior. Both jobs also run the setup-retirement rollback
integration test; its logs are retained with the other verification artifacts.

Only verification logs/results are retained, for seven days. CI does not publish
installers, use signing secrets, register startup, or launch the resident. The
workflow has read-only repository access and uses pinned official actions.

These are fresh hosted build/test environments, not a test of physical typing
or installation on another person's desktop. Word, InDesign, Obsidian, browser
editors, sleep/wake, sign-in, and monitor interactions still need manual checks.
The shorter [first-run check](FIRST_RUN_CHECK.md) is suitable for initial testers.

Repeated local installer checks clean their owned fixture automatically:

```powershell
python tools/verify_build.py
```

For a deliberately retained inspection fixture, use a fresh target:

```powershell
python tools/verify_build.py --target out/verification/another-run/install --keep-artifacts
```

Targets must stay beneath `out/`. Existing target directories are refused.
