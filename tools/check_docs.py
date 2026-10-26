"""Validate local Markdown links and image headers without network or UI access.

Header checks catch broken assets; they do not replace visual layout inspection.
Importing this module is safe for tests and other developer tools.
"""

import argparse
from pathlib import Path
import re
import struct
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parent.parent


def check_markdown_links(root: Path) -> int:
    """Reject broken or escaping relative links; leave external URLs untouched."""
    root = root.resolve()
    checked = 0
    for path in root.rglob("*.md"):
        relative = path.relative_to(root)
        if relative.parts[0] in (".git", "out"):
            continue
        text = path.read_text(encoding="utf-8-sig")
        targets = re.findall(r'\]\(([^\s)]+)(?:\s+"[^"\n]*")?\)', text)
        targets += re.findall(r'(?:src|srcset)="([^"]+)"', text)
        for target in targets:
            parsed = urlsplit(target.strip("<>"))
            if parsed.scheme or parsed.netloc or not parsed.path:
                continue
            resolved = (path.parent / unquote(parsed.path)).resolve()
            if not resolved.is_relative_to(root):
                raise ValueError(f"Nonportable link in {relative}: {target}")
            if not resolved.exists():
                raise ValueError(f"Broken link in {relative}: {target}")
            checked += 1
    return checked


def check_image_headers(root: Path) -> int:
    """Check PNG/GIF signatures and plausible dimensions without decoding pixels."""
    checked = 0
    for path in (root / "docs/images").glob("*"):
        if path.suffix not in (".png", ".gif"):
            continue
        raw = path.read_bytes()
        if path.suffix == ".png":
            if len(raw) < 24 or raw[:8] != b"\x89PNG\r\n\x1a\n":
                raise ValueError("Invalid PNG: " + path.name)
            width, height = struct.unpack(">II", raw[16:24])
        else:
            if (
                len(raw) < 10
                or not raw.startswith((b"GIF87a", b"GIF89a"))
                or not raw.endswith(b";")
            ):
                raise ValueError("Invalid GIF: " + path.name)
            width, height = struct.unpack("<HH", raw[6:10])
        if not (0 < width <= 2000 and 0 < height <= 2000):
            raise ValueError("Unexpected image dimensions: " + path.name)
        checked += 1
    return checked


def main(argv: list[str] | None = None) -> None:
    argparse.ArgumentParser(description=__doc__).parse_args(argv)
    links = check_markdown_links(ROOT)
    images = check_image_headers(ROOT)
    print(
        f"PASS {links} local documentation links and {images} image headers; no network requests."
    )


if __name__ == "__main__":
    main()
