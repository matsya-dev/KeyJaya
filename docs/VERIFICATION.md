# Release verification

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
