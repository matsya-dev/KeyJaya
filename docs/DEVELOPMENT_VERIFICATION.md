# Ctrl scripture insertion, ranges, and cleaner cycles

This is the development-stage record. The completed 1.9.5 release checks are
recorded in [Release verification](VERIFICATION.md).

Verified locally on 23 October 2026 on `codex/ctrl-verse-insertion`.
This section records logic-only feature verification before the subsequent
[Excel investigation](EXCEL_DEBUGGING.md). It is not a packaged release. No
installer was built. The Excel report separately records its isolated resident
and disposable-workbook tests.

## Automated results

- **43 isolated suites passed; 25,161,990 assertions; zero failures.** Results
  combine the full regression run with the new range suite and targeted reruns
  after the ACBSP and SSR updates. Logs are in `out/tests/combined-feature-verification.json`.
- Checked **36,684 adjacent verse ranges across BG, SB, CC, CB, ISO, BS, and NOI**
  in Unicode, Balarama, and plain Roman output.
- Checked 1,131 distributed formatting cases, including both forced verse italics
  and prose translation boundaries. Word span merging, rich-editor HTML, and
  Markdown output preserve the text and verse/translation order.
- Checked 4,011 shared-translation cases across the encodings. Adjacent verses
  sharing a translation include it once; nonadjacent repetitions stay in place.
- Bare references and ranges remain verse-only; `T`, `VT`, and `R` retain their
  explicit meanings. Ctrl, selected-reference conversion, and symbol endings
  produce consistent content. Punctuation, surrounding text, and abbreviation
  priority are covered.
- Synthetic data verifies missing first/middle/last records, translation-only
  records, the exact 200-verse limit, and the exact 200,000-character output limit.
  Missing or oversized passages return no partial insertion.
- Cleaner cycles retain validated alternatives and the original plain spelling,
  remove tested partial/case duplicates, and loop continuously. The original undo
  spelling survives 100 complete loops, including through the plain option.
  English protection and preferred-spelling tests also pass.
- ACBSP defaults now use `A.C. Bhaktivedanta Swami Prabhupāda`, with corresponding
  Balarama and plain forms. The upgrade correction preserves custom and exact
  replacements, disabled/deleted variants, comments, encoding, and unrelated YAML.
- All 30 runtime-data checksums and local documentation links were checked.
  Only the requested short-trigger TSV changed in the runtime data pack.
- SSR expands to `The Science of Self-Realization` with Ctrl or `>`, `<`, and `?`.
  Its independent upgrade marker allows installation after the earlier SBB
  addition, preserving custom/disabled SSR entries and subsequent deletions.

## Remaining release checks

These tests exercise conversion logic, caret targeting, formatting output,
ownership checks, and undo state. They do not prove physical input timing or an
editor's native undo stack. Before packaging, use disposable Word documents to
check Ctrl+Z after range insertion, Backspace after cleaned word cycles, original
font/style restoration, Track Changes, and queued typing. The examples are in
[Testing KeyJaya](TESTING.md#physical-release-checks).

The accepted source baseline and existing release verification remain for v1.9.4;
they have not been refreshed to label these unbuilt changes as a release.
