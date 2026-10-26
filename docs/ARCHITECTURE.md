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
| Dictionaries | `src/IndexedStore.cs` | Indexed lookup, exact-key validation, streams and bounded caches |
| Configuration | `src/Configuration.cs`, `src/ShortcutSettings.cs`, `src/TriggerEditorStore.cs` | Parsing, settings, trigger persistence and migration boundaries |
| Host adapters | `src/WordSelection.cs`, `src/InDesignSelection.cs`, `src/InDesignFindField.cs`, `src/InDesignFindSyntax.cs`, `src/ExcelCellField.cs` | Editor-specific selection, font, replacement and undo behavior |
| Word instance binding | `src/WordApplication.cs` | Acquire automation from the requested Word frame; verify window/process ownership and release rejected COM references |
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

Word automation must bind to the requested editor window. A global
`GetActiveObject("Word.Application")` lookup can return another or background Word
instance, including one with no active document window. `WordApplication` obtains
Word's native object model from the frame's `_WwG` pane and verifies the returned
application's active window and process. Global registration is a compatibility
fallback only after the same ownership checks. COM references are acquired per
operation and are not cached across apartments. Selection, document identity,
formatting and undo checks remain in `WordSelection` after binding succeeds.

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
