"""Check package bytes and exercise installation only inside ignored out/fixtures."""
from pathlib import Path
import hashlib, json, subprocess, zipfile

root = Path(__file__).resolve().parent.parent
app = root/'out/app'
version = (root/'VERSION').read_text().strip()
installer = root/f'out/KeyJaya-Setup-v{version}.exe'
files = {p.relative_to(app).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()
         for p in app.rglob('*') if p.is_file()}
with zipfile.ZipFile(root/'out/payload.zip') as archive:
    assert set(archive.namelist()) == set(files), 'Package inventory mismatch'
    for name, sha in files.items():
        assert hashlib.sha256(archive.read(name)).hexdigest() == sha, name
assert {name for name in files if name.startswith('engine/')} == {
    'engine/keyjaya-app.exe','engine/keyjaya-app.exe.config'}
for name in ('LICENSE','THIRD_PARTY_NOTICES.md','docs/LICENSING.md'):
    assert (app/name).read_bytes() == (root/name).read_bytes(), 'Missing or changed notice: '+name
for name in files:
    assert not name.startswith(('logs/','state/','backups/')), name
target = root/'out/verification/install'
assert not target.exists(), 'Preserve an existing install fixture; choose a fresh verification directory.'
target.parent.mkdir(parents=True,exist_ok=True)
assert target.resolve().is_relative_to((root/'out').resolve())
def install():
    result = subprocess.run([str(installer),'--test-install',str(target)],capture_output=True,text=True,timeout=90)
    assert result.returncode == 0, result.stdout+result.stderr
install()
assert (target/'VERSION').read_text().strip() == version
assert (target/'engine/keyjaya-app.exe.config').read_bytes() == (app/'engine/keyjaya-app.exe.config').read_bytes()
for name in ('LICENSE','THIRD_PARTY_NOTICES.md','docs/LICENSING.md'):
    assert (target/name).read_bytes() == (root/name).read_bytes(), 'Installed notice mismatch: '+name
yaml = target/'config/short-triggers.yml'
yaml.write_text(yaml.read_text(encoding='utf-8-sig')+"\n  - trigger: 'LOCALVERIFY'\n    replace: 'Keep this fixture edit'\n",encoding='utf-8')
before = yaml.read_bytes()
install()
assert yaml.read_bytes() == before, 'Upgrade changed user triggers'
summary = {'version':version,'package_files':len(files),'fresh_install':True,
           'upgrade_preserves_trigger_edit':True,'live_installation':False,'failures':0}
(root/'out/verification/build-results.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(f'PASS {len(files)} packaged file hashes; isolated clean install and edited-trigger upgrade; live app untouched.')
