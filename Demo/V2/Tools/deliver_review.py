"""Deliver the reviewed revision; refuse to overwrite changes since the initial snapshot."""
from pathlib import Path
import hashlib, json, shutil, zipfile

root=Path(__file__).resolve().parents[1]
target=Path(r'F:\Documents\GitHub\GameDev_Performance_System\Demo\V2')
assert target.resolve().parent==Path(r'F:\Documents\GitHub\GameDev_Performance_System\Demo').resolve()
baseline=json.loads((root/'QA/visual-review/delivery-before-hashes.json').read_text(encoding='utf-8'))
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1048576),b''):h.update(block)
    return h.hexdigest()

changed=[p for p,digest in baseline.items() if not (target/p).is_file() or sha(target/p)!=digest]
assert not changed,'Delivery changed since review began; inspect before overwriting: '+str(changed)
checks=(root/'QA/runtime-review-final/runtime-verification.txt').read_text(encoding='utf-8-sig').splitlines()
build=root/'QA/build-summary.txt'
assert build.read_text().startswith('Succeeded')
assert len(checks)==118 and all(line.startswith('PASS:') for line in checks)
# Full functional regression preceded the final prop-only geometry adjustment.
# That boundary is explicit in Docs/03. Both visual captures below must be fresh.
for name,count in [('final',985),('matrix',612)]:
    complete=root/'QA/visual-review'/name/'complete.txt'
    assert complete.stat().st_mtime>=build.stat().st_mtime
    assert complete.read_text().startswith(str(count))

files={}
def tree(folder):
    for source in (root/folder).rglob('*'):
        if not source.is_file() or source.suffix=='.blend1' or '__pycache__' in source.parts:continue
        if 'Downloads' in source.parts and source.suffix in ('.html','.js'):continue
        files[source.relative_to(root)]=source
for folder in ['Build','Docs','Source','Tools','UnityProject/Assets','UnityProject/ProjectSettings','UnityProject/Packages',
               'QA/visual-review/before-visible','QA/visual-review/final','QA/visual-review/matrix']:
    tree(folder)
for name in ['README.md','THIRD_PARTY_NOTICES.md','Launch.cmd','Rebuild.ps1','Verify.ps1','ReviewFrames.ps1','.gitignore','新版说明.html','画面复查.html',
             'QA/import-report.txt','QA/build-summary.txt','QA/original-blend.json','QA/unity-build.log','QA/rebuild.log',
             'QA/visual-review/final.log','QA/visual-review/matrix.log']:
    p=root/name
    if p.exists():files[Path(name)]=p
for p in (root/'QA/runtime-review-final').iterdir():
    if p.is_file():files[Path('QA/runtime')/p.name]=p
files[Path('QA/runtime/player.log')]=root/'QA/player-review-final.log'

for rel in files:
    dest=target/rel
    assert not dest.exists() or str(rel) in baseline,'Unexpected destination file: '+str(rel)
backup=root/'QA/visual-review/V2_before_visual_review.zip'
with zipfile.ZipFile(backup,'w',zipfile.ZIP_DEFLATED) as z:
    for rel,src in files.items():
        old=target/rel
        if old.is_file() and sha(old)!=sha(src):z.write(old,rel)
    old=target/'QA/delivery-manifest.json'
    if old.exists():z.write(old,'QA/delivery-manifest.json')

manifest=[]
for rel,source in files.items():
    dest=target/rel;dest.parent.mkdir(parents=True,exist_ok=True)
    digest=sha(source)
    if not dest.exists() or sha(dest)!=digest:shutil.copy2(source,dest)
    assert sha(dest)==digest,str(rel)
    manifest.append({'path':rel.as_posix(),'bytes':dest.stat().st_size,'sha256':digest})
archive=target/'QA/backups'/backup.name;archive.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup,archive)
assert sha(archive)==sha(backup)
(target/'QA/delivery-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'target':str(target),'files':len(manifest),'bytes':sum(r['bytes'] for r in manifest),'all_hashes_match':True,'backup':str(archive)},ensure_ascii=False))
