# Contributing to KeyJaya

KeyJaya edits text in other applications. A missed conversion is inconvenient;
editing the wrong text, losing a keystroke, or leaking formatting is worse. Keep
changes small enough to review and preserve editor-specific safeguards.

Start with [the code map](docs/ARCHITECTURE.md),
[build instructions](docs/BUILDING.md), and [testing guide](docs/TESTING.md).
Builds and tests write beneath ignored `out/`; they do not update an installed
profile. Use separate disposable documents for physical editor tests.

## Working on code

- Keep runtime code compatible with C# 5 and .NET Framework 4.8. The resident is
  one executable; Python tools are development utilities, not runtime services.
- Add a comment when it explains ownership, ordering, an external API constraint,
  or a nonobvious compatibility decision. Prefer a clear name to narration of
  ordinary assignments. Update comments when the contract changes.
- Reuse conversion policy in `Core`; keep native editor behavior in its adapter.
  Do not replace target verification with blind Backspace or unbounded retries.
- Retain legacy configuration readers until there is an explicit migration plan.
  Loading settings must not rewrite edits, resurrect deleted triggers, or save
  preferences merely because a word was cycled repeatedly on one occurrence.
- Preserve data/index bytes and third-party notices. Their manifests are
  integrity checks, not formatting inputs.

## Checks before review

```powershell
./test.ps1
./test.ps1 -Full
python -m unittest discover -s tests/python -v
python tools/check_docs.py
python tools/fetch_hindi_sources.py --check
python tools/audit_project.py --development
./tools/benchmark.ps1 -Check
```

The default C# selection is useful during iteration; the full command runs all
44 registered suites. Choose a focused suite with `-Suite CtrlShortTests`.
Python tests use temporary fixtures and mock the network and installer.
The engine benchmark enforces a warm p95 budget and reports startup/first calls
separately. See [Responsiveness verification](docs/RESPONSIVENESS.md) for its scope
and the shared editor contract matrix.

Optional formatting tools:

```powershell
python -m pip install -r requirements-dev.txt
python -m black --check package.py tools tests/python
pwsh -File tools/format_csharp.ps1 -Check
```

Omit `--check` / `-Check` to apply formatting. The C# formatter requires PowerShell
7 with its Roslyn assemblies; the ordinary build still works in Windows
PowerShell 5.1. It verifies C# 5 parsing and identical executable tokens before
writing a file. Pillow is needed only when regenerating documentation images.

For a behavior change, add a regression that would fail before the change and
exercise the observable result. Use boundary/interruption cases for edits, not
tests that simply repeat the implementation. Automated suites do not replace
physical checks of editor timing, native undo, Track Changes, or pasted text.

## Preparing a release

Update the version and accepted source baseline only as part of a reviewed
release. Development audit reports intentional source drift; it does not accept
it. The strict audit must pass after the baseline is deliberately refreshed.
Never refresh a data manifest merely to silence an unexplained mismatch.

Build and installation commands are separate from the checks above. The optional
installation verifier executes an installer in a new fixture beneath `out/`;
run it only when installation testing is intended. Preserve user configuration
and keep development reports, source ebooks, and personal state out of payloads.
