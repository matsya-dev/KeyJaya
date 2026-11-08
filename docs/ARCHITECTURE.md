# Code map and editing contracts

KeyJaya compiles into one resident executable plus a separate installer and
uninstaller. The resident supervises a hidden copy of its executable for Word
focus events. It does not need a Python process in the background. `build-common.ps1`
lists the conversion and resident compilation inputs; `setup-build.ps1` lists the
installer inputs. Explicit source lists make the contents of each executable
reviewable.

Development-only `tools/workspace.ps1`, `tools/build-output.ps1` and
`tools/temporary_workspace.py` implement [owned scratch cleanup](OUTPUT_CLEANUP.md),
bounded evidence and guarded build publication. They are not runtime sources and
never manage an installed profile.

## Where responsibilities live

| Area | Sources | Responsibility |
| --- | --- | --- |
| Startup | `src/Program.cs`, `src/LaunchRequests.cs` | Entry point, single-instance handling, launch requests |
| Resident | `src/Runtime.cs` | Keyboard decisions, conversion dispatch, UI and editor coordination |
| Input lifecycle | `src/ResidentInput.cs` | Input-hold ownership, manual/automatic completion, bounded retry and replay acknowledgment |
| Hook dispatch | `src/InputHookThread.cs` | Dedicated native message thread; installs, dispatches and releases keyboard/mouse hooks away from UI and editor automation |
| Edit evidence | `src/SafeEdit.cs`, `src/SelectionCommit.cs` | Target/caret validation and commit ownership |
| Native input | `src/Native.cs` | Windows interop, clipboard ownership and bounded input replay |
| Conversion policy | `src/Core.cs`, `src/CopiedConversion.cs`, `src/Vocabulary.cs` | Recognition, shared word plans, spelling choices, phrases and scripture |
| Transliteration | `src/IndicScripts.cs`, `src/Itrans.cs`, `src/Hindi.cs` | Script parsing/output and language-specific policy |
| Small transforms | `src/SingleLetters.cs`, `src/Number.cs`, `src/ResidentReplacement.cs` | Letter cycles, numbers and correction/replacement decisions |
| Formatting | `src/SelectionFormatting.cs` | Formatting spans, names and italic exceptions |
| Ctrl italics policy | `src/ControlItalics.cs` | Individual-word versus passage rules, unchanged-spelling first choices and explicit original-formatting cycle positions |
| Dictionaries | `src/IndexedStore.cs` | Indexed lookup, exact-key validation, streams and bounded caches |
| Configuration | `src/Configuration.cs`, `src/ShortcutSettings.cs`, `src/TriggerEditorStore.cs` | Parsing, settings, trigger persistence and migration boundaries |
| Word shortcut inspection | `src/WordShortcutInspector.cs`, `src/WordShortcutWindow.cs` | User-requested read-only binding metadata in a bounded helper process; navigation differences and KeyJaya overlaps |
| Host adapters | `src/WordSelection.cs`, `src/InDesignSelection.cs`, `src/InDesignFindField.cs`, `src/InDesignFindSyntax.cs`, `src/ExcelCellField.cs` | Editor-specific selection, font, replacement and undo behavior |
| Word timing diagnostics | `src/WordNativeTiming.cs` | Opt-in transaction stage/call timings, including direct revision enumeration and COM release, without text, arguments or document names |
| Word instance binding | `src/WordApplication.cs` | Acquire automation from the requested Word frame; verify window/process ownership and release rejected COM references |
| Word Navigation query | `src/WordSearchField.cs` | Recognize the owned docked query control, validate its live editor and keep native query text separate from Word document selection |
| Modern Word comment focus | `src/WordCommentFocus.cs`, `src/WordFocusMonitor.cs` | Isolate UIA focus subscription, supervise its helper, resolve metadata locators, validate live editor/Word ownership and reject the stale body provider |
| Clipboard backup isolation | `src/ClipboardSnapshot.cs` | Read foreign OLE formats in a bounded STA child; transport typed snapshots without CLR-object deserialization or clipboard writes |
| UI | `src/TrayMenu.cs`, `src/CheatSheet.cs`, `src/TriggerEditorWindow.cs`, `src/ShortcutWindow.cs`, `src/TrayAppearance.cs`, `src/WindowAppearance.cs` | Tray/windows, theme and monitor scaling |
| Installation | `src/Setup.cs`, `src/SetupWindow.cs`, `src/InstallTransaction.cs`, `src/ApplicationShortcuts.cs`, `src/EspansoMigration.cs`, `src/Uninstall.cs` | Transactional installation, shortcuts and compatibility cleanup |

