# User installation confirmation — 1 November 2026

The user reports everything seems to work after the normal installer/smoke-check
instructions. A read-only check confirms disk VERSION 1.9.10, executable version
1.9.10.0 and its exact verified-build hash. All 16 preexisting configuration-file
hashes match. The resident is a new process from after release preparation; no
app was started/stopped and no desktop input or installed write was sent by this
confirmation. The report does not claim an extended stability trial.

At that point, uninstall DisplayVersion still reported 1.9.7 in HKCU's 32/64 views. A further
read-only identity check confirms the reader and installed resident use the
same Windows SID, and the exact HKEY_USERS hive has that same stale value. It
is therefore not explained by reading a different account or registry view.
The actual installed binary is 1.9.10. The earlier compiled-assignment proof
remains valid, but does not establish the historical/live cause of the stale
entry. No registration correction was applied. User report, binary hashes and
metadata-only evidence are saved under `out/release-1.9.10`; no publication or
push is included.

The user then explicitly authorized correcting only the Windows version label.
The first automatic permission review timed out before execution; the permitted
single retry succeeded. The existing uninstall entry was backed up and only
`DisplayVersion` changed from 1.9.7 to 1.9.10. Read-only checks confirm both
registry views now show 1.9.10, all other uninstall values and startup are
unchanged, and all 24 engine/config/state hashes still match. No app restart,
settings rewrite or desktop input occurred. Backup/result evidence is retained
under `out/version-label-1.9.10-20261101-161828`, with a result copy beside the
release reports. This is a registry-label repair, not a demonstrated cause/fix
for the historical mismatch or an authorization to publish.

# KeyJaya 1.9.10 local release preparation — 1 November 2026

The user authorized the proposed release preparation and local installer checks.
All 69 dirty/untracked files were snapshotted under `out/release-1.9.10/before`,
with source hashes, Git inventory and HEAD. Read-only preservation captures cover
26 installed binary/config/state files, resident/helper identity and seven existing
startup/uninstall properties. The retained 1.9.9 installer matches its recorded
release checksum. No branch switch, commit/push, live editor test, publication
or installed update is included.

Version.Current, VERSION, assembly/file versions and both manifests now agree
on 1.9.10/1.9.10.0. The accepted source inventory contains the same 50 production
C# files, with 1.9.9 retained as the previous release and its baseline archived.
Direct byte comparisons prove Core/AssemblyInfo changes are version-only; the
remaining reviewed behavior/test inputs match the successful native session.
All 158 frozen release inputs remain unchanged. Factory defaults, triggers,
runtime data and the data manifest are not refreshed. The strict audit passes
1,944 checks.

Windows PowerShell 5.1 builds all three executables and payload. File/assembly
versions are 1.9.10.0, the 19 compiled-app checks pass without resident startup,
all 38 staged files match the payload and the embedded ZIP matches by SHA-256.
The installer is unsigned. The versioned source passes all 44 isolated suites /
26,999,576 assertions, zero failures, followed by 26 Python tests, 129 C# 5
format/token checks and five pinned Hindi source hashes. All 17 engine budgets
pass with 301 warm samples each, maximum warm p95 5.5331 ms against 150 ms and
initialization 181.8104 ms. This does not establish native Word response time.

The fresh `out/release-1.9.10/clean/install` fixture passes clean installation,
edited-trigger upgrade and injected post-swap rollback, restoring all 56 hashes.
The exact retained 1.9.9 installer creates a separate disposable upgrade target:
a forced failed upgrade restores all 46 hashes; successful upgrade preserves all
eight configuration and five state hashes. Seventeen upgraded-runtime checks
cover custom/deleted/disabled triggers, preferred/protected words, legacy/unified
overrides, style, shortcuts, encoding, disabled options and learned spellings/
votes. Loading the runtime does not change fixture configuration/state. The
separate setup-retirement integration passes 25 checks under PowerShell 5.1.

The historical Windows version-label mismatch is not reproduced as an incorrect
compiled assignment: a read-only IL probe of each actual installer locates the
normal `RegistryKey.SetValue("DisplayVersion", version)` assignment and confirms
1.9.10 and 1.9.9 respectively. A wrong expected version is rejected. The code is
inside the non-test setup path, and test mode intentionally skips registration,
resident startup and user shortcuts. The user's existing registry values are
preserved; no live registration/write result or definitive historical cause is
claimed. The small probe and results are retained with release evidence.

Final read-only preservation matches all 26 installed hashes, seven existing
registration properties and the same installed resident PID. Both installed
executable file versions remain 1.9.9.0. The 69-file Git inventory is unchanged,
with only the twelve authorized version/baseline/release-document inputs changed
since this stage's snapshot. All 158 frozen release inputs still match after the
final strict audit. Artifact hashes and focused release notes are retained.

After retaining results/hashes, guarded native PowerShell cleanup removes only
11 new targets, freeing 2,589,163,293 bytes: the new regression/benchmark corpora,
disposable clean/upgrade installations and backups, setup integration outputs,
and the retired development app. All 38 retired app files first matched the
retained 1.9.9 installer payload. Absolute paths, reparse points, protected
fixtures and active processes were checked before deletion. Historical fixtures,
trial evidence/profiles, recovery archives and release installers remain. One
current `out/app` tree remains. The local installer is ready; it is unsigned and
has not been published or applied to the installed app.

# Automated Word conversion/recovery session — 1 November 2026

The user authorized unattended scratch testing and a 30-minute idle window.
Read-only inventories preserved the 69 dirty/untracked source inputs, 26 installed
binary/config/state hashes, exact app identities and seven startup/uninstall
properties. Reviewed runtime/test hashes matched 31 October. Preparation rebuilt
and self-tested the body, comment, normal-resident and private recovery binaries
without changing production code. Source/binary hashes were checked before live
execution and sources again after it.

Two complete `All` comment matrices used the same normal production-source
resident (with fresh bounded diagnostic settlement enabled), without a restart
between runs. Both passed: 33 actions/890 checks and 33 actions/884 checks,
**66 actions / 1,774 checks** together. Both contextual and panel new/edit/reply
editors passed word, selection, letter, expansion, undo and typing cases as
declared by the scope. There are 24 reply actions across the two runs. Coverage
validation still requires every declared case exactly once per run; polling can
change the number of successful checks. No failed ordinary case was retried to
manufacture a pass.

A separate fresh private recovery resident consumed one `silent-focus-once.flag`.
The `ContextualRecovery` case passed **80 checks**, retaining its resident PID,
editor and caret without refocusing/reseeding while the helper recovered.
Conversion, plain formatting, native undo and unchanged body passed. The fixture
targeted only its verified own helper; no real installed accessibility client
was suspended or stopped for fault injection.

Installed 1.9.9 was then restored before the full same-source body matrix. All
nine fixtures with tracking off/on passed **144 samples / 6,384 checks**, with
full matrix coverage and restored revision display. Fonts, styles, mixed text,
selection/caret, conversion undo and prior undo history passed. Timing differences
between identical adapters are calibration variability, not a speedup result.

The session finished successfully within the approved quiet window, recording
**8,238 native checks**. All test runners/private residents and their scratch Word
processes exited; the remaining Word processes predate the session. Installed
binary/config/state hashes (26) and registration properties (7) all match, with
no settings copied back. A bounded read-only clipboard guard encoded sorted
typed data only in memory and retained its SHA-256 fingerprint; before/after
fingerprints match. It never writes the clipboard or reports its contents.
One private factory test profile was used across all resident phases.

Evidence and the authorized orchestration are retained under ignored
`out/automated-word-2026-11-01`; individual comment/body artifacts remain at the
paths in `phases.json`. No new runtime/tester behavior, version acceptance,
installer, installation or publication is included. Existing isolated-suite
evidence remains valid because its reviewed runtime/test inputs did not change.
Physical keyboard-hook delivery and an extended normal-use trial remain separate
from these automated registered-shortcut and same-source adapter checks.

# Intermittent Word reply and Zen investigation — 31 October 2026

The final user-led reply check passes repeated conversions in two replies,
including leaving and re-entering them. The frame-only check also passes new
comments, existing-comment edits, Word body and Zen. The final reply session has
one helper readiness, twelve resolved locators and no helper refresh; one early
unavailable locator does not trigger the previous restart loop. Installed 1.9.9
is restored with all 26 saved binary/config/state hashes and seven registration
properties unchanged. No test preferences are copied back. The final frozen
full regression run passes all **44 suites / 26,999,576 assertions**, zero failures,
with runtime/test hashes unchanged before and after. Earlier failed candidates below are retained
as investigation history, not final results or accepted releases.

This is unreleased development against installed 1.9.9. The task-start snapshot
preserves all 69 preexisting dirty/untracked files. Separate read-only inventories
cover installed binaries, preferences/state and registration. User-authorized
manual sessions use one separate profile under
`out/intermittent-input-2026-10-31`; no preferences are copied back, and no
installer, version identity or accepted source baseline is changed.

The failures are not established keyboard-hook loss: Word body conversion remains
available, and browser diagnostics reach target selection before clipboard backup
refuses. Word focus events sometimes describe an exact, live reply that the
legacy HWND tree omits. Approved metadata probes find zero matching descendants
in that tree, while point lookup finds the same runtime identity and verifies
live comment ancestry/ownership. Global focus can still report the unfocused body.
Earlier relaxed-identity and deferred-format candidates did not resolve manual
failures and were discarded; the current changes retain exact runtime identity.

The revised focus resolver adds bounded read-only retry, exact cached-reference
invalidation and cancellation of pending recovery after a delayed live success.
Point lookup now precedes construction of the optional legacy root. Diagnostics
record failure stages/exception types without reading extra properties or text;
a failed diagnostic writer cannot suppress fallback. Disposable child tests cover
a lookup completing after the caller's 150 ms wait without replacing its healthy
helper. No desktop hooks or Word processes are used by these regressions.

Clipboard changes distinguish normal refusal from child failure, retry ambiguous
empty OLE views only while the original sequence persists, and restore a genuinely
format-free backup with the existing ownership-checked native clear. Explicit
empty text, unsupported formats, newer clipboard items and crash/hang suppression
retain their boundaries. Pure injected providers and disposable children test
these paths without accessing the system clipboard. A proposed separate-window-
station native fixture was denied by Windows (error 5) before clipboard access;
it was not rerouted to the user's clipboard.

The pre-lazy-root revision passes all 44 isolated suites: **26,999,557 assertions,
zero failures** (85 focus-monitor and 63 clipboard-snapshot checks). The latest
lazy-root/diagnostic revision passes the 19 compiled-app checks, 155 comment-focus
contracts and both focused suites (**818,178 assertions**, zero failures).
All 129 C# formatting/token/C# 5 checks, 1,893 development audit checks and 77
local documentation links/four image headers pass. The audit reports five changed
production sources, with no accepted baseline update. All 26 installed hashes
still match. Five completed task-generated corpus fixtures were removed after
absolute-path/link/process checks; the preexisting fixture, evidence and current
trial profile remain. Final current-source evidence will be recorded after
the remaining manual check. The latest user reports all repeated Zen conversions
working, but repeated replies still fail in the preceding private copy. The new
reply-lookup copy is prepared, awaiting permission to switch only that copy.
The issue is therefore still open; no final Word-reply fix is claimed.

The user subsequently reproduced reply failures in the lazy-root copy while Zen
continued working. Those attempts occurred about 29 minutes after startup,
outside the existing 15-minute diagnostic window. Only operational helper-refresh
records were retained, so they do not establish which live identity/focus check
failed. The lazy-root change alone is not established as a fix. A separately
approved private diagnostic copy now caps reply-only metadata at 1,000 records
without a wall-clock expiry. Its two source copies add refusal-stage logging and
the capped logger under ignored `out/`; production sources remain unchanged.
It passes all 19 compiled-app checks. The next user-led check still fails after
the first conversion. Detailed records show `same runtime=False` during point
resolution, before any clipboard transaction. This does not establish a lost
keyboard hook or justify relaxing runtime identity checks.

A further approved 45-second passive monitor captures 55 metadata samples from
two replies. One editor's notice resolves to its exact live focused editor for
ten samples. The other editor's reported point resolves to the unfocused legacy
document for 42 samples and to a different comment's toolbar/posted editor for
three samples. Native Word ownership/focus identity remains unchanged in those
records. The monitor stops and its private child exits; the running trial is
untouched. This confirms that the failing locator cannot be trusted as edit
permission. A bounded read-only comparison of the frame and other visible
Word-owned comment hosts is prepared to identify an independent recovery route;
it requires separate user approval. No completed reply fix is claimed.

The subsequent approved owned-host comparison identifies an independent route:
the Word frame tree exposes the exact focused reply (same editor ID, runtime and
native comment host), while the native focus HWND's tree omits it. All native
focus identities remain stable during the comparison. This establishes a missing
discovery scope in KeyJaya, rather than permission to accept another comment at
the reported point. The new fallback searches only the verified Word frame for
the notice's exact editor/process/runtime and exact live native host. It requires
a unique valid candidate within the existing 64-item bound. Helper startup and
recovery likewise try a unique live focused comment in the owned frame if the
native focus host fails, retaining the original native-focus identity checks.

Eight added frame-discovery contracts cover omitted/failed native roots, lazy
avoidance of the broader query, unavailable focus and changed ownership. A private
mutation disabling frame fallback fails the omitted-reply regression. The exact
production build and private build (only capped logging differs) each pass 19
compiled-app checks. Both focused suites pass **818,186 assertions**, including
163 comment-focus contracts. All 44 isolated suites are being rerun against the
frame-only source. That intermediate run passes **26,999,569 assertions** with
zero failures. The subsequent manual check passes new/edit/body/Zen but still
fails replies. Its log shows successful frame resolution completing after the
250 ms recovery path has already retired/replaced the helper session, discarding
the otherwise valid result. The frame fallback alone is therefore insufficient.

Startup catch-up and known in-flight resident locators now receive a bounded
three-second recovery grace, without extending the caller's 150 ms wait or
renewing the deadline for repeated requests. Ordinary missing focus after startup
retains its 250 ms interval; child-death recovery and the five-second refresh
cooldown remain unchanged. The asynchronous-success fixture now uses a 1.1-second
provider instead of the earlier 220 ms read and requires the same helper to
survive while that read is pending. A private mutation restoring the short delay
fails this regression. Focus-monitor contracts now total 92; both focused suites
pass **818,193 assertions**, and normal/capped-logging builds each pass 19
compiled-app checks. The authorized private frame/grace copy is running for the
reply check, which the user confirms now works repeatedly in both replies after
leaving/re-entering. The fresh frozen full run passes all 44 suites / 26,999,576
assertions, zero failures. The preceding frame-only run remains intermediate
evidence. All 129 C# 5 parsing/format/token checks, 1,893 development audit checks
and 77 local documentation links/four image headers pass. Only the five intended
production sources differ from accepted 1.9.9; no source/data baseline is accepted
or refreshed. Unrelated work across all 69 preexisting dirty/untracked files and
the Git status inventory are preserved.

The last private resident closes normally and original installed 1.9.9 restarts.
All 26 binary/config/state hashes and seven existing registration properties
match; settings are not copied back. Completed task-generated corpus fixtures
and the stopped private profile's generated data/assets/engine are removed after
path, reparse-point and active-process checks. Its configuration/state/log evidence,
small diagnostic sources/results and verified executable remain under ignored
`out/intermittent-input-2026-10-31`. The preexisting regression fixture, other
profiles, release artifacts and recovery archives remain. No automated Word input,
desktop focus changes or clipboard writes were sent during this investigation;
native conversions were performed by the user. This is a short verified reply
trial and isolated regression result, not an extended stability guarantee.

# KeyJaya 1.9.9 installer preparation — 30 October 2026

The user authorized the previously proposed version/baseline update and local
installer build/verification. All 69 preexisting dirty/untracked files were
snapshotted under ignored `out/release-1.9.9/before`, with Git state and source
hashes. A read-only preservation snapshot covers 26 installed engine/config/state/
version/uninstaller hashes, resident/helper identities and startup/uninstall
registration. There is no installed update, app switch or new live editor test.

Version identities are 1.9.9/1.9.9.0 throughout. The accepted inventory now covers
all 50 production C# files, with 1.9.8 retained as the previous release and its
old baseline saved in the evidence folder. All 158 frozen release inputs remain
unchanged. Comparing the final reviewed runtime/tests finds only Core/AssemblyInfo
version differences; direct checks of those, VERSION and both manifests prove
version-only substitutions. No data manifest, factory settings or triggers were
refreshed. The strict audit now passes 1,944 checks.

Windows PowerShell 5.1 built all three executables and the payload. File and
assembly identities are 1.9.9.0; the embedded installer resource equals the ZIP
by SHA-256. All 38 staged file hashes equal the ZIP inventory/bytes. The compiled
app probe passes 19 checks without resident startup. The installer is unsigned.

`verify_build.py` uses a fresh `out/release-1.9.9/clean/install`: clean install,
edited-trigger upgrade and forced post-swap rollback pass, restoring 56 hashes.
The exact retained 1.9.8 installer matches its saved release checksum and creates
one additional disposable upgrade target. An injected failure restores all 46
old-install hashes; the successful upgrade preserves all 8 config and 5 state
hashes. Seventeen read-only runtime checks retain custom/deleted/disabled triggers,
legacy/unified/protected words, learned spellings/votes, formatting, shortcuts,
encoding and disabled settings, and verify new devanagari recognition. Loading
the upgraded engine changes no config/state file. Neither installer test launches
a resident or performs Windows startup/shortcut registration.

