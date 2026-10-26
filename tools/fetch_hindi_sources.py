"""Verify or retrieve the pinned upstream Hindi dictionary sources.

Only this explicit developer command may fetch these files. The resident never
downloads dictionaries. Check mode performs no network requests or writes.
"""

import argparse
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parent.parent
MAX_SOURCE_BYTES = 16 * 1024 * 1024


def ensure_sources(root: Path, check_only: bool = False) -> int:
    """Accept only pinned filenames/URLs and verify bytes before atomic replacement."""
    manifest = json.loads(
        (root / "data/hindi-source-manifest.json").read_text(encoding="utf-8-sig")
    )
    destination = root / "third_party/hindi"
    checked = 0
    for item in manifest["files"]:
        name = item["name"]
        if not name or Path(name).name != name:
            raise ValueError("Only plain source filenames are accepted")
        expected = (
            "https://raw.githubusercontent.com/LibreOffice/dictionaries/"
            f"{manifest['commit']}/hi_IN/{name}"
        )
        if item["url"] != expected:
            raise ValueError("Unexpected source URL")
        target = destination / name
        if (
            target.exists()
            and hashlib.sha256(target.read_bytes()).hexdigest() == item["sha256"]
        ):
            print("Verified", name)
            checked += 1
            continue
        if check_only:
            raise SystemExit("Missing or changed Hindi source: " + name)

        request = urllib.request.Request(
            expected, headers={"User-Agent": "KeyJaya-source-preparation"}
        )
        with urllib.request.urlopen(request, timeout=45) as response:
            data = response.read(MAX_SOURCE_BYTES + 1)
        if len(data) != item["bytes"]:
            raise ValueError("Unexpected source length: " + name)
        if hashlib.sha256(data).hexdigest() != item["sha256"]:
            raise ValueError("Source hash mismatch: " + name)
        destination.mkdir(parents=True, exist_ok=True)
        temporary = target.with_name(target.name + ".download-tmp")
        temporary.write_bytes(data)
        temporary.replace(target)
        print("Fetched and verified", name)
        checked += 1
    return checked


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify local sources without network access",
    )
    args = parser.parse_args(argv)
    ensure_sources(ROOT, check_only=args.check)


if __name__ == "__main__":
    main()
