from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import csv,json
root=Path(__file__).resolve().parents[1]/'QA/visual-review/matrix'
rows=list(csv.DictReader((root/'frames.csv').open(encoding='utf-8')))
out=root/'sheets';out.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',13)
for a in range(4):
    for start in [0,6,12]:
        actions=range(start,min(start+6,17));canvas=Image.new('RGB',(9*300,len(actions)*174),'#101810');draw=ImageDraw.Draw(canvas)
        for y,n in enumerate(actions):
            for shot in range(9):
                row=next(r for r in rows if r['file'].startswith(f'matrix_{a}_{n:02d}_{shot}_'))
                im=Image.open(root/row['file']).crop((16,109,1200,675)).resize((300,144))
                canvas.paste(im,(shot*300,y*174));draw.text((shot*300+3,y*174+146),f'{a}/{n:02d}/{shot} '+row['action'][:23],font=font,fill='#ddd5bb')
        canvas.save(out/f'matrix_{a}_{start}.jpg',quality=90)
suspects=[]
for r in rows:
    shot=int(r['file'].split('_')[3])
    if shot==7:continue
    if not(.02<float(r['head_x'])<.98 and .02<float(r['head_y'])<.85):suspects.append(r)
(root/'framing-suspects.json').write_text(json.dumps(suspects,indent=2))
print(len(rows),'matrix frames,',len(suspects),'head-anchor outliers (not an aesthetic verdict)')
