# Clean repository verification

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
