from pathlib import Path
import hashlib, json, zipfile
root = Path(__file__).resolve().parents[1]
delivery = Path(r'F:\Documents\GitHub\GameDev_Performance_System\Demo\V2')
out = root/'QA/visual-review'; out.mkdir(parents=True, exist_ok=True)
hashes = {}
with zipfile.ZipFile(out/'before-source.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for sub in ['UnityProject/Assets', 'Tools', 'Docs']:
        for f in (root/sub).rglob('*'):
            if f.is_file(): archive.write(f, f.relative_to(root))
for f in delivery.rglob('*'):
    if f.is_file() and 'Library' not in f.parts:
        hashes[str(f.relative_to(delivery))] = hashlib.sha256(f.read_bytes()).hexdigest()
(out/'delivery-before-hashes.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')
print('Backed up source and recorded', len(hashes), 'delivery file hashes')
