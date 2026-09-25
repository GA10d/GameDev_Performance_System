from pathlib import Path
import csv,json
root=Path(__file__).resolve().parents[1]
review=root/'QA/visual-review'
def rows(folder):
    return list(csv.DictReader((review/folder/'frames.csv').open(encoding='utf-8')))
data={k:rows(v) for k,v in [('before','before-visible'),('after','final'),('matrix','matrix')]}
html='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>ASTRA V2.1 · 画面复查</title><style>
body{margin:0;background:#101712;color:#d8deca;font:16px/1.7 'Microsoft YaHei',sans-serif}main{max-width:1500px;margin:auto;padding:30px}h1{font-size:30px}a{color:#b0d6be}small{color:#98ad9d}select,button{background:#263b2e;color:#e3e5d7;padding:10px;border:1px solid #627862;font:inherit;margin:5px}input{width:100%}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;display:block}section{border-top:1px solid #425746;margin-top:30px;padding-top:20px}.single{max-width:1200px;margin:auto}label{display:inline-block;margin:4px 12px 4px 0}@media(max-width:850px){.pair{grid-template-columns:1fr}}
</style><main><small>ASTRA / VISUAL REVIEW / V2.1</small><h1>逐镜复查与修复前后对照</h1>
<p>来自实际 Unity 播放器。下方是检查帧，不是概念图。剧情约每 0.27 秒采一帧，源动作约每 0.33 秒采一帧；组合矩阵取动作中段。低帧率回放仅用于审查，实际表演请运行 Demo。</p>
<p><a href="Launch.cmd">启动 Demo</a> · <a href="Docs/04_逐镜复查.md">问题与修复记录</a> · <a href="新版说明.html">实现说明</a></p>
<section><h2>16 段剧情</h2><label>镜头 <select id="beat"></select></label><button id="play">播放检查帧</button><input id="scrub" type="range" min="0" max="1000" value="500">
<div class="pair"><figure><figcaption>修复前 · <span id="bt"></span></figcaption><a id="bl"><img id="bi"></a></figure><figure><figcaption>修复后 · <span id="at"></span></figcaption><a id="al"><img id="ai"></a></figure></div></section>
<section><h2>四种人物 × 十七种动作 × 九种镜头</h2><p>612 组中段画面。坐蹲类动作的近景使用带缓动的姿态高度适配；双人／过肩在试演模式使用统一配对站位。</p>
<label>人物 <select id="actor"></select></label><label>动作 <select id="action"></select></label><label>镜头 <select id="lens"></select></label><div class="single"><a id="ml"><img id="mi"></a><small id="mt"></small></div></section>
<section><h2>完整动作检查帧</h2><p>选择人物和动作后，拖动下方进度，观察开始、中段和结束。</p><input id="ascrub" type="range" min="0" max="1000" value="500"><div class="single"><a id="gl"><img id="gi"></a><small id="gt"></small></div></section>
<p>口型、眼睑、配音和正式接触动画清理仍未完成；这些限制没有被改名为“验收通过”。</p></main><script>const data=__DATA__;
const $=id=>document.getElementById(id),base='QA/visual-review/';
const beats=['一份迟到的记录','名字','异乡的声音','倾听','维修员','留下痕迹','值守技师','机器的时间','两种记忆','一个回答','短暂休息','故乡','继续工作','走向记录','给未来的人','空白也是记录'];
const actors=['林 / 人类','回声 / 外星人','鲁克 / 半兽人','沃斯 / 人类'];const shots=['全景','中景','近景','双人','过肩','侧面','低机位','设备插入','全身'];
const actions=['待机','站立说话','行走','正式步行','取物','操作','跪姿维修','坐下','坐姿待机','坐姿说话','起身','推移','蹲姿待机','胸口受击','头部受击','小跑','舞蹈'];
function options(id,list){list.forEach((n,i)=>$(id).add(new Option(String(i+1).padStart(2,'0')+' · '+n,i)));}
options('beat',beats);options('actor',actors);options('action',actions);options('lens',shots);
function show(prefix,row,folder){if(!row)return;let path=base+folder+'/'+row.file;$(prefix+'i').src=path;$(prefix+'l').href=path;$(prefix+'l').target='_blank';if($(prefix+'t'))$(prefix+'t').textContent=row.action+' / '+row.time+' 秒';}
function story(){for(const [key,prefix,folder] of [['before','b','before-visible'],['after','a','final']]){let list=data[key].filter(r=>r.mode==='story'&&r.beat==$('beat').value);show(prefix,list[Math.round((list.length-1)*$('scrub').value/1000)],folder);}}
function matrix(){const p='matrix_'+$('actor').value+'_'+$('action').value.padStart(2,'0')+'_'+$('lens').value+'_';show('m',data.matrix.find(r=>r.file.startsWith(p)),'matrix');animation();}
function animation(){const p='gallery_'+$('actor').value+'_'+$('action').value.padStart(2,'0')+'_';let list=data.after.filter(r=>r.file.startsWith(p));show('g',list[Math.round((list.length-1)*$('ascrub').value/1000)],'final');}
$('beat').onchange=story;$('scrub').oninput=story;$('actor').onchange=matrix;$('action').onchange=matrix;$('lens').onchange=matrix;$('ascrub').oninput=animation;
let timer=null;$('play').onclick=()=>{if(timer){clearInterval(timer);timer=null;$('play').textContent='播放检查帧';}else{timer=setInterval(()=>{let v=Number($('scrub').value)+36;$('scrub').value=v>1000?0:v;story();},267);$('play').textContent='暂停检查帧';}};story();matrix();</script>'''
(root/'画面复查.html').write_text(html.replace('__DATA__',json.dumps(data,ensure_ascii=False)),encoding='utf-8')
print('Review page:',{k:len(v) for k,v in data.items()})
