"""Copy verified V2 into its own new folder. Never overwrites V1 or Project-Astra."""
import pathlib,shutil,hashlib,json
root=pathlib.Path(__file__).resolve().parents[1]
target=pathlib.Path(r'F:\Documents\GitHub\GameDev_Performance_System\V2_Quaternius')
assert target.parent.resolve()==pathlib.Path(r'F:\Documents\GitHub\GameDev_Performance_System').resolve()
checks=(root/'QA/runtime-release/runtime-verification.txt').read_text(encoding='utf-8-sig').splitlines()
assert (root/'QA/runtime-release/runtime-verification.txt').stat().st_mtime >= (root/'QA/build-summary.txt').stat().st_mtime,'Report predates final build'
assert len(checks)>=114 and all(x.startswith('PASS:') for x in checks),'Runtime checks incomplete or failed'
assert (root/'QA/build-summary.txt').read_text().startswith('Succeeded'),'Build is not successful'
if target.exists() and any(target.iterdir()):raise RuntimeError('Target already contains files; review before updating')
files=[]
for name in ['Build','Docs','Source','Tools','UnityProject/Assets','UnityProject/ProjectSettings','UnityProject/Packages']:
    for p in (root/name).rglob('*'):
        if not p.is_file() or p.suffix=='.blend1' or '__pycache__' in p.parts:continue
        if 'Downloads' in p.parts and p.suffix in ['.html','.js']:continue
        files.append((p,p.relative_to(root)))
for name in ['README.md','THIRD_PARTY_NOTICES.md','Launch.cmd','Rebuild.ps1','Verify.ps1','.gitignore','新版说明.html']:
    files.append((root/name,pathlib.Path(name)))
for name in ['import-report.txt','build-summary.txt','original-blend.json','unity-build.log','rebuild.log']:
    files.append((root/'QA'/name,pathlib.Path('QA')/name))
for p in (root/'QA/runtime-release').iterdir():
    if p.is_file():files.append((p,pathlib.Path('QA/runtime')/p.name))
files.append((root/'QA/player-release.log',pathlib.Path('QA/runtime/player.log')))
def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()
manifest=[]
for source,rel in files:
    dest=target/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
    digest=sha(source);assert sha(dest)==digest,str(rel)
    manifest.append({'path':rel.as_posix(),'bytes':dest.stat().st_size,'sha256':digest})
(target/'QA/delivery-manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'target':str(target),'files':len(manifest),'bytes':sum(x['bytes'] for x in manifest),'runtime_checks':len(checks),'all_hashes_match':True}))
