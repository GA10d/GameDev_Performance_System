import pathlib,html,markdown,re,posixpath
root=pathlib.Path(__file__).resolve().parents[1]
docs=[root/'README.md',*sorted((root/'Docs').glob('*.md')),root/'THIRD_PARTY_NOTICES.md']
parts=[]
for i,p in enumerate(docs):
    content=markdown.markdown(p.read_text(encoding='utf-8'),extensions=['tables','fenced_code'])
    if p.parent.name=='Docs':
        content=re.sub(r'(href|src)="([^"]+)"',lambda m:m.group(0) if ':' in m[2] or m[2].startswith('#') else f'{m[1]}="{posixpath.normpath("Docs/"+m[2])}"',content)
    parts.append(f'<section id="d{i}">{content}</section>')
gallery=''.join(f'<figure><img loading="lazy" src="QA/runtime/{name}.png"><figcaption>{title}</figcaption></figure>' for name,title in [
 ('01_establishing','四人 / 中继站全景'),('03_alien_close','外星人 / Blender 头部改造'),('05_orc_low_angle','半兽人 / 低机位'),('06_repair','动作 / 跪姿维修'),('10_orc_seated','动作 / 坐姿说话'),('13_gallery_voss','人物、动作、镜头试验台')])
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ASTRA · THE RELAY / V2</title>
<style>body{margin:0;background:#111b17;color:#dedcc9;font:16px/1.8 'Microsoft YaHei',sans-serif}header{padding:64px max(6vw,24px) 38px;border-bottom:1px solid #5c6047}header small{color:#bca06b;letter-spacing:.2em}h1{font-size:34px;line-height:1.4}h2{margin-top:38px;color:#c7cbb3;font-size:23px}a{color:#9cc8b5}nav{display:flex;gap:22px;flex-wrap:wrap}main{max-width:1120px;margin:auto;padding:24px}section{padding:30px 0;border-bottom:1px solid #3d473b}table{width:100%;border-collapse:collapse;margin:18px 0}td,th{padding:12px;text-align:left;border:1px solid #3a4539}th{background:#25342b}code,pre{background:#0a120e;color:#bed0ba}pre{padding:18px;overflow:auto}figure{margin:0;background:#0b130f}figure img{width:100%;display:block}figcaption{padding:10px 16px;color:#a9b69c}.gallery{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin:28px 0}p,li{max-width:100ch}section>h1{font-size:28px}@media(max-width:720px){.gallery{grid-template-columns:1fr}body{font-size:14px}main{padding:15px}}</style>
<header><small>QUATERNIUS CAST STUDY / VERSION 02</small><h1>ASTRA · 旧中继站录像</h1><p>四个人，三类种族，十七种可试演动作，九种镜头。来自同一个世界的旧录像。</p><nav><a href="Launch.cmd">启动 Demo</a><a href="#d1">演出设计</a><a href="#d2">技术实现</a><a href="#d3">验收与限制</a><a href="#d4">资产许可</a></nav></header><main>'''+f'<div class="gallery">{gallery}</div>'+''.join(parts)+'</main></html>'
page=page.replace('<a href="#d4">资产许可</a>','<a href="#d4">逐镜复查</a><a href="#d5">资产许可</a><a href="画面复查.html">前后对照</a>').replace('VERSION 02','VERSION 02.1')
(root/'新版说明.html').write_text(page,encoding='utf-8')
print('HANDBOOK_OK')
