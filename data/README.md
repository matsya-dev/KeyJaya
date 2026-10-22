# Runtime data pack

These files are the runtime inputs of KeyJaya 1.9.4. The personal email expansion
was removed from `short-triggers.tsv`; the other runtime data is unchanged.
`manifest.json` records byte hashes. `.gitattributes` disables text transformations
for the pack because indexes use byte offsets and notice hashes must remain stable.

The build copies the runtime inputs into `out/app/data`; it does not rebuild them
from the developer's ebook library. Do not edit `.dat` and `.idx` pairs independently.

Scripture and verse-normalization records contain book-derived text. The project
owner confirmed permission to distribute the included texts and translations;
see `docs/LICENSING.md`. Their rights are not replaced by the code's MIT license.
This folder also includes the full third-party dictionary notices and the modified readable
Sanskrit morphology source. Corresponding upstream sources are in `third_party/`
or pinned in the source manifests.
