# Default configuration

These are application defaults, not a copy of a user's settings directory.
Use **Edit triggers** in the installed app to customize abbreviations. The editor
supports generated encoding forms, overrides, exact replacements and disabling.
Manage shortcuts and other options from the tray menu.

Advanced spelling, script, italics and replacement preferences are grouped in
`customizations.ini`. Font mappings remain in `font-encoding.tsv`; shortcut keys
remain in `shortcuts.ini`. Header comments explain each format. Existing legacy
TSV customization files remain supported and are not rewritten on loading.

Fresh editable triggers are copied from `assets/short-trigger-defaults.yml` by
the build and test scripts. That single factory list also supplies restoration,
upgrade additions and fallback lookup. It is never overlaid onto an existing
editable trigger file, so deleted or disabled entries stay that way.

Runtime preferences and backups belong to the installed app, not this source directory.
The internal script_language=sanskrit setting routes Ctrl+backslash to ITRANS;
script_language=hindi routes it to Hindi. The other script shortcuts remain active.
