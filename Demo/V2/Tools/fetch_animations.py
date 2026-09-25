import pathlib,json,requests,hashlib,zipfile
from bs4 import BeautifulSoup
out=pathlib.Path(__file__).resolve().parents[1]/'Source'/'Downloads'
s=requests.Session(); base='https://quaternius.itch.io/universal-animation-library'
r=s.get(base,timeout=60);r.raise_for_status()
csrf=BeautifulSoup(r.text,'html.parser').select_one('meta[name="csrf_token"]')['value']
r=s.post(base+'/download_url',data={'csrf_token':csrf},headers={'Referer':base,'X-Requested-With':'XMLHttpRequest'},timeout=60);r.raise_for_status()
url=r.json()['url'];r=s.get(url,timeout=60);r.raise_for_status()
(out/'animation-download.html').write_text(r.text,encoding='utf-8')
soup=BeautifulSoup(r.text,'html.parser')
csrf=soup.select_one('meta[name="csrf_token"]')['value']
a=soup.select_one('a[data-upload_id]')
r=s.post(base+'/file/'+a['data-upload_id'],data={'csrf_token':csrf},headers={'Referer':url},timeout=60);r.raise_for_status()
r=s.get(r.json()['url'],timeout=180);r.raise_for_status()
dest=out/'UniversalAnimationLibrary_Standard.zip';dest.write_bytes(r.content)
with zipfile.ZipFile(dest) as z:z.extractall(out/'UniversalAnimationLibrary_Standard')
print('DOWNLOADED',len(r.content),'SHA256',hashlib.sha256(r.content).hexdigest())
