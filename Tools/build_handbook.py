"""Turn local research markdown into a portable, offline HTML handbook."""
from pathlib import Path
import html,re
ROOT=Path(__file__).resolve().parents[1]
try:
    import markdown
except ImportError:
    markdown=None
docs=sorted((ROOT/'Docs').glob('*.md'))
css='''
:root{color-scheme:dark;--paper:#c8be99;--dim:#89947c;--green:#a8ce89}
*{box-sizing:border-box} body{margin:0;background:#171e1a;color:#d2d2bd;font:16px/1.8 "Microsoft YaHei",sans-serif}
aside{position:fixed;inset:0 auto 0 0;width:278px;background:#202821;padding:34px 25px;border-right:1px solid #475340;overflow:auto}
aside b{font:26px Consolas,monospace;letter-spacing:.13em;color:var(--paper)}aside p{color:var(--dim);font-size:13px}nav a{display:block;margin:20px 0;color:#c8be99;text-decoration:none;font-size:14px}
main{margin-left:278px;max-width:1260px;padding:50px 62px}article{margin:0 0 100px;padding-bottom:65px;border-bottom:1px solid #465341;scroll-margin-top:35px}
h1{font-size:34px;line-height:1.4;color:var(--paper)}h2{margin-top:42px;color:var(--green);font-size:23px}h3{color:var(--paper)}a{color:#adc996}strong{color:#e5ddbc}
table{border-collapse:collapse;width:100%;font-size:14px;margin:24px 0}td,th{vertical-align:top;border:1px solid #4b5545;padding:12px 13px}th{background:#303d2e;text-align:left}
pre{background:#111914;border-left:3px solid #778869;padding:20px;overflow:auto;font:13px/1.6 Consolas,monospace}code{font-family:Consolas,monospace}img{max-width:100%;border:1px solid #4b5545}blockquote{border-left:3px solid #a8ce89;margin:20px 0;padding:10px 22px;background:#253125}footer{color:var(--dim)}
@media(max-width:900px){aside{position:relative;width:100%}main{margin:0;padding:25px}table{display:block;overflow:auto}}@media print{body{color:#222;background:white}aside{display:none}main{margin:0;padding:0}article{break-before:page}h1,h2,h3,strong,a{color:#222}td,th{border-color:#aaa}pre{background:#eee;color:#222}}
'''
nav=[];articles=[]
for i,doc in enumerate(docs):
    text=doc.read_text(encoding='utf8');title=text.splitlines()[0].lstrip('# ')
    nav.append(f'<a href="#doc{i}">{i:02} / {html.escape(title)}</a>')
    if markdown:
        content=markdown.markdown(text,extensions=['tables','fenced_code'])
    else:content='<pre>'+html.escape(text)+'</pre>'
    for j,target in enumerate(docs):content=content.replace('href="'+target.name+'"','href="#doc'+str(j)+'"')
    # Images in markdown are relative to Docs; handbook is at the root.
    content=re.sub(r'src="(?!https?:|/)([^"]+)"',r'src="Docs/\1"',content)
    articles.append(f'<article id="doc{i}">{content}</article>')
page='<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ASTRA / 人物演出系统研究</title><style>'+css+'</style><aside><b>A S T R A</b><p>PERFORMANCE SYSTEM<br>调研 · 制作 · 技术 · 验收<br>2026 / 09 / 23</p><nav>'+''.join(nav)+'</nav><p>离线阅读，无网络依赖。<br>源文档位于 Docs。</p></aside><main>'+''.join(articles)+'<footer>Project Astra · 站桩演出系统研究与可运行样板</footer></main></html>'
(ROOT/'调研手册.html').write_text(page,encoding='utf8')
print('Handbook written; markdown rendering:',bool(markdown))
