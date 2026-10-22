# Building KeyJaya

## Requirements

- Windows x64 with .NET Framework 4.8 installed.
- The Framework x64 C# compiler and WPF framework assemblies. `build-common.ps1`
  locates these beneath the Windows directory; no Visual Studio solution or NuGet
  restore is currently required.
- Windows PowerShell 5.1 or newer (PowerShell 7 also works).
- Python 3.11 or newer on PATH for installer packaging.

Run from the repository root:

```powershell
.\build.ps1
```

If Python uses a different executable, pass its path:

```powershell
.\build.ps1 -Python 'C:\Python311\python.exe'
```

For an app/uninstaller build without Python or installer packaging:

```powershell
.\build.ps1 -AppOnly
```

If PowerShell blocks local scripts, use a process-scoped policy for the command
rather than changing the machine's permanent policy:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

## Outputs

| Path | Purpose |
| --- | --- |
| `out/app/engine/keyjaya-app.exe` | Compiled resident |
| `out/app/engine/keyjaya-app.exe.config` | Required Framework/DPI configuration |
| `out/app/KeyJaya-Uninstall.exe` | Uninstaller |
| `out/payload.zip` | Installer input |
| `out/KeyJaya-Setup-v1.9.4.exe` | Installable package |

The build copies checked-in data, defaults, and licensing notices, compiles the
app and uninstaller, then packages the installer. It never launches the resident, installs anything,
changes startup registration, or reads the installed user's configuration.

Keep the `.exe.config` beside the resident executable. The window DPI behavior
depends on it and `src/runtime.manifest`.

## Data and repeatability

The runtime data pack is included so a clean checkout does not need this
computer's ebook library or development folders. `data/manifest.json` records
its hashes. `tools/audit_project.py` checks these, source provenance and exclusions.
Dictionary/source licenses are described in `THIRD_PARTY_NOTICES.md`.

After building, `python tools/verify_build.py` checks all packaged bytes and runs
clean-install/upgrade checks using only `out/verification/install`. It refuses to
overwrite an existing verification installation. These checks do not affect the
installed app or Windows startup registration.

This workflow reproduces the app from its source/data inputs. The legacy Framework
compiler embeds build-specific PE metadata, so independently compiled executables
are not promised to have identical byte hashes.

## Changing the version

Update `VERSION`, `Version.Current` in `src/Core.cs`, the assembly/file versions in
`src/AssemblyInfo.cs`, and both manifest identities together. Test before releasing.
The current sources preserve the accepted 1.9.4 runtime logic; build scripts and
repository defaults were organized separately.

`docs/SOURCE_BASELINE.json` records the initial 1.9.4 source hashes. The audit
deliberately detects drift from that baseline. After an intentional reviewed
source/data update, refresh the corresponding baseline/manifest and version;
do not bypass a mismatch without understanding the changed files.