All 44 suites were rerun after the version-only update: **26,999,534 assertions,
zero failures**. The final 17-case benchmark passes with 301 warm samples per case;
maximum warm p95 is **5.8726 ms** against 150 ms and engine initialization is
**169.11 ms**. The report counts zero budget failures. These are engine timings,
excluding native Word discovery, clipboard and input delivery.

The separate setup-retirement integration passes 25 checks in Windows PowerShell
5.1. All 26 Python tooling tests, 129 C# token/format/C# 5 checks and five pinned
Hindi source hashes pass. Earlier native Word evidence remains valid for the
reviewed behavior; this version-only packaging stage makes no new native-editor
or general Word latency claim. Git commit/push, publication and live installation
remain separate actions.

After recording hashes/results, guarded native PowerShell cleanup removed only
13 newly generated targets: the new corpus fixtures, disposable installs/backups,
setup fixtures/archive/library and retired development app. All 38 retired app
files first matched the retained 1.9.8 installer's embedded payload. Existing
regression fixtures, trial profiles, scratch documents, recovery archives and
release installers were preserved. One current `out/app` tree remains.

The final read-only preservation comparison matches all 26 installed hashes,
resident PID 76936, and startup/uninstall registration. The resident and its
focus helper remain in the installed path. No personal preference was rewritten,
no clipboard content accessed and no installed file replaced. Local installer
SHA-256 is recorded in `out/release-1.9.9/SHA256SUMS.txt`; the final artifact,
regression, upgrade, preservation and cleanup results are bound in `summary.json`.

# Final release-readiness review and isolated rerun — 30 October 2026

The user authorized final source review and all 44 isolated suites, retaining
the installed app and requiring permission before any new live editor work.
Current dirty tracked/untracked files were snapshotted under ignored
`out/release-readiness-2026-10-30/before`. The accepted 1.9.8 inventory identifies
11 changed runtime files and three additions; the broader Git diff also contains
already-released 1.9.8 work, which was preserved. Hook/thread/gate lifecycle,
clipboard typed transport and child exit/sequence checks, focus-session ordering
and recovery, Word revision batches and fresh formatting proofs, font/replay/
undo ownership, query routing and vocabulary scope were reviewed.

The first complete run passed **26,999,488 assertions**. Review then reproduced
a query-policy gap: `SafeEdit.AllowsOutput` accepted Unicode C1 controls and
line/paragraph separators, despite the documented single-line contract. The
new regression fails at U+0080 on the original guard. `WordSearchField.Output`
now rejects `Char.IsControl` plus U+2028/U+2029, keeping Unicode spaces, legacy/
Indic outputs and other surfaces' multiline behavior. Forty new checks pass;
the subsequent complete run passed **26,999,528 assertions**.

Review also found `IsReferenceWindow` reading a same-process window caption
before checking its PID. That can send WM_GETTEXT and wait for the UI thread,
contrary to the hook-thread boundary. The new observation helper first returns
for own-process windows, then reads external captions and only obtains a class
for a possible cheat sheet. A throwing-reader regression reproduces the old
behavior without creating a window; it passes after the early check. Six tests
retain other-app/missing-owner behavior and the external cheat-sheet proof.
Reference: [GetWindowTextW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowtextw).

After both fixes, the final **44-suite / 26,999,534-assertion run passes with
zero failures**. All 128 runtime/test/fixture/build inputs are frozen and
rechecked. `/langversion:5` is now explicit in `test.ps1`. All 129 C# token/
format checks pass, alongside 26 Python tests, the five pinned Hindi source
files and 19 noninteractive checks against a separately compiled candidate.
The engine benchmark passes all 17 cases: maximum warm p95 **5.1132 ms** against
150 ms; startup/first requests are separate and this is not a new Word latency
claim. No runtime entry point, resident, UIA provider, clipboard, working document
or editor was started/accessed by these checks.

The unreleased release-note section was rewritten around final behavior, the
requested Devanagari scope and actual evidence/limits. Historical 1.9.8 notes
were retained. The accepted source baseline and version were not refreshed;
strict audit still reports its expected old-inventory gate, while development
audit passes 1,893 checks with data integrity intact. Version/baseline acceptance,
installer build, payload/upgrade verification and publication remain separate
release work. Installed 1.9.8 files/preferences/startup are preserved. No new
live Word/Excel/desktop test, app switch, installer, release or push was run.

# Final live Word verification — 30 October 2026

The user authorized ending the trial, fresh diagnostic sessions and full scratch
Word tests while leaving installed files/settings unchanged. The trial was closed
normally through its verified hidden resident window; its profile and final log
were retained. One new factory profile under `out/word-full-verification/profiles`
was reused for normal and deliberately faulted sessions. No installer was run.
Installed file hashes and startup were checked before/after each app switch.

The first comment run passed two actions before native undo removed rapid setup
typing as well as the conversion. Both controlled seven-action variants passed:
an established ValuePattern baseline (170 checks), and native typing with an owned
Left/Right caret boundary (191 checks). The standard fixture now applies that
boundary before conversion, requiring stable unchanged text and the exact original
caret. It sends input once only. Recovery returns before the new input or any of
its readiness observations. Seven isolated checks and two rejected mutations
cover ownership loss, bounded proof, no repeat input and preserving the silent
recovery condition. Runtime conversion/undo behavior was not changed.

Two body attempts were interrupted when input moved foreground focus to ChatGPT;
the user confirmed input in both periods. The first had 43 successful samples
and one failed transaction; the second completed 18 samples before a guard stopped
it. Their failures and rollback traces are retained. After renewed quiet readiness,
the strict unchanged ownership/context guards passed the complete body matrix:
**nine cases, tracking off/on, 144 samples and 6,384 checks**. Source fonts,
mixed formatting and character styles, conversion/prior undo, revisions, selection,
caret and revision-display restoration pass. Timing is same-source calibration.
Evidence: `out/word-performance-tests/run-6a31fbb5fe074c51a2e446ca2a024c1a`.

The corrected full comment run passed **33 actions and 886 checks** across all
six contextual/panel new/edit/reply editors, with body unchanged and exact
complete coverage. Evidence:
`out/modern-word-comments/native-7cbf65f04a2b495387ee3384edbb58dd`.

The initial native silent-helper recovery passed 71 checks. A repeat with the
updated setup fixture hit a tester-only sharing race: `File.ReadAllText` denied
an existing append writer. Recovery now reads with read/write/delete sharing,
enforces a one-megacharacter cap, ignores incomplete final lines, and derives
the helper ID and resolved-locator evidence from one snapshot. It verifies that
same ID rather than rereading a potentially newer epoch. Isolated tests keep a
real append writer open, observe a split/completed PID record, check the size cap
and reject zero/overflow identities. Restoring the exclusive reader reproduces
the failure. All **238 isolated setup/coverage/log checks** and **1,403 Word
assertions pass**, including C# 5 compilation and Windows PowerShell 5.1.

With fresh two-minute approval, the final native recovery run passed **83 checks**:
the silent helper was replaced, the successor resolved the same still-focused
editor, the test-resident PID stayed unchanged, conversion/plain formatting and
native undo passed, and the body stayed unchanged. No refocusing or reseeding
followed the recovery dispatch. A second dispatch is permitted only if the
verified helper replacement left the original query untouched. Evidence:
`out/modern-word-comments/native-d178139ef10a4fb99a8fd8aa2d49666e`.

All test copies and their scratch Word windows were closed. Installed 1.9.8 was
restored; the original trial preferences remain in their retained profile. All
43 runtime sources stayed unchanged during these tester repairs. No installed
update, release, push, version/baseline refresh or automatic preference transfer
was performed. Full session records: ignored `out/word-full-verification`.

# Full Word test preparation, no live execution — 30 October 2026

The user authorized preparation while keeping the current trial, installed app
and settings unchanged and requiring permission before live Word tests.
`tools/prepare_word_verification.ps1` has no live-execution path. It invokes only
the compile/self-test modes of the existing body/comment tools and builds an
unarmed test-only recovery entry point from unchanged runtime sources. No corpus
copy, profile, installation, Word instance, focus helper or resident is created.
Input hashes are captured before compilation and rechecked afterward; body
binary hashes must still match their self-test report. The report records all
seven executable/configuration artifacts and distinguishes prepared from live.
The normal runtime candidate is compiled separately and probed only with
`--version`, which returns before resident or profile initialization.

Review found that both existing-comment views were reported as `posted edit`,
and no final check required the full set of cases. Reports now use separate
contextual/panel posted-edit names. Pure coverage validation rejects a missing
or repeated instance of any of the 33 actions, unknown view/action names and
case changes. It validates narrower scopes too. **221 pure setup/coverage
assertions** and **1,386 isolated Word assertions pass** using the retained
private fixture. No native providers or desktop input are used by these checks.

The recovery entry point refuses ordinary/no-argument starts. A later explicit
resident launch requires the fixture flag in its arguments and a direct
GUID-named profile under its output folder's `profiles` directory; the installed and trial
roots are refused. It can replace only one deliberately armed first focus child
with a ready-but-silent pipe child; subsequent helpers delegate to the unchanged
production entry point. The self-tests exercise fakes only, perform no file or
helper operations, and pass **15 checks**. Default-deny is verified in the actual
compiled executable. Three source mutations are rejected: unconditional full
coverage success, aliasing posted-edit views and silencing every focus child.

The preparation tool passes in PowerShell 7 and Windows PowerShell 5.1. Its
manifest describes 144 body samples (nine fixtures, tracking off/on, paired
same-source variants, three repetitions plus one warmup), 33 comment actions
and one contextual recovery scenario. This is not a new speed or live-conversion
claim. The current trial's expired diagnostic probe cannot support resident
settlement assertions; it is neither bypassed nor extended. Body checks may
later run under fresh permission with the trial unchanged. Full resident phases
wait until the user explicitly authorizes ending the trial and fresh private
diagnostic sessions. [Execution plan](WORD_VERIFICATION_PLAN.md).

# Comment tester repair and approved setup verification — 30 October 2026

The user authorized tester fixes while continuing the trial and explicitly
required fresh permission before live Word work. Relevant dirty source/docs
were snapshotted under ignored `out/comment-tester-repair-2026-10-30`; runtime
hashes and trial identity were recorded. The initial changes and checks used no
Word window, UIA subscription or desktop input. The later approved scratch runs
are recorded below. No app restart, installer or settings write was performed.

Static review reproduced three fixture assumptions: Comments required an exact
English Button under the outer Word frame; Panel/PanelReply could reach panel
setup without an editor; and posting assumed only one card existed. Native
evidence from the preceding run also shows temporary GUID editor IDs becoming
posted numeric IDs, so deriving a posted ID by changing the draft prefix would
be incorrect. The repaired fixture uses fresh owned native UIA hosts, role/pattern
support, available Office labels, bounded unique action selection and verified
panel/thread anchors. New posts are selected using before/after card IDs; edits
and replies retain their actual parent. Unknown or ambiguous transitions stop.
No cursor movement or coordinate-click fallback remains in these panel methods.

`ModernWordSetup` contains the pure selection/readiness/input/dispatch policies.
`ModernWordPanel` is an explicitly listed native test input, never a runtime
source. Normal seeds use guarded native typing and three stable focus samples;
no conversion is silently retried. Recovery retains value-only seeding and
omits normal readiness/input signals. Its catch-up wait requires successful
locator resolution after the current replacement helper's readiness record,
and rejects retired/failed helper epochs. UTF-8 streams and JSON-quoted mismatch
text preserve the distinction between `prasada` and `prasāda`.

The native wrapper self-tests before desktop opt-in. `--scope-info` supplies
canonical scope names and whether settlement is needed. Both its preflight and
the C# dispatch policy exclude setup scopes from conversions/probes. `SetupViews`
creates, edits and replies in contextual and panel views without needing the
trial's expired diagnostic probe. Conversion scopes keep the existing live
diagnostic deadline. Physical modifier checks precede native fixture input.

All **128 pure setup assertions** and **1,293 WordSnapshot assertions pass**.
The pure contracts also compile/run without UIA, Office or runtime references.
Eight mutations fail: raw-label matching, omitted readiness, accepting multiple
actions, ignoring posted-thread evidence, dispatching in setup and equating
helper readiness with catch-up completion, scanning unanchored regions and
comparing full view-specific IDs instead of the actual posted thread.
The actual C# 5 tester passes its
self-tests, scope and default-deny probes under both PowerShell 7 and Windows
PowerShell 5.1. The 44-suite registry is unchanged; runtime code did not change.

All **43 runtime hashes**, the running trial's **9307dfda…0731d1** binary and
**24 installed file hashes** remain unchanged. No new corpus fixture or test
installation was created; the retained private probe fixture was reused.
The user subsequently approved five quiet minutes. A modifier-state preflight
first refused before opening Word or sending input. Read-only inspection then
found all modifiers released; a shorter run within the same window passed
**98 checks and three contextual setup views**, including temporary-to-posted ID
transition and edit/reply targeting. It reached the panel post-open scan, where
the original combined enumeration-size/current-focus guard stopped it. No
complete six-view pass is claimed. Scratch Word closed; original Word processes
40200/27480 and trial resident 79264 remain, and the body is unchanged.

That guard now reports its two reasons separately. Anchored discovery proves the
posted-thread host before enumerating controls, skipping unrelated ribbon command
trees without raising any bound. Foreground ownership uses the existing verified
Word root/PID predicate, allowing the owned subwindow while still rejecting a
foreign process or frame. Failure reports include native window/class/process/
owner metadata, without another UIA query. Modifier preflight now uses bounded
stable-release sampling and a clean refusal rather than an unhandled exception.
Five more pure assertions and a seventh rejected mutation cover anchored discovery.
A second approved run passed **91 checks and all three contextual setup views**.
The anchor-first scan no longer hits the combined guard; it times out waiting
for a verifiable panel New action. Failure metadata proves the foreground root
and PID still match the owned scratch Word window, and the body is unchanged.
Its scratch process closed; the two original Word processes and trial resident
79264/helper 78392 remain. The 24 installed hashes still match.

The tester now captures bounded control metadata from its own scratch frame on
panel failure, before closing it. This records actual card IDs, matching action
labels, visibility, patterns and native hosts without invoking another action or
guessing an ID mapping. An approved diagnostic run then passed 94 checks and the
same three contextual editors. Its metadata identified the same posted thread as
`cardRoot_4_947445465` in contextual view and `cardRoot_1_947445465` in the panel,
where a visible supported `NewCommentButton` was present. No focus guard failed.
The action had opened the correct panel; the full-ID anchor comparison was wrong.

`SamePostedThread` now accepts the observed numeric view/thread schema, comparing
the complete thread component inside the already verified scratch frame. It
rejects drafts, post/reply IDs, different threads, malformed/oversized IDs and
numeric normalization. Anchor discovery also requires a unique visible card.
Eight new pure regressions include these actual IDs; restoring full-ID equality
fails the regression. Matching-action diagnostics no longer include unrelated
menu captions.

