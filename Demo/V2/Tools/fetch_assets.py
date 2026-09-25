"""Download only public CC0 assets from the author's linked Google Drive."""
import json, pathlib, sys, hashlib
import requests
from bs4 import BeautifulSoup

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Source' / 'Downloads'
def folder(fid):
    url = 'https://drive.google.com/drive/folders/' + fid
    r = requests.get(url, timeout=60); r.raise_for_status()
    soup = BeautifulSoup(r.text, 'html.parser')
    items=[]
    for row in soup.select('tr[data-id]'):
        label=row.select_one('[data-tooltip]')
        items.append({'id':row['data-id'],'name':label.get('data-tooltip') if label else row.get_text(' ',strip=True)})
    print(json.dumps(items,indent=2,ensure_ascii=False))
    return items

def download(fid,name):
    url='https://drive.usercontent.google.com/download?id='+fid+'&export=download&confirm=t'
    r=requests.get(url,timeout=180);r.raise_for_status()
    if 'text/html' in r.headers.get('Content-Type',''):
        raise RuntimeError('HTML instead of asset: '+name)
    dest=OUT/name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(r.content)
    info={'name':name,'source':url,'sha256':hashlib.sha256(r.content).hexdigest(),'bytes':len(r.content)}
    print(json.dumps(info));return info

if __name__=='__main__':
    if sys.argv[1]=='list':folder(sys.argv[2])
    elif sys.argv[1]=='get':download(sys.argv[2],sys.argv[3])
