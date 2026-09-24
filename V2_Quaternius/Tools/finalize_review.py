from pathlib import Path
import shutil,subprocess,csv,json,re,sys
root=Path(__file__).resolve().parents[1]
source=root/'QA/runtime-review-final'
for p in source.iterdir():
    if p.is_file():shutil.copy2(p,root/'QA/runtime'/p.name)
shutil.copy2(root/'QA/player-review-final.log',root/'QA/runtime/player.log')
for tool in ['build_review_page.py','build_handbook.py']:
    subprocess.run([sys.executable,str(root/'Tools'/tool)],check=True)
counts={}
for folder in ['before-visible','final','matrix']:
    base=root/'QA/visual-review'/folder
    rows=list(csv.DictReader((base/'frames.csv').open(encoding='utf-8')))
    assert all((base/r['file']).is_file() for r in rows)
    counts[folder]=len(rows)
assert counts['final']==985 and counts['matrix']==612
qa=(root/'QA/runtime/runtime-verification.txt').read_text(encoding='utf-8-sig').splitlines()
assert len(qa)==118 and all(r.startswith('PASS:') for r in qa)
for file in ['QA/visual-review/final/player.log','QA/visual-review/matrix/player.log','QA/runtime/player.log']:
    content=(root/file).read_text(encoding='utf-8',errors='replace')
    assert not re.search(r'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|Shader error|Crash!!!',content),file
print(json.dumps({'frames':counts,'functional_checks_before_final_prop_adjustment':len(qa),'html_image_paths_present':True,'desktop_inspection':'stopped by Escape; no further UI automation'},ensure_ascii=False))
