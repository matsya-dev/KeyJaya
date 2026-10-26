# Third-party resources

This is a development/source provenance record. It is not included in the
installed application under the owner's separate-permission confirmation of
25 October 2026, recorded in `docs/LICENSING.md`. KeyJaya's own MIT notice remains
included. Upstream source records below are retained verbatim in the repository.

Original KeyJaya code uses the MIT license in `LICENSE`. These notices apply to
the named third-party resources, which retain their own rights and terms.

Source code and corresponding dictionary sources for this release are available at
https://github.com/matsya-dev/KeyJaya/tree/v1.9.4.

| Resource | Included notices/source | Provenance |
| --- | --- | --- |
| Sanskrit Heritage morphology, version 3.84 | `data/sanskrit-morphology-NOTICE.txt`, `data/sanskrit-morphology-LGPLLR.pdf`, modified readable `data/sanskrit-morphology-source.tsv.gz`, original XML/DTD in `third_party/sanskrit-heritage/` | Gerard Huet, Sanskrit Heritage; source URL and counts in `data/sanskrit-morphology-manifest.json` |
| Hindi Aspell/Hunspell word list | `data/hindi-COPYING`, `data/hindi-Copyright`, `data/hindi-README_hi_IN.txt`; original files in `third_party/hindi/`, pinned URLs/hashes in `data/hindi-source-manifest.json` | LibreOffice dictionaries mirror; original Hindi word-list attribution in the included notices |
| English Hunspell/SCOWL-derived word lists | `data/english-dictionary-notices.txt`; original `en_US.dic`/`en_US.aff` in `third_party/scowl/` | Attribution and component terms are preserved in the complete notice file |
| BBT editorial name/style data | Source URL in `data/bbt-style-source.json` | Derived style/name metadata; this file does not grant a license to the guide or books |
| Scripture, translations and book-derived vocabulary | Runtime data pack; see `docs/LICENSING.md` | The project owner confirmed permission to distribute the included book texts and translations on 22 October 2026; their rights are not replaced by MIT |

Keep the upstream notices and corresponding sources in the development record.
The application exclusion policy relies on the owner's separate permissions;
it does not change the published upstream terms or relicense those resources.
The included Sanskrit linguistic resource uses LGPLLR; the Hindi word-list notice
specifies GPL v2 or later. The English notice includes multiple component notices.

`tools/fetch_hindi_sources.py --check` verifies the included original Hindi source
files. Without `--check` it can restore missing files using the pinned URLs and
hashes. It is separate from building.
