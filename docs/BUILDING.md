# Building KeyJaya

For module responsibilities, edit-safety contracts and configuration precedence,
see [the code map](ARCHITECTURE.md). Review conventions and optional formatting
commands are in [Contributing](../CONTRIBUTING.md).

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
| `out/KeyJaya-Setup-v1.9.7.exe` | Installable package |

The build copies runtime data, defaults, and KeyJaya's original MIT notice, compiles the
app and uninstaller, then packages the installer. It never launches the resident, installs anything,
changes startup registration, or reads the installed user's configuration.

Editable trigger defaults are copied from `assets/short-trigger-defaults.yml`;
factory restoration and upgrade additions use that same source. The seven old
customization templates are replaced by the sectioned `customizations.ini`.
Existing TSV files are still accepted without being rewritten. Each build uses a
fresh `out/app`; an earlier output is preserved under `out/previous-app-*` so
obsolete defaults cannot leak into a new package.

Keep the `.exe.config` beside the resident executable. The window DPI behavior
depends on it and `src/runtime.manifest`.

`assets/distribution-exclusions.txt` is the single list of source-only files.
The build and ZIP packager omit those paths, and setup retires previous copies
through its backed-up transaction. The list cannot remove user configuration,
executables or runtime dictionary/index files. Upstream notices and provenance
remain in the development repository; the owner's separate-permission
confirmation and distribution scope are recorded in [Licensing](LICENSING.md).

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
`docs/SOURCE_BASELINE.json` records the accepted release's source hashes. The audit
deliberately detects drift from that baseline. After an intentional reviewed
source/data update, refresh the corresponding baseline/manifest and version;
do not bypass a mismatch without understanding the changed files.

During authorized development, `python tools/audit_project.py --development`
reports source drift while checking data integrity, exclusions and the current
file layout. It does not update the accepted release baseline. The default audit
still requires the accepted source hashes before a release.

## Documentation preview and compiled-app probe

After building, run `./tools/verify_app.ps1` to check the compiled app in a
noninteractive probe. It loads conversion code without starting the resident.

`./tools/render_tray_menu.ps1` regenerates `docs/images/tray-menu.png` from the
app's actual tray controls and factory settings. It does not display the menu,
capture the desktop, or read installed user configuration. Review the image
before committing it, then run `python tools/check_docs.py` to check local links.

`./tools/render_readme_demo.ps1 -Python 'C:\Python311\python.exe'` regenerates the
README typing animation and still image. This optional documentation tool needs
Pillow and the Windows Cambria font. It verifies the displayed spellings and cycle
order against the compiled conversion engine, then draws an illustrated typing
sequence; it does not record or control a live editor. Its pace is illustrative,
not a performance benchmark. Pillow is not required to build or use KeyJaya.
