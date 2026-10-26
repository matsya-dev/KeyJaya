# Code map and editing contracts

KeyJaya compiles into a single resident process plus a separate installer and
uninstaller. It does not need a Python process in the background. `build-common.ps1`
lists the conversion and resident compilation inputs; `setup-build.ps1` lists the
installer inputs. Explicit source lists make the contents of each executable
reviewable.

## Where responsibilities live

| Area | Sources | Responsibility |
| --- | --- | --- |
| Startup | `src/Program.cs`, `src/LaunchRequests.cs` | Entry point, single-instance handling, launch requests |
| Resident | `src/Runtime.cs` | Keyboard decisions, conversion dispatch, UI and editor coordination |
| Input lifecycle | `src/ResidentInput.cs` | Input-hold ownership, manual/automatic completion, bounded retry and replay acknowledgment |
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
| Word instance binding | `src/WordApplication.cs` | Acquire automation from the requested Word frame; verify window/process ownership and release rejected COM references |
| Modern Word comment focus | `src/WordCommentFocus.cs` | Observe UIA focus events, validate the live comment card/editor and foreground Word ownership, reject the stale body provider |
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

Typing in resident-owned settings windows and message boxes bypasses conversion
by process ownership, so new dialog titles need no hook exception. Registered
shortcut dispatch uses the same guard before acquiring an input hold. The
separate cheat-sheet process retains its existing reference-window exception.

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

Classic Word comments and threaded replies use `wdCommentsStory` (4) and the same
native replacement, font, undo and replay ownership checks. Their UIA provider
can be an Edit without a Body ID or Document ancestor. Focus is accepted only
for a writable TextPattern on a native `_WwG` editor in the foreground Word
process. Ribbon fields, search controls and menus do not qualify through this
additional path. A Word range selected by automation does not alone establish
typing focus in the corresponding pane.

Modern Comments panels and contextual bubbles are separate UIA editors hosted
by Word-owned `NetUIHWND` or `_WwG` windows. A direct focused-element query can return the legacy
body provider even while a comment is being typed. The resident owns an MTA UIA
focus listener; cached event elements are used only after live focus, process,
comment-card ancestry and native host ownership are checked. A body provider
explicitly reporting lost focus is rejected. Listener registration/removal runs
off the hook and the UI thread, and shutdown has a bounded wait.

Focus resolution tries the live event editor before requesting the global focus
provider. Each ancestry step fetches its properties together with a UIA
`CacheRequest`; that data is local to the call. No ancestry, text or permission
proof is reused across calls. The successful focus classification is returned to
the document-focus guard instead of walking the same tree again. Modern comment
word shortcuts skip native-body Paste Options preparation and its two 40 ms
settling waits; the actual UIA caret/selection, expected-text, modifier, focus,
generation and replacement checks still run.

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