Some policy classes are partial declarations. Follow their shared state and
initialization order before moving members. `SafeEdit`, `Program`, and
`IndexedStore` were separated along existing type boundaries; that separation
adds no process or runtime layer.

`ResidentInput.cs` is an incremental partial-class extraction: its methods share
the same resident instance and synchronization gate. Fields and their initializers
stay in `Runtime.cs`, in their original order. This preserves timer, hook and
ownership behavior while making replay lifecycle code reviewable in one place.
Do not move field initializers between partial declarations without separately
proving initialization order.

## A conversion is an owned edit

The resident receives keyboard events, identifies the action, resolves a target,
prepares conversion output, and commits through the appropriate editor path.
Selection-based and caret-based actions share conversion policy, while native
editing retains its own verification and formatting rules.

The low-level hook must remain responsive. UI-provider discovery, conversion,
clipboard work and editor automation belong outside the immediate event decision.
The operation generation, target context, foreground editor and expected text
must still agree before a delayed operation may edit. A cached caret or a reused
accessibility element is evidence to validate, not permission to edit new text.

Keyboard and mouse hooks are installed and pumped on their own MTA thread.
WinForms, clipboard/OLE and Word calls must not block that thread or hold its
state gate while waiting on a provider. Pending paste-focus classification runs
outside the gate, then checks generation, foreground owner and native focus
again before applying a reset. Closed-target native undo cleanup is queued away
from hooks. UI timers remain on the resident's UI thread. The reference-window
cache publishes its owner and result together as an immutable observation.

If the hook message loop fails or exits unexpectedly, its owner releases that
attempt's hooks on the installing thread and retries registration with bounded
250 ms to five-second backoff. Initial registration failure still fails startup;
it does not announce an operational resident. Status logging runs separately
from hook dispatch. Resident operational/diagnostic writes originating in a
keyboard or mouse callback also run off that thread; ordinary callers retain
synchronous logging. This recovers a message-loop failure, not Windows' silent
removal of an individual hook. Startup rollback and normal shutdown share
idempotent input cleanup, including a helper started before hook registration.

Selection-origin comparisons, occurrence locators, modern-comment undo
classification and post-conversion clipboard reads also run outside the input
gate. Observations are followed by generation/selection checks before state is
committed. Clipboard cycling requires matching sequence numbers before and after
the read and again at state commit; unavailable or newer data cannot authorize
cycling. A completed conversion still retains its ordinary undo record.

Input holds have a bounded lifetime and an owner. Accepted queued input must be
replayed in order, exactly once; modifier state and delivery already in flight
matter during cancellation. Preserve these invariants when shortening delays.
When typing follows a selected conversion, the caret moves to its end before
queued typing is replayed. Without queued typing, the converted selection stays
selected.

The native editor adapters preserve their own undo, font and formatting behavior.
Do not generalize them into a blind universal replacement routine: Word ranges,
InDesign text frames, search fields, browser editors and Excel edit controls have
different contracts. Prefer a quiet skip to an edit whose target cannot be verified.

Ctrl-word preparation is separate from ordinary conversion plans. A recognized,
already-correct term gets a formatting-only first choice, while an explicit
original-formatting index distinguishes an identical text value at the end of
the cycle. A one-word selection can use the same guarded word transaction while
retaining its selection. Passage formatting instead emits regular-type spans for
loanword exceptions and names. Explicit style preferences and book-title rules
remain authoritative. English protection is retained; the existing editorial
loanword list supplies specific recognized exceptions for individual styling.

