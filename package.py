from pathlib import Path
import zipfile,argparse,re
ROOT=Path(__file__).resolve().parent
def included(relative):
    return not any(part in ('logs','state','backups') for part in relative.parts) and not any('backup-' in part or '.before-' in part or '.tmp-' in part or part.endswith(('.bak','.tmp')) for part in relative.parts) and relative.name not in ('personal_triggers.yml','personal-triggers.yml','english-guard.txt','hindi-build-counts.txt')
def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--release',type=Path,default=ROOT/'release')
    parser.add_argument('--output',type=Path,default=ROOT/'payload.zip')
    args=parser.parse_args()
    release=args.release.resolve()
    version=re.search(r'Current\s*=\s*"([^"]+)"',(ROOT/'src/Core.cs').read_text(encoding='utf-8-sig')).group(1)
    assert (release/'VERSION').read_text().strip()==version,'Payload version must match current source'
    with zipfile.ZipFile(args.output,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
        for path in sorted(release.rglob('*')):
            relative=path.relative_to(release)
            if path.is_file() and included(relative):archive.write(path,relative.as_posix())
    print('Prepared release payload; no logs, personal state or retired trigger backups included.')
if __name__=='__main__':main()