Within that same authorized quiet period, the corrected setup run passed
**186 checks and all six contextual/panel new/edit/reply editors**. Body text is
unchanged, `conversion_dispatched` and `queue_probe_used` are both false. Scratch
Word closed, leaving only original Word processes 40200/27480. Trial resident
79264 and helper 78392 were not restarted; the 24 installed hashes match.
Evidence: `out/modern-word-comments/native-58ef9dcfe50741288802c8723742886f`.
The full resident-conversion/recovery matrix remains separate: it requires fresh
permission and a fresh diagnostic session after the current uninterrupted trial.
API references: [Office labels](https://learn.microsoft.com/en-us/office/vba/api/office.commandbars.getlabelmso)
and [UI Automation control patterns](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-control-patterns-overview).

# Native recovery trial — 30 October 2026

The user authorized live tests during one quiet hour. The initial recovery
fixture could not discover a visible, focused contextual comment from Word's
outer UIA frame. Searching the owned native editing host repaired that setup.
The first real fault run then replaced the ready-but-silent child but failed
conversion: both global focus and the native caret still described the body.

`CurrentNotice`, used only inside the disposable MTA focus helper, now falls
back to the current native editing host. It verifies process/root/class and an
unchanged native focus identity, admits at most 64 candidates, requires a unique
live comment with the existing editor/card ancestry and revalidates before
publication. No text, clipboard, settings, pointer position or document Selection
is read. Existing locator retirement and event-revision ordering remain intact.
Twelve provider-model assertions cover lazy fallback, failed caret metadata,
ownership, ambiguity, stale focus, final revalidation and the candidate bound.
A private mutation disabling host catch-up fails the new recovery assertion.

All **44 suites / 26,999,274 assertions**, including **1,189 Word assertions**,
pass against 130 frozen source entries. The runtime and isolated test inputs
stay unchanged afterward; later changes affect only native integration fixtures.
Both explicit C# 5 candidates pass **19 compiled-app checks**. Production hash:
`9307dfdaada47f47efb5aeef4bbea23e8de5b7f07e19ae0f9698e448800731d1`.
The fault-only build is `fb9e9559…d7d5be3`; its added entry point makes only the
first focus child ready but silent and otherwise delegates to production.

The revised native fault run passes **58 checks**: helper-only recovery with
unchanged resident PID and comment/caret, conversion, unchanged plain formatting,
native undo and unchanged body. The initial failing run and both source states
are retained. The ordinary production trial was restored afterward, preserving
all 22 current private preference files and all 24 installed hashes/startup.

The body matrix passes **144 samples / 6,384 checks** across nine cases and both
tracking states. The first run stopped before conversion because UIA included
the tracked deleted X (`krXsna`) while the oracle expected final text (`krsna`).
The corrected fixture explicitly selects final/no-markup in its own Word
instance, restores and reads back the original display values before shutdown,
and passes the focused deletion case followed by the complete matrix. The
exact tested private fixture was transferred to the public runner with token-
preserving C# formatting. This is same-source variability, not a new speed claim.
The display values follow Microsoft's [revision view](https://learn.microsoft.com/en-us/office/vba/api/word.wdrevisionsview)
and [markup](https://learn.microsoft.com/en-us/office/vba/api/word.wdrevisionsmarkup)
definitions.

The broader comment fixture completed **17 contextual actions / 290 checks**
(new draft, posted edit and reply), retaining body text, before its separate
Comments-panel selector failed. Its metadata-binding assertion was replaced by
an explicitly nonediting probe of the actual document-replacement exclusion;
the general Word focus check intentionally accepts modern comments. Normal
fixture seeding now emits a balanced text-free Shift signal, excluded from
recovery mode. Repeats still show intermittent initial focus timing. A scoped
`ContextualAll` option is prepared, but no complete pass of that final scope or
the Comments panel is claimed. An unverified native-typing experiment was not
retained. The last run was refused before opening Word because the 15-minute
diagnostic queue probe had expired; zero was not treated as reliable settlement.

The running trial retains the 1.9.8 label and includes the Devanagari addition.
A separate guarded rollback restores the pre-test `db98f9b3…2d648` ordinary
binary while keeping latest private preferences; neither rollback nor trial
startup changes the installed profile. The fault marker is retired after
archiving. Evidence is under ignored `out/word-final-recovery-2026-10-30`.
No installer, publication, push, version or accepted-baseline update occurred.

# Requested Devanagari spelling — 30 October 2026

The user narrowed the vocabulary request to `devanagari` → `devanāgarī`, leaving
Bengali, Tamil and other terms untouched. The existing English dictionary
protects the unaccented word, so a vocabulary entry alone was insufficient.
The change adds that entry and only that word to the existing explicit Indic
recognition exceptions. No indexed data, manifests or formatting rules change.

`RecognitionTests` adds 108 assertions spanning lowercase/title/all-capital forms,
Unicode/Balarama/plain output, word/copied/selected/Ctrl plans, canonical-first
cycles, explicit suffixes, protected URLs and unchanged Bengali/Tamil output and
styling. Temporary private-fixture customization entries prove that personal
protection and preferred spelling override the new default; the fixture file's
original bytes are restored in `finally`. Installed preferences are never used.
The new checks fail 42 assertions against the prior runtime. After the change,
the suite passes **218,637** assertions; existing vocabulary/alias loops also
exercise the new entry.

All **10 focused suites / 7,394,952 assertions pass**: recognition, book style,
preferences, occurrence preferences, font cycling, copied conversion, stringent
English protection, short Ctrl forms, selection and replay. All **46 frozen
runtime/test/build hashes** match after the run. Three changed C# files pass
formatting/token-preservation checks. The explicit C# 5 resident-only candidate,
SHA-256 `3ec3c1ca2081489e65e9774b326e5b35a15a319b3ce5c28742eb9fb59e209c76`,
passes **19 compiled-app checks**. A nonresident probe confirms canonical output
and case variants while Bengali/Tamil stay English-protected. All **1,865
development audit checks**, **68 documentation links/four image headers** and
the whitespace check pass; data integrity and accepted baselines are unchanged.

Evidence is retained under ignored `out/devanagari-term-2026-10-30`, including
before/after output, source snapshot, frozen hashes and focused results. All
newly generated private test data was removed after reporting (194,990,801 bytes);
the pre-existing reusable probe fixture and recovery material were retained. All
**24 installed hashes** still match the pre-review snapshot, and the active
trial executable remains the preceding `db98f9b3…2d648` build. The vocabulary
addition is staged only: no live Word test, resident switch, installed update,
installer, release, push or accepted baseline refresh was performed. The separate
simulated comment-focus recovery check remains pending.

# Word Ctrl+F Navigation query — 30 October 2026

The user reported failure in Word's Navigation-panel search bar. Document-focused
shortcut preparation was rejecting this separate editor; process-wide Word
replacement/font/replay decisions also had to exclude it. Existing uncommitted
work, including both final-review fixes, was snapshotted and retained.

The user authorized metadata-only inspection of an empty field and a later
read-only check of a prepared `krsna` query. Observed native focus is
`RICHEDIT60W`, ID 1, under `NetUICtrlNotifySink`, `NetUIHWND`, `NUIPane`,
`MsoWorkPane`, `MsoCommandBar`, `MsoCommandBarDock`, and the foreground `OpusApp`.
UIA exposes a focused enabled non-password `NetUITextbox` Edit. The native reader
finds the exact prepared five-character query and collapsed caret at 5. A second
read-only probe using the revised adapter verifies its word, last letter, absent
selection and Unicode Auto mode. No input, conversion, search submission,
document text, Word Selection automation or clipboard was used by these probes.

`WordSearchField` revalidates the complete bounded native path and process/frame
ownership. Writable live UIA metadata must refer to that same native field.
Structural query recognition blocks document bridges even during a provider
failure; missing/stale query metadata cannot fall back to a document provider.
The existing native Rich Edit reader supplies query text/selection/caret.
Captured targets also retain query-specific revalidation during commit and undo.

Query conversion bypasses document paste-menu preparation, font lookup/snapshots,
native Word letter/replay anchors and Word Selection replacement. Modern comments
keep their established text-field behavior. `DocumentFocused` is not widened.
Query Auto mode uses Unicode; explicit Balarama/Unicode overrides remain, without
automatic italics. Query output containing C0/DEL controls is declined before
typing/clipboard work, covering word, selected, copied and trigger paths. Other
surfaces retain their multiline output behavior. This is deliberately the
observed docked native layout, without localized titles or guessed body ranges.

All **44 isolated suites / 26,999,169 assertions pass** against **130 frozen
source/test/build hashes**, including **1,177 Word checks**. Sixty-eight new query
assertions cover ownership, native/editor identity, stale/missing/cyclic paths,
disabled/read-only/password/multiline fields, document fallback, unchanged body
focus, provider precedence, encoding and control output. Three private mutations
(foreign process, document routing, permitted controls) are rejected. The exact
app candidate is SHA-256
`db98f9b3ea0cb22c17343e3f786e3b2252ed30180d310c816a2ac6f7c212d648`;
its explicit C# 5 build passes **19 compiled-app checks** using retained private
probe data. Documentation and **1,865 development audit checks** pass; data and
accepted release baselines are unchanged.

A guarded temporary-trial switch defaults to read-only preflight. An initial
Windows PowerShell 5.1 run rejected a
false mismatch: wrapping ConvertFrom-Json in `@(...)` yielded one nested array
instead of 24 records. Independent read-only comparison found no changed/missing/
added installed files. Corrected direct array assignments validate all 24 hashes,
44 suite results and three mutations in the same PowerShell environment. No
installed baseline or preference was overwritten to satisfy the check.

The user approved updating only the private trial and user-led checks in
disposable content. The old resident closed normally; the exact candidate is
now resident **60792** with ready helper **83040**. All **22 current private
preference hashes**, **24 installed hashes**, engine configuration and startup
remain unchanged. The old `7f4be2b3…da60` executable and preference snapshot are
retained under ignored `out/word-speed-trial/word-search-rollback-20261030-123953-467`.
`word-search-switch.json` and session metadata record the switch. The same
guarded script can restore that executable while keeping latest preferences.

The user confirms query caret conversion, one selected word, repeated cycling,
backtick letter conversion, immediate Backspace restoration and Ctrl+Z after a
selected-query conversion, with the document body unchanged. Ordinary body
conversion and conversion while editing an existing comment also work. No
automated Word input was sent. The separate simulated comment-focus recovery
check remains pending. Installed 1.9.8/startup are
unchanged; normal sign-in still launches the installed release. No installer,
release, push or version update occurred. Evidence and compact private builds
are retained under ignored `out/word-navigation-2026-10-30`.

# Final code review and isolated regression run — 30 October 2026

The user authorized final review and the full isolated tests, retaining the
installed app/settings and requiring permission before live Word tests. Existing
dirty files were copied into a small ignored source snapshot before edits.
Installed engine/config/state/personal hashes, startup and the running private
resident identity were recorded read-only. No resident was restarted or updated.

The review covered input-thread registration, dispatch, recovery and shutdown;
input-gate/provider boundaries; focus-helper lifecycle, session retirement and
locator resolution; clipboard ownership/protocol/provider failure; Word revision
enumeration, plain-run proof, source fonts and native undo/rollback; Acrobat's
guarded native edit preference; and the integration tools' desktop boundaries.
Two actionable findings were corrected:

1. Clipboard capture rejected unsuccessful helper exit after a complete frame,
   but did not mark that late failure for unchanged-sequence suppression. A
   provider could therefore repeatedly fault or hang after closing stdout.
   A new real-child regression fails against the preceding production source.
   Failed/late exit now enters the existing failure cache. Twelve added checks
   cover post-frame crash/hang, owned-child cleanup, suppression, recovery on a
   new sequence, normal unsupported refusal and startup failure. All **52**
   clipboard checks pass without system clipboard access.
2. The hook owner's status logs were already asynchronous, but resident callback
   diagnostics/operational logs could still append/rotate files on the hook
   thread. Thread-local dispatch identity now routes those writes away from the
   callback; non-hook callers retain their synchronous contract. Five added
   injected-backend checks prove message delivery, callback return/later input
   while the writer stalls, and failed-logger containment. All **26** hook checks
   pass; a private synchronous-log mutation fails the stalled-writer test.

The final source was frozen before the complete rerun: **128 runtime/test/build
hashes** still match afterward. All **44 suites / 26,999,101 assertions pass**,
including **1,109 Word** assertions. The first complete 44-suite pass, before the
additional hook-log change, is archived separately and is not substituted for
the final result. Twenty-six Python unit tests, the pinned local Hindi sources,
all **1,850 development audit checks**, documentation, and C# 5 formatting/token
checks for **36 changed files** pass. Accepted source/data baselines and version
were not refreshed. The optional Black check was attempted but unavailable
because that Python environment has no `black` module; Python source was not
changed or reformatted, and no package installation was performed.

The exact final private app is SHA-256
`ff08e387dcfb99f8bb530911a68989549f20c593b55e1810e48ffa7d4e7538fb`,
compiled with explicit C# 5 flags and the production runtime manifest/icon. It
passes **19 compiled-app checks** using the retained isolated fixture, without
calling the resident entry point. No app/data installation was generated.
Both Word tools compile from final source and pass default-deny checks; the body
fixture also passes **155** reporting/oracle assertions. These are compile/pure
checks, not native Word runs or simulated-focus-loss recovery evidence.

After the full suite finished, all **17 engine benchmark cases** passed their
150 ms warm p95 budget with 301 warm samples per case. The worst p95 was
**7.183 ms**, selected-long-passage. Engine timing excludes native editor access,
clipboard work, gesture recognition and OS input replay; no new Word timing
claim is made.

Final read-only preservation verifies all **24 installed file hashes** and the
startup value. Trial resident **77976** and its unchanged `7f4be2b3…da60` executable
remain running; the new private build was never started. Its helper remains
owned by that resident. Only owned, link-free disposable full-suite/benchmark
fixture directories were removed after their reports were saved, including the
first run's redundant data copy. Older retained probe data, current trial data,
recovery archives and crash dumps were preserved. Evidence, manifests, negative
regressions and reports are under ignored `out/final-review-2026-10-30`.

No Word/Excel session or document was inspected or controlled. The separate live
Word recovery check still requires fresh approval and preparation of the revised
temporary build. No installer, release, push or installed configuration change
was made.

# Posted-comment failure was installed 1.9.8 — 30 October 2026

The user reported that new comments and replies worked but editing an existing
comment did not. Read-only process inspection found installed resident **88152**
and its helper **57048**, both started that morning. The temporary trial was not
running despite the previous session JSON still saying `running`. Startup still
points to the unchanged installed executable; the trial was deliberately never
registered for automatic startup. No claim is made about why the earlier private
resident stopped.

An isolated Framework reflection probe loaded each executable without invoking
the resident entry point. The installed SHA-256 `bab32562…08142` editor guard
returns true for `cardEditor_fixture` and false for `postEditor_fixture`. The
verified private SHA-256 `7f4be2b3…da60` returns true for both. Both reject `Body`
and the empty `postEditor_` prefix. This exactly accounts for the reported
surface difference without inventing another focus-helper regression. The probe
uses no Word, input, hooks or clipboard. Evidence is under ignored
`out/comment-edit-investigation-2026-10-30`.

Resumed the previously authorized trial using a guarded, read-only-by-default
helper. It validates exact executable/profile identities, startup, link-free
paths and native hidden-window/process ownership, refuses open KeyJaya dialogs,
then closes installed KeyJaya normally and starts only the existing private copy.
It never replaces binaries or copies one preference profile over the other.
Post-close installed hashes form the current end-trial preservation baseline;
the earlier baseline and session are retained in the small resume evidence
folder. In this run all **24 installed files also match before normal close**,
all **22 private preference files** match, and startup is unchanged. The one
pre-existing learned-spelling difference between the two profiles remains intact.

Resident **77976** and its ready helper **90616** run the unchanged verified
`7f4be2b3…da60` build. `out/word-speed-trial/resume-result.json` and the updated
session record the resumption. The user then confirmed that conversion while
editing a posted comment works. No Word/Excel automation, installer, release,
push, startup change or runtime-source change was performed. The separate
simulated silent-focus recovery check remains outstanding.

# Temporary batching trial updated — 29 October 2026

After reviewing the completed native comparison, the user explicitly authorized
switching only the existing temporary trial and retaining its current private
preferences. The running executable is now SHA-256
`7f4be2b309969f5d5df6eecea1555006c79ebe4b22f27b9c663273a85caeda60`,
resident **82204**, focus helper **82920**, with its ordinary readiness record.
The version label stays 1.9.8.0. Current source/native evidence/binary hashes and
all 46 comparison source entries were checked before the switch.

- The preceding resident closed normally. Its latest saved preferences were
  captured after shutdown; all **22 private preference files** match after
  restart. Nothing was re-cloned from the installed profile. The engine config,
  **24 installed hashes** and startup registration also match their originals.
- The preceding executable `36349c…57c5f`, configuration, current private
  preferences, session metadata and diagnostic log are retained under ignored
  `out/word-speed-trial/batching-rollback-20261029-150845-564`. Its executable hash
  was verified after startup. `batching-switch.json` and `batching-verification.json`
  record the outcome. The guarded script can roll back the executable without
  replacing the latest private preferences; the existing end-trial procedure
  still restores the installed app.
- An initial helper error passed PowerShell null as an empty backup filename
  to `File.Replace`; automatic recovery restored the preceding temporary copy.
  An owned two-file check verified the corrected actual-null argument. Several
  automatic-review attempts then failed because its review model was at capacity
  and were not executed. The later approved retry completed the update with
  preference/installed/startup verification. An additional independent process
  query hit the same review capacity failure; startup had already verified the
  exact resident/window/helper family in the successful update itself.
- No Word or Excel was controlled or tested during this switch. No working
  document, installed binary/configuration, data baseline, accepted source
  baseline, version, installer or release was updated or published.

# Batched Word revision reads — 29 October 2026

The user requested fewer repeated revision checks while preserving the installed
app/settings and current temporary trial. Source snapshots and Git status were
saved under ignored `out/word-revision-speed-2026-10-29`; existing uncommitted work
is retained. No resident switch, installer, release, version/baseline refresh or
push is authorized or performed.

The current candidate retains the original preliminary range-scoped count and
complete filtering but fetches native revisions in batches of at most 32. The
Framework's public `ICustomAdapter` exposes its existing native iterator; local
Framework metadata inspection confirmed this bridge. Its owner stays alive and
the borrowed alias is not released separately. Managed-only providers retain
all their original `MoveNext` calls. There is no cache across edits and the
count-free experiment has not returned.

The [native iterator contract](https://learn.microsoft.com/en-us/windows/win32/api/oaidl/nf-oaidl-ienumvariant-next)
requires processing the fetched items even when a partial final batch returns
`S_FALSE`. The code validates status/fetched-count consistency, null/extra items,
late failures and the 10,000-item ceiling, including revisions beyond the initial
count. Every item retains the original type/deletion-coordinate checks. Failed
providers cannot become an empty projection. Range text/bounds, font restoration,
focus/generation and owned undo/rollback guards are unchanged.

- Five focused suites pass **1,464,004 assertions**, including **1,104 Word checks**
  and **58 new batch assertions**. They cover native/direct and Framework-style
  adapters, managed fallback, partial/full/end batches, later deletions/unsupported
  types, growth and malformed/failed results. No desktop/Word/clipboard is used.
- Three private mutations are rejected: dropping a partial final batch, disabling
  the native bridge, and weakening the native 10,000-item limit. The final fake
  adapter also models the old valid per-item path, so call-reduction checks do
  not depend on making the original API fail artificially.
- The separate C# 5 app build passes **19 compiled-app checks** without starting
  its resident. The comparison compiles both lanes against the exact pre-change
  adapter and passes 155 reporting/oracle checks and default-deny probes.
- The approved native comparison **passes all nine cases, both tracking states,
  144 samples and 6,384 assertions**, with formatting, exact text, retained
  selection/caret, one-step conversion undo and the prior undo history intact.
  The source in its preparation manifest matches the current source and private
  app manifest. Evidence: `run-f482314706234f2f92c79f705d19b801`.
- Native patch traces confirm the intended reduction: tracked word patch calls
  fall **24 to 23**, passage calls **48 to 46**, and mixed-format calls **96 to 92**.
  Patch-stage medians fell in every tracked text-changing fixture (about 8–24%).
  No type/end/count checks were dropped to achieve this reduction.
- Total tracked passage median was **371.48 to 295.69 ms (20.40%)**. Tracked
  existing-deletion, length-changing, character-style and mixed-word cases had
  13–19% lower medians. The total tracked caret-word median was 7.03% worse and
  selected-word only 2.25% better; untracked cases varied in both directions.
  These small samples do not establish a general individual-word gain. The
  timing scope is known-output native discovery/selection, transaction and
  continuation finalization; full physical-key/resident latency is excluded.
  Each lane/case/tracking state has one warmup and three measured samples.

The batch candidate's first approved run (`run-873de8bc1f164189bfec86299ae6aac4`)
stopped after 51 successful samples when ChatGPT became foreground during an
edit. The native transaction rolled back and the owned scratch **33848** closed.
The user confirmed keyboard/mouse use. After fresh explicit readiness, the
complete matrix above passed and scratch **74744** closed. Original Word
**40200**, trial resident **44092** and helper **1052** remained running. These
interrupted runs are not silently discarded or accepted as speed evidence.

The earlier single-item experiment is archived, not active source. Its original
property-only lookup passed 44 suites / 26,999,048 assertions but failed in Word
with HRESULT `0x800A16E6`. Word exposes
[`Item` through method dispatch](https://learn.microsoft.com/en-us/office/vba/api/word.revisions.item);
the corrected method/property binding added a regression and passed all 44 suites
/ 26,999,049 assertions. The approved repeated native run then stopped when focus
moved to ChatGPT before undo: 60 successful samples and one interrupted sample
(`run-a5cab7177dc24a7ab874b6091d69a0f5`). The first failed run is
`run-4f138cbe4a8d4ca089e4e385c4b39867`. Neither is accepted speed evidence. The
scratch processes **84388** and **90696** both closed without touching original
Word **40200**. The candidate now uses full batched enumeration, without the
single-item lookup or its additional confirming count.

All **24 installed hashes** and startup registration are unchanged; trial
resident **44092** and helper **1052** remain running. The paused quiet session
has ended; no further Word tests start without fresh permission/readiness.
Four redundant data fixtures were removed (**779,963,816 bytes**), retaining
compact evidence and the older compiled-app probe fixture. Final batch reports,
source/binary manifests and negative probes remain in the private output folder.

# Automated Word fixture and speed investigation — 29 October 2026

The user requested repair of the recovery automation while continuing the exact
`36349c…57c5f` temporary trial. This follow-up does not switch/restart that trial,
update the installed app, or change preferences, startup registration or accepted
release outputs. Desktop tests require fresh readiness.

- The earlier scratch runs did not prove body typing focus or creation of a
  comment before waiting for its editor. The fixture now activates its owned
  pane, checks stable document focus, verifies that `ReviewNewComment` is enabled,
  invokes that built-in Word command and validates a writable comment plus the
  unchanged body. Range selection alone is insufficient evidence. The identifier
  is checked against Microsoft's current Word control list. Setup stages and
  interrupted/disabled/missing/read-only/body-change cases are explicit.
- `SetupOnly` validates those steps without conversion dispatch, helper fault
  injection or a resident restart. The runner itself now refuses desktop work
  without its explicit authorization argument; compile-only preparation verifies
  that refusal. The wrapper adds a 180-second default deadline, stopping only its
  own test runner on expiry. A hung scratch Word window is left for review.
- The first newly approved setup run opened a comment, but the fixture's own
  notification monitor missed it. Its failure metadata showed a focused,
  writable `cardEditor_` in the owned frame. Setup now also resolves a unique
  eligible editor directly inside that frame/process and rechecks its live
  ownership/focus. It never sets focus to a guessed candidate. The repeated
  setup-only run **passed nine checks**, including unchanged body and no
  conversion dispatch (`native-f087ae40db1548569fd4b041063dbefd`). Its scratch
  window closed normally. The discovery policy was then extracted for isolated
  regressions covering ambiguity, interrupted ownership and stale focus; no
  further desktop input was run after the quiet period ended.
- Existing valid speed evidence was reanalysed rather than rerunning Word during
  the user's trial. In the final saved paired matrix
  (`run-20420d9fac274573ba50dcbac0479a29`), candidate warm medians put the word's
  XML proof at approximately 31–33 ms. Passage patch work was approximately
  68 ms without tracking and 141 ms with tracking; only 77 ms of the latter was
  attributed to reflection calls. This is a small native sample, not end-to-end
  keyboard latency. Unattributed time cannot be assumed to be COM release.
- Diagnostic-only coverage now separately times COM releases and includes direct
  managed/native revision-enumerator calls. Isolated checks preserve results,
  original exceptions, disabled behavior and privacy. No font, revision-count,
  exact-text, undo or focus safeguard has been removed, and no new speedup is
  claimed. The running trial does not contain this additional instrumentation.
- The approved same-source word/passage calibration reached its remaining
  quiet-session deadline after **28 completed samples**. The raw report remains
  incomplete (`run-a0b6fef2bb2447c39980885ff19cd84f`); it is not accepted as a
  completed matrix or a speed comparison. Diagnostic attribution in the saved
  samples puts patch-stage COM release at approximately **1–4 ms**, while the
  formerly unattributed tracked-edit time is now largely covered by direct
  revision enumeration. Revision `Count` and `MoveNext` are expensive calls;
  these are the next profiling/round-trip-reduction targets. XML proof and the
  separate 40 ms resident body-settling wait remain potential costs. The settling
  wait protects immediate-after-paste behavior and has not been removed. No
  revision checks, font preservation or undo safeguards were weakened.
- The timed-out test runner alone was stopped. Its Word process **86776** remained
  open and was subsequently closed without saving through its verified frame,
  creation time and single-document ownership. Original Word **40200**, trial
  resident **44092** and helper **1052** remained running. No further Word test
  starts without fresh permission/readiness.
- Final focused checks pass: **1,046 Word assertions**, including **24 setup**
  and **20 timing** regressions. Final compile-only preparation verifies C# 5
  compilation and the runner's default refusal of desktop execution. Documentation
  links and the development audit pass; the accepted source baseline and release
  outputs have not been refreshed. All **24 installed hashes** and the startup
  setting remain unchanged.
- Final review also guards conversion scopes against an expired 15-minute
  diagnostic queue probe. A zero reply after expiry is not proof that queued
  input has settled. Setup-only runs do not use that probe; other scopes require
  enough diagnostic time for their deadline before opening any document. This
  check never restarts the trial or extends runtime diagnostics. Five isolated
  checks against the actual wrapper guard pass without starting Word or reading
  desktop state. Four redundant test data copies created during this follow-up
  were removed (**779,963,204 bytes**); compact evidence, the older retained
  fixture, trial files and recovery archives remain.

Preparation, source backups, the published control list and compact phase
analysis are private under `out/word-followup-2026-10-29`. Native setup validation
passed; the same-source calibration is incomplete and retained as such. The full
silent-helper recovery scenario remains unverified and
requires its separately prepared fault-test copy after an approved switch.

# Comment-focus and clipboard recovery candidate — 29 October 2026

The user requested a fix for the intermittent comment failure without restarting
KeyJaya or changing the installation. The original focus failure site is unconfirmed:
the old resident/helper stayed alive, and restarting the unchanged trial restored
new/edit/reply conversion. Review identified two reproducible recovery gaps: an
alive but silent helper was never refreshed, and resolving its UIA locator on the
pipe reader could stop later metadata delivery.

- Missing live focus in foreground Word now schedules a helper-only refresh
  after 250 ms. Fresh metadata or verified live focus cancels it; repeated guards
  coalesce and refreshes are at least five seconds apart. The resident and hooks
  remain running. The triggering edit can skip safely and may need another tap.
- Locators publish without UIA work on the pipe reader. One long-lived MTA
  worker resolves them with a single pending slot and at most a 150 ms caller
  wait. Identical metadata coalesces within its own helper session.
  Transient failures retry; a newer event, clear or retired helper invalidates
  a lookup that completes late. Every actual edit retains its existing live
  focus, process/card/host, writable-text, expected-text and generation guards.
- A refreshed helper can catch up with an already-focused comment using global
  focus or its verified native-caret location. This optional metadata-only probe
  runs in the disposable child, reads no text/pointer/clipboard, sends no input,
  and loses to any newer real focus event. Resident provider calls are not given
  a new timeout guarantee.
- All **44 isolated suites passed: 26,998,993 assertions, zero failures**.
  The **1,013 Word assertions** include **73 supervisor** and **139 comment-focus**
  checks. Actual pipe children simulate a ready but silent helper. Four deliberate
  mutations disabling recovery or accepting stale/unverified targets are rejected.
  No desktop UIA subscription, Word window or installed profile is used by these
  regressions. Explicit C# 5 compilation and **19 compiled-app checks** pass.
- Before the first approved switch, the previous trial was already absent.
  Windows recorded a resident crash at **10:21 IST**, PID **76192**, exception
  **0xc0000005** / CLR **80131506**. The existing 22 MB dump identifies main STA
  thread **82512**, `Marshal.GetObjectForIUnknown` and WinForms
  `DataObject.OleConverter.GetDataFromBoundOleDataObject`. Only stack metadata
  was inspected; no strings, locals or clipboard payloads were extracted.
- All foreign WinForms clipboard reads now run only in a short-lived STA child.
  Resident plain-text reads use the guarded native Unicode reader. Full backups
  cross a private typed pipe; unknown objects/invalid payloads decline the whole
  capture. Existing restore/sequence checks remain, and a failed unchanged
  provider cannot cause a loop of crashing children. **40 isolated clipboard
  assertions** cover typed copies, bitmap pixels/alpha/DPI, malformed/truncated
  data, child death, timeout, stale reads and retry after a new sequence. The
  protocol never uses a CLR-object deserializer or writes the clipboard.
- The first candidate (`d17ad1…f7ac3eb`) was launched in the private profile.
  Its frozen-helper test and ordinary-comment follow-up both stopped at editor
  discovery before conversion. The owned scratch processes closed; their bodies
  stayed unchanged. The previous binary was restored, resident **79904**, helper
  **83668**. Its original Word process **40200** stayed untouched. The first
  attempt is not accepted as native recovery proof.
- An isolated probe against that saved candidate reproduced locator callbacks
  executing on an STA caller. The revision retains a dedicated MTA resolver;
  that negative regression and metadata-coalescing/stale-result checks pass.
  The revised binary SHA-256 is
  `36349cace58d128926763ea93af3a7aa788deb0188b4d771e6c940e93ad57c5f`.
  It is now running in the private trial. The user confirmed Ctrl conversion in
  **new comments, existing-comment edits and replies** in this exact revision.
  The fresh log contains successful locator resolution and comment discovery
  for all three user-led actions. The tested Word body adapter is unchanged. All
  **24 installed hashes** and startup
  registration remain unchanged. No installer/release/baseline/version change
  or push occurred.
- The revised `ContextualRecovery` scratch test is C# 5 compiled. A separate
  development-only entry point substitutes a ready-but-silent first child;
  subsequent helpers and all runtime sources are production code. No real UIA
  client is suspended. PID/parent/path/command line and a private fixture marker
  identify that silent child. The test requires its automatic recovery,
  unchanged resident PID, conversion without refocusing, plain formatting, native
  undo and unchanged body. Cleanup stops only that test helper if still frozen.
  Its 15-second emergency deadline releases only the injected helper fault.
  The separately authorized silent-child run also stopped before conversion at
  fixture editor discovery (`native-a58c7acc160e4903a02c75194963033b`). Its scratch
  window closed and the silent child was cleaned up. This does not establish
  automatic recovery in native Word. The exact production revision was then
  started, removing the test-only entry point/markers, resident **44092**, helper
  **1052**. Its normal comment paths passed the user's three manual checks.
- The authorized read-only clipboard probe **passed**: three formats captured,
  sequence unchanged, no clipboard writes and no contents reported. It used the
  production snapshot code/child entry point without starting a resident or
  loading preferences. Current trials keep the installed app/settings unchanged;
  no further Word automation runs without fresh readiness. Longer-term stability
  and the native simulated-notification-loss scenario remain unverified.

Compact reports, deliberate mutations, source/binary manifests and the prepared
candidate remain private under `out/comment-recovery-2026-10-29`. A guarded switch
with rollback to the preceding temporary copy is prepared under the trial folder.

# Temporary Word speed trial running — 29 October 2026

The user requested an isolated temporary build after the native speed checks.
The trial is running from ignored `out/word-speed-trial/profile/engine`, with
one resident and its own focus helper. It started at approximately **09:03 IST**.
The executable retains the 1.9.8.0 version label; its SHA-256 is
`7a089ba406c054c5ada4ef885bfefcda72a47bf3b665441f05fe401e4f075b15`.
All runtime sources match the final tested source, including Word adapter
`a20eee73b37d27429c42e2fa403b60917097fb9723bbbe28942f4afa38bcb6d4`.

- Compiled explicitly as C# 5 with the Framework/DPI configuration beside the
  executable. All **19 compiled-app checks** passed against this exact binary.
- One private runtime copy was prepared; **29 runtime input files** matched their
  release inputs. No installer or uninstaller was built and `out/app` was preserved.
- The installed 1.9.8 resident closed normally after its hidden-window/process
  ownership and lack of visible settings dialogs were verified. All **24 installed
  program/configuration/state hashes** matched before/after switching. All **22
  copied preference files** match the originals after trial startup, including
  custom triggers and spelling choices. Startup registration is unchanged.
- The trial binary and its helper both run from the private engine directory;
  children and restarts therefore retain the separate root. Preferences learned
  during the trial stay there and are not copied back automatically.
- Startup logging confirms resident/hook and helper readiness. Bounded diagnostics
  use the existing 15-minute/2,000-record limit; operational recovery logging
  continues. There is no automatic trial timeout.
- The user initially reported that **new-comment, existing-comment and reply
  conversions all failed**. Ctrl taps reached the resident, but the log did not
  reach comment-editor preparation/discovery. Both resident and helper remained
  running. After an authorized restart of only the temporary copy, with the
  **same executable**, the user confirmed that all three comment actions work.
  The fresh log contains resolved comment locators and Ctrl comment discovery.
  This is recovery after restart, not a confirmed code fix or stability result.
- Failure and recovery logs, session metadata and original focus/runtime sources
  are retained privately under `comment-investigation`. Authorized metadata-only
  checks read no document/comment text; the one approved activation found body
  focus rather than the prepared comment. No runtime source was changed for this
  incident. Review found that helper exit/startup is supervised, but an alive
  event subscription or resident locator lookup can still stall; the evidence
  does not identify which, if either, caused this incident.
- The separately authorized scratch automation ran twice. Each run passed its
  six new-draft conversion/undo cases, then stopped because the fixture could
  not discover the posted card. These are incomplete runs, not automated proof
  of posted-edit/reply correctness. Both owned scratch windows closed and the
  user was told to resume normal computer use. Working documents were not read
  or edited. Fixture discovery, private-root targeting and an optional
  `ContextualNew` scope were revised and compile as C# 5; their latest changes
  remain unverified live. No further desktop tests run without fresh readiness.
- A guarded `end_trial.ps1` is prepared to close only the trial, verify installed
  hashes/startup and restart the existing installed executable. Private evidence
  is retained for review before later cleanup. No installed update, version or
  accepted-baseline refresh, release publication or Git push occurred.

Preparation, source/runtime manifests, original hashes, session/process IDs and
the restoration procedure remain private under `out/word-speed-trial`. Do not
package this folder or publish the copied preferences. The live user trial is
not yet evidence of perceived speed or longer-term stability.

# Word speed validated — 29 October 2026

The user authorized Word speed improvements with automated formatting, Track
Changes and undo checks, keeping installed 1.9.8 and preferences unchanged.
Earlier uncommitted work remains. The original development adapter was saved as
`out/word-speed-2026-10-29/WordSelection.baseline.cs`, SHA-256
`c85c714285a22d3d69d861ea16ecd0106e81c027c1ce1eff35e2cf0b9b84d18b`.
No accepted source baseline or release artifact was rebuilt/refreshed.

The complete segmented native run passed **nine cases, 144 samples and 6,384
assertions**, with tracking off/on, one warmup and three measured repetitions per
lane. It is recorded in `segmented-e0bf21e1302e42daaa16f92ca1f458ac`. Each case
has an independent complete report; no failed samples were removed from earlier
attempts. Exact text/neighbors, font/style/language, retained selection/caret,
typing continuation, existing deletions and both conversion/prior-edit undo
passed. All nine scratch Word processes exited; original Word and installed
KeyJaya kept their process IDs.

Warm native medians in milliseconds, reference to candidate:

| Case | Tracking off | Tracking on |
| --- | ---: | ---: |
| Ctrl-word | 215.47 → 190.14 | 385.02 → 233.05 |
| Selected word | 273.52 → 152.24 | 353.49 → 191.31 |
| Selected passage | 238.62 → 167.06 | 372.44 → 277.19 |
| Mixed-format passage | 256.39 → 225.15 | 425.55 → 440.62 |
| Formatting only | 205.18 → 133.58 | 132.95 → 139.03 |
| Existing deletion | 222.50 → 222.23 | 382.17 → 356.81 |
| Changed length | 216.25 → 264.32 | 356.74 → 407.30 |
| Character style | 185.73 → 260.89 | 364.26 → 377.95 |
| Mixed-font word | 182.86 → 217.52 | 371.82 → 478.09 |

The principal word/passage cases improved, but these small samples are not
latency guarantees. The changed-length lanes made identical native calls and
performed no XML export, so their difference does not establish added work.
The styled/mixed-word traces did expose a redundant 20–30 ms XML export before
declining grouping. The latest source rejects character styles and an empty
font name through cheaper metadata first. Unavailable metadata still requires
the full XML proof. Counted regressions verify zero exports for those known
fallbacks and that failed metadata/XML never authorizes grouping.

The full native matrix tested adapter SHA-256
`eaefe7f38ff57176ac7108552a3dd5e684fb9b28709a3a97ddf833b84a684fb5`.
The final early-rejection source is
`a20eee73b37d27429c42e2fa403b60917097fb9723bbbe28942f4afa38bcb6d4`;
its focused live follow-up passed **80 samples / 3,520 assertions** across
Ctrl-word, selected word, selected passage, character style and mixed-font word,
both tracking states (`run-20420d9fac274573ba50dcbac0479a29`). All changed native
paths retained formatting, style, language, selection/caret and both undo levels.
Both fallback traces confirm the cheap rejection and no formatting XML export.
The other four matrix paths bypass this new rejection and remain unchanged.

Final-source warm medians in milliseconds; each lane has one warmup and three
measured repetitions, with alternating order:

| Case | Tracking | Reference | Final | Reduction |
| --- | --- | ---: | ---: | ---: |
| Ctrl-word | Off | 205.24 | 172.91 | 15.75% |
| Ctrl-word | On | 341.50 | 236.18 | 30.84% |
| Selected word | Off | 179.10 | 146.54 | 18.18% |
| Selected word | On | 320.62 | 208.47 | 34.98% |
| Selected passage | Off | 204.21 | 165.39 | 19.01% |
| Selected passage | On | 351.67 | 259.74 | 26.14% |
| Character style | Off | 200.66 | 183.62 | 8.49% |
| Character style | On | 342.74 | 345.26 | -0.73% |
| Mixed-font word | Off | 163.87 | 173.53 | -5.89% |
| Mixed-font word | On | 289.09 | 300.06 | -3.80% |

The main word/passage fixtures improved about **16–35%** against the same
font-preserving reference. Fallbacks still pay a few cheap metadata reads; these
small samples do not establish faster fallback editing. The 20–30 ms XML export
was removed there. Full keyboard/resident latency, mode/engine choice, paste UI
and Modern Comments remain outside these timings; no direct installed-app
speed claim is made. The earlier failed runs remain failed and are not pooled
into these accepted comparisons.

The optimization groups at most 32 adjacent one-to-one changed Latin letters
in the first (rightmost) patch group. A current contiguous native span and exact
single-run range XML must prove ordinary uniform formatting. An explicitly
defined unmodified default character-style marker is permitted; named styles,
mixed runs, tracked markup/gaps, fields, anchors, unsupported properties and
failed exports retain letter edits. Non-main stories, expansion, changed lengths
and other scripts bypass grouping. Context, full owned raw text, foreground and
selection bounds are checked again after export. Fresh inserted projections,
native growth, final formatting/text, continuation and owned rollback remain.

Native tests caught two issues before acceptance:

- Exporting a partial range **inside** an open custom undo record could make the
  first undo leave converted text, even when grouping was declined. Export now
  precedes record creation, refuses an already active record, and proves only
  the first group before edits. A second undo must also restore a known earlier
  scratch edit. Corrected short runs passed both undo checks in both tracking
  states. The first failed report is `run-77948edca7fb47188275c4c17de48625`.
- The expanded named-style oracle found the original adapter changing Georgia
  diacritics to Cambria while retaining the named style. Baseline-first runs
  reproduced this, including one confirming both script-font settings initially
  reported Georgia (`run-43ff5a298c5c4283a844bbf9a5a925f4` and
  `run-00306bd3f8e8437bb40297506fe70677`). Every nonempty patch now captures and
  reapplies its source font, including one-letter changes. Its isolated injected
  substitution regression passes. The revised native Georgia check passed
  **20 samples / 820 assertions**, both tracking states, with original style,
  fonts and both undo levels intact (`run-b478655d5acf4fb791c998f4db134993`).

The first large comparison stopped at the named-style warmup after 168 successful
samples (`run-c01d31f730134ca98e7b18470c027b8c`). It is a failed comparison and
provides no accepted overall speed claim. A preceding short caret-only pilot
passed 16 samples; its exploratory medians were about 16%/27% faster with tracking
off/on, but it predates the final font fix and does not establish the final result.
Word's new-document template initially gave plain fixture text the Book Title
character style. Setup now explicitly resets only scratch paragraph/character
styles. The named-style and mixed-word cases remain separate preservation tests.

The revised comparison reference is the immutable original adapter **plus the
same source-font fix**, with no batching. Its SHA-256 is
`6871bc3e32e58c81badc9f32088051749c6e1518809d9aa7cd9f94f35d78f26c`;
`controlled-baseline.json` records the one shared change and reason. This isolates
batching performance between two implementations required to pass the same font
oracle; it is not a direct installed-1.9.8 keyboard-latency comparison.

Nine native fixtures check text/neighbors, fonts, style identity, language,
selection/caret, typing continuation, retained deletion and both undo levels,
with Track Changes off/on. They compile as C# 5; **155** pure fixture/report/XML
contracts pass. **42** production-loop regressions include counted writes/scans,
mixed/style/export fallbacks, cancellation, partial rollback evidence and font
substitution. Disabling grouping, one-letter font restoration or the cheap
formatting rejection in isolated source copies makes its regression fail. No Word is started by
these isolated checks.

- The revised focused Word suite passes **975 assertions** (automation total
  **155**, patch/font regressions **42**); all **19 compiled-app
  checks**, **26 Python tests**, **120 C# formatting/token checks**, **1,814
  development audit checks**, **66 documentation links/four image headers** and
  five pinned-source hashes pass. The latest full **44-suite run passed
  26,998,915 assertions with zero failures** on the final early-rejection source.
- All **17 engine budgets** passed earlier in this task, highest warm p95
  **4.058 ms**, initialization **173.92 ms**. These exclude Word automation.
- All **24 checked installed engine/configuration/state hashes** remain
  unchanged. Only the original Word process (40200) and installed resident/helper
  (71168/69316) remained after scratch cleanup. No installed switch/update,
  installer build, release, push or version/baseline refresh occurred. The existing
  installer hash remains `57b2b62e697a85e87754c110cbfd61022b544ac7a802487ac8b3b71a03b8f8d6`.
- Two new complete-comparison attempts stopped on focus guards. The first
  (`run-b58f67ca62f44a0e99fbf92d72d1dab7`) returned to ChatGPT before any edit.
  The second (`run-0b2010f985924985a9b946415a784a9f`) passed 182 samples,
  then Windows reported no foreground window before a baseline undo check.
  Both reports remain failed; no overall speed claim is accepted from them.
  Read-only inspection afterward found the normal desktop and original app
  processes, without changing settings or sending input.
- Verification now waits at most 100 ms for a null foreground reading, only
  before its undo oracle. A different foreground window aborts immediately and
  the exact original native focus proof is still required. It never reclaims
  focus. Production conversion guards are unchanged. The subsequent segmented
  matrix and final cost-rejection follow-up passed as recorded above. All scratch
  windows closed; original Word/resident/helper retained their process IDs.
  No further desktop testing is required for this change.

Evidence is under ignored `out/word-speed-2026-10-29` and the named compact runs
under `out/word-performance-tests`. No dictionary/runtime installation is cloned
for native comparisons. Commands and measured scope are in [Testing](TESTING.md).
The current copy remains development source, not installed.

# Word automation prepared and native body calibration passed — 28 October 2026

The user asked to automate the proposed Word correctness and performance tests.
The new `tools/test_word_performance.ps1` defaults to compilation and isolated
self-tests. It creates no installed/test profile and copies no runtime dictionary
pack. One build directory is reused; each compact run records source/binary/
fixture hashes and both adapter source snapshots. Optional baseline source
overrides only `WordSelection.cs`, preserving identical test code and other
runtime inputs. The accepted release artifacts and existing uncommitted work
were preserved. No production source changed in this automation pass.

Seven canonical fixtures cover a caret word, selected word, passage, mixed
fonts, formatting-only conversion, a retained tracked deletion and changed text
length. Both tracking states and both implementation lanes receive identical
warmups/repetitions in alternating order. A separately implemented range-XML
oracle checks the entire visible scratch body; font assertions check name,
size, bold, italic, color and underline at every visible character. Caret/
selection retention, original typing style after owned continuation finalization,
one-step undo, original fonts/neighbors and the initial revision population are
asserted. The model contracts reject invalid fixtures, duplicate/missing/failed
samples, failed warmups and invalid timing values. A partial-case run is marked
as partial and cannot be mistaken for the complete matrix.

The focused regression run passed **901 Word snapshot assertions** (including
the new **123 automation contracts**) and **15,799 single-letter/paste-guard
assertions**. Default preparation and its default-deny entry-point probe passed
under PowerShell 7 and Windows PowerShell 5.1 with explicit C# 5 compilation.
The existing native Word fault fixture and revised Modern Comments fixture also
compiled without opening Word. All **26 Python tests**, **119 C# formatting/token
checks**, **1,807 development-audit checks**, **61 documentation links** and four
image headers passed. The previously prepared installer and accepted baseline
hashes stayed unchanged. The prior 44-suite production verification remains
recorded below; no production behavior was changed by these test additions.

The user separately authorized live automation in a new exclusively owned Word
window, keeping installed KeyJaya running. Early fixture validation exposed an
incorrect test order: inspecting revisions can reset Word's pending typing
format, so native continuation must finalize before that inspection. The fixture
was aligned with the resident's native focus callback and bounded initial body
settling; no production guard was changed. Early interrupted runs were rejected.
The user confirmed they had been using the computer, then explicitly provided
quiet periods. A 16-repetition caret calibration passed, followed by a full quiet
matrix. No incomplete/interrupted run was accepted as a performance comparison.

The full run passed **112 repetitions and 4,848 native assertions**: seven
fixtures × two tracking states × two lanes × (one warmup + three measured).
Both lanes used the same adapter source, so this is calibration, not a speedup.
It measures known-output discovery/selection, native replacement and continuation
finalization, with setup and verification excluded. It does not measure engine/
mode choice, resident dispatch, physical Ctrl, OS input replay, Paste Options or
Modern Comments. Small warm sample populations do not establish a latency budget.

The native helper never starts a resident, uses the clipboard, injects Windows
input, reads existing documents or changes installed preferences. It refuses an
existing/shared Word process and aborts when native focus/foreground ownership
changes. Its timeout stops only the owned test helper; a leftover scratch Word
window would be retained for review. Normal cleanup quits only the exclusively
owned single-document Word instance; if another document appears, it closes only
the scratch document. The completed run's Word process exited. The original Word
process **40200**, installed resident **71168** and helper **69316** remained.
All **24 checked installed program/configuration/state hashes** matched their
pre-run values. No installed app was stopped, updated or replaced.

The Modern Comments fixture also received a test-only follow-up: it uses the
current focus resolver instead of the removed private `last` field, opens a
posted fixture comment's Edit action using unique owned English action labels,
requires a live `postEditor_`, and tests word/selection/undo/typing there. It
aborts on desktop interruption and protects documents newly joined to the
scratch process during cleanup. `-ResidentAssembly` permits hash-verifying an
explicitly authorized private diagnostic resident without rebuilding `out/app`.
These extensions compiled; the new UI selectors were **not live-verified** in
this pass, and no resident switch was authorized or performed.

Native evidence is under ignored
`out/word-performance-tests/run-2c128af3b86f497db30772a3b8bd0c89`, with the
preparation manifest, source snapshots, JSON samples/scenario summaries and
stdout/stderr. `out/word-performance-tests/native-verification.json` records
counts, installed-file preservation and process cleanup. Earlier rejected runs
remain separate. The command and coverage boundaries are in [Testing](TESTING.md).
The two new ordinary regression data fixtures were removed after preserving
their results, reclaiming **389,981,602 bytes**. The native comparison used no
runtime-data copies or saved Word document. Small reports/source snapshots and
the shared comparison build remain.
No installer, release, push, version change or baseline refresh occurred.

# Unreleased Word follow-up after installed 1.9.8 — 28 October 2026

The user installed 1.9.8 themselves and reported slow Ctrl-word, one selected
word and passage conversion, with satisfactory backtick/new-comment behavior
and a failure while editing a previously posted comment. Existing uncommitted
work was preserved. Development source remains version 1.9.8 with intentional
drift from its accepted release baseline; the previously prepared installer and
`out/app` release tree were not rebuilt or overwritten.

With explicit permission, a read-only inspector compared control metadata from
Word's global focus, native caret and pointer hit. The posted editor was an
enabled, focused Edit with ID `postEditor_4_1272421377`, writable Value/Text
interfaces and a `cardRoot_` ancestor in Word's owned `_WwG` host. Global focus
still returned the unfocused legacy body provider. Recognizing the posted-editor
prefix fixes that rejection while preserving live focus, process, native host,
comment-card and writable-text checks. The user confirmed that the prepared
diagnostic copy converted text in a previously posted comment.

The private copy used its own profile, cloned preferences and opt-in logging.
New Word timing scopes record stage wall time, reflection-call counts, elapsed
call time, slowest member and commit status without values, arguments, document
names or exception messages. Observers cannot alter results or suppress failures.
The existing diagnostic duration/entry limits still apply. Timings showed native
revision projection and per-patch/formatting work dominating these samples. This
identifies the current cost, not a demonstrated historical cause of a release
slowdown. See [Responsiveness](RESPONSIVENESS.md) for complete sample ranges and
the distinction between native stage, full conversion and engine-only timing.

A direct-enumeration experiment retained revision filtering and the 10,000-item
limit but omitted the preliminary count. The delay moved to enumeration and
the user reported speed about the same, so that experiment was reverted. The
retained body/classic-comment focus change batches identity properties per node
and rechecks foreground ownership. It preserves the process/menu/writable-editor
conditions and twelve-node traversal bound. The final user check passed
Ctrl-word, selected word, passage and immediate after-paste conversion, with
speed again reported about the same. There is no demonstrated overall speedup.
The paste settling wait and native revision, formatting, undo, expected-text,
document/story, generation and rollback safeguards remain.

Review of revision enumeration also found a separate fail-open condition:
IEnumVARIANT failure/unknown statuses ended enumeration as though no revisions
remained. They now reject the projection and release acquired references; an
unavailable count is not an empty list, and invalid/excessive counts refuse the
range. This retains the existing owned rollback path after a partial edit.
Deterministic comparisons with the saved original sources rejected the posted
editor and failed to detect the enumeration error; both checks pass with the
corrected sources. These comparisons never opened Word.

- All **44 isolated suites passed: 26,998,718 assertions, zero failures**.
  The focused suite passed **778 assertions** under Windows PowerShell 5.1 with
  explicit C# 5 compilation, including posted focus, batched body ancestry,
  revision errors/deletion coordinates/limits, timing privacy, formatting/undo,
  hook dispatch and helper recovery. The corrected executable passed all
  **19 compiled-app probe checks** without starting its resident.
- All **26 Python tests**, **116 C# formatting/token checks**, **1,772 development
  audit checks**, five pinned-source hashes and local documentation checks passed.
  All **17 engine benchmarks**, 301 warm samples each, passed their budgets:
  highest warm p95 **4.268 ms**, initialization **191.50 ms**. These are not Word
  conversion timings. Runtime data, factory defaults and the accepted source
  baseline remained unchanged.
- The user confirmed conversion, formatting and undo with Track Changes enabled
  during the direct-enumeration experiment. Final native enumeration returned
  to the release's preliminary-count path, plus the reviewed error handling.
  The last focus-batching copy received the separate body/after-paste confirmation
  above. This was a brief diagnostic session, not another long stability trial.
- The private resident closed normally and its helper exited. All **24 installed
  program/configuration/state hashes** still matched the baseline. The original
  **1.9.8.0** resident and its helper were restarted and verified. Trial settings
  and preferences were not copied back. All non-runtime trial content (**26
  files**, including its executable, preferences and logs) was archived and
  verified entry-by-entry; remaining files matched the release tree. The private
  generated profile was then removed. No installed file, startup registration,
  working document, release artifact or accepted baseline was changed.
- Test and benchmark reports were retained; the three newly generated runtime
  fixture copies were removed after their checks completed, reclaiming
  **583,480,543 bytes**. Existing recovery archives and earlier evidence remain.

Evidence is under ignored `out/word-performance-1.9.8`: metadata, complete logs
for all three phases, `timing-summary.json`, focused/full results, benchmark
results, regression comparisons, installed hash baselines, `restoration.json`
and the verified private `trial-evidence.zip`. The archive SHA-256 is
`17fc89f1a8155cbaa94778e96509d81102b260e554ebcfddadfbd95ec2a35e0c`.
No new installer, live update, release publication or Git push was performed.
Body speed remains an open performance issue; posted-comment correctness is
fixed in development source and not yet installed.

# Authorized 1.9.8 installer preparation — 28 October 2026

The user authorized the proposed version/release-note update, installer build
and isolated clean-install/upgrade verification. Publishing, Git push and updating
the live installation were explicitly excluded. All preceding uncommitted Acrobat,
Word and reliability work was preserved.

- Updated VERSION, runtime/assembly versions and both Windows manifests to 1.9.8.
  The previous accepted source baseline was retained under `out/release-1.9.8`.
  Its drift was checked against exactly the ten known changed sources (including
  version metadata) and two reviewed additions before accepting all **47 current
  production-source hashes**. Runtime data and factory defaults were unchanged.
- A clean build produced `out/KeyJaya-Setup-v1.9.8.exe` (**67,819,008 bytes**).
  Installer SHA-256 is
  `57b2b62e697a85e87754c110cbfd61022b544ac7a802487ac8b3b71a03b8f8d6`.
  The embedded payload matched `out/payload.zip` byte-for-byte by SHA-256;
  all **38 payload file hashes** matched the clean application tree. The installer
  is unsigned. The original published 1.9.7 installer/checksum was retained.
- Final **44-suite run: 26,998,668 assertions, zero failures**. The versioned
  app passed all **19 noninteractive probe checks**. Windows PowerShell 5.1
  compiled the runtime explicitly as C# 5 and passed **728 focused assertions**,
  including 47 helper and 21 hook lifecycle checks. All **26 Python tests**,
  **113 C# formatting/token checks**, **1,791 strict source/data checks**, local
  documentation and five pinned-source hashes passed.
- All **17 engine benchmarks**, 301 warm calls each, passed their budgets.
  Highest warm p95 was **3.680 ms**; initialization **186.91 ms**. These measure
  the engine, not full Word/Acrobat conversion latency. The prior user-led trial
  used the same reviewed behavior before the version-only packaging changes.
- The actual installer passed isolated clean installation and preservation of an
  edited trigger file on upgrade. Injecting a failure after replacement restored
  all **56 fixture file hashes**. The exact production setup code also passed
  **25 retirement/rollback checks in each PowerShell shell**, including locked
  early/middle/final retirements, post-swap failure and complete original backups.
- The retained published 1.9.7 installer matched its public release checksum
  `bea7e18211fa947f803cf9e2bbbe03cbe8a615ed946bbb023d4d066ef5560e52`.
  Its isolated installation, seeded only with synthetic preferences, survived a
  failed 1.9.8 upgrade with all **46 original file hashes restored**. Successful
  upgrade then preserved all **8 configuration and 5 state hashes**, including
  custom/deleted/disabled triggers, learned spellings and pending votes. Loading
  the upgraded converter passed **16 behavior/configuration checks** and left
  those bytes unchanged. No resident was started in any installation fixture.
- The live 1.9.7 resident retained its original PID throughout. All **24 installed
  program/configuration/state hashes remained unchanged**, with original startup
  and uninstall registration. No editor was activated or driven, and no installed
  app update, live installer, release publication or Git push occurred.
- Small reports, checksum and the preserved prior baseline/development settings
  remain under ignored `out/release-1.9.8`. Large clean/upgrade/rollback fixtures,
  source-setup fixture payloads, the test/benchmark data copies and the previous
  development tree were removed after verification. The prior development tree's
  non-runtime content was archived and verified first; unchanged runtime inputs
  were matched by hash. One clean current app tree, current payload/installer,
  prior published installer and existing recovery archives remain.

These checks cover the tested contracts and isolated setup paths. The prepared
installer has not updated the owner's live app or been published; physical
installation on another user's machine, sign-in/sleep/wake and permanently hung
providers remain outside the completed checks.

# Revised temporary stability trial completed — 28 October 2026

The user-authorized trial ran from **18:08 to 19:37 IST (12:38 to 14:07 UTC)**,
approximately **89 minutes**. The user reported that everything seemed to be
working fine and explicitly ended the trial. This is one user-led session,
not continuous automated stress or a guarantee of indefinite uptime.

- The trial used the exact reviewed executable recorded in `out/review/summary.json`,
  copied with its Framework/DPI configuration into the private, ignored
  `out/stability-trial/profile/engine`. Its own root contained runtime data and
  copies of installed configuration, state and any legacy personal preferences.
  Child processes and restarts therefore also resolved to the trial profile.
- Installed 1.9.7 closed gracefully after verifying its process/window ownership
  and that it had no visible settings dialog. All **24 checked installed program,
  configuration and state SHA-256 hashes remained unchanged** across shutdown
  and trial startup. All **22 copied preference files** matched their originals.
  No installed file, Windows startup registration or working document was edited.
- The same temporary resident and its original `--watch-word-focus` child remained
  running at trial end. Its log contains one resident readiness, one helper
  readiness and one normal shutdown, with **zero helper failure/retry or hook
  dispatch failure/retry records**. Both processes closed normally.
- No editor was activated and no diagnostic input was sent. Physical checks were
  user-led in disposable content. Bounded startup diagnostics were enabled;
  operational helper/hook failure and recovery logging continued after their limit.
- The existing installed **1.9.7.0** was restarted, with one installed resident
  and no trial processes remaining. All **24 checked installed hashes remained
  unchanged**, including settings, custom triggers and spelling preferences.
- Session IDs, reviewed binary hash, original hashes and restoration evidence
  remain private beneath `out/stability-trial`. All **23 trial log/config/state
  files** were archived and verified individually by SHA-256; the private archive
  is about **1.47 MB**. Trial preferences were preserved separately and were not
  copied over installed preferences. The disposable runtime profile was removed
  only after every file matched either that archive or its known runtime input.

No editor was activated or driven by diagnostics. Working documents and recovery
archives were preserved. No installer was built, release published, Git changes
pushed or installed app updated. Sleep/wake, sign-in, permanently hung providers
and other editor versions remain outside this session's verified scope.

# Focused keyboard/helper code review — 28 October 2026

Reviewed the unreleased input-thread, focus-helper, startup/shutdown and resident
state integration, preserving the preceding Acrobat and Word development work.
No installed resident or live editor was controlled during this review.

## Findings corrected

- **Helper protocol race:** UIA can deliver a focus callback while registration
  is still returning. Its notice could precede `ready`, causing the supervisor
  to reject and restart a healthy helper. Output now buffers the latest event
  until readiness. Event revisions also reject a slow older lookup completing
  after a newer event; a failed lookup clears evidence. A healthy minute resets
  the retry streak instead of accumulating delay across unrelated failures.
- **Silent hook-worker exit:** after successful registration, an exception or
  unexpected message-loop return previously left an apparently running resident
  with no hooks. The owning thread now releases that attempt and retries with
  bounded backoff, including failed re-registration. Logging is dispatched off
  that thread. Initial registration failure remains a startup error. This does
  not implement a detector for Windows silently removing an individual hook.
- **Remaining provider work under the input gate:** selected-range comparison,
  occurrence discovery, modern-comment undo classification and copied-conversion
  clipboard reads could still wait on Word/OLE while blocking input decisions.
  Observations now precede the gate; generation and selection ownership are
  checked afterward. Clipboard cycling needs one stable read plus a matching
  sequence at commit. A changing/unavailable clipboard clears cycle evidence
  while the completed conversion retains its normal undo state.
- **Startup cleanup:** `FormClosed` is not guaranteed if native handle creation
  fails. Startup rollback now uses the same idempotent input cleanup as normal
  shutdown, retiring the helper, hook owner and focus-event hooks already acquired.

## Verification

- All **44 isolated suites passed: 26,998,665 assertions, zero failures**,
  followed by the final focused startup-cleanup checks below. C# 5 compilation,
  the 19-check app probe, all 26 Python tests, 113 C# formatting/token checks,
  five pinned-source checks, 1,743 development audit checks and documentation
  checks passed. Runtime-data hashes and the accepted 1.9.7 baseline are unchanged.
  All 17 engine benchmarks passed with 301 warm samples each: highest warm p95
  4.253 ms, initialization 163.94 ms. These exclude native editor latency.
- Final focused Word contracts pass **728 assertions**, including **47** helper
  and **21** hook lifecycle assertions. Real disposable children test registration
  events, startup/protocol failures, recovery and shutdown. Injected hook queues
  test independent dispatch, unexpected return/failure, failed re-registration,
  stalled logging, shutdown and actual resident cleanup without creating a form.
- Five new replay assertions cover stable/empty clipboard reads, changing data,
  later invalidation and provider failure through injected readers. No clipboard
  was opened. Three mutation checks deliberately restored premature helper
  output, disabled hook-loop recovery or bypassed clipboard sequence checking;
  each new regression rejected its corresponding broken build.
- The C# 5 development app and its 19-check noninteractive probe pass. Small
  review evidence is retained beneath ignored `out/review`; previous reliability
  and Acrobat reports remain. The final startup-cleanup refinement received the
  focused Word-contract rerun after the full regression run began.
- No installer, live Word/Excel test, installed-app update, release, publishing
  or Git push occurred. Version, accepted source baseline, runtime data, user
  settings and working documents remain outside the edits. Full-size disposable
  fixtures are removed after verification; one development app tree is retained.

References checked against primary documentation:
[low-level keyboard hook dispatch](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc),
[CallNextHookEx handle semantics](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-callnexthookex),
[UIA threading and registration](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading).
At completion of this review, these were isolated checks. The subsequent
user-led temporary trial and its limits are recorded above.

# Resident exits and Word conversion latency — 28 October 2026

Requested scope: investigate intermittent disappearance and slow Ctrl-word,
selected-text and backtick conversions, including a single selected word that
failed. This work preserves the preceding uncommitted Acrobat changes. The
version and accepted release baseline remain 1.9.7.

## Diagnosis and changes

- The two latest KeyJaya .NET crash records (28 October 09:29 UTC and 26 October
  22:28 UTC) show the same `NullReferenceException` in Framework
  `NonClientArea.CreateSystemMenu`, through `FocusTracker.GetFocusedElementFromWinEvent`
  and its focus-event dispatch. The failure precedes the application callback,
  so catching exceptions inside that callback cannot protect the resident.
  Six older records were distinct historical font-cleanup/startup failures;
  they were not treated as evidence that the current focus fix covers all exits.
- Managed UIA focus subscription now runs only in a supervised hidden copy of
  the executable, before profile loading, mutex or resident startup. The local
  pipe carries bounded editor/window metadata, not text. Exit retires the helper's
  cached evidence immediately and triggers bounded restart. Fresh focus, native
  host, process, comment ancestry, writable text and edit context are still
  required before replacement. No unhandled exception is resumed.
- Exactly one captured selected word previously entered a caret-only preparation
  guard. It now uses exact selected text/range plus current context, without
  navigation. The default path still rejects unsolicited preselected ranges.
- Word body focus no longer traverses comment ancestry, and Paste Options
  ancestry stops at the document while retaining its separate native popup scan.
  Word letter selection batches existing native checks and reuses its own
  discovered target. Word styling reads its context once per request. Final
  target/selection, formatting, undo, cancellation and replay guards remain.
- One intermediate temporary copy stopped receiving Ctrl-tap and backtick while
  the resident remained responsive and Ctrl+. still converted. This distinguished
  a lost low-level hook from a dead app or failed editor adapter. Windows documents
  silent removal of timed-out hooks. Keyboard/mouse hooks now install and pump
  on a dedicated thread; pending Word focus and clipboard/OLE reads run outside
  the input state gate. Closed-target COM cleanup leaves the hook thread, and UI
  timer handling stays on the UI thread. This addresses the observed starvation
  path; it is not a claim to detect every possible hook loss.

Primary references:
[Framework FocusTracker source](https://source.dot.net/UIAutomationClient/MS/Internal/Automation/FocusTracker.cs.html),
[Microsoft UIA threading guidance](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-threading-issues),
[Microsoft low-level keyboard hook guidance](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc).

## Verification and boundaries

- Final **44-suite run passed: 26,998,638 assertions, zero failures**. C# 5 app
  compilation, the 19-check compiled-app probe, all 26 Python developer tests,
  113 C# formatting/token checks, five pinned-source checks and 1,743 development
  repository checks passed. Audit reported eight changed and two new production
  sources against 1.9.7; runtime data and the accepted baseline are unchanged.
  All 17 engine benchmarks passed with 301 warm samples each: highest warm p95
  3.794 ms, initialization 143.17 ms. These exclude native editor latency.
- `WordSnapshotTests` passed 703 assertions, including 37 real disposable-child
  failure/recovery checks, six isolated hook-thread checks and 111 comment-focus
  checks. The fixtures neither subscribe to desktop UIA nor install hooks.
  `SelectionTests` covers explicit selection acceptance, stale/wrong selection
  rejection and native selection batching; existing replay and Word formatting
  regressions remain included.
- With permission, only the temporary resident's helper was stopped once. The
  parent PID remained unchanged and its replacement helper was observed within
  1.777 seconds. This verifies process isolation/recovery without deliberately
  crashing the Framework or changing a document.
- A separately authorized metadata-only body-focus comparison measured 12 calls
  per version: baseline median 8.663 ms versus corrected 3.508 ms. Maxima were
  24.829 and 61.123 ms respectively. These are focus checks, not full conversion
  latency; see [Responsiveness](RESPONSIVENESS.md).
- The user performed conversions in disposable Word content. In the final tested
  copy, Ctrl-word, one selected word, a selected passage and backtick all worked;
  input outside Word worked, speed was acceptable, and both comments and replies
  worked. Earlier comment-focus and hook-loss failures were corrected before
  those confirmations. The final context-read reuse and timer/cache housekeeping
  received automated checks after that trial, without another live Word test.
- The temporary resident and helper were closed, and the existing installed
  **1.9.7.0** restarted. All **23 program/configuration/state SHA-256 hashes were
  unchanged**; one installed resident and no temporary processes remained. The
  installed settings, custom triggers and spelling preferences were preserved.
  Diagnostic helpers sent no keyboard input; the user performed the physical checks.
  No live Excel test, installer, installed-app update, release, publishing or Git
  push was performed.
- Small evidence is retained beneath ignored `out/reliability`, including the
  selected crash records, timing reports, temporary-copy logs, helper recovery
  and installed-restoration report. Generated dictionary fixtures and the private
  temporary profile are removed after checks; recovery archives and historical
  working material are preserved. One development app tree remains.

These checks cover the diagnosed Framework crash path, bounded helper failures
and reported Word interactions. Indefinite uptime, permanently stalled provider
lookups, every Office version and all possible process exits remain unproven.

# Acrobat Find and Advanced Search — 28 October 2026

Requested scope: conversion in both Ctrl+F Find and Advanced Search, starting
from installed/released 1.9.7. The working tree was clean before this work.
Development code now prefers the verified native Edit reader for these fields.
The version and accepted release baseline remain 1.9.7.

- Reviewed CONTRIBUTING, architecture, testing, release verification, 1.9.7
  release notes, building instructions and both output-cleanup records.
- User-authorized metadata inspection found native `Edit` controls in both
  `AcrobatSDIWindow` (Find) and `AVL_AVWindow` (Advanced Search). A separate
  nonresident probe read, selected and converted the exact synthetic `krsna`
  query in Advanced Search. A passive Ctrl/focus monitor accepted the tap.
- After starting installed KeyJaya, the user confirmed that selected conversion
  worked in both fields, but caret conversion failed. A separately authorized
  temporary resident reproduced the failure: its UIA TextPattern supplied empty
  context on all 25 discovery attempts despite the nonempty native edit buffer.
  Thus merely checking for an absent TextPattern was insufficient.
- The fix makes caret/letter, selection, insertion and behind-caret discovery
  consistently use native coordinates for the verified foreground Acrobat Edit.
  Executable/host class, native keyboard focus, process/root ownership and
  writable/non-password/enabled control checks constrain this preference. Text,
  selection, generation, cycling, cancellation and replay guards remain in place.
  Other editors, PDF page views and Acrobat Rich Edit comments keep their paths.
- Native Edit/Rich Edit regressions use a separate hidden owned process, with
  no desktop focus, clipboard or keyboard input. They exercise Unicode buffers,
  caret/selection, newline coordinates, rejection of wrong controls/handles,
  stalled-provider timeouts and empty-TextPattern precedence. A mutation restoring
  the old precedence failed the new search-host regression as expected.
- Both the baseline and corrected compiled app passed all 19 probe checks;
  the corrected app was explicitly compiled with C# 5. All 26 Python developer
  tests, five pinned Hindi-source files and documentation checks passed.
  Development audit passed 1,706 checks with exactly two intentional production
  source changes and unchanged runtime data. All 109 C# formatting checks passed.
  All **44 suites passed: 26,998,577 assertions, zero failures**, including
  **141 new native Edit/provider checks**. All 17 engine benchmarks passed
  with 301 warm samples each; highest warm p95 was 7.631 ms and initialization
  was 149.93 ms. These engine timings exclude native editor response.
- The user confirmed the corrected temporary copy works in **both Find and
  Advanced Search**, including caret conversion, repeated Ctrl cycling and normal
  typing afterward. The temporary resident was closed gracefully and installed
  **1.9.7** restarted; **23 program/configuration/state SHA-256 hashes remained
  unchanged** across restoration. This is not an installed-app update. The
  observed Acrobat build was tested; other versions and native undo remain
  outside this focused physical check.
- Diagnostic helpers default to compile-only/no inspection and require opt-in
  for live use; see [Testing](TESTING.md). Small diagnostics and the before-fix
  log are beneath ignored `out/acrobat-search`; test logs are in `out/tests`.
- Only the explicitly authorized synthetic query received probe input. No live
  Word/Excel test, installer build, installed-app update, publishing or Git push
  was performed. Temporary residents use `out/app` and its factory profile;
  installed settings/triggers/spelling preferences and working documents were
  untouched. Historical scratch documents and recovery archives were preserved.
- Removed the two test fixtures and single benchmark fixture generated in this
  session after verification, retaining one development app tree and the small
  runners/logs/reports. No installer-test installations were generated.

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

## Word comments and replies — 26 October 2026

Development from released 1.9.6. The original native reproduction could read and
select `prasada` in a comment, but focus recognition returned false and native
replacement was rejected. The comments story (4) was excluded; its writable
`_WwG` Edit provider also had neither a Body ID nor a Document ancestor.

The native Word path now accepts comments and threaded replies while retaining
its existing window/process, document, exact-text, formatting, undo and replay
checks. The additional focus path requires the foreground Word process, the
native `_WwG` editor and a writable TextPattern selection. It does not admit
ribbon/search controls, menus or arbitrary Edit fields. No configuration loading,
trigger defaults, queue ownership or other editor adapter was changed.

Verification: all **44 isolated suites passed, 26,940,793 assertions; zero
failures**. The comments-story regression failed before the fix. All **26 Python
tooling tests**, **19 compiled-app checks** and **17 engine benchmarks** passed.
The highest warm engine p95 was **11.207 ms**, within the 150 ms budget; these
measure the engine rather than complete editor latency. Development audit,
pinned Hindi sources, documentation links and formatting checks passed.

The expanded native Word fixture passed **213 checks**. It creates a real comment
and threaded reply in its exclusively owned scratch document and checks Draft
and Print Layout comment panes, Unicode and ScaGoudy/Balarama, word/letter edits,
abbreviation expansion, cycling and original fonts, native undo, continued typing,
expired context and neighboring-target rejection. Body text and the other
comment/reply remain unchanged. The fixture must display and activate its own
comment pane after a view change; a COM selection in a hidden pane does not prove
typing focus. A separate UI-focused Print Layout reproduction also converted.
The existing bounded COM-delay, focus-loss rollback and disconnection checks pass.
Native tests inject no keys. Modern Comments and physical end-to-end hotkey timing
are not newly verified by this pass.

An application-only development executable was activated with the existing
installed configuration after a keyboard-idle check and graceful resident close.
All **41 protected installed configuration/data/asset/executable hashes** remained
unchanged, as did Windows startup registration. The temporary build's reference
window reads the same live configuration through a junction beneath ignored
`out/word-comments`; dictionary/asset copies remain under that temporary root.
Existing working documents were not read, edited or closed. The installed
executable, version and accepted 1.9.6 baseline are unchanged. Windows startup
still launches the released executable. No installer was built or published.

Evidence is under `out/word-comments`: `reproduction.json`,
`print-conversion.json`, `full44-results.json`, `native-check-6.txt`,
`native-check-6/editing-trace.txt`, `activation.json` and `verification.json`.

## Modern Comments and conversion delay — 26 October 2026

Modern Word comments use UIA card editors rather than the document's COM
Selection. Their focus event identifies the real editor even when the global
focused-element query returns a stale body provider. The adapter checks live
focus, Word process, card ancestry and the exact owned native host (`_WwG` for
contextual bubbles; `NetUIHWND` for the Comments panel). Modern text replacements
retain the draft's existing formatting, and body COM/font operations are excluded.

The user confirmed that conversion now works in the comment fields, then reported
excessive delay. Earlier native contextual new-comment/reply cases completed;
the full actual-panel reply fixture was interrupted and is not recorded as a
complete native pass. This responsiveness pass did not control the desktop or
open any Word document.

Existing diagnostic samples showed approximately 1.18–1.26 seconds from an
accepted word shortcut to held-input release. Focus resolution queried the stale
global provider first, fetched individual ancestry properties repeatedly and
walked the same comment tree twice. Word shortcuts also repeated native-body
Paste Options preparation in the modern comment editor.

The development path now resolves the live event editor first, fetches properties
in a per-call UIA CacheRequest and returns the successful classification to the
document-focus guard. Modern word shortcuts use their own caret/selection proof
directly, removing duplicate selection/menu probes and two fixed 40 ms waits.
Current focus, process, ancestry, native ownership, exact text/ranges, modifier
release and input-generation checks remain. No ownership or text proof is cached
between calls.

All **12 targeted isolated suites passed, 1,583,043 assertions; zero failures**.
WordSnapshotTests includes **103 new provider-model assertions** (593 total in
that suite) covering zero global-focus fallback calls for repeated valid comment
resolutions, fresh ancestry on every call, stale focus, changed/mixed processes,
normal body fallback, missing providers, cycles and the 18-node bound. Selection,
replay, correction races, native-key contracts, resident replacement, shortcut
settings, Word ranges/font modes and abbreviation checks passed. All **19
compiled-app checks**, development audit, documentation-link and C# formatting
checks passed. The optional modern native fixture compiled without running.

These checks establish the removed work and retained safeguards; they do not
measure a new live Word end-to-end latency. Release 1.9.6 and its accepted baseline
are unchanged. No installer was built and nothing was published. Evidence for
this pass is `out/modern-word-comments/speed-targeted-results.json`.

The optimized application-only build is running from the temporary
`out/word-comments/resident-comments-speed-1` folder with the existing installed
configuration. Activation verified the expected prior resident and its hidden
window/process, modifier release and queue acknowledgement, then requested a
graceful close and confirmed the new resident window. Active Word editing blocks
the restart. All **41 protected installed configuration/data/asset/executable
hashes** and Windows startup registration were unchanged. The installed/startup
executable remains released 1.9.6; this development activation is temporary.
The activation record is `out/modern-word-comments/activation.json`.

## Word selection shortcut and offline Excel review — 26 October 2026

The reported Ctrl+Shift+Left problem had a verified Word assignment conflict.
A read-only query against the application bound to the visible Word frame found
Ctrl+Shift+Left assigned to `RejectChangesAndAdvance`, Ctrl+Shift+Right to
`AcceptChangesAndAdvance`, and Ctrl+Left to `RejectChangesSelected`. The modern
comment editor handles navigation separately, explaining the different behavior.
The user removed the conflicting assignments. A subsequent read-only query
confirmed `WordLeftExtend`, `WordRightExtend`, `WordLeft` and unchanged
Shift+Left `CharLeftExtend`. The agent did not modify or save Word shortcuts,
templates or documents. The template still reported unsaved changes at that
check; their persistence remains under Word's normal save lifecycle.

Code review separately found native Word font/document-reference release in the
low-level keyboard hook when navigation clears the previous conversion ticket.
A deliberately stalled release backend reproduced that blocking path in the
resident's actual cleanup method. Cleanup now retires the ticket synchronously
and releases its detached references on a worker. Active font leases retain
their references until their existing user releases them; retirement cannot
authorize another edit. Eight regression assertions cover nonblocking return,
immediate invalidation, repeated cleanup and active-lease lifetime. This is a
separate responsiveness correction, not the cause of the Word key assignment.

Excel mirror identity/pattern properties now use a fresh batched UIA request.
Collapsed carets prove endpoint equality and read their prefix once; selected
ranges retain both offset and exact-text checks. Read-plan tests fail if a
collapsed caret requests the unnecessary end-prefix/selected-text reads. Formula
exclusion, provider deadlines, single-request ownership, epoch/current editor and
Enter/Edit checks remain. Hidden Formula Bars, multiline cells, localized mode
labels and larger buffers are documented separately in [Excel findings](EXCEL_DEBUGGING.md).
They were investigated but were not enabled without native selection evidence.
Comment formatting/italics were deliberately left as requested by the user.

All **12 targeted isolated suites passed, 1,584,902 assertions; zero failures**.
This includes WordSnapshotTests (601), ExcelCellTests (2,220), ReplayTests
(47,890), and the existing selection, correction-race, native-key, letter,
replacement, shortcut, Word range and font-mode checks. The expanded replay
coverage includes **256 Ctrl+Shift navigation chords**, both modifier sides and
press/release orders, autorepeat, FIFO delivery and extended-key flags. The six
editor-capability profiles and nine shared selection scenarios passed again.
These are model/production-method checks, not new native editor interaction.
All **19 compiled-app checks**, **98 C# formatting checks**, development audit
(1,591 checks) and documentation links passed; the optional modern-comment
fixture compiled only. No desktop input, foreground activation, document text
read or Word/Excel document edit was used in this pass. New live Word/Excel
keypress latency was not measured.

The application-only development update was activated from temporary
`out/word-comments/resident-navigation-excel-1` with the existing installed
profile. All **41 protected installed file hashes** and startup registration
were unchanged. Released 1.9.6, its installed executable and accepted baseline
remain unchanged; no installer was built and nothing was published. Evidence is
under `out/word-navigation-excel`: targeted results, read-only shortcut reports
and a copy of the activation record.

## Excel multiline cells and live capability review — 26 October 2026

The user authorized the next Excel pass without desktop takeover. Read-only
capability probes inspected existing window/editor metadata before and after the
user hid the Formula Bar in a disposable edited cell. The visible logical
`FormulaBar` exposed TextPattern; the hidden state removed that provider. Neither
native editor supplied a usable text/selection interface. Standard Edit messages
returned unusable values and were not adopted as a production fallback. No cell
text was read by the probes, and no workbook, foreground or display setting was
changed by them. The Formula Bar requirement remains for this Excel version.

The development adapter now supports words/letters and selected single-line
regions within multiline cells. Relative Left/Right navigation starts from a
verified selection/caret, maps LF/CRLF boundaries and never issues line-local
Home/End or spreadsheet Ctrl commands on that path. It rejects unproven cursor
clusters before input. Selected passages spanning line breaks still skip because
ordinary Unicode typing is not a verified line-break insertion transport. Existing
formula and size limits remain. Navigation and replacement events use the
foreground-owned correction tag; fresh text/range/mode and epoch checks remain.

Word discovery now resolves selection/caret from one captured Excel buffer;
clipboard discovery resolves selection/insertion from one capture. Replacement
uses EnsureEdit's existing text/range proof rather than repeating that read
immediately before it, and retains the final pre-input proof. The real adapter's
transaction methods are exercised through an independent field backend, without
native input. The new fixture covers 49,825 assertions, including relative
selection, both line endings, retained selection, continuation, punctuation,
restoration, stale ownership, ignored selection, unknown modes and formula output.

All **14 targeted suites passed, 1,660,035 assertions; zero failures**.
ExcelCellTests passed **52,045 assertions**. A final Excel rerun passed after the
backend declaration was changed to an auto-property to remove an unused-field
compiler warning. All **19 compiled-app checks**, **100 C# formatting checks**,
development audit (1,612 checks) and documentation links passed. The capability
tool compiled separately and is compile-only by default; `-ReadOnly` explicitly
enables its bounded metadata inspection.

After activation, the user performed word/letter checks and second-line conversion
in a disposable Excel cell and confirmed all worked normally. Passive diagnostic
timings contained seven visible-bar manual attempts between **99.1 and 362.2 ms**
(median **200.7 ms**). Six involved abbreviation expansion and one was ordinary
word conversion (**260.2 ms**), so these do not establish a controlled word/letter
speedup or the 150 ms goal. One rejected range read occurred during the observed
successful checks and is retained as a transient read observation. Three earlier
hidden-bar attempts were correctly skipped and are excluded from conversion
timings. Timing boundaries are accepted manual action to empty-hold release;
they are not independent hardware key timestamps or queued-typing delivery.

The temporary application-only build is running from
`out/word-comments/resident-excel-broader-1` with the existing installed profile.
Activation verified all **41 protected installed file hashes** and unchanged
startup registration. The agent did not move focus, inject test input, open/edit
a working workbook or change Word shortcuts/formatting. The user operated the
scratch cell and Formula Bar. Release 1.9.6, the installed executable and accepted
baseline remain unchanged. No installer/add-in was built or installed, and
nothing was published.

Evidence is under `out/excel-broader-support` (targeted and final Excel results,
passive timing metadata and activation record) and `out/excel-capabilities`
(visible/hidden metadata reports).

## Excel passages, complete regression and persistent app update — 26 October 2026

The earlier temporary activations above are superseded by this app-only update.
The verified development executable now runs from the normal installed path,
`%APPDATA%\KeyJaya\engine\keyjaya-app.exe`. Its default installed-profile resolution
was verified without a `--root` argument. The Windows startup command was not
changed and now resolves to this binary. The executable version and accepted
release baseline remain 1.9.6; this is an unpublished development update.

Excel relative selection now chooses the nearest verified selection edge even
in single-line cells. Cursor-unit validation covers the selected span and actual
travel path, allowing emoji/combining sequences elsewhere to remain untouched.
The established single-line script/cluster fallback remains. Selected LF/CRLF
passages use balanced Alt+Enter insertion, with preflight for both insertion and
retained selection. Current buffer/range/mode, formula, size, epoch and native
ownership guards remain. Status-mode metadata uses a fresh batched UIA request.
No clipboard transport was introduced.

The user operated a disposable Excel cell and confirmed conversion across two
lines, retained selection, one-step Ctrl+Z, and a nearby word with emoji or a
combining accent elsewhere all worked. The agent did not inject test input,
move foreground focus or open/read/edit working documents. Eight mixed passive
action timings ranged from 12.0 to 874.7 ms; they include selection/undo and
provider retries and are not a controlled conversion-speed comparison. The
Formula Bar and English mode-label requirements remain, as do exclusions for
formula-like/oversized buffers and unverified multiline cursor clusters.

Verification completed:

- All **44 noninteractive suites passed: 26,997,690 assertions, zero failures**.
  ExcelCellTests passed **53,101**, including **50,881** transaction-contract
  assertions. WordSnapshotTests passed **601**. The shared editor matrix still
  covers six profiles and nine scenarios without controlling those applications.
- All **26 Python developer-tool tests** passed; network and installer calls in
  those fixtures are mocked.
- The app-only build and all **19 compiled-app checks** passed. No installer or
  payload package was built.
- All **100 C# formatting checks**, the **1,612-check development audit**, pinned
  Hindi-source verification, documentation checks and `git diff --check` passed.
  Intentional development drift remains separate from the accepted release data.
- All **17 engine benchmark cases** passed with 301 warm samples per case and no
  budget failures. Highest warm p95: **11.293 ms**; initialization: **209.905 ms**.
  These do not measure native editor latency. A long-cell navigation fixture
  independently proves **10 events instead of 3,614** for the same nearby word.
- A disposable binary-update fixture passed **25 checks**, including successful
  replacement, locked-target refusal, hash mismatch and rollback after an actual
  swap followed by injected failure. No live profile was used by that fixture.

The live app-only update gracefully closed only the verified owned candidate,
atomically replaced the installed executable, and launched it hidden with its
default profile. The original executable and unchanged framework configuration
were backed up beneath `out/stability-next/installed-backup-*`. All **47 protected
profile files** (configuration, custom triggers, dictionaries/assets, retained
state and framework configuration) had identical before/after SHA-256 hashes.
Startup registration was unchanged. Tray Restart resolves the running executable
path, so subsequent launches use the installed development binary as well.

Evidence is under `out/stability-next`: `full44-results.json`,
`binary-transaction-tests.json`, `app-only-update.json`, passive timing metadata
and the original binary backup. The installed SHA-256 is
`4DC74DB230D890467A861549D897124DF775F534E5460290E8D4F08C14535B47`.
The engine report is beneath
`out/benchmarks/run-127d885b16a74ae58d07e7d16e6ce13d`. No installer/add-in was built
or installed, no Word shortcuts or templates were changed, and nothing was
published.

## Spelling editor and read-only Word shortcut inspection — 26 October 2026

The tray now includes **Edit spellings and style…** and **Check Word shortcut
conflicts…**. The spelling editor manages preferred Unicode spellings, protected
words and roman/italic/name exceptions using the existing configuration policy.
It shows legacy rules without rewriting legacy files and saves only personal
word/style overrides in `customizations.ini`. Untouched sections, comments, line
endings and BOM are preserved. Save creates an exact backup, refuses changed
snapshots, commits atomically and rolls back failed reloads while retaining newer
external edits. No test changed the user's actual rules or triggers.

The Word check is also available inside Customize shortcuts, where it can inspect
proposed bindings without saving them. It reads only shortcut and active-window
ownership metadata from already-open Word instances. A separate hidden helper
keeps Word COM work outside the resident; a five-second parent deadline stops
only that helper if it stalls. It does not activate Word, read document text,
set CustomizationContext, execute a Word command, change assignments or save a
template. Reports distinguish KeyJaya overlaps, changed navigation assignments,
Word shortcut prefixes, unassigned keys and unavailable/incomplete checks.

The native read-only check found one accessible Word instance. All six checked
navigation chords were normal, including **Ctrl+Shift+Left → WordLeftExtend**.
Five KeyJaya/Word overlaps were reported: Ctrl+Comma and Ctrl+Slash were Word
shortcut prefixes; Ctrl+Backslash was ToggleMasterSubdocs; Ctrl+LeftBracket and
Ctrl+RightBracket were ShrinkFontOnePoint and GrowFontOnePoint. These existing
assignments were not changed. A report applies to each instance's active window,
rather than every open document/template.

Verification completed:

- All **44 isolated suites passed: 26,997,748 assertions, zero failures**.
- Final focused checks after the editor input-guard change passed **seven suites,
  1,817,271 assertions**. Spelling-editor contracts passed **44** assertions and
  shortcut-inspection contracts **26**, including production-parent helper startup,
  malformed output and termination after a simulated five-second stall. Helpers
  in these tests never connect to Word.
- Resident-owned editor windows and message boxes now bypass conversion by
  process ownership. Both keyboard decisions and registered-shortcut dispatch
  use that guard. The separate cheat-sheet exception remains. This prevents
  automatic or manual conversion from changing text being entered into settings.
- Five forms were rendered privately without displaying any windows, at 96, 144
  and 192 DPI: **15 renders and 132 visibility/layout checks**. A clipped footer
  at 200% scaling was corrected. Offscreen printing does not fully paint native
  combo boxes; their standard controls and selection state remain separate from
  this visual evidence. No desktop screenshot or input automation was used.
- The installed spelling configuration loaded successfully in a read-only
  preflight. All **26 Python tests**, **19 final compiled-app checks**, **106 C#
  formatting checks**, **1,658 development-audit checks** and pinned Hindi-source
  verification passed. All **17 engine benchmark cases** passed, with no budget
  failures. Engine measurements remain separate from native editor response time.

Evidence is under `out/spelling-word`: `full44-results.json`,
`guard-focused-results.json`, `word-inspection.json`, private render PNGs and
app-only update records/backups. The engine report is under
`out/benchmarks/run-ee9726a608eb431d8084e35f92f34c1f`.

After the user confirmed readiness, the final build, including the editor-input
guard, was activated at the normal installed/startup executable path. The restart
verified an idle, stable chat/desktop foreground, released buttons/modifiers,
resident ownership and absence of visible KeyJaya settings windows. The chat host
was verified by its Codex product identity; this packaged installation's executable
is named ChatGPT.exe. No foreground activation or desktop input was sent.

The previous app was backed up, only the executable was replaced, and the new
resident's default profile was verified without `--root`. All **47 protected
profile files** and Windows startup registration remained unchanged. A separate
final read-only verification confirmed the running executable matches the tested
source, verified its backup, and found all 47 hashes still unchanged. The final
SHA-256 is `BEFBF6CCB015930D4BDD52504E4CACC672ED9CF30ABB7EFC620432060858CB55`.
Activation and final checks are in `app-only-update.json` and
`final-verification.json` beneath `out/spelling-word`. Working documents were not
opened/read/edited by these checks. No installer was built or run, nothing was
published, and the accepted 1.9.6 release baseline remains unchanged.

## Ctrl-word italics and requested menu cleanup — 26 October 2026

The spelling editor has been removed, including its store/window and compilation
inputs. Existing configuration and spelling/style preferences remain supported
and were not rewritten or removed. The standalone Word shortcut-overlap tray
action is also removed; its read-only check remains inside Customize shortcuts.
The menu fixture and regenerated menu image reflect these changes.

The owner defined the new formatting policy before implementation:

- An already-correct recognized Sanskrit/Bengali spelling, including `bhakti`,
  `nitya`, `dvija` and `dvijas`, is italicized unchanged on the first Ctrl tap.
- Individual terms and loanwords can be italic, including `dharma`, `guru`,
  `yoga`, `karma` and their plurals. One selected word counts as an individual
  word conversion. Ordinary English and unrecognized words remain protected.
- Selected passages italicize eligible terms but actively make the regular-type
  loanword exceptions regular, even if they were previously italic. Names keep
  their regular-type policy; book-title and personal override rules remain.
- Whole Sanskrit compounds such as `bhakti-yoga`, `nitya-dharma` and `guru-sevā`
  are treated as one italic term, including in passages.
- Repeated word taps cycle alternative spellings and the original text/format.
  A word without alternatives cycles between italic and its original formatting.
- The existing automatic-italics setting controls the behavior. Word comments,
  plain text, text-only Excel editing and script outputs gain no unsupported
  formatting. Existing supported rich-text/Markdown formatting paths are used.

The production Ctrl-word plan records an explicit original-formatting position,
so the same text can appear once as italic and once with its original formatting.
Word retains native font snapshots, expected-text/range checks and its undo
record; repeated cycles reuse the original continuation style. A single-word
selection can share the word path while retaining selection. Explicit regular
spans clear italics without changing unrelated text. InDesign preserves font
weight when choosing a nonitalic face; rich HTML emits normal-style spans.
Markdown handles owned emphasis markers while preserving bold and protected
code/links/headings. Wrapper expansion verifies provider text before selection.

Verification completed:

- All **44 isolated suites passed: 26,998,329 assertions, zero failures**.
- Final book-title/personal-rule review was followed by **five focused suites,
  647,496 assertions**, including **606 Ctrl-italics contract assertions**.
  Mixed-font passage checks include both italic and regular spans; the shared
  editor matrix, Word ownership/font lifetime, navigation and replay checks pass.
- All **26 Python developer-tool tests**, **19 compiled-app checks**, **105 C#
  formatting checks**, **1,650 development-audit checks**, pinned source checks,
  documentation links and `git diff --check` passed.
- All **17 engine benchmark cases** passed with 301 warm samples and no budget
  failures. Highest warm p95: **4.264 ms**; initialization: **206.74 ms**. These
  measure the engine, not native editor timing or the entire new Ctrl workflow.

The candidate is running from the normal installed/startup executable path,
using the default installed profile. Its previous executable was backed up.
All **47 protected profile files** and startup registration were unchanged during
activation and separate final read-only verification. The executable SHA-256 is
`FAAE60DA009A6038F555006A7400E74BF248E9356942C6C82E419DDCE0E83FD4`.
The owner reported that everything in the scratch Word check worked, including
the requested formatting cycle and passage undo, but that both individual words
and passages were slower than desired. This is user-observed native behavior;
the offline models alone do not prove visible formatting or undo. The agent sent
no test input and did not open/read/edit working documents. No installer was built or
run, nothing was published, and the accepted 1.9.6 release baseline is unchanged.

Evidence is beneath `out/ctrl-italics`: `agreed-policy.json`, `full44-results.json`,
`final-focused-results.json`, `compiled-checks.json`, app-only update/verification
records and the prior binary backup. The engine report is under
`out/benchmarks/run-6c8985685fe84c71a7724b8c4c81d71c`.

## Word body latency follow-up — 26 October 2026

After confirming the new Ctrl formatting behavior, the owner reported excessive
delay for both individual words and selected passages. Read-only diagnostics for
the initial candidate showed ten Word word actions with an accepted-action to
empty-input-hold-release median of **771.8 ms**, range **616.3–3,411.1 ms**. Two
selected-passage actions were **1,735.3 ms** and **2,089.5 ms**. These are sparse,
uncontrolled samples; empty releases can also occur on skipped work. The summary
includes only actions that entered native formatting with no logged rejection;
the owner's visible-result observation remains separate.

The follow-up removes duplicate work while retaining edit ownership:

- Word Ctrl discovery reuses its own completed paste-UI preparation and selection
  observation within the same owner/generation. Final focus, caret/selection,
  expected-text and native commit checks remain.
- Replay ownership is captured inside the native edit from its verified document
  and visible projection, removing a second Word binding and revision scan.
  An owned font snapshot can supply the original continuation style directly.
- Original-font restoration uses the transaction's verified result projection,
  preserving character fonts and tracked-deletion coordinates. Live native
  styles already matching an italic/regular request avoid an unnecessary write.

Six focused suites passed **647,569 assertions**, including **29** new production
replay/font/style contracts, the selection matrix, formatting rules, shortcut
inspector, navigation and ordered replay checks. The fake range rejects duplicate
text/revision reads and counts unchanged style writes; ownership, cancellation,
provider failures and one-record undo are exercised without Word or desktop input.
All **19 compiled-app checks**, **106 C# formatting checks**, **1,657 development
audit checks**, **39 local documentation links**, **4 image headers** and
`git diff --check` passed. The earlier complete 44-suite run is recorded above;
the follow-up used focused checks appropriate to these adapter changes.

The app-only candidate was activated from the normal installed/startup path.
Separate read-only verification confirmed the running binary, backup and default
profile. All **47 protected profile files** and startup registration were unchanged.
The SHA-256 is `41518FB5ACBF01B462E985A102118B132C6F412E2AACF7DEDC8826303C34E454`.
The owner confirmed **“Faster, and all still works”** for individual words,
passages, formatting and one-step undo in a scratch Word document. The seven
accepted word actions in the follow-up diagnostics had a median of **588.8 ms**,
range **561.1–1,132.6 ms**. Two passage actions were **1,075.0 ms** and
**1,088.5 ms**. These are uncontrolled samples with different action histories;
they are not a controlled percentage-speedup claim. No agent desktop input or
working-document access was used. No installer was built/run,
nothing was published and the accepted 1.9.6 release baseline is unchanged.

The final review retains fresh selection discovery after recovering a paste
popup that had hidden the selection provider. Positive pre-popup selection
evidence still requires an exact match. This last guard passed Word snapshot and
ordered-replay checks again: **48,520 assertions**, plus the compiled-app probe.
Its tested binary is `84A0324A0A5629A883F4C88EF8A3C0D189719BB5F2065D2FA590CDE4BB31EABE`.
The guarded restart initially detected desktop activity and left the first
faster build running. After the owner confirmed idle-input readiness, the final
guard was activated at the normal installed/startup path. Separate read-only
verification confirmed the running executable matches that tested SHA-256,
the previous executable backup is valid, and all **47 protected profile files**
and startup registration are unchanged. No desktop input was sent, no working
document was accessed, no installer was built/run and nothing was published.

The owner then confirmed that the final scratch check passed: paste `bhakti`,
tap Ctrl immediately, and press Ctrl+Z once. This confirms visible italics and
native undo after pasting on the final activated build, separately from the
automated snapshot/replay models. The result is recorded in
`out/ctrl-italics-speed/manual-word-after-paste-check.json`.

Evidence is beneath `out/ctrl-italics-speed`: `focused-results.json`,
`compiled-checks.json`, `baseline-timings.json`, `candidate-timings.json`,
`manual-word-check.json`, guarded activation/final-verification records and the
prior executable backup. The timing summarizer emits only phase
timings and generic action metadata, without document text.

## Four review findings resolved — 27 October 2026

The review of checkpoint `da9c8df` identified four actionable formatting/undo
defects. Each is addressed in the development candidate:

- Markdown emphasis removal validates opening/closing delimiter boundaries and
  escape parity, including context immediately outside a selection. Literal
  identifiers such as `note_dharma_id` retain their underscores. Reconstruction
  preserves bold and avoids stray emphasis markers beside punctuation.
- Native Word font capture accepts the same bounded one-word shapes as Ctrl
  classification, including surrounding whitespace/punctuation, up to 321
  characters. Original fonts remain owned while the Ctrl cycle is active, even
  after the separate Backspace deadline. A missing snapshot cannot silently
  commit an original-formatting state. Ordinary invalidation still retires
  references without blocking navigation.
- Formatting-only conversions arm manual Backspace undo. The saved original
  anchor passes explicitly into restoration after the current anchor is cleared;
  supported rich editors restore the original italic payload and InDesign uses
  its native snapshot. Restoration does not run automatic style policy again.
  Modern Word comments retain their text-only restoration path.
- Both positive and regular-type policy respect the established protected
  literal ranges. URLs, email addresses, paths and literal comments receive no
  new spans; eligible terms on either side still follow normal policy.

Verification completed:

- All **44 isolated suites passed: 26,998,434 assertions, zero failures**.
- Final Markdown selection-boundary refinements passed **five focused suites,
  647,601 assertions**, including **660 Ctrl-italics assertions** and **22 native
  font-cycle/undo assertions**. The latter call production capture, restoration,
  expiry and undo-decision code against fake ranges; they preserve differing
  font names, sizes, bold and italic states for each captured character.
- The punctuation regression was observed failing before its correction. The
  earlier review's production probes record the identifier corruption,
  protected-range spans and missing formatting evidence before these fixes.
- All **26 Python developer-tool tests**, **19 compiled-app checks**, **107 C#
  formatting checks**, **1,671 development-audit checks**, pinned Hindi-source
  checks, **42 local documentation links**, **4 image headers** and
  `git diff --check` passed. Python installer/network operations were mocked.
- All **17 engine benchmarks**, with 301 warm samples each, passed their budgets.
  Highest warm p95: **4.490 ms**; initialization: **219.880 ms**. These are engine
  measurements, not native editor response measurements.

The app-only executable passed the compiled probe. KeyJaya was already closed;
only its installed executable was atomically replaced, preserving that state.
The previous executable was backed up and verified. Separate read-only checks
confirmed the installed executable matches the tested candidate, all **47
protected profile files** and startup registration are unchanged, and no app was
started or stopped. The SHA-256 is
`8DBFDCDA574554A30B974A7AEB29AB739041648F7C888779DCEF069A7163AFA1`.

No agent desktop input or working-document access was used. The new native
interaction cases have automated model/helper coverage; visible browser/InDesign
Backspace and the padded/delayed Word cases were not physically exercised in this
pass. The earlier user confirmations apply to the preceding build. No installer
was built or run, nothing was pushed/published, and version/release baseline 1.9.6
remains unchanged.

Evidence is beneath `out/review-fixes`: `full44-results.json`,
`final-focused-results.json`, `compiled-checks.json`, guarded stopped-app update,
backup and final-verification records. Earlier reproduction output is beneath
`out/reviews/checkpoint-da9c8df`. The engine report is under
`out/benchmarks/run-31fedea549dd49129713434757ed3b0e`.

## Authorized 1.9.7 release preparation — 27 October 2026

After the four review fixes were committed as `dadf3a6`, the owner explicitly
approved **“Build and publish 1.9.7.”** GitHub's existing main branch contained
four README-only commits after 1.9.6. They were merged without discarding the
owner's wording, alongside the new shortcut-checker and release-note links.
Version, assembly and manifest identities were updated together. The reviewed
source baseline was deliberately refreshed to 1.9.7; runtime data and factory
configuration remain unchanged. The strict audit passed **1,717 checks**.

The final 1.9.7 compiled app passed **19 probe checks**. The installer contains
the exact verified payload ZIP; all **38 package file hashes** match. A disposable
clean installation and edited-trigger upgrade passed. An injected post-swap
failure restored all **56 fixture file hashes**. Source-level setup checks passed
**25 checks each** in PowerShell 7 and Windows PowerShell 5.1.

The actual published 1.9.6 installer was verified against GitHub's SHA-256
`abac503d7de53ddfcde63a3118cf853d55a9c748bd85cefccf497c8d76f47160`
before it was executed in a separate disposable fixture. A deliberately failed
upgrade to 1.9.7 restored all **45 old-installation file hashes**. The successful
upgrade preserved **8 configuration hashes** and **4 existing state hashes**;
**8 runtime checks** confirmed custom triggers, legacy and unified spellings,
kept words, style overrides, custom shortcuts and disabled automatic italics.
Loading the new converter did not rewrite existing configuration.

The reviewed functional full/focused results above remain applicable: the only
subsequent production-code changes are version identities. The release also
passed all **26 Python developer-tool tests**, **107 C# formatting checks**,
**44 documentation links**, **4 image headers**, pinned dictionary-source checks
and `git diff --check`. No working document, clipboard or foreground app was used
for these installation checks, and the resident was not started.

The unsigned installer is `KeyJaya-Setup-v1.9.7.exe`, **67,811,840 bytes**, SHA-256
`bea7e18211fa947f803cf9e2bbbe03cbe8a615ed946bbb023d4d066ef5560e52`.
The embedded payload hash is
`5e386d4098a169f06a6adadeae36370c36d8b4c3817f7cf5622c8194cc97b9e4`.
The installer and `SHA256SUMS.txt` are the intended release assets.

Release evidence is beneath `out/release-1.9.7`, including artifact checks,
published-installer metadata, previous-version upgrade/rollback results,
PowerShell setup logs and live-profile preservation metadata. The verification
tools installed only into disposable `out/verification` folders.

The initial hosted Windows CI run exposed a test-helper encoding mismatch in
`ShortcutSettingsTests`, after the earlier suites passed. Reproducing the run
with UTF-8 console input failed before the correction. The disposable helper now
matches production's explicit UTF-8 reader with BOM detection and UTF-8 output;
the production helper already used that protocol. The corrected suite passed
**44 checks each** under UTF-8 input in PowerShell 7 and Windows PowerShell 5.1.
No production source or installer bytes changed for this test-only correction.
The draft release is held for a successful complete hosted Windows matrix.
