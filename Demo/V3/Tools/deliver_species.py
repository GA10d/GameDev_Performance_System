"""Publish a new sibling demo without overwriting either previous version."""
from pathlib import Path
import hashlib,shutil,json
root=Path(__file__).resolve().parents[1]
dest=Path(r'F:\Documents\GitHub\GameDev_Performance_System\Demo\V3')
assert dest.parent.resolve()==Path(r'F:\Documents\GitHub\GameDev_Performance_System\Demo').resolve()
assert not dest.exists(),'Destination exists: inspect before updating'
build=root/'QA/build-summary.txt';assert build.read_text().startswith('Succeeded')
for report in ['QA/core-verification.txt','QA/runtime/runtime-verification.txt','QA/species/checks.txt']:
    p=root/report;rows=p.read_text(encoding='utf-8-sig').splitlines();assert rows and all(r.startswith('PASS:') for r in rows),report
    if report!='QA/core-verification.txt':assert p.stat().st_mtime>build.stat().st_mtime,'QA predates build'
files=[]
for folder in ['Build','Docs','Source','Tools','UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings','QA']:
    for p in (root/folder).rglob('*'):
        if p.is_file() and '__pycache__' not in p.parts and p.suffix not in ['.blend1','.blend2']:files.append(p)
for name in ['README.md','THIRD_PARTY_NOTICES.md','Launch.cmd','Rebuild.ps1','Verify.ps1','VerifySpecies.ps1','异种扩展说明.html','.gitignore']:files.append(root/name)
def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1048576),b''):h.update(b)
    return h.hexdigest()
manifest=[]
for p in files:
    relative=p.relative_to(root);target=dest/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
    digest=sha(p);assert sha(target)==digest
    manifest.append({'path':relative.as_posix(),'sha256':digest,'bytes':p.stat().st_size})
(dest/'QA/delivery-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'destination':str(dest),'files':len(manifest),'bytes':sum(f['bytes'] for f in manifest),'hashes_verified':True},ensure_ascii=False))
