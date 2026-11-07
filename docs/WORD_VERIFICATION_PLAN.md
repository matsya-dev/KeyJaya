# Prepared Word conversion and recovery checks

Executed again on **1 November 2026**, after one explicit 30-minute quiet-window
approval, against the reviewed reply/recovery fixes. Two full comment runs kept
the same normal resident and passed **66 actions / 1,774 checks**. The separate
private silent-helper recovery passed **80 checks**. All nine body fixtures,
tracking off/on, passed **144 samples / 6,384 checks**. All phases passed and
finished within the approved window. Scratch processes/private residents closed;
installed 1.9.9, its preferences/registration and the clipboard fingerprint were
preserved. Evidence: `out/automated-word-2026-11-01`. No diagnostic deadline was
extended or bypassed. Calibration and physical-hook limits below still apply.

Executed on 30 October 2026 after explicit user approval: the final body run
passes 144 samples/6,384 checks; comments pass all 33 actions/886 checks; and the
final recovery run passes 83 checks. The private trial ended, its profile was
retained, and installed 1.9.8 was restored unchanged. Earlier interrupted or
failed attempts are retained separately in the session evidence. See
[verification](VERIFICATION.md) for tester repairs and coverage limits.

Preparation is separate from permission to run Word or change the active app.
The current trial must continue uninterrupted until the user explicitly ends it.
Installed files, settings, custom triggers, spelling preferences, startup and
working documents must remain unchanged throughout.

## Prepare without desktop access

```powershell
./tools/prepare_word_verification.ps1
```

This command has no desktop option. It compiles C# 5 runners, runs pure
self-tests and default-deny probes, and writes
`out/word-full-verification/preparation.json`. It does not start Word, a resident
or a focus/clipboard helper; inspect UIA; read the clipboard; access either user
profile; create a test installation; or arm the recovery fault. The build folder
is reused, and no dictionary corpus is copied.

The report records source and executable hashes. Source hashes are taken before
compilation and rechecked afterward. Body binaries must still match their own
self-test report. Recheck every recorded hash before a live run; regenerate
preparation if any input or shared body build changes. Preparation success is
not live verification.
The normal candidate is compiled separately and checked only with `--version`,
which returns before resident initialization. Live wrappers rebuild their runners;
retain those fresh hashes and check their source identity against this plan.

## Matrix and limits

| Phase | Coverage | Required evidence |
| --- | --- | --- |
| Word body | Nine fixtures, Track Changes off/on, paired same-source runs, three measured repetitions plus one warmup: 144 samples | Text, original fonts and mixed formatting, character styles, italics, retained selection/caret, grouped conversion undo, prior undo history, existing deletions, continued typing and revision-display restoration |
| Modern Comments | 33 conversion actions across contextual new/edit/reply and panel new/edit/reply | Word, selection, letter, expansion, native undo and continued English typing as declared by the scope; plain comment formatting, unchanged body and writable editor; every declared view/action exactly once |
| Silent focus-helper recovery | One explicitly armed contextual-comment fixture | Same test-resident PID and comment/caret without refocusing; replacement helper resolves that editor; conversion, plain formatting, native undo and unchanged body |

Body timing is a same-source calibration, not evidence of a speed improvement.
Comments use production registered-shortcut dispatch and include Word Ctrl/focus
delivery in the new contextual editor. Injected Ctrl does not prove the physical
keyboard hook. The recovery case does not cover every failure in every comment
view. Word Navigation search and physical keyboard gestures retain their earlier
user-led evidence; this matrix does not claim new coverage for them.

The recovery protocol permits a second explicit dispatch only after successful
replacement-helper resolution if the first attempt left the original text.
It does not reseed or refocus the editor, and does not claim that every first
attempt completes during helper replacement.

The comment report distinguishes `contextual posted edit` and `panel posted edit`.
Missing, duplicate, unknown or incorrectly named actions prevent a full pass,
even when every action that did execute appeared successful.

## Live execution gates

1. Obtain fresh user approval and a stated quiet period before each live phase,
   or one approval covering the complete stated session. Opening Word, changing
   focus and typing into scratch content are live work. Pause if the period ends.
2. The body phase can run with the current trial unchanged. Use the prepared
   all-case body runner through `tools/test_word_performance.ps1`, with explicit
   desktop opt-in, three iterations, one warmup and a bounded timeout. It creates
   only its new Word process/document and closes that scratch application.
3. Full resident comment and recovery phases cannot use the current trial's
   expired fifteen-minute diagnostic window. A zero reply from that expired
   probe is not settlement evidence. Do not extend, bypass or restart it.
4. First obtain separate authorization to end the trial and use fresh diagnostic
   test sessions. Do not run two residents concurrently. Check exact executable,
   profile, process/parent identity and the remaining diagnostic deadline before
   opening Word. The full comments scope is `All`; the recovery scope is
   `ContextualRecovery`. Each requires enough time for the complete bounded run.
   The prepared normal candidate is
   `out/word-full-verification/build/normal/keyjaya-app.exe`; it is not launched
   as a resident during preparation.
5. Recovery uses only the prepared test entry point in
   `out/word-full-verification/build/keyjaya-app.exe`, with its adjacent Framework
   configuration. A later approved launch must explicitly supply
   `--fixture-resident --root <private-profile> --diagnostic`. The profile must be
   a direct GUID-named child of `out/word-full-verification/profiles`; it is not
   created by preparation. Populate only that new test profile after approval.
6. Arm `silent-focus-once.flag` only in the prepared fixture's executable folder
   after verifying the approved test session. Its first focus child consumes the
   flag, records its PID and announces readiness without focus notifications.
   Subsequent helpers use unchanged production code. The native runner verifies
   the marker, parent PID, executable and command line before retaining an exact
   helper handle, with a fifteen-second emergency fault-cleanup deadline. Never
   suspend a real UIA client or target the trial/installed helper.
7. Stop on focus loss, identity changes, text/formatting mismatch, duplicate or
   missing cases, or an expired diagnostic/quiet period. Do not refocus or retry
   ordinary conversion cases to turn a failed case into a pass. A timeout stops
   only its runner;
   inspect any remaining owned scratch window before further action.
8. Retain results and failures separately. Verify scratch cleanup and installed
   file/startup preservation. Switching back to any app session must follow the
   user's explicit authorization; these preparation scripts perform no switch.

The current trial, installed app and settings remain unchanged by this plan.
