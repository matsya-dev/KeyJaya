# Release verification

## Release publication inputs — 7 November 2026

The user authorized the release commit, `v1.9.10` tag, push and GitHub publication.
Publication reuses the exact installer verified on 1 November. Initial preflight
matched all **158 frozen release inputs**, the installer and checksum file.
Fresh GitHub CI then exposed serializer startup inside the synthetic focus
helper's event-settling interval. The fixture now prepares its metadata before
announcing readiness, keeping cold test setup outside that interval. This is a
test-only change; the application sources and verified installer remain unchanged.
No rebuild, installed-app update or additional live editor test is involved.
The installer SHA-256 is
`6235bdf51bca8dd86fe954c3cf9a3c207df44a710ed97dbfacc09a2526eb1248`.
The installer and `SHA256SUMS.txt` belong to the
[1.9.10 release](https://github.com/matsya-dev/KeyJaya/releases/tag/v1.9.10).
Local publication evidence is retained in `out/release-1.9.10/publication`.

## User installation confirmation — 1 November 2026

After the normal-install/smoke-check instructions, the user reported that
everything seems to work. Read-only checks confirm installed VERSION 1.9.10,
executable file version 1.9.10.0 and an exact hash match with the verified build.
All **16 existing installed configuration files** retain their pre-upgrade hashes.
No additional desktop test or installed write was performed by this check.

The initial post-install check found **1.9.7** in both registry views of
the resident's actual user hive. It is a stale display-label observation; the
installed executable is verified as 1.9.10. The compiled installer assignment
check below does not establish why that label remained stale. The label was
not changed by that read-only check, and no definitive historical cause or live-registration pass is
claimed. Evidence: `out/release-1.9.10/local-install-confirmation.json` and the
adjacent process/registry metadata reports. Publication had not been authorized
at the time of that check.

After separate explicit user authorization, only the existing uninstall entry's
`DisplayVersion` was corrected to **1.9.10**. Both registry views confirm it.
The entry was backed up first; all other uninstall values and startup registration
are unchanged. All 24 engine/config/state file hashes match, and the app was not
restarted. This repairs the current label without claiming a historical cause.
Backup and proof: `out/version-label-1.9.10-20261101-161828`.

## KeyJaya 1.9.10 local installer — 1 November 2026

- The user authorized release metadata/baseline preparation, a local installer
  build and isolated installation verification. All 69 preexisting dirty/untracked
  files were preserved before edits. The accepted inventory contains all 50
  production C# files, including the new helper sources. Only version identities
  differ from the reviewed runtime/test inputs; all 158 frozen release inputs
  match after building and testing. No data manifest or factory preferences changed.
- Windows PowerShell 5.1 built the app, uninstaller and installer. All three
  file/assembly identities are **1.9.10.0**. The compiled app passes **19 checks**
  without starting a resident; all 38 staged/payload files match by SHA-256, and
  the embedded installer ZIP equals the payload. The installer is **unsigned**.
- All **44 isolated suites / 26,999,576 assertions** pass, with zero failures.
  All 26 Python tests, 129 C# 5 formatting/token checks, five pinned Hindi source
  hashes and the **1,944-check strict audit** pass. The 17 engine benchmarks pass
  with 301 warm samples each; maximum warm p95 is **5.5331 ms** against 150 ms,
  initialization **181.8104 ms**. These are engine timings, not Word latency.
- Disposable clean installation and edited-trigger upgrade pass. The injected
  failed swap restores all **56 fixture hashes**. An upgrade from the exact
  retained 1.9.9 installer restores all **46 old-installation hashes** on failure;
  success preserves all **eight configuration** and **five state file hashes**.
  The upgraded runtime passes **17 checks** for custom/deleted/disabled triggers,
  learned spelling/votes, legacy/unified preferences, formatting and shortcuts
  without rewriting them. Setup-retirement integration passes **25 checks**.
- Read-only inspection of the actual compiled setup method confirms its normal
  `RegistryKey.SetValue` assignment uses `DisplayVersion=1.9.10`. The retained
  1.9.9 installer similarly assigns 1.9.9. No incorrect hardcoded label was found;
  the historical live 1.9.7 registration was not repaired or overwritten.
  Isolated `--test-install` deliberately skips registry/shortcut registration
  and resident startup, so this is not a live Windows registration test.

Earlier native Word/reply/browser results below apply to the unchanged behavior;
this version-only release preparation performs no new live editor tests. Evidence,
checksums and configuration-preservation reports are in ignored
`out/release-1.9.10`. Publishing, Git commit/push and updating the installed app
remain separate actions.

The final preservation audit matches all **26 installed hashes**, all **seven
startup/uninstall properties** and the same installed resident PID. Unrelated
work and the 69-file Git inventory are preserved. Guarded cleanup removed only
11 newly generated targets (about 2.59 GB): disposable installations/backups,
setup and corpus fixtures, and the retired development app. Its 38 files first
matched the retained 1.9.9 installer's embedded payload. One current app tree,
release installers, reports, historical fixtures and recovery archives remain.

## Automated Word release checks — 1 November 2026

The user authorized one unattended 30-minute quiet window. Fresh preparation
matched the reviewed 31 October runtime/test hashes; all prepared inputs still
matched after execution. No runtime or tester source changed.

- Two complete Modern Comments runs passed with the same production-source
  diagnostic resident: **66 actions / 1,774 checks** across contextual and panel
  new/edit/reply editors. This includes 24 reply actions, native undo, plain
  formatting and continued typing. The resident was not restarted between runs.
- The separately armed private silent-helper fixture passed **80 checks**:
  recovery, conversion and undo with the same resident, editor and caret, without
  refocusing the comment. Only the test fixture's own helper was targeted.
- All **nine body fixtures**, tracking off/on, passed **144 samples / 6,384
  checks**, including fonts/styles, mixed formatting, retained selection/caret,
  conversion undo and earlier undo history. Revision display was restored.
  This is a same-source calibration, with no speedup claim.
- All scratch Word processes and private residents closed. Original installed
  1.9.9 was restored: **26 binary/config/state hashes and seven registration
  properties unchanged**, with no settings copied back. Read-only fingerprints
  confirm identical clipboard data before/after.

All four phases passed, recording **8,238 native checks**. Evidence is under
ignored `out/automated-word-2026-11-01`. Registered-shortcut dispatch and injected
Ctrl/focus checks do not prove physical keyboard-hook delivery; its earlier
user-led evidence remains separate. These targeted runs do not establish an
extended normal-use stability trial. No installer, version/baseline update or
publication is part of this session.

## Unreleased reply/browser investigation — 31 October 2026

The final temporary candidate passes the user-led repeated-reply check in two
Word replies, including leaving/re-entering them. The frame-only check also
confirmed new comments, existing-comment edits, Word body and Zen conversions.
The final reply log retains the same helper throughout, with no refresh loop.
The fix adds verified frame-tree discovery when the native host omits a reply,
and a bounded recovery grace for startup/in-flight lookup without extending the
caller's 150 ms wait. Exact runtime, host, focus and ownership checks remain.
Clipboard changes retain complete typed backup, sequence and restoration guards.

Both focused suites pass **818,193 assertions**, with 163 comment-focus, 92
focus-monitor and 63 clipboard contracts. Normal and diagnostic builds each pass
19 compiled-app checks; two private mutations reproduce the missing-frame and
premature-refresh failures. The fresh frozen full run passes all **44 suites /
26,999,576 assertions, zero failures**, with runtime/test hashes unchanged before
and after. All 129 C# 5 parsing/format/token checks, 1,893 development audit checks
and 77 local documentation links/four image headers pass. Data integrity is
verified; the accepted release baseline remains unchanged.
Installed 1.9.9 is restored: all 26 binary/config/state hashes and seven existing
startup/uninstall properties match. No preferences are copied back, and no
installer, version identity, accepted source baseline or publication is changed.
See [Development verification](DEVELOPMENT_VERIFICATION.md) for limits and the
discarded/intermediate candidates. This is unreleased source work.

## KeyJaya 1.9.9 installer preparation — 30 October 2026

- The user authorized version/source-baseline acceptance, a local installer build
  and isolated installation verification. `VERSION`, runtime/assembly identities
  and both manifests are now **1.9.9**. The accepted source baseline covers all
  **50 production C# files**. Only version identity differs from the reviewed
  runtime/test inputs; all **158 frozen release inputs** remain unchanged.
- Windows PowerShell 5.1 built the resident, uninstaller and setup. All three
  file/assembly identities are **1.9.9.0**. The compiled resident passed its
  **19-check noninteractive probe** without starting the app.
- All **38 packaged file hashes** match the payload ZIP. The installer's
  embedded ZIP matches that payload by SHA-256. Clean installation, edited-trigger
  upgrade and an injected post-swap failure pass in fresh folders under `out/`;
  rollback restores all **56 fixture file hashes**.
- The retained 1.9.8 installer matches its previous release checksum. Its
  disposable installation survives a forced failed 1.9.9 upgrade with all
  **46 file hashes restored**. A successful upgrade preserves all **8 config**
  and **5 state file hashes**, including edited/deleted/disabled triggers, learned
  spellings/votes, legacy and unified preferences, formatting, shortcuts and
  encoding. The upgraded runtime passes **17 configuration/conversion checks**,
  including the Devanagari addition, without starting a resident or rewriting
  the fixture's preferences.
- Setup-retirement integration passes **25 checks** under Windows PowerShell
  5.1, including locked retirement and post-swap failures with complete recovery
  backups. All **26 Python tests**, **129 C# 5 formatting/token checks**, local
  pinned Hindi sources and the **1,944-check strict release audit** pass.
- The final version passes **all 44 isolated suites / 26,999,534 assertions,
  zero failures**. All 17 engine benchmarks pass with 301 warm samples each;
  maximum warm p95 is **5.873 ms** against the 150 ms budget, with initialization
  and first requests reported separately. These measure the engine, not Word
  response time.
- Disposable installations, the new regression/benchmark corpora and setup
  fixtures were removed after retaining reports. The retired development app's
  38 files match the retained 1.9.8 installer's embedded payload before removal.
  One current app tree and the retained release installers remain; historical
  fixtures, trial profiles and recovery archives are preserved.
- The final read-only preservation check matches all **26 installed file hashes**,
  the same resident PID, and startup/uninstall registration. Installed files,
  preferences and the running 1.9.8 app are unchanged.

The installer is **unsigned** and prepared locally. No live Word/Excel test,
installed-app update, Git commit/push or publication is part of this preparation.
Evidence: ignored `out/release-1.9.9`.

## Final release-readiness review — 30 October 2026

- Reviewed the unreleased changes against the accepted 1.9.8 source inventory,
  focusing on hook/gate ownership, clipboard-helper isolation, focus-helper
  epochs/recovery, Word formatting/revision batches/undo, query routing and the
  Devanagari addition. No further blocking code findings remain after two fixes.
- The Word query output guard now refuses all control characters, including C1
  and NEL, plus Unicode line/paragraph separators. Normal Unicode spacing and
  non-query multiline behavior remain supported. Its new regression fails on
  the old guard; **108 query checks** pass after the fix.
- Own-process reference windows bypass caption/class readers before those can
  wait for the resident UI thread. Ordinary external captions skip class reads;
  the cheat sheet retains both checks. Six regressions cover blocking/missing
  readers and preserved classification. The pre-fix reader regression fails.
  [Microsoft documents the same-process title-read behavior](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowtextw).
- The final frozen run passes **all 44 suites / 26,999,534 assertions, zero
  failures**, including **1,443 WordSnapshot assertions**. The test compiler now
  explicitly requests C# 5. All **129 C# formatting/token checks**, **26 Python
  tests**, **19 compiled-app smoke checks**, pinned local Hindi sources, data
  integrity and development audit pass. The 17-case engine benchmark has no
  budget failures; worst warm p95 is **5.113 ms** against the 150 ms budget.
- Release notes now describe the final changes and limitations, with superseded
  investigation states retained in development verification. This review ran no
  Word/Excel/desktop tests, started no resident and built no installer. Prior
  native Word evidence remains recorded below; these narrow review fixes add
  isolated coverage, not new native timing or hardware-keyboard evidence.
- At this pre-packaging review stage, version and accepted source baseline
  remained 1.9.8. The strict release audit intentionally failed the old inventory
  because the unreleased source had
  **11 changed and three added files**; the development audit passes **1,893
  checks** without refreshing it. Selecting the next version, accepting its
  source baseline and installer/payload verification remain release-preparation
  steps requiring their own authorization. No push, publish or installed update
  was performed. Evidence: ignored `out/release-readiness-2026-10-30`.

## Final live Word matrix and recovery — 30 October 2026

- The user explicitly authorized ending the private trial and using fresh
  diagnostic sessions for the full Word checks. The original trial profile is
  retained. One factory test profile was created and reused; installed files,
  preferences and startup were not copied back or updated.
- The complete body matrix passes **all nine fixtures, both Track Changes states,
  144 paired samples and 6,384 checks**. Text, source fonts, styles/mixed
  formatting, italics, selection/caret, conversion undo, earlier undo history,
  existing deletions and revision-display restoration pass. This is same-source
  timing calibration, not evidence of a new speed improvement.
  Evidence: `out/word-performance-tests/run-6a31fbb5fe074c51a2e446ca2a024c1a`.
- Modern Comments passes **33 actions and 886 checks** across contextual and
  panel new/posted-edit/reply editors. Every requested case runs exactly once;
  the body remains unchanged. Evidence:
  `out/modern-word-comments/native-7cbf65f04a2b495387ee3384edbb58dd`.
- Final silent-helper recovery passes **83 checks**: helper replacement and
  resolved editor, same test-resident PID and active editor/caret without
  refocusing, conversion/plain formatting, native undo and unchanged body.
  Evidence: `out/modern-word-comments/native-d178139ef10a4fb99a8fd8aa2d49666e`.
- Earlier failed attempts are retained separately. Two body runs lost foreground
  focus to ChatGPT during input, confirmed by the user; the strict guard stopped
  them. A comment undo check exposed Word coalescing rapid fixture typing with
  conversion. Two controlled seven-action runs passed with an established value
  or an explicit setup caret boundary. The normal fixture now separates setup
  typing with owned Left/Right input, requiring unchanged text and the exact
  original caret. Recovery performs none of that new input or readiness work.
- A recovery repeat hit the tester's `File.ReadAllText` append-sharing race.
  Its reader now takes bounded shared snapshots, discards incomplete records,
  and derives/verifies the helper from one snapshot. A real open-writer fixture
  reproduces the old failure. All **238 isolated setup/coverage/log checks** and
  **1,403 isolated Word assertions pass**. Deliberately omitting the setup
  boundary, warming recovery or restoring the exclusive reader is rejected.
- Runtime sources were unchanged throughout these tester repairs. The test copies
  and owned scratch windows were closed; installed 1.9.8 was restored unchanged.
  No installer, release, push, baseline refresh or installed update was performed.
  Full session evidence is under ignored `out/word-full-verification`.

## Full Word test preparation, no live execution — 30 October 2026

- `tools/prepare_word_verification.ps1` prepares the complete existing body,
  Modern Comments and contextual silent-helper recovery phases. The report
  records sources before compilation, verifies they remain unchanged and binds
  all prepared executable/configuration bytes. Preparation has no desktop option
  and does not access either active profile or create an installation.
- The declared matrix is nine body fixtures with tracking off/on and 144 paired
  samples including warmups; 33 comment actions across all six editors; and one
  contextual silent-helper recovery scenario. Body runs are same-source
  calibration, not a new speed claim. See the [run plan](WORD_VERIFICATION_PLAN.md)
  for coverage limits and separate live-session requirements.
- The comment report now distinguishes both posted-edit views and requires every
  requested view/action exactly once. **221 pure comment checks** and **1,386
  isolated Word assertions pass**. The test-only recovery entry point passes
  **15 isolated checks** for launch refusal, one silent first child, normal later
  helpers and a separate GUID-named profile. Three new deliberate regressions
  (accepting incomplete coverage, aliasing the posted views and silencing later
  helpers) are rejected without native helper or desktop operations.
- Preparation succeeds under PowerShell 7 and Windows PowerShell 5.1 with C# 5.
  No new live Word result is claimed. The ongoing trial is not stopped, switched
  or restarted; full resident phases remain gated on a later explicitly approved
  fresh diagnostic session. Evidence: ignored `out/word-full-verification`.

## Comment tester repairs and approved setup verification — 30 October 2026

- Replaced the exact-English, outer-frame-only Comments lookup with fresh owned
  native-host discovery and supported Invoke/Toggle actions. Split controls,
  Office labels, count/key hints and duplicate UIA aliases are handled; stale,
  disabled, hidden, foreign and ambiguous controls are refused. Cursor-click
  fallbacks were removed. Panel-only setup no longer depends on a prior editor.
- Posting/editing/replying now retains the specific thread, including new-parent
  before/after discovery when a temporary draft receives its posted server ID.
  Normal seed typing and stable readiness checks replace fixed sleeps;
  recovery mode deliberately omits those input/normal-readiness signals.
- All **128 pure fixture checks** and **1,293 isolated Word assertions pass**.
  Eight deliberate regressions are rejected. The actual tester compiles as C# 5
  and passes self-tests, scope decoding and default-deny probes in PowerShell 7
  and Windows PowerShell 5.1. No Office/UIA references are needed by the separate
  pure-contract executable.
- Added `SetupViews` for an approved scratch check of all six editors.
  Its tested dispatch guard prevents resident conversions and settlement probes,
  allowing the current trial to continue without a diagnostic restart. Full
  conversion scopes still refuse an expired diagnostic window.
- A freshly approved setup check passed **98 checks and all three contextual
  editors**, with unchanged body, then stopped during the panel's post-open
  action scan. The initial combined count/focus guard was split into precise
  diagnostics; panel enumeration now proves its thread anchor before scanning
  controls. Owned foreground checks use the verified Word root and PID, retaining
  foreign-window refusal. The original modifier preflight also now waits for
  stable release and exits cleanly before opening Word if keys remain down.
  The scratch process closed; only both pre-existing Word processes remain.
  A second approved run passes 91 checks and the same three contextual editors,
  then cannot verify the panel New action. Native failure metadata confirms the
  owned scratch root/PID retained foreground focus; the body is unchanged.
  Its scratch process also closed. Bounded panel-failure metadata then exposed
  a changed view component in the same posted thread's UIA ID. The tester now
  matches the complete numeric thread component only inside its owned scratch
  frame and refuses different, draft, malformed or ambiguous candidates.
- The corrected native setup passes **186 checks and all six contextual/panel
  new/edit/reply editors**, with unchanged body, no conversion and no settlement
  probe. Its scratch process closed; only both original Word processes remain.
- No trial restart was run. All **43 runtime
  source hashes**, the **9307dfda…0731d1** trial binary and **24 installed file
  hashes** remain unchanged. The full resident-conversion/recovery matrix is
  pending a fresh diagnostic session after the current uninterrupted trial and
  fresh permission; this setup pass does not establish that matrix.
  Evidence: ignored `out/comment-tester-repair-2026-10-30`.

## Native comment recovery and final body checks — 30 October 2026

- The authorized one-hour quiet session reproduced a real gap: the silent
  helper was replaced, but its successor could not rediscover the still-focused
  contextual comment. The revised helper queries only the verified current
  native editing host, retaining process/frame, editor/card, live-focus and
  stale-result checks. Twelve added isolated assertions pass; disabling the
  new fallback fails its regression.
- All **44 isolated suites / 26,999,274 assertions pass**, including **1,189
  Word checks**. The C# 5 production and fault candidates each pass **19
  compiled-app checks**. Runtime and isolated-suite inputs remain unchanged
  after that run; subsequent edits are confined to live-test fixtures.
- The revised native recovery test passes **58 checks**, including the same
  resident PID, same comment/caret, helper-only replacement, conversion, plain
  formatting, native undo and unchanged body. The ordinary private trial now
  runs production SHA-256 `9307dfda…0731d1`, without the fault entry point.
- The full body matrix passes **nine cases / 144 samples / 6,384 assertions**,
  with tracking off/on, fonts/styles, selection/caret and both undo levels.
  An initial tracked-deletion setup failure exposed inherited All Markup;
  the fixture now selects final/no-markup to match its XML oracle and verifies
  restoration of the inherited display settings. This is a same-source
  calibration, not evidence of a new speed improvement.
- A broader comment run completes **17 contextual new/edit/reply actions /
  290 checks**, with unchanged body, then stops at its separate Comments-panel
  switch. Other repeats remain intermittent during fixture focus setup; the
  new `ContextualAll` scope is prepared, not a claimed complete native pass.
  A final repeat was refused before opening Word because the resident's
  diagnostic settlement window had expired. These limitations are retained.
- Installed files/settings and startup are unchanged. A read-only rollback
  preflight verifies the clean pre-test trial and retains latest private
  preferences; rollback never restores the test-only fault entry point.
  No installer, release, push or accepted-baseline refresh was performed.
  Evidence: ignored `out/word-final-recovery-2026-10-30`.

## Requested Devanagari spelling — 30 October 2026

- Added only `devanagari` → `devanāgarī` and its specific English-protection
  exception. Bengali and Tamil remain unchanged; existing formatting and
  personal spelling/protection rules apply.
- The added regression fails **42 assertions** against the preceding runtime.
  The corrected recognition suite passes **218,637 assertions**, including
  case variants, Unicode/Balarama/plain output, caret/selection/copied paths,
  cycling, protected URLs and personal overrides.
- All **10 focused isolated suites / 7,394,952 assertions pass** with **46 frozen
  source entries** unchanged. The C# 5 candidate passes **19 compiled-app
  checks**; documentation and all **1,865 development audit checks** pass.
  Evidence is retained under ignored `out/devanagari-term-2026-10-30`.
- All **24 installed file hashes** and the running trial executable remain
  unchanged. This vocabulary addition has not been deployed to either app.
  No live editor test, installer, release or baseline refresh was performed.

## Word Ctrl+F Navigation candidate — 30 October 2026

- The approved empty-field inspection identifies the docked query as a
  `NetUITextbox` over `RICHEDIT60W`, native control ID 1, with a distinct owned
  work-pane/dock ancestry. A prepared-query reader verifies `krsna` and caret 5;
  the corrected read-only adapter also verifies word/letter targeting and Unicode
  Auto output. No query input, search submission or document access was used.
- Added guarded text-field routing without broadening `DocumentFocused`.
  Document selection/font/replay bridges refuse this query even if its UIA
  provider becomes unavailable. The query retains live native/UIA/text checks,
  uses plain output and declines multiline/control characters.
- All **44 isolated suites / 26,999,169 assertions pass**, including **1,177 Word
  assertions** and **68 new query checks**. Three deliberate ownership/routing/
  control-output mutations fail. The C# 5 candidate passes **19 compiled-app
  checks** and the **1,865-check development audit**; all **130 frozen source
  entries** remain unchanged during the full run.
- The user authorized the guarded temporary-only update to `db98f9b3…2d648`.
  Resident **60792** and its ready helper **83040** are running. The switch
  preserves **24 installed hashes**, startup and all **22 current private
  preference hashes**; a verified `7f4be2b3…da60` rollback copy is retained.
  A Windows PowerShell 5.1 nested-array compatibility
  issue initially caused a false hash mismatch; the read-only diagnostic proved
  all files identical, and correcting only array assignments made preflight pass.
  No baseline was refreshed to hide a difference.
- The user confirms caret/selected-word query conversion, repeated cycling,
  backtick letter conversion, immediate Backspace restoration and selected-query
  Ctrl+Z undo, with document body text unchanged. Ordinary body and
  existing-comment conversion also still work. No automated input, installer or
  release was used; installed settings/startup remain unchanged. This does not
  complete the separate simulated comment-focus recovery test.

## Final isolated review — 30 October 2026

- Fixed two review findings in development source: late clipboard-helper
  crash/hang suppression now covers complete frames, and resident log writes
  originating in keyboard/mouse callbacks run off the dispatch thread.
  Normal clipboard refusal/startup retries and ordinary logging stay supported.
- The final frozen source passes **44 isolated suites / 26,999,101 assertions**,
  including **1,109 Word**, **52 clipboard-snapshot** and **26 hook-thread**
  assertions. The late-helper regression fails against the previous production
  code; a synchronous-log mutation fails the stalled-writer regression.
- **26 Python tests**, **19 compiled-app checks**, C# 5 parsing/formatting of
  **36 changed files**, pinned-source verification, documentation and the
  **1,850-check development audit** pass. Both Word tools compile and refuse
  unauthorized desktop execution; the performance fixture passes **155** pure
  assertions. No live Word/Excel test was run.
- All **17 engine benchmark cases** pass the 150 ms warm p95 budget; the worst
  measured p95 is **7.183 ms**. This excludes native editor/physical-key latency
  and establishes no additional Word speedup.
- All **24 installed file hashes**, startup and the existing trial resident/
  executable remain unchanged. The new C# 5 app `ff08e387…538fb` was compiled
  privately and never started. The running trial remains `7f4be2b3…da60`.
  Generated large fixtures from this review were removed; compact evidence is
  retained under ignored `out/final-review-2026-10-30`.
- The approved scratch Word recovery test is still a separate pending step.
  No installer, release, push, version or accepted-baseline update was performed.

## Existing-comment report traced to installed release — 30 October 2026

- The user reported that new comments/replies worked while editing posted
  comments failed. Actual process inspection found installed 1.9.8 running,
  not the temporary trial; the saved trial session alone was stale evidence.
- An offline probe invoked only each binary's editor-ID guard. Installed
  `bab32562…08142` accepts `cardEditor_` but rejects `postEditor_`; the previously
  verified trial `7f4be2b3…da60` accepts both. Both reject unrelated/empty IDs.
  No resident entry point, hook, clipboard or Word access was used by the probe.
- Resumed the already-authorized trial without rebuilding or replacing either
  executable: resident **77976**, ready helper **90616**. All **22 private
  preference hashes**, **24 installed file hashes** and startup registration
  remain unchanged, including across normal shutdown of installed KeyJaya.
  Both preference profiles were retained separately; neither was overwritten.
- The user confirms posted-comment editing now converts successfully. This is
  the existing unreleased fix, not a new runtime change or proof of simulated
  focus-loss recovery. No automated Word/Excel test was run. Normal startup
  still launches installed 1.9.8, so the temporary trial is not resumed at sign-in.

## Batching trial updated — 29 October 2026

- The user authorized updating only the existing temporary trial. Its verified
  executable is now running, SHA-256
  `7f4be2b309969f5d5df6eecea1555006c79ebe4b22f27b9c663273a85caeda60`,
  resident **82204** and its ready focus helper **82920**. The 1.9.8.0 label remains.
- All **22 current private preference hashes**, **24 installed file hashes**,
  engine configuration and startup registration are unchanged. A verified copy
  of the preceding temporary executable and private preferences is retained for
  rollback. No new Word/Excel test, installer, release or push was performed.

## Batched Word revision reads — 29 October 2026

- Native revision iterators use bounded batches while retaining the original
  count, full filtering, terminal check and 10,000-item ceiling. Partial/malformed
  results are checked; managed-only providers retain their original scan.
- Five focused suites / **1,464,004 assertions pass**, including **1,104 Word
  assertions** and **58 new batch checks**. Three deliberate regressions fail.
  The C# 5 comparison also passes all **nine native cases / 144 samples / 6,384
  assertions**, including formatting, Track Changes off/on, retained selection/
  caret and both undo levels. The scratch window closed normally.
- Native traces confirm fewer iterator calls and lower patch-stage medians in
  all tracked text-changing cases. The tracked passage median fell from about
  **371 to 296 ms (20%)**; several complex tracked cases improved 13–19%.
  Individual-word totals and untracked cases varied, including regressions, so
  no universal word-speed or full keyboard-response improvement is claimed.
  The comparison has three warm samples per lane/case/tracking state.
- The installed app/settings and accepted release outputs are unchanged. The
  temporary-only switch is recorded above. Its separate C# 5
  app build passes **19 compiled-app checks** without starting a resident. All
  **24 installed hashes** and startup registration are unchanged.
- The preceding item-access experiment's first native run exposed a method/
  property dispatch mismatch; the corrected run stopped after 60 successful
  samples when focus moved to ChatGPT. Both scratch windows closed safely.
  Neither failed run is accepted as a speed comparison. The current batch
  candidate's first run likewise stopped after 51 successful samples on a move
  to ChatGPT; the user confirmed keyboard/mouse use. The fresh user-confirmed
  idle run then completed the whole matrix. Earlier failed runs remain excluded.

## Automated Word fixture follow-up — 29 October 2026

- Scratch setup now proves body typing focus and uses the enabled built-in
  `ReviewNewComment` command before requiring a writable comment editor. An
  isolated setup policy covers interruptions and unsuccessful creation.
- A separate setup-only scope dispatches no conversion and leaves the current
  temporary trial running. Both entry points default to no desktop execution;
  the wrapper has a time limit. The approved native setup check passed with
  unchanged body text. Scoped discovery also handles a missing fixture focus
  notification; ambiguity and interrupted/stale focus are rejected. The full
  silent-helper fault scenario still requires a separately approved trial switch.
- Saved speed traces identify XML proof and patch/revision work as useful next
  targets. Additional opt-in timing covers COM release and direct revision
  enumeration; it changes no edit/formatting/undo policy and is not in the
  running trial. This investigation does not establish another speedup.
- A same-source native calibration timed out after 28 completed samples; it is
  retained as incomplete. Diagnostic attribution identifies revision counting
  and enumeration as useful next targets; patch-stage COM release took roughly
  1–4 ms in these samples. The scratch Word instance was safely closed and the
  original Word session and trial resident/helper remained running.
- Final focused verification: **1,046 Word assertions**, including **24 setup**
  and **20 timing** checks; explicit C# 5 fixture compilation, default desktop
  refusal, documentation and development audit pass. Installed files/settings,
  the active trial executable, startup and accepted release outputs are unchanged.

## Comment-focus and clipboard recovery candidate — 29 October 2026

- Implemented automatic helper-only refresh for unavailable Word focus, with
  settling, cancellation and cooldown. Locator lookups no longer block its pipe
  reader; late results cannot cross a new event or helper retirement. A child-only
  metadata probe can catch up with an already-focused comment. Editing safeguards
  and the tested body adapter are unchanged.
- An existing crash dump identified a separate resident CLR failure during
  foreign clipboard reads. Plain text now uses the guarded native reader; full
  backup reads run in a short-lived STA child. Invalid/unavailable data leaves
  the clipboard untouched. Forty process/protocol/typed-copy regressions pass.
- All **44 isolated suites / 26,998,993 assertions** pass, including **1,013 Word
  checks**. Four deliberate focus faults are rejected. Explicit C# 5 compilation,
  **19 compiled-app checks**, formatting, documentation and development audit pass.
- The original intermittent focus stall site remains unconfirmed. The first
  candidate's two scratch runs failed at editor discovery, and the previous
  temporary binary was restored. An STA-thread regression was reproduced and
  corrected with a dedicated MTA resolver. The revised silent-child test also
  stopped at fixture editor discovery, so native automatic recovery is unverified.
  The exact production-source revision is now running privately: the user confirms
  **new-comment, existing-comment and reply conversion all work**. The read-only
  clipboard capture check passed without changing the clipboard or reporting its
  contents. All **24 installed hashes** and startup are unchanged. Longer-term
  stability remains under trial.
  Evidence and limits are in [Development verification](DEVELOPMENT_VERIFICATION.md).

## Temporary Word trial running — 29 October 2026

- The user-requested trial is active with a separate resident/helper and private
  copies of preferences. Its exact C# 5 binary passed **19 compiled-app checks**.
- All **24 installed file/state hashes**, **22 copied preference hashes** and
  startup registration are unchanged across the normal process switch. No
  installed file was replaced; the existing installed app will be restarted
  when the user ends the trial.
- The user initially reported failure in new comments, existing-comment edits
  and replies. Restarting only the temporary copy, using the same binary,
  restored all three in user-led checks. The fresh log agrees with restored
  comment focus recognition; the intermittent cause remains unconfirmed and no
  runtime fix or lasting stability result is claimed.
- Separately authorized scratch automation passed six new-draft cases on each
  of two runs, then stopped at posted-card discovery in the fixture. Automated
  posted-edit/reply validation remains incomplete. Both scratch windows closed;
  no working document was read or edited. Latest fixture revisions compile only.
  No perceived-speed result is claimed for this trial.
  Session and restoration evidence are recorded
  in [Development verification](DEVELOPMENT_VERIFICATION.md).

## Word speed validated — 29 October 2026

- The first-group Latin-letter optimization is implemented with fresh single-run
  XML proof, unchanged ownership/revision/rollback checks and guarded fallbacks.
  Native tests exposed undo interference when exporting inside a custom record;
  proof now precedes that record and checks that no other record is active.
- Expanded native coverage found an original-adapter Georgia-to-Cambria font
  substitution in a named character style. The candidate restores source fonts
  for one-letter edits too. Native confirmation passed **20 samples / 820
  assertions** in both tracking states, preserving style, fonts and both undo levels.
  The prepared speed reference shares this correctness fix and contains no batching.
- The complete segmented native run passed **nine cases / 144 samples / 6,384
  assertions**, including all formatting, tracking and both undo checks. Word and
  passage warm medians improved; all rows, including slower fallbacks, are recorded
  in [Development verification](DEVELOPMENT_VERIFICATION.md). Both lanes share the
  source-font fix. These timings exclude full keyboard/resident latency and are
  not a direct installed-1.9.8 comparison.
- Traces exposed redundant XML exports for styled/mixed-font words. The final
  source adds cheap rejection before that export, with mandatory XML proof still
  required for grouping. Its targeted live follow-up passed **80 samples / 3,520
  assertions**, including all five changed/native target cases in both tracking
  states. Traces confirm no XML export for the rejected style/mixed-font cases.
  Other matrix paths are unchanged. Earlier interrupted reports remain failed.
- Final word/passage warm medians were about **16–35% lower** against the same
  font-preserving reference. Fallbacks still pay cheap metadata reads; small-sample
  variation is reported rather than claimed as a universal gain. Full keyboard
  response and installed-app speed were not measured. Both complete tables and
  source hashes are in [Development verification](DEVELOPMENT_VERIFICATION.md).
- Revised isolated checks pass: **975 Word assertions**, including **42 patch/font
  regressions** and **155 automation contracts**; **19 compiled-app checks**,
  **26 Python tests**, **120 C# formatting/token checks**, **1,814 development
  audit checks**, documentation and pinned-source verification. All three deliberate
  disabling mutations are rejected by their regressions. All **44 suites passed
  26,998,915 assertions with zero failures** on the latest source. Verification can wait briefly
  for a null foreground reading before undo, while refusing foreign windows and
  still requiring the original native focus identity. Production guards are unchanged.
- All **17 engine budgets** passed earlier, highest warm p95 **4.058 ms**;
  initialization **173.92 ms**. These numbers exclude Word and physical input.
- All **24 checked installed file/settings/state hashes**, the accepted source
  baseline and release artifacts remain unchanged. Scratch Word processes closed;
  original Word and installed KeyJaya stayed running. No installer, release,
  push or installed update occurred. Evidence and limits are in
  [Development verification](DEVELOPMENT_VERIFICATION.md).

## Word test automation — 28 October 2026

- The authorized quiet native run passed **112 repetitions and 4,848 assertions**:
  seven fixed body workloads, Track Changes off/on, two implementation lanes,
  one warmup and three measured repetitions per lane. Exact text/neighbors,
  mixed fonts/formatting, selection/caret, existing tracked deletion, continuation
  finalization and one-step undo were checked automatically.
- This was **same-source calibration**, with no performance optimization under
  test and no speedup claim. Earlier interrupted runs were not accepted. The
  harness stops on foreground/focus changes instead of reclaiming the desktop.
- **123 isolated fixture/report/XML contracts** passed in both PowerShell hosts.
  They are registered in the existing Word snapshot suite. The revised Modern
  Comments fixture compiles, including posted-edit coverage; its new UI selectors
  have not been live-verified against a revised temporary resident.
- The scratch Word process exited. The original Word process and installed
  KeyJaya resident/helper retained their process IDs. All **24 checked installed
  engine/configuration/state hashes** remained unchanged. No installed-app update,
  clipboard operation, keyboard injection, working-document access, installer,
  release or Git push occurred.

Commands, measured scope and limits are in [Testing](TESTING.md) and
[Responsiveness](RESPONSIVENESS.md). Evidence is summarized in
[Development verification](DEVELOPMENT_VERIFICATION.md).

## Unreleased Word follow-up after installed 1.9.8 — 28 October 2026

- Fixed posted modern-comment editing (`postEditor_`) with the same live
  writable-editor, comment-card and Word-process checks as new drafts. The user
  confirmed conversion while editing an existing posted comment in a private copy.
- Retained fresh batched body/classic-comment focus properties and foreground
  revalidation. A revision-reader failure now refuses projection rather than
  masquerading as an empty revision collection. Both regressions fail against
  the retained pre-change source and pass against the corrected source.
- The user confirmed final Ctrl-word, selected-word, passage and immediate
  after-paste conversion, but reported speed **about the same**. Track Changes
  conversion/formatting and undo were confirmed during the preceding experimental
  phase. The count-free revision experiment was reverted; there is **no verified
  end-to-end speed improvement**. Timing scope is in [Responsiveness](RESPONSIVENESS.md).
- All **44 isolated suites passed: 26,998,718 assertions, zero failures**.
  Explicit C# 5 compilation, **778 Windows PowerShell 5.1 focused assertions**,
  **19 compiled-app checks**, **26 Python tests**, **116 C# formatting/token
  checks**, **1,772 development-audit checks**, pinned-source checks and all
  **17 engine benchmark budgets** passed. The highest engine warm p95 was
  **4.268 ms**, which excludes Word provider/automation time.
- Installed **1.9.8.0** was restored and its resident/helper verified. All
  **24 checked installed program/configuration/state hashes** remained unchanged.
  The trial profile was removed after verifying a private archive of **26 files**.
  No working document was read or edited by a diagnostic tool; manual checks
  were user-led in disposable Word content. No new installer, installed-app
  update, release, push, version bump or accepted-baseline refresh occurred.

Evidence and limitations are recorded in [Development verification](DEVELOPMENT_VERIFICATION.md).

## KeyJaya 1.9.8 installer prepared — 28 October 2026

- All **44 isolated suites passed: 26,998,668 assertions, zero failures**.
  The C# 5/.NET Framework 4.8 app passed its **19-check compiled probe**;
  Windows PowerShell 5.1 also compiled and passed **728 focused Word/helper/hook
  assertions**. All 26 Python tests, 113 C# formatting checks, 1,791 strict
  repository checks, documentation links and pinned-source hashes passed.
- The deliberately refreshed 1.9.8 source baseline covers **47 production files**.
  Runtime data and factory preferences/triggers are unchanged. All **17 engine
  benchmarks** passed with 301 warm samples each; highest warm p95 was
  **3.680 ms**, initialization **186.91 ms**. These exclude editor response time.
- All **38 packaged file hashes** matched the payload ZIP. The installer's
  embedded payload matched that ZIP by SHA-256. Isolated clean installation,
  edited-trigger upgrade and post-swap failure rollback passed, restoring all
  **56 fixture file hashes**. Setup-retirement integration passed **25 checks
  each** in PowerShell 7 and Windows PowerShell 5.1.
- An isolated installation from the **exact published 1.9.7 installer**, verified
  against its retained public checksum, survived an injected failed upgrade with
  all **46 file hashes restored**. A successful 1.9.8 upgrade preserved all
  **8 configuration and 5 state file hashes**. Its converter passed **16 checks**
  for custom/deleted/disabled triggers, legacy/unified spellings, learned spelling
  and votes, protected words, style, shortcuts, encoding and disabled settings.
- Before version-only packaging, the reviewed code completed the user-led
  approximately 89-minute temporary trial recorded below. No additional live
  editor test was run during installer preparation.
- The original installed **1.9.7.0 remained running with the same process ID**.
  All **24 installed program/configuration/state hashes were unchanged**, and
  registration/startup still targeted that installation. No live installer was
  executed, installed app updated, Git change pushed or release published.
  Large disposable fixtures and the preserved previous development tree were
  removed after retaining verification evidence; one current app tree remains.

The installer is **unsigned**. It is prepared locally; publication and live
installation remain separate user-authorized actions. Evidence and limitations
are in [Development verification](DEVELOPMENT_VERIFICATION.md).

## Revised stability trial completed — 28 October 2026

The exact reviewed build ran for approximately **89 minutes**, 18:08–19:37 IST,
with separate preference copies. The user reported everything working fine.
Logs show no helper/hook failure or retry, and the trial resident/helper closed
normally. Installed **1.9.7.0** was restarted with all **24 checked installed
file hashes unchanged**. Trial preferences/logs were archived separately and the
generated runtime profile removed. This is one user-led session, not an uptime
guarantee; see [Development verification](DEVELOPMENT_VERIFICATION.md).

## Unreleased focused reliability review — 28 October 2026

- Corrected helper readiness/event ordering, cumulative recovery delays,
  unexpected hook-loop exit, remaining provider/clipboard reads under the input
  gate and cleanup after failed startup. Live target/selection and generation
  checks remain; clipboard cycling additionally requires stable sequence proof.
- All **44 isolated suites passed: 26,998,665 assertions, zero failures**. The
  final startup-cleanup refinement then passed **728 focused Word assertions**,
  including 47 helper and 21 hook lifecycle checks. Three deliberately broken
  builds were rejected by their corresponding regressions.
- C# 5 compilation, the 19-check app probe, 26 Python tests, 113 C# formatting
  checks, 1,743 development audit checks, documentation/pinned-source checks and
  all 17 engine benchmark budgets passed. The installed app, settings and working
  documents were untouched; no resident, live editor test or installer was run.
  A further physical trial and extended uptime test remain outside this review.
  Details are in [Development verification](DEVELOPMENT_VERIFICATION.md).

## Unreleased resident reliability and Word fixes — 28 October 2026

- Two recent Windows crash records identified an exception in the Framework UIA
  focus tracker before the application callback. Its subscription now lives in
  a supervised helper. An authorized test stopped only that helper: the resident
  stayed running and a replacement was observed within 1.777 seconds.
- Dedicated keyboard/mouse hook dispatch and moving provider/clipboard waits
  outside the input gate address the observed lost-hook symptom. Word conversion
  now accepts an exactly captured single-word selection and avoids repeated
  comment/menu discovery, native letter bindings and styling-context reads.
- The user confirmed Ctrl-word, one selected word, a selected passage, backtick,
  input outside Word, and both comments and replies in a temporary copy. Speed
  was acceptable. The last request-local context reuse received automated checks
  after that trial. This does not establish indefinite uptime or every editor.
- All **44 isolated suites passed: 26,998,638 assertions, zero failures**. The
  C# 5 app build, 19-check app probe, 26 Python tests, 113 C# formatting checks,
  1,743 development audit checks, pinned-source checks and all 17 engine benchmark
  budgets passed. No runtime-data or accepted-release baseline changes were made.
- Installed **1.9.7.0** was restored with all **23 checked program/configuration/
  state hashes unchanged**. The temporary resident/helper and private profile
  were removed. No installer, installed-app update, release or Git push occurred.
  Diagnosis, timing scope and limitations are in
  [Development verification](DEVELOPMENT_VERIFICATION.md).

## Unreleased Acrobat search fix — 28 October 2026

- All 44 isolated suites passed: **26,998,577 assertions, zero failures**,
  including 141 new native Edit/provider regressions. Restoring the previous
  provider precedence made the new search-host regression fail as expected.
- The user confirmed caret conversion, repeated Ctrl cycling and continued
  typing in both Ctrl+F Find and Advanced Search using a temporary development
  copy. The installed 1.9.7 was then restored; all 23 checked program/configuration/
  state hashes remained unchanged. No live Word/Excel tests were run.
- C# 5 compilation, the 19-check compiled-app probe, 26 Python developer tests,
  109 C# formatting checks, source/data audit, pinned-source verification and
  all 17 engine benchmark budgets passed. Detailed evidence and the scope of
  physical checks are in [Development verification](DEVELOPMENT_VERIFICATION.md).

At that development stage, the version and accepted baseline remained 1.9.7.
No installer was built, release published, Git changes pushed or installed app
updated during those earlier fixes.

## KeyJaya 1.9.7 — 27 October 2026

- The reviewed functional changes passed all **44 isolated suites: 26,998,434
  assertions, zero failures**. Final selection-boundary checks passed **five
  focused suites, 647,601 assertions**, including **660 Ctrl-italics checks** and
  **22 production font-cycle/undo checks**. Version-only release changes passed
  the **19-check compiled-app probe** and strict source/data audit.
- All **26 Python developer-tool tests**, **107 C# formatting checks**, **1,717
  strict repository checks**, documentation links and pinned dictionary-source
  checks passed. The accepted source baseline is now 1.9.7; runtime data hashes
  and factory configuration were not changed.
- All **38 packaged files** matched the payload ZIP by SHA-256. The installer's
  embedded ZIP matched that verified payload. Isolated clean installation,
  preservation of an edited trigger file and injected post-swap rollback passed;
  rollback restored all **56 fixture file hashes**.
- The actual published **1.9.6 installer**, verified against GitHub's SHA-256,
  created a separate upgrade fixture. An injected failed upgrade restored all
  **45 previous-installation hashes**. The successful upgrade preserved all
  **8 configuration hashes** and **4 existing state hashes**. Eight runtime checks
  confirmed custom triggers, legacy and unified spelling preferences, kept words,
  style overrides, custom shortcuts and disabled automatic italics. Loading the
  upgraded runtime did not rewrite existing configuration.
- Source-level setup rollback passed **25 checks** in PowerShell 7 and another
  **25** in Windows PowerShell 5.1. These tests use disposable `out/` folders;
  neither installation testing nor release building changes the live profile,
  starts the resident or registers startup.
- All **17 engine benchmarks**, with 301 warm samples each, passed their budgets.
  Highest warm p95 was **4.490 ms**; initialization was **219.880 ms**. These are
  engine measurements, not complete editor-response timings.
- Earlier user-led scratch checks confirmed modern Word comments/replies,
  Ctrl italics, faster word/passage conversion, retained selection, one-step undo
  and conversion immediately after pasting, plus multiline Excel editing. The
  final review-specific padded/delayed Word and rich-editor Backspace cases have
  production-helper/model coverage; they were not physically rechecked for this
  release. Results do not establish every editor or Office version.

The installer is unsigned. Sign-in, sleep/wake, permanently hung providers and a
separate user's physical installation remain outside the completed checks.
Detailed evidence and boundaries are in
[Development verification](DEVELOPMENT_VERIFICATION.md).

## KeyJaya 1.9.6 — 26 October 2026

- All **44 isolated suites passed**. The full review run plus the final focused
  trigger-editor restoration checks total **26,940,784 assertions; zero failures**.
  All **26 Python developer-tool tests** passed. Both review findings were
  reproduced before being fixed: first-upgrade BB deletion preservation and
  unified ordered-replacement precedence.
- Native Word fault tests passed **34 checks**, including a measured three-second
  COM stall, disconnected COM cleanup, actual font/italic preservation, one-step
  native undo, focus-loss rollback after a patch and closed-window queue recovery.
  The owner physically tapped Ctrl and immediately typed ` next Word` in a
  separate disposable document while only that Word server was paused for three
  seconds. All **13 physical-test checks passed**: the word converted, preceding
  text and every suffix character remained intact, English stayed nonitalic at
  its original size, modifiers were released and undo restored the original.
  The helper injected no keys; the running temporary resident performed conversion.
- The focus fixture now hides only its owned test frame and verifies actual
  foreground loss before evaluating rollback. This avoids confusing a refused
  foreground request with a product failure. No production keyboard/clipboard
  code changed during this final reliability pass.
- Source-level setup integration passed **25 checks** in PowerShell 7 and 5.1.
  Locked-file failures at early, middle and final retirements and a failure after
  all retirements restored exact configuration, migration markers and retired
  files. The underlying transaction also checks every distribution retirement
  position as part of `StandaloneTests`.
- The final compiled 1.9.6 app passed **19 probe checks**. All **38 packaged files**
  matched the ZIP; the embedded installer payload matched that ZIP by SHA-256.
  Isolated clean installation and edited-trigger upgrade passed. An injected
  post-retirement failure restored all **56 fixture file hashes**.
- The actual 1.9.5 installer was used to create another isolated profile. Its
  first attempted upgrade to 1.9.6 deliberately failed after retirement and
  restored **58 old-release file hashes**. The successful upgrade then preserved
  all **13 configuration file hashes**, including custom, disabled and deleted
  triggers, the absence of a BB marker, and conflicting legacy/unified rules.
  The packaged converter produced the unified replacement and retired obsolete files.
- All **17 engine benchmarks**, with 301 warm samples each, passed the 150 ms
  p95 budget; the highest measured p95 was **9.348 ms**. Engine initialization was
  measured separately. These are engine timings, not full editor response times.
  Source/data integrity, documentation and 94 C# formatting checks also passed.
- Existing working documents, installed program files and live configuration
  were not modified by automation. Tests used disposable content beneath ignored
  `out/` directories. The pre-review installer was archived as superseded.

The installer is unsigned. The native stall tests are bounded and resume the
owned Word server independently. A permanently hung COM server, Windows sign-in,
sleep/wake and every possible editor are outside the completed checks. Results
do not establish universal compatibility or guaranteed latency. Detailed review
history remains in [Development verification](DEVELOPMENT_VERIFICATION.md).

## KeyJaya 1.9.5 — 23 October 2026

- All 44 isolated suites passed: **26,935,872 assertions; zero failures**. The
  final development run includes the browser paragraph-boundary fix and repaired
  scripture identifiers. Release packaging changes only the runtime version.
- All **2,734,965 indexed records** passed bounds, order, UTF-8, and key/hash
  checks. All 30 runtime-data file hashes passed. The 276 identifier repairs
  preserved all 226,032 scripture payloads: **34,825,664 text bytes unchanged**.
- The owner reported passing the final manual checks in disposable Word content
  (ScaGoudy, paired verse ranges, italics, formatting, undo and Track Changes),
  WhatsApp (range insertion followed by immediate typing), and Excel/Obsidian
  (word cycles, single-letter conversion and subsequent normal typing).
- The 1.9.5 compiled application passed 19 noninteractive probe checks. All 53
  packaged file hashes matched the ZIP payload. Isolated fresh installation and
  upgrade preserved an edited trigger file. These checks did not install over
  the active app, change startup registration, or touch open documents.
- The installer resource matched the packaged ZIP byte-for-byte by SHA-256.
  An injected failure after swapping the isolated installation restored all 57
  fixture files with their original hashes, including the edited triggers.
- Source, bundled Hindi-source hashes, workflow syntax, and local documentation
  checks passed. The accepted source baseline is now 1.9.5; earlier baselines
  remain available in Git history.

The installer is unsigned. These results cover the tested logic and reported
editor interactions; they do not guarantee universal compatibility, response
times, or Windows sign-in/sleep/wake behavior. Those interruption checks remain
outside this release's completed manual checks.

## Initial public source preparation — 1.9.4

Verified on 22 October 2026 against the 1.9.4 source baseline.

- All 35 production C# files match the accepted source, allowing only line-ending
  normalization. No runtime refactoring was introduced during repository preparation.
- Personal email trigger entries were removed from editable defaults, factory
  restoration defaults and the TSV fallback. Obsolete default-setting keys were removed.
- All 42 included noninteractive regression suites passed: **24,215,404 assertions**.
- A probe against the newly compiled app passed 19 checks for version, defaults,
  vocabulary, scripts, English protection and removal of the personal expansion.
- All 53 public-release package files matched the ZIP payload, including the MIT
  license, third-party notices and licensing documentation. Isolated clean
  installation and upgrading an edited trigger file passed.
- An export containing only Git-included files built successfully without the
  old development directory. The disposable export was removed afterward.
- Windows PowerShell 5.1 built the app and ran a test suite successfully; PowerShell
  7 built the installer and ran the full included test set.
- The pinned original Hindi source files passed checksum verification. Repository
  audits passed for source/data integrity, exclusions, personal paths and email triggers.
- Generated output is ignored by Git. Existing installed app files and development
  history were preserved during public-release preparation.

The original-code MIT license and the owner's confirmation of permission to
distribute included book texts are recorded in `LICENSING.md`. This verification
covers source preparation and the included automated checks; it does not replace
physical editor-specific checks.