`ItalicSpan.Italic` defaults to true; false means actively remove italics from the
owned span. Word applies the property inside its existing native undo record,
and its font snapshot keeps original continuation styling during repeated cycles.
InDesign chooses the corresponding nonitalic face without discarding bold weight.
Rich HTML carries explicit normal spans. Markdown adjusts owned emphasis markers
while protecting code, links and headings; bounded wrapper expansion verifies
provider text before selecting any surrounding marker. Comments, plain fields,
Excel's text-only adapter and script outputs do not gain unsupported formatting.

Clearing a Word font snapshot during keyboard navigation retires its ownership
immediately. It detaches unused font/document references and releases them on a
worker, because releasing a Word COM reference from the low-level hook can wait
on the Word UI thread that is itself waiting for that hook. An active font lease
keeps its references until its existing user releases them; retired snapshots
cannot authorize or acquire another edit.

Original Word fonts are retained while their Ctrl-word cycle remains active,
including after the short Backspace deadline. Cycle completion/invalidation and
ordinary input still retire the ticket; expiry cannot create an original-format
state without evidence. A one-word selection can include bounded surrounding
whitespace or punctuation, so font capture accepts the same shapes as word
classification, with a 321-character ceiling. Native document, story, start,
expected-text and current-context checks still authorize each restoration.

Formatting-only rich-editor changes use the same manual Backspace decision as
text-changing words. The saved original anchor passes explicitly into rich/native
restoration, because accepting Backspace retires the resident's current anchor.
Original text is restored without running automatic italic policy again.
Modern comment fields retain their text-only restoration path.

Typing in resident-owned settings windows and message boxes bypasses conversion
by process ownership, so new dialog titles need no hook exception. Registered
shortcut dispatch uses the same guard before acquiring an input hold. The
separate cheat-sheet process retains its existing reference-window exception.
Process ownership is checked before reading captions/classes: same-process
`GetWindowText` can wait for the UI thread, which the input hook must avoid.
Ordinary external captions need no class read; the separate cheat sheet still
requires its exact caption and Windows Forms class.

Word automation must bind to the requested editor window. A global
`GetActiveObject("Word.Application")` lookup can return another or background Word
instance, including one with no active document window. `WordApplication` obtains
Word's native object model from the frame's `_WwG` pane and verifies the returned
application's active window and process. Global registration is a compatibility
fallback only after the same ownership checks. COM references are acquired per
operation and are not cached across apartments. Selection, document identity,
formatting and undo checks remain in `WordSelection` after binding succeeds.

Word shortcut preparation passes its selection observation only to the same
owner/generation's word discovery, avoiding a second paste-UI preparation. The
native replacement still verifies current document, range and expected text.
Replay ownership is captured inside that transaction from its verified visible
projection instead of acquiring another Word application/selection and scanning
revisions twice. Continuation styling can use the same owned font snapshot, and
original-font restoration reuses the verified result map. These are per-request
proofs; no COM reference or range permission is cached for a different edit.

A captured one-word selection can pass preparation with its exact selection/text
proof and current context; it need not report a collapsed caret or receive new
navigation. Other selection requests retain the caret-first contract. Native Word
letter selection batches its own document/story/caret/text/result checks into
one binding, followed by the existing live commit check. A letter request can
reuse its own discovered target, and a word request reads its styling context
once. These reuse conversion input within the request, never edit permission.

Classic Word comments and threaded replies use `wdCommentsStory` (4) and the same
native replacement, font, undo and replay ownership checks. Their UIA provider
can be an Edit without a Body ID or Document ancestor. Focus is accepted only
for a writable TextPattern on a native `_WwG` editor in the foreground Word
process. Ribbon fields, search controls and menus do not qualify through this
additional path. A Word range selected by automation does not alone establish
typing focus in the corresponding pane.

