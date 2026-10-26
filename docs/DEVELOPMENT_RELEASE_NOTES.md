# Unreleased development notes

These changes follow released KeyJaya **1.9.6**. They are saved as a local
development checkpoint; the application version and accepted release baseline
remain 1.9.6. No installer or published release has been prepared for this work.

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

## Verification

The complete 44-suite regression run passed, followed by focused checks for the
final formatting/performance changes and paste-popup safeguard. The compiled
application, source/data integrity, documentation and formatting checks passed.
The owner confirmed Word comment behavior, Ctrl italics, faster word/passage
conversion, native one-step undo and conversion immediately after pasting in
scratch content. Multiline Excel conversion, selected passages, retained
selection and one-step undo were also confirmed in disposable cells.

Activation preserved all 47 checked profile files and startup registration.
These checks cover the tested adapters and interactions rather than every editor
or Office version. Details and test boundaries are in
[Development verification](DEVELOPMENT_VERIFICATION.md); current usage is in the
[User guide](USER_GUIDE.md).
