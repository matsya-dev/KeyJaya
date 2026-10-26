# Privacy and local data

This describes the source of KeyJaya 1.9.6, rather than making promises about
other software or future releases.

## Keyboard and editor access

KeyJaya registers Windows keyboard/mouse hooks and conversion shortcuts so it
can recognize letter/word actions, short triggers, spelling, and contractions
across apps. It keeps recent typing and conversion state in memory. It does not
write a continuous keystroke history to disk.

For a conversion, Windows accessibility interfaces and editor-specific adapters
can read the active field or selection, surrounding text, caret positions, and
formatting. An editor can expose a larger text range than the word being changed;
that context is used to verify the target, preserve formatting, and support undo.
This is not a background crawl of files on the computer.

**Pause KeyJaya** stops conversion hooks/shortcuts and restores the app's ordinary
key behavior. **Exit** closes the resident process. Do not assume every custom,
protected, or elevated editor is supported.

## Clipboard

Copied-text conversion reads the current clipboard. Some editor adapters also
temporarily copy selected text or paste a replacement while preserving the
previous clipboard. The code checks clipboard ownership/sequence and attempts
to restore the original formats without overwriting newer clipboard contents.
If it cannot safely capture or verify a target, a conversion can be skipped.

Temporary conversion payloads are marked to exclude them from Windows clipboard
history and cloud clipboard processing. Those flags are not a guarantee about
third-party clipboard managers. Clipboard data is not sent to a KeyJaya service.

## Local files and preferences

A normal installation is in `%APPDATA%\KeyJaya`. Its files include:

- `config/`: settings, custom shortcuts, triggers, and spelling/style overrides.
- `state/preferred-spellings.tsv`: learned word spellings and preference votes.
- `state/`: setup choices and migration markers as well as spelling preferences.
- `logs/keyjaya.log` and its rotated `.1` file: operational errors/status messages.
- Trigger/configuration backups; setup can also leave backup folders beside the
  installation. A failed installer can write a `KeyJaya-Setup-*.log` in `%TEMP%`.

Custom replacements and learned spellings can reveal words or phrases you use.
These files and backups are ordinary local files, readable by anyone with access
to them. Windows account permissions and your own backup/sync settings apply.
Installed preferences and logs are excluded from the public source and installer.

The uninstaller stops KeyJaya and removes its registration/startup entries and
owned shortcuts; it deliberately preserves installed files and configuration.
For complete data removal, first exit/uninstall, save any triggers you want,
then review and remove your KeyJaya installation and its backup folders yourself.

## Logs and diagnostics

The optional **Check Word conflicts…** button in Customize shortcuts reads assignments
and active-window ownership from already-open Word instances. A hidden,
time-limited helper performs this check only when requested. It does not read
document text, activate Word, execute Word commands, change assignments or save
templates. Results appear locally in the check window; nothing is uploaded.

Normal runtime logs contain status/error types rather than a transcript of text
you type. Logs rotate after roughly 256 KiB, retaining one previous file. Setup
failure logs contain exception details and can include local file paths.

The optional `--diagnostic` launch flag enables additional local troubleshooting
messages, limited to 15 minutes and 2,000 diagnostic entries. These can include
control identifiers, selection lengths/positions, key codes, timings, and error
details. Inspect and redact logs, screenshots, paths, and sample text before
posting them publicly. Diagnostics are off in an ordinary launch.

## Network and updates

The resident performs conversions and dictionary lookup locally. The reviewed
source has no analytics, crash-upload service, online conversion service, or
automatic update check. It does not send typed text or clipboard contents over
the network.

Downloading the installer, visiting GitHub, and reporting an issue use your
browser/GitHub and their own privacy policies. A developer-only source-fetching
helper can download pinned public Hindi dictionary files when explicitly run;
that helper is not a background component of the installed app.