Modern Comments panels and contextual bubbles are separate UIA editors hosted
by Word-owned `NetUIHWND` or `_WwG` windows. A direct focused-element query can return the legacy
body provider even while a comment is being typed. Only the hidden
`--watch-word-focus` child subscribes to managed UIA focus events: the Framework
tracker can throw before invoking the application's callback. This entry point
runs before profile loading, hooks or the resident mutex. The child sends bounded
metadata records (window/process, editor ID, runtime ID and bounds), without
document text. The resident publishes locators without calling UIA on its pipe
reader. One process-lifetime MTA worker resolves locators, with one pending item;
a caller waits at most 150 ms for that candidate. It never executes locator
providers on a UI/STA caller, following [Microsoft's threading guidance](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading).
Identical metadata within the same helper session coalesces instead of invalidating
a lookup in flight. A transient failure can retry without another event.
A locator now makes at most three read-only attempts, 20 ms apart, while its
observation remains current. A rejected cached element is invalidated by exact
reference before rediscovery. A late successful lookup can cancel helper refresh
only after the same active notice and live comment ownership pass again.
Point lookup precedes construction of the legacy native-host UIA root: that
root can omit modern reply fragments or be unavailable while their independently
verified point provider remains usable. If point and native-host discovery fail,
the resolver searches only the verified Word frame for that exact editor ID,
process and runtime identity. At most 64 matching candidates are considered;
a unique live match must retain the notice's exact native host. Discovery and
the final candidate are revalidated against the foreground owner. Exact runtime
identity and live editor, card, process and native-host checks remain required;
bounds alone grant nothing.
A resolved element is only a candidate. Every
use still validates live focus, process, comment-card ancestry and native host ownership.
An early event may be observed before focus settles, but cannot authorize an edit
until live focus passes. A body provider reporting lost focus is rejected.

Helper exit retires its session immediately; late locator lookups from a retired
session cannot republish evidence. The supervisor restarts only its own child
with bounded backoff (250 ms through 30 seconds), enforces a five-second startup
deadline and bounds shutdown even when the child cannot unregister. If a request
cannot obtain live focus in foreground Word, it schedules a helper-only refresh
after a 250 ms ordinary settling interval. Startup catch-up and a known in-flight
locator instead receive a bounded three-second grace, because a frame lookup
can outlive the caller's 150 ms wait. Repeated requests do not extend that
deadline. Fresh locator metadata or verified live focus
cancels that request. Repeated requests coalesce and refreshes are at least five
seconds apart, independently of crash backoff. The triggering conversion may skip
safely; it is never replayed against an unverified editor. The resident, hooks and
preferences stay running. A permanently stalled provider call in the resident
remains outside that recovery guarantee, but it no longer blocks the pipe reader.
Neither the resident nor helper resumes an unhandled Framework exception.

Word-focus and clipboard helpers retain their parent lease until the redirected
input pipe reaches EOF or fails. `Program.WaitForParent` drains any encoding
preamble or other startup bytes; receipt of a byte alone never means the parent
exited. Each watcher owns and closes its stream. The private silent-helper
fixture uses the same lifetime routine.

Resident clipboard text reads use the bounded Win32 Unicode reader with stable
sequence checks. An unavailable/busy/malformed read cannot become readable empty
cycling evidence. Full-format backups use `--capture-clipboard` before profile,
mutex or hook startup. That short-lived STA child alone invokes foreign
WinForms/OLE clipboard providers: an observed CLR access violation while
unmarshalling data cannot reliably be caught in the resident.

The child never writes the clipboard. Its private stdout pipe carries bounded
typed text, bytes, streams, file lists, bitmaps and enhanced metafiles, without a
CLR-object deserializer. Unknown types or incomplete data decline the whole
backup. UTF-16 units, stream bytes, raster pixels/DPI and vector bytes are retained.
The parent rechecks the sequence and native Unicode data before accepting a backup;
existing restore ownership/sequence checks remain. Timeout, process death or
malformed output release only that child. A failed provider is not retried for an
unchanged clipboard sequence, avoiding repeated crashing helpers. Startup failure
or a normally unsupported format is not a terminal provider failure. A crash or
hang after sending a complete snapshot frame still counts as provider failure;
valid data alone cannot authorize capture before the child exits successfully.
The caller's STA services delayed-render messages while waiting, including when the resident
owns a restored clipboard. Work remains outside the input hook and its state gate.
An empty managed OLE view of a nonempty native clipboard gets at most three
read-only observations of the original sequence, 20 ms apart. Persistent absence,
missing advertised values and unsupported formats still refuse the whole backup.
The helper reports numeric refusal reasons without data contents; ordinary
refusals remain retryable, while crashes and hangs retain sequence memoization.
Restoring a genuinely format-free backup uses the ownership-checked native clear
operation rather than publishing an empty OLE object. Explicit empty text is a
real format and retains the ordinary restoration path. Neither operation can
clear a newer clipboard item.

The helper buffers events received during registration so `ready` is always the
first protocol record. Event revisions reject an older provider lookup that
finishes after a newer event. Failed lookups clear evidence. A full healthy minute
resets the retry streak, so occasional failures do not accumulate a permanent
30-second recovery delay.

After registration, the helper can seed a metadata-only observation of an already
focused comment. It first tries global focus, then a hit test at the native caret
in a verified Word-owned `_WwG`/`NetUIHWND`. When these fail, startup catch-up
searches the current native host and then the owned Word frame for a unique live
focused comment; it rechecks the original native-focus identity throughout.
A ten-node caret-hit ancestor bound, live editor
focus, recognized comment/card ancestry, foreground/native-focus revalidation and
the existing host/process checks still apply. The caret supplies a location hint,
never edit permission. No text, clipboard, mouse position or input is used. A
newer real focus event supersedes this optional startup probe, so refreshing the
helper need not require leaving and re-entering the comment. An old locator
resolved after a newer event, clear or helper retirement cannot supply a target.

Focus resolution tries the live event editor before requesting the global focus
provider. Each ancestry step fetches its properties together with a UIA
`CacheRequest`; that data is local to the call. No ancestry, text or permission
proof is reused across calls. The successful focus classification is returned to
the document-focus guard instead of walking the same tree again. Modern comment
word shortcuts skip native-body Paste Options preparation and its two 40 ms
settling waits; the actual UIA caret/selection, expected-text, modifier, focus,
generation and replacement checks still run.

Ordinary Word body providers are rejected as comment candidates after one
property batch, before ancestry traversal. Paste-menu ancestry stops at the
document, while the separate native popup scan still runs. These are bounded
classification fast paths; they do not relax writable-editor or commit checks.

Excel discovery derives either a selection/caret or selection/insertion from one
captured buffer, with live guards still applied at edit time. Selection prefers
relative Left/Right navigation from the nearest verified selection edge, including
single-line cells. LF/CRLF boundaries are mapped explicitly; cursor-step validation
covers the selected span and actual travel path, so complex characters elsewhere
need not block the operation. The established single-line Home/End fallback
remains for script/cluster ranges; multiline paths refuse unverified clusters.
Selected passages can insert LF/CRLF through Alt+Enter while remaining in Edit
mode, without using the clipboard. Both insertion and retained selection must
have supported plans before replacement. Status-mode metadata uses a fresh
batched UIA request, and text/range/mode, ownership and epoch checks remain.
All cell input uses the foreground-owned correction tag. The native backend is
the runtime default; an internal field backend permits tests of the same
transaction methods without desktop input.

Modern comment/reply drafts use the verified UIA edit path and never Word's
document `Selection`, native font snapshots or native Word replay anchors. Text
replacement inherits the comment editor's current formatting; automatic HTML
italics are excluded from this surface so English continuation and native undo
are preserved. Other rich editors and native Word stories keep their formatting
paths. Input queues, target checks and clipboard ownership remain shared.

Word's docked Ctrl+F Navigation query has its own `RICHEDIT60W` control (ID 1),
under the observed NetUI/NUIPane/work-pane/command-bar/dock path to its `OpusApp`
frame. Every native ancestor must belong to the same foreground Word process.
The live `NetUITextbox` editor must match that exact native focus and be enabled,
non-password and writable. The bounded native query reader supplies text/caret
coordinates, rather than using an accessible document range. Missing or stale
query UIA metadata cannot fall back to Word's body provider.

This field bypasses document paste-menu preparation and the Word selection/font/
replay bridges. `DocumentFocused` remains restricted to native stories and the
established comment paths; it does not grant document access for the query.
The query uses plain text, Unicode in Auto mode, and an explicit encoding override
when selected. Automatic italics and document font snapshots are excluded.
Captured query targets revalidate the same live control, native path and expected
text before replacement/retention/restoration. Control-character/multiline output
is declined before input or clipboard changes so expansions cannot inject Enter
or Tab into the query. This includes C1 controls/NEL and Unicode line/paragraph
separators; ordinary Unicode spaces remain valid literal text. This recognizes
the observed docked pane; unknown or
floating layouts remain unsupported until independently verified.

Acrobat's Find and Advanced Search controls are native `Edit` windows. In the
resident they can advertise a writable UIA TextPattern whose ranges are empty
even while a query is present. `SafeEdit` gives the bounded native edit reader
precedence only for a writable, non-password Edit with the exact native focus,
foreground root, process ownership and recognized Acrobat executable/host class.
Caret, letter, selection, insertion and behind-caret discovery share that choice.
Existing native text/selection verification, input generation, cycling and replay
guards still authorize replacement. This does not grant access to PDF page text
or change the Rich Edit path used by Acrobat comments or other editor adapters.

The follow-up Word changes after the prepared 1.9.8 installer also recognize
`postEditor_` controls used when editing a posted modern comment. They require
the same live focus, comment-card ancestry, writable TextPattern and native Word
ownership as `cardEditor_` drafts. Native body selection remains excluded.
The body/classic-comment focus guard fetches each node's identity properties
together using a per-call CacheRequest, preserving process/menu checks and the
twelve-node bound. It rechecks the foreground owner after the provider query.
Neither these batches nor a comment locator grant permission across edits.

Native Word revision projection retains its preliminary count, range-scoped
access and 10,000-item ceiling. Native enumerators read batches of at most 32
items, including an end-of-sequence check. The Framework's public `ICustomAdapter`
can expose its borrowed native iterator without resetting or cloning it; the
adapter stays alive and that alias is not released twice. Managed-only providers
keep their full `IEnumerator` scan. Every returned item, including a partial final
batch and additions beyond the preliminary count, retains type/deletion-coordinate
filtering. Invalid fetched counts, inconsistent status, missing/extra items and
provider failures refuse projection. Nothing is cached across edits.
A failed or unknown IEnumVARIANT status is
an unavailable projection, never an empty collection. Replacement failures keep
their existing owned rollback path. Diagnostic scopes count calls and elapsed
time by transaction stage only when diagnostic tracing is enabled; observers
cannot change a successful result or suppress a provider failure.

The 29 October body optimization can combine up to 32 consecutive changed Latin
letters in the first (rightmost) patch group, with one-to-one replacements. It requires one contiguous native span
and a fresh range-XML export proving one plain formatting run with exact source
text and supported ordinary font properties. Only an explicitly defined,
unmodified default character-style marker is allowed; named character styles, revision markup,
anchors, links, fields, mixed runs and unknown structure decline the optimization;
an unavailable export also keeps the previous letter edits. Non-main stories,
expansions, length-changing patches, combining sequences and other scripts never
attempt this grouping. Spelling-proof markers do not constitute text/anchors.
Before exporting XML, cheap range metadata rejects character styles and an
empty/mixed font name. These checks only decline grouping. Missing metadata
still requires the full XML proof; it never authorizes grouping on its own.

Export runs before starting the custom undo record and is refused when another
custom record is active. Native testing found that exporting a partial range
inside an open record could break one-step undo even when grouping was declined.
Only the first group is proved before any edits; later groups keep letter edits.
Export is optional evidence for this operation, never later edit permission.
After it returns, context, foreground ownership, full owned raw text and native
selection bounds are rechecked even when grouping was declined. The existing patch replacement still
captures the source font, checks fresh inserted revisions/visible text and tracks
native story growth. Final formatting, expected output, selection, continuation
and owned undo/rollback checks are unchanged. The patch loop publishes its map,
text and native progress by reference so a failure retains the same rollback
evidence as before. No provider work moves into the input hook or its state lock.

Every nonempty patch now captures and reapplies its source font, including
one-letter changes. The expanded native fixture exposed the old adapter letting
Word substitute Cambria for Georgia diacritics within an unchanged named
character style. Batching does not bypass this font-restoration step.

## Data and configuration

`assets/short-trigger-defaults.yml` is the sole factory trigger source. Fresh
installs copy it to `config/short-triggers.yml`. An existing installed file remains
authoritative, including deletions and disabled entries. Specific approved
one-time additions use independent state markers; they are not synchronization
with the factory list. Restoration is an explicit user action.

`defaults/customizations.ini` documents the six customization sections. Existing
legacy TSV files are read first in their established order; unified entries
override entries of the same kind. Preferred/protected words take priority over
general aliases. Ordered replacement rules run last. A unified replacement with
the same case-sensitive FROM key replaces matching legacy rules at their first
position; new keys append afterward. Loading these files is
read-only, and values are not evaluated as scripts or shell commands.

The Word shortcut helper runs through `--check-word-shortcuts` before resident
startup, hooks or mutex acquisition. It binds to an existing verified Word frame
using `WordApplication.ForFrame`; that metadata binding grants no editing
permission. It reads only active-window ownership and shortcut metadata, checking
ownership before and after each binding read. It neither reads document text nor
sets CustomizationContext, changes bindings, executes commands or activates Word.
The parent runs the helper hidden on a worker with a five-second deadline and
stops only its own helper if Word stalls. At most eight visible frames are
considered; one active window per Word process is checked. Missing, changed or
unavailable windows produce an incomplete/unavailable report, not a clean result.

Word key codes are built with its own [BuildKeyCode method](https://learn.microsoft.com/en-us/office/vba/api/word.application.buildkeycode)
and read through [FindKey](https://learn.microsoft.com/en-us/office/vba/api/word.application.findkey).
Control/Shift/Alt values differ from Windows hotkey modifiers; standalone Ctrl
taps are not queried as Word chords. Navigation expectations are reported as
differences that may be intentional; assigned KeyJaya chords are overlaps rather
than proof of a fault. No template/context is selected or saved by the helper.
The check is available in Customize shortcuts; it has no standalone tray action.

The dictionary pack stays separate from editable configuration. Each index entry
is 20 bytes in little-endian order: 8-byte key hash, 8-byte data offset and 4-byte
record length. Records contain UTF-8 key/value bytes separated by NUL. A matching
hash is followed by an exact-key comparison; it is not sufficient on its own.
Shared seek/read operations and cache access are synchronized. Preserve paired
`.idx` / `.dat` files and their checksums together.

## Development tools

Python commands expose import-safe functions and a `main()` entry point. Importing
a tool must not parse the caller's arguments, perform downloads, create archives,
render images or execute an installer. `package.py` excludes personal state;
`audit_project.py` checks source/data integrity; `verify_build.py` is an explicit
isolated installer test. Documentation renderers illustrate verified engine output
and do not record a live editor.

`tests/csharp` contains the 44 registered regression suites. `tests/python`
exercises packaging, documentation, pinned-source verification and installer-test
boundaries with disposable inputs. Their outputs belong under ignored `out/`.
See [Contributing](../CONTRIBUTING.md) for commands and review expectations.

The shared editor contract matrix lives in `tests/fixtures/editor-contracts.json`.
`tests/contracts/EditorContracts.cs` runs each scenario through six deterministic
selection-capability profiles from `SelectionTests`. It also checks production
Word revision mapping, InDesign GREP protection, Excel formula exclusion and
browser paragraph normalization. These are model contracts, not native-app tests.

`tools/benchmark.ps1` compiles a separate runner and a fresh fixture. It reports
engine startup, first calls and warm median/p95/p99/max, with a checked-in 150 ms
warm p95 budget. The report explicitly excludes editor discovery, clipboard and
physical key delivery. See [Responsiveness verification](RESPONSIVENESS.md).
