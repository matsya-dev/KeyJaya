# Licensing and data review

## Original code

Original KeyJaya code and original project documentation use the MIT license in
[`LICENSE`](../LICENSE), copyright (c) 2026 matsya-dev. This license does not replace
the licenses or rights in third-party dictionaries, scripture, translations,
book-derived resources, or other separately identified material.

## Runtime data

The checked-in runtime pack supports the complete current app without access to
the development computer. It contains both word-level data and scripture/translation
records, including `scripture.dat`/`.idx` and verse-normalization data.

Raw ebook files, exported NFO/RTF books, BVT style-guide documents, personal reading
libraries, corpus extraction reports and local paths are excluded.

On **22 October 2026**, the project owner, **matsya-dev**, explicitly confirmed
permission to distribute the included book texts and translations and authorized
publication of this repository and app. This records the owner's confirmation;
it is not a new license grant from a third-party rights holder. Book content and
book-derived resources are not relicensed under MIT. Anyone planning separate
reuse of that content should obtain the applicable terms from its rights holder.

Text from BBT publications is courtesy of The Bhaktivedanta Book Trust
International, Inc. (www.krishna.com). Used with permission, as confirmed by the
project owner. Other included works retain their respective rights and attribution.

## Included dictionary sources

The Sanskrit linguistic resource includes its license, original XML/DTD and the
readable modified table used by KeyJaya. The original Hindi dictionary files are
included in `third_party/hindi/`; their source URLs are pinned to a specific upstream
revision. Use `tools/fetch_hindi_sources.py --check` to verify them. English source
dictionaries and full notices are included.

## Application distribution

On **25 October 2026**, the project owner explicitly confirmed separate permissions
to distribute the Sanskrit Heritage, Hindi Aspell/Hunspell and English
SCOWL-derived data without their copyright/license notices. The owner requested
that those third-party notices be omitted from the installed application while
KeyJaya's own copyright and MIT notice remain.

This records the owner's confirmation; the upstream license documents have not
been altered. Original sources, notices and provenance stay in this development
repository. `assets/distribution-exclusions.txt` identifies source-only files
omitted by the build and package tools and retired, with a backup, during upgrades.
The original-code `LICENSE` is retained in the application and checked byte for
byte during verification. Conversion dictionaries and editable settings are
protected from removal by the exclusion-policy validator.

No Balarama font binaries are included; `defaults/font-encoding.tsv` contains font
names and encoding choices only. The app icon is the approved KeyJaya asset.

## Future releases

- Preserve the original-code license and copyright notice.
- Confirm that permission covers any newly added book content.
- Retain the full upstream dictionary records in development sources; apply the
  documented separate-permission policy to the application distribution.
- Review the staged Git changes and run the repository audit.
- Keep installers in GitHub Release assets rather than ordinary Git history.
