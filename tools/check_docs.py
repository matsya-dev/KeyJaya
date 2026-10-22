"""Check repository Markdown targets and documentation images without network access."""
from pathlib import Path
from urllib.parse import unquote, urlsplit
import re
import struct

root = Path(__file__).resolve().parent.parent
checks = 0
for path in root.rglob('*.md'):
    relative = path.relative_to(root)
    if relative.parts[0] in ('.git', 'out'):
        continue
    text = path.read_text(encoding='utf-8-sig')
    targets = re.findall(r'\]\(([^\s)]+)(?:\s+"[^"\n]*")?\)', text)
    targets += re.findall(r'(?:src|srcset)="([^"]+)"', text)
    for target in targets:
        parsed = urlsplit(target.strip('<>'))
        if parsed.scheme or parsed.netloc or not parsed.path:
            continue
        resolved = (path.parent / unquote(parsed.path)).resolve()
        assert resolved.is_relative_to(root), f'Nonportable link in {relative}: {target}'
        assert resolved.exists(), f'Broken link in {relative}: {target}'
        checks += 1

images = 0
for path in (root/'docs/images').glob('*'):
    if path.suffix not in ('.png','.gif'):
        continue
    raw = path.read_bytes()
    if path.suffix == '.png':
        assert raw[:8] == b'\x89PNG\r\n\x1a\n', 'Invalid PNG: '+path.name
        width, height = struct.unpack('>II', raw[16:24])
    else:
        assert raw.startswith((b'GIF87a',b'GIF89a')) and raw.endswith(b';'), 'Invalid GIF: '+path.name
        width, height = struct.unpack('<HH', raw[6:10])
    assert 0 < width <= 2000 and 0 < height <= 2000, 'Unexpected image dimensions: '+path.name
    images += 1
print(f'PASS {checks} local documentation links and {images} image headers; no network requests.')
