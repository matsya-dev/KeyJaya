"""Create a payload ZIP from an already compiled, clean application tree.

Build-time tool only: no resident, installed-profile access, or Windows integration.
Temporary/user files are excluded even if accidentally present in the staging tree.
"""

import argparse
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parent
EXCLUDED_DIRECTORIES = frozenset({"logs", "state", "backups"})
EXCLUDED_FILES = frozenset(
    {
        "personal_triggers.yml",
        "personal-triggers.yml",
        "english-guard.txt",
        "hindi-build-counts.txt",
    }
)


def distribution_exclusions(root: Path = ROOT) -> frozenset[str]:
    """Read the same source-only path list used by build and upgrade cleanup."""
    excluded = set()
    critical = {".dat", ".idx", ".hash", ".tsv", ".ini", ".exe", ".dll", ".ico"}
    for raw in (
        (root / "assets/distribution-exclusions.txt")
        .read_text(encoding="utf-8-sig")
        .splitlines()
    ):
        value = raw.strip()
        if not value or value.startswith("#"):
            continue
        parts = value.split("/")
        allowed = value.casefold() == "third_party_notices.md" or (
            len(parts) == 2
            and parts[0].casefold() in {"data", "docs"}
            and parts[1] not in {"", ".", ".."}
            and not parts[1].endswith(".")
        )
        if (
            not allowed
            or any(char in value for char in "\\:*?~\0")
            or any(ord(char) < 32 for char in value)
            or Path(value).suffix.casefold() in critical
            or value.casefold() in excluded
        ):
            raise ValueError("Invalid or duplicate distribution exclusion: " + value)
        excluded.add(value.casefold())
    return frozenset(excluded)


def included(relative: Path, source_only: frozenset[str] = frozenset()) -> bool:
    """Check every path component, so nested backups cannot enter the payload."""
    if any(part in EXCLUDED_DIRECTORIES for part in relative.parts):
        return False
    if any(
        "backup-" in part
        or ".before-" in part
        or ".tmp-" in part
        or part.endswith((".bak", ".tmp"))
        for part in relative.parts
    ):
        return False
    return (
        relative.name not in EXCLUDED_FILES
        and relative.as_posix().casefold() not in source_only
    )


def create_payload(release: Path, output: Path, root: Path = ROOT) -> None:
    """Package sorted inputs only when their version matches the source engine."""
    release = release.resolve()
    source = (root / "src/Core.cs").read_text(encoding="utf-8-sig")
    match = re.search(r'Current\s*=\s*"([^"]+)"', source)
    if match is None:
        raise ValueError("Current source version was not found")
    if (release / "VERSION").read_text().strip() != match.group(1):
        raise ValueError("Payload version must match current source")
    source_only = distribution_exclusions(root)

    # These settings make archive construction predictable, but the Framework
    # compiler still embeds build-specific metadata in the executables.
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in sorted(release.rglob("*")):
            relative = path.relative_to(release)
            if path.is_file() and included(relative, source_only):
                archive.write(path, relative.as_posix())


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--release", type=Path, default=ROOT / "release")
    parser.add_argument("--output", type=Path, default=ROOT / "payload.zip")
    args = parser.parse_args(argv)
    create_payload(args.release, args.output)
    print(
        "Prepared release payload; no logs, personal state or retired trigger backups included."
    )


if __name__ == "__main__":
    main()
