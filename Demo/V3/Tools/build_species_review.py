from pathlib import Path
import json,html,markdown
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];qa=root/'QA/species';sheets=qa/'sheets';sheets.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',18)
def sheet(names,path,cols=2,crop=False):
    w,h=(600,242) if crop else (720,450);margin=28
    out=Image.new('RGB',(w*cols,(h+margin)*((len(names)+cols-1)//cols)),'#101810');draw=ImageDraw.Draw(out)
    for i,n in enumerate(names):
        im=Image.open(qa/n)
        if crop:im=im.crop((100,143,1340,643))
        im=im.resize((w,h));x=(i%cols)*w;y=(i//cols)*(h+margin);out.paste(im,(x,y));draw.text((x+9,y+h+4),n,font=font,fill='#d3d5b9')
    out.save(path,quality=92)
for actor in ['xian','ruk','fu']:
    for n in range(1,4):sheet([f'{actor}{n:02d}_{stage}.png' for stage in range(4)],sheets/f'{actor}{n:02d}.jpg')
sheet([f'crawl_{n:02d}.png' for n in range(24)],sheets/'crawl_cycle.jpg',cols=3,crop=True)
sheet([f'actor_{n}_{shot}.png' for n in range(6) for shot in ['portrait','full']],sheets/'all_cast.jpg',cols=3)
sheet([f'actor_{n}_portrait.png' for n in [3,4,5]],qa/'new_cast_preview.jpg',cols=3,crop=True)

names=['林 / 人类','赫 / 人类','余 / 人类','弦 / 站立外星人','砾 / 半兽人','伏 / 四肢爬行外星人']
cards=''.join(f'<figure><img src="QA/species/actor_{i}_portrait.png"><figcaption>{html.escape(names[i])}</figcaption></figure>' for i in [3,4,5])
options=''.join(f'<option value="{i}">{html.escape(n)}</option>' for i,n in enumerate(names))
qa_files=['QA/core-verification.txt','QA/runtime/runtime-verification.txt','QA/species/checks.txt']
stats=[]
for p in qa_files:
    text=(root/p).read_text(encoding='utf-8-sig');rows=text.splitlines();assert all(r.startswith('PASS:') for r in rows),p
    stats.append((p,len(rows)))
report='；'.join(f'{Path(p).parent.name}: {n} 项' for p,n in stats)
body=markdown.markdown((root/'Docs/07_第一版异种扩展.md').read_text(encoding='utf-8'),extensions=['tables','fenced_code'])
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ASTRA · 第一版异种角色扩展</title>
<style>body{margin:0;background:#111810;color:#dbdec6;font:16px/1.8 'Microsoft YaHei',sans-serif}main{max-width:1480px;margin:auto;padding:36px}h1{font-size:34px}h2{color:#bdcc9f;margin-top:38px}small{color:#a8b394}a{color:#bad498}select{background:#263626;color:#dce2cd;font:inherit;padding:10px}img{width:100%;display:block}figure{margin:0;background:#0a100c}figcaption{padding:12px}.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}.box{padding:24px 0;border-top:1px solid #455140;margin-top:28px}input{width:100%}table{border-collapse:collapse;width:100%}td,th{border:1px solid #455140;padding:10px;text-align:left}code{color:#bfcea6}p{max-width:100ch}@media(max-width:850px){.cards,.pair{grid-template-columns:1fr}main{padding:16px}}</style><main>
<small>ASTRA / V1 SPECIES EXPANSION</small><h1>三个新档案，同一套旧终端。</h1><p>基于第一版追加站立外星人、半兽人和四肢爬行外星人。原有人类角色保留在通道 1–3。</p>
<p><a href="Launch.cmd">启动扩展 Demo</a> · <a href="Source/Astra_Six_Species.blend">六角色 Blender 源文件</a> · <a href="README.md">操作说明</a></p>
<div class="cards">__CARDS__</div>
<section class="box"><h2>角色与构图</h2><select id="actor">__OPTIONS__</select><div class="pair"><figure><img id="portrait"><figcaption>默认构图</figcaption></figure><figure><img id="full"><figcaption>B 键 / 全身观察</figcaption></figure></div></section>
<section class="box"><h2>四肢爬行检查帧</h2><p>拖动查看一个步态周期的实际播放器采样；流畅动画请在 Demo 中按 6。C 切换爬行与停驻。</p><input id="crawl" type="range" min="0" max="23" value="6"><img id="gait"></section>
<section class="box"><h2>验证记录</h2><p>__STATS__。真实播放器截图和检查文件随交付保留。自动检查不代表制作级动画验收。</p><p><a href="QA/species/checks.txt">异种专项</a> · <a href="QA/runtime/runtime-verification.txt">第一版功能回归</a> · <a href="QA/species/sheets/crawl_cycle.jpg">完整步态拼图</a></p></section>
<section class="box">__DOC__</section></main><script>const $=id=>document.getElementById(id);function actor(){for(const k of ['portrait','full'])$(k).src='QA/species/actor_'+$('actor').value+'_'+k+'.png';}function gait(){$('gait').src='QA/species/crawl_'+$('crawl').value.padStart(2,'0')+'.png';}$('actor').value='3';$('actor').onchange=actor;$('crawl').oninput=gait;actor();gait();</script></html>'''
page=page.replace('__CARDS__',cards).replace('__OPTIONS__',options).replace('__STATS__',report).replace('__DOC__',body)
(root/'异种扩展说明.html').write_text(page,encoding='utf-8')
(root/'QA/review-summary.json').write_text(json.dumps({'checks':dict(stats),'screenshots':len(list(qa.glob('*.png'))),'image_sources':'actual Unity player, 1440x900'},ensure_ascii=False,indent=2),encoding='utf-8')
print(report)
