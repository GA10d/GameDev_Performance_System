from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
root=Path(__file__).resolve().parents[1];qa=root/'QA/ChinHair';frames=qa/'Runtime'
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
def sheet(name,files,cols=6):
 files=[p for p in files if p.exists()]
 if not files:return
 w=300;h=322;out=Image.new('RGB',(w*cols,h*((len(files)+cols-1)//cols)),(17,25,21));d=ImageDraw.Draw(out)
 for i,p in enumerate(files):
  im=Image.open(p).convert('RGB');im.thumbnail((w,w));x=i%cols*w;y=i//cols*h;out.paste(im,(x,y))
  d.text((x+4,y+w+2),p.stem,font=font,fill=(225,234,213))
 out.save(qa/(name+'.jpg'),quality=93)
for n in range(12):sheet('chin-'+str(n),[frames/f'chin_{n:02d}_{c}_{a}.png' for a in [0,75] for c in range(3)],3)
for start in [1,6]:sheet('female-hair-'+str(start),[frames/f'hair_{h:02d}_{a}.png' for h in range(start,start+5) for a in [0,-65,65]],3)
sheet('female-hair-extreme',sorted(frames.glob('hair_extreme*.png')),4)
for n in range(3):sheet('mouth-'+str(n),[frames/f'mouth_{n}_{m}_{a}.png' for a in [0,85] for m in range(6)],6)
sheet('actions',sorted(frames.glob('action*.png')),4)
(qa/'screenshot-index.json').write_text(json.dumps([p.name for p in frames.glob('*.png')],indent=2))
print('Sheets:',len(list(qa.glob('*.jpg'))),'Frames:',len(list(frames.glob('*.png'))))
