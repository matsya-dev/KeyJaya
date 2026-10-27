# KeyJaya 1.9.7 release notes

Changes since **1.9.6**, prepared for release on **27 October 2026**.
KeyJaya supports Windows x64 with .NET Framework 4.8.

## Word comments and navigation

- Word conversions work in the Comments panel and contextual comment/reply
  bubbles. Comment text keeps its current formatting without automatic italics.
- Ctrl+Shift+Left navigation no longer waits for Word font-reference cleanup
  inside KeyJaya's keyboard hook.
- Word conversion avoids duplicate paste-UI preparation, application binding and
  revision scans. Already-correct italic/regular formatting avoids another write.
- Ctrl immediately after pasting retains selection checks and paste-popup
  recovery. Native formatting and conversion remain grouped for one-step undo.

## Ctrl-word italics

- With **Auto-italicize conversions** enabled, the first Ctrl tap italicizes an
  already-correct recognized term unchanged, including `bhakti`, `nitya`, `dvija`
  and plural forms such as `dvijas`.
- Individual words, including exactly one selected word, can italicize loanwords
  such as `dharma`, `guru`, `yoga` and `karma`, including their plurals.
- Multiword passages make those loanword exceptions regular even if they were
  already italic. Other eligible Sanskrit/Bengali terms become italic; proper
  names retain their regular-type policy and personal style rules remain supported.
- Complete Sanskrit compounds such as `bhakti-yoga`, `nitya-dharma` and
  `guru-sevā` are treated as one term, including in passages.
- Further Ctrl taps cycle spelling alternatives and eventually restore the
  original text and formatting. Terms without alternatives cycle between italic
  and their original formatting. English protection remains.
- Supported rich-text and Markdown paths apply these rules. Word comments,
  text-only Excel editing, plain-text fields and script output retain their
  existing formatting limits.

## Excel editing

- Word and letter conversion work on later lines of a multiline cell. Selected
  passages can span LF/CRLF line breaks, remain selected after conversion and
  retain one-step undo.
- Discovery uses fewer accessibility-provider reads. Relative selection starts
  from the nearest verified edge, reducing navigation in long cells.
- Emoji or combining accents elsewhere in a cell can remain untouched while a
  nearby word converts when the navigation path can avoid those characters.
- The Formula Bar must remain visible. English Edit/Enter mode labels, the
  4,096-character bound and formula-like-buffer exclusions still apply. Unverified
  cursor paths through complex character clusters can be skipped.

## Interface and configuration

- **Check Word conflicts…** is available inside **Customize shortcuts**. Its
  bounded, read-only helper reports assignments and navigation overlaps from
  already-open Word instances without changing Word or its documents.
- The standalone shortcut-overlap tray action and development spelling/style
  editor were removed as requested. Existing settings and spelling/style
  configuration remain supported; resident settings windows keep ordinary input.

## Review fixes — 27 October 2026

- Markdown conversion preserves literal identifiers such as `note_dharma_id`
  and validates emphasis boundaries before removing existing italics. Escaped
  markers and punctuation remain text rather than becoming stray formatting.
- Original Word formatting is captured for one-word selections with surrounding
  whitespace. An active Ctrl cycle retains its formatting evidence through a
  pause; Backspace keeps its separate immediate-undo deadline.
- Formatting-only conversions arm Backspace undo in supported rich editors and
  InDesign. Restoration uses the saved original italic state or native font
  snapshot instead of treating an unchanged spelling as an untouched word.
- URLs, email addresses, file paths and protected literal comments are excluded
  from both italic and regular-type spans. Eligible terms around them retain
  the individual-word versus passage rules.

## Verification

All 44 functional regression suites passed, followed by five focused suites for
the final boundary cases. The compiled 1.9.7 application, strict source/data audit,
developer-tool tests and engine benchmarks passed. The 38 packaged files and
embedded payload were verified by SHA-256.

Disposable installation tests covered a clean install, edited-trigger upgrade,
upgrade from the exact published 1.9.6 installer and complete failure rollback.
Existing configuration and state were preserved, and the upgraded converter
honored synthetic custom triggers, spellings, style rules and shortcuts. No live
installation or working document was used for these tests.

Earlier user-led scratch checks confirmed Word comments, Ctrl italics, faster
conversion, native undo and multiline Excel editing. Review-specific padded Word
and rich-editor Backspace cases have automated helper/model coverage. These checks
do not establish every editor or Office version. The installer is unsigned.
Details are in [Release verification](VERIFICATION.md) and
[Development verification](DEVELOPMENT_VERIFICATION.md); usage is in the
[User guide](USER_GUIDE.md).
