"""Retrieve the pinned original Hindi dictionary sources, with hash verification."""
from pathlib import Path
import argparse, hashlib, json, urllib.request

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--check', action='store_true', help='Verify local sources without network access')
args = parser.parse_args()
manifest = json.loads((root/'data/hindi-source-manifest.json').read_text(encoding='utf-8-sig'))
destination = root/'third_party/hindi'
destination.mkdir(parents=True, exist_ok=True)
for item in manifest['files']:
    name = item['name']
    assert Path(name).name == name, 'Only plain source filenames are accepted'
    expected = f"https://raw.githubusercontent.com/LibreOffice/dictionaries/{manifest['commit']}/hi_IN/{name}"
    assert item['url'] == expected, 'Unexpected source URL'
    target = destination/name
    if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest() == item['sha256']:
        print('Verified', name)
        continue
    if args.check:
        raise SystemExit('Missing or changed Hindi source: '+name)
    request = urllib.request.Request(expected, headers={'User-Agent':'KeyJaya-source-preparation'})
    with urllib.request.urlopen(request, timeout=45) as response:
        data = response.read(16*1024*1024+1)
    assert len(data) == item['bytes'], 'Unexpected source length: '+name
    assert hashlib.sha256(data).hexdigest() == item['sha256'], 'Source hash mismatch: '+name
    temporary = target.with_name(target.name+'.download-tmp')
    temporary.write_bytes(data)
    temporary.replace(target)
    print('Fetched and verified', name)
