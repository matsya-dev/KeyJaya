# Generated files and automatic cleanup

Normal builds and isolated tests use fresh, owned directories under `out/w/`.
The short random directory name leaves room for .NET Framework's nested test
paths. An ownership token and an exclusive lease identify the run. Cleanup
validates the path and rejects junctions/symlinks before removing any tree.
Source, installed profiles, release folders, recovery archives and preexisting
unmarked output are outside this policy.

## Default behavior

- `test.ps1` puts its fixture, copied inputs, runner and all neighboring C# test
  artifacts in the same workspace. After each successful suite it removes those
  files, including the distribution/rollback copies that previously accumulated.
  Each suite starts with fresh data/configuration. Logs and `results.json` remain
  in `out/reports/tests/<run-id>`; the usual latest `out/tests/*.txt` and
  `out/tests/results.json` paths remain available to CI and local tools.
- `build.ps1` compiles and packages in a private stage. It publishes the app tree,
  payload and installer only after the requested build succeeds. An output
  receipt records their exact hashes. Replacement refuses edited, unmarked,
  linked or running app output. A failed promotion restores the prior outputs;
  if rollback itself fails, the full workspace is pinned for inspection.
  Successful replacement removes the old app tree instead of accumulating
  `previous-app-*` directories. Final `out/app`, payload and versioned installers
  are retained deliberately.
- `tools/benchmark.ps1`, `tools/verify_app.ps1` and
  `tools/test_setup_transaction.ps1` retain reports and remove their private
  compiled runners, fixture data, installation copies and rollback backups.
- `python tools/verify_build.py` uses an owned disposable parent for the install
  and its sibling backup/staging folders. It retains
  `out/verification/build-results.json` and a report, then removes that parent.
  The lower-level Python `verify_build()` function still leaves an explicitly
  supplied fixture for its caller to own; the CLI uses `run_verification()`.

PowerShell `finally` blocks and the Python context manager clean up after ordinary
failure as well as success. A subsequent PowerShell tool also checks for marked
workspaces whose original process has exited and preserves an interrupted-run
snapshot before removing them. Active leases, PID/start-time matches, pinned
workspaces and unknown ownership records are left alone. A locked or unsafe tree
is preserved with a cleanup error, rather than forcing removal or killing an app.

## Evidence retention

Reports include output, input hashes and cleanup status. PowerShell reports also
record the shell/OS/bitness. Native stdout/stderr is captured even when a command
fails under Windows PowerShell 5.1. Large command output is bounded to its first
and last 128 KiB of characters.
`-KeepArtifacts` also disables that log truncation.

Failed runs additionally retain a verified `failure.zip`: source inputs and
small configuration, state and diagnostic files, with an inventory of captured
and omitted files. Individual captures are limited to 1 MiB and the captured
payload to 16 MiB. Large dictionary/index copies and compiled binaries are
regenerated instead of archived. Use the explicit inspection option below when
the full failed fixture is needed.

Each tool retains its five latest completed reports plus its latest failure if
that is older. A report's hash seal and file inventory must still match before
pruning. A `.keep` file, modified/annotated evidence or `-KeepArtifacts` pins it.
Historical output without these ownership records is never swept automatically.
CI uploads `out/reports/`, including compact failure evidence.

## Keeping a full fixture deliberately

```powershell
./test.ps1 -Suite StandaloneTests -KeepArtifacts
./build.ps1 -AppOnly -KeepArtifacts
./tools/benchmark.ps1 -Check -KeepArtifacts
python tools/verify_build.py --keep-artifacts
python tools/verify_build.py --target out/verification/inspection/install --keep-artifacts
```

An explicit Python `--target` requires `--keep-artifacts`; it is an inspection
fixture, not an automatically removed directory. Kept runs can be large and
remain until explicitly retired.
In a kept multi-suite run, `s00`, `s01`, etc. hold separate fresh fixtures in
the order recorded by `results.json`.

Live Word/Excel tests still require permission. Their saved documents, diagnostic
trial profiles and prepared builds retain their existing inspection lifecycle;
this cleanup does not remove them or stop a running trial.
