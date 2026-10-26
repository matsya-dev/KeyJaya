"""Audit the complete source tree and immutable runtime data without changing either.

The release gate checks accepted source hashes. Development mode only reports
intentional source drift; inventory, licensing, data, and private-input checks
remain mandatory in both modes. Importing the module performs no audit.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent


def sha(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def text_sha(path):
    return hashlib.sha256(path.read_bytes().replace(b"\r\n", b"\n")).hexdigest()


def run_audit(root: Path, development: bool = False) -> int:
    """Run repository checks with a local counter; never refresh the release baseline."""
    root = root.resolve()
    checks = 0

    def check(value, label):
        nonlocal checks
        if not value:
            raise SystemExit("FAIL: " + label)
        checks += 1

    version = (root / "VERSION").read_text(encoding="utf-8-sig").strip()
    license_text = (root / "LICENSE").read_text(encoding="utf-8-sig")
    check(license_text.startswith("MIT License\n"), "Original code license retained")
    check(
        "Copyright (c) 2026 matsya-dev" in license_text,
        "Original copyright notice retained",
    )
    check(
        version
        == re.search(
            r'Current\s*=\s*"([^"]+)"',
            (root / "src/Core.cs").read_text(encoding="utf-8-sig"),
        ).group(1),
        "Version matches source",
    )
    baseline = json.loads(
        (root / "docs/SOURCE_BASELINE.json").read_text(encoding="utf-8-sig")
    )
    check(baseline["version"] == version, "Accepted source baseline version")
    assembly = (root / "src/AssemblyInfo.cs").read_text(encoding="utf-8-sig")
    for attribute in ("AssemblyVersion", "AssemblyFileVersion"):
        check(
            f'{attribute}("{version}.0")' in assembly, "Assembly version: " + attribute
        )
    for name in ("app.manifest", "runtime.manifest"):
        check(
            f'assemblyIdentity version="{version}.0"'
            in (root / "src" / name).read_text(encoding="utf-8-sig"),
            "Manifest version: " + name,
        )
    expected_sources = baseline["production_source_normalized_sha256"]
    actual_sources = {
        path.relative_to(root).as_posix()
        for path in (root / "src").rglob("*.cs")
        if path.is_file()
    }
    added_sources = actual_sources - set(expected_sources)
    removed_sources = set(expected_sources) - actual_sources
    if not development:
        check(
            not added_sources and not removed_sources,
            "Complete production source inventory",
        )
    drift = []
    for name, expected in expected_sources.items():
        if not (root / name).is_file() or text_sha(root / name) != expected:
            drift.append(name)
        if not development:
            check(
                text_sha(root / name) == expected, "Production source baseline: " + name
            )
    manifest = json.loads((root / "data/manifest.json").read_text(encoding="utf-8-sig"))
    actual = {
        "data/" + p.name
        for p in (root / "data").iterdir()
        if p.is_file() and p.name not in ["manifest.json", "README.md"]
    }
    check(actual == set(manifest["files"]), "Complete runtime data inventory")
    for name, expected in manifest["files"].items():
        check(sha(root / name) == expected, "Runtime data integrity: " + name)
    check(
        "email"
        not in (root / "assets/short-trigger-defaults.yml")
        .read_text(encoding="utf-8-sig")
        .lower(),
        "No personal email trigger in authoritative defaults",
    )
    for name in (
        "defaults/short-triggers.yml",
        "assets/short-trigger-additions.yml",
        "data/short-triggers.tsv",
    ):
        check(
            not (root / name).exists(), "No separately maintained trigger copy: " + name
        )
    for name in (
        "overrides.tsv",
        "diacritics-overrides.tsv",
        "word-spellings.tsv",
        "script-spellings.tsv",
        "script-phrases.tsv",
        "italics-exceptions.tsv",
        "conversion-rules.tsv",
    ):
        check(
            not (root / "defaults" / name).exists(),
            "No empty legacy customization template: " + name,
        )
    check(
        (root / "defaults/customizations.ini").exists(),
        "Unified customization template included",
    )
    required_settings = {
        "start_with_windows",
        "autocorrect_spelling",
        "auto_contractions",
        "encoding_override",
        "italicize_conversions",
        "script_language",
        "script_precise",
        "preserve_verse_markers",
    }
    settings = {
        line.split("=", 1)[0]
        for line in (root / "defaults/settings.ini")
        .read_text(encoding="utf-8-sig")
        .splitlines()
        if line and not line.startswith("#")
    }
    check(settings == required_settings, "Current default settings only")
    allowed = {
        "src",
        "assets",
        "data",
        "defaults",
        "tests",
        "third_party",
        "tools",
        "docs",
        ".github",
    }
    denied = {
        ".exe",
        ".dll",
        ".pdb",
        ".lnk",
        ".log",
        ".tmp",
        ".bak",
        ".epub",
        ".mobi",
        ".azw",
        ".azw3",
        ".kfx",
        ".nfo",
        ".rtf",
        ".doc",
        ".docx",
        ".asd",
        ".zip",
    }
    home_user = os.environ.get("USERNAME", "").lower().encode()
    for path in root.rglob("*"):
        relative = path.relative_to(root)
        if (
            relative.parts[0] in [".git", "out", "__pycache__"]
            or "__pycache__" in relative.parts
        ):
            continue
        if path.is_dir():
            check(
                not path.is_symlink(), "No linked source directories: " + str(relative)
            )
            continue
        check(
            path.suffix.lower() not in denied,
            "No generated/private artifact: " + str(relative),
        )
        if len(relative.parts) > 1:
            check(
                relative.parts[0] in allowed,
                "Approved source directory: " + str(relative),
            )
        check(
            path.stat().st_size < 50 * 1024 * 1024,
            "File size under 50 MiB: " + str(relative),
        )
        raw = path.read_bytes()
        normalized = raw.lower().replace(b"\\\\", b"\\")
        if home_user:
            check(
                b"c:\\users\\" + home_user + b"\\" not in normalized,
                "No local home path: " + str(relative),
            )
            check(
                b"c:/users/" + home_user + b"/" not in normalized,
                "No local home path: " + str(relative),
            )
        # License files intentionally retain third-party contact attribution. User
        # configuration, scripts and original sources must not contain personal mail.
        if relative.parts[0] in ["defaults", "assets", "src"]:
            check(
                not re.search(rb"[\w.+-]+@(?:gmail|outlook|yahoo)\.com", raw, re.I),
                "No personal email: " + str(relative),
            )
        check(
            not re.search(rb"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", raw),
            "No private key: " + str(relative),
        )
        check(
            not re.search(rb"gh[pousr]_[A-Za-z0-9]{30,}", raw),
            "No GitHub token: " + str(relative),
        )
    check(
        (root / "data/sanskrit-morphology-LGPLLR.pdf").exists(),
        "Sanskrit license retained",
    )
    check((root / "data/hindi-COPYING").exists(), "Hindi license retained")
    check(
        (root / "data/english-dictionary-notices.txt").exists(),
        "English notices retained",
    )
    check(
        len(json.loads((root / "tests/suites.json").read_text(encoding="utf-8-sig")))
        == 44,
        "Isolated test registry complete",
    )
    if development:
        print(
            f'Development source drift from accepted {baseline["version"]}: {len(drift)} changed/missing, {len(added_sources)} added, {len(removed_sources)} removed; release baseline unchanged.'
        )
    print(
        f"PASS {checks} repository checks; data integrity verified; generated outputs, personal paths and email expansions excluded."
    )

    return checks


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--development",
        action="store_true",
        help="Report intentional source drift without refreshing the accepted release baseline.",
    )
    args = parser.parse_args(argv)
    run_audit(ROOT, development=args.development)


if __name__ == "__main__":
    main()
