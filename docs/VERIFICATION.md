# Release verification

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
