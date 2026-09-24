from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import sys,csv
root=Path(sys.argv[1]); out=root/'sheets';out.mkdir(exist_ok=True)
rows=list(csv.DictReader((root/'frames.csv').open(encoding='utf-8')))
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',16)
def sheet(name, entries, cols=4, w=480):
    h=round(w*900/1600);canvas=Image.new('RGB',(cols*w,((len(entries)+cols-1)//cols)*(h+27)), '#111911');d=ImageDraw.Draw(canvas)
    for i,row in enumerate(entries):
        im=Image.open(root/row['file']);im.thumbnail((w,h));x=i%cols*w;y=i//cols*(h+27);canvas.paste(im,(x,y));d.text((x+5,y+h+4),f"{row['beat'] if row['mode']=='story' else row['actor']} / {row['action']} / {row['time']}s",font=font,fill='#ddd5b4')
    canvas.save(out/(name+'.jpg'),quality=88)
represent=[]
for beat in range(16):
    rr=[r for r in rows if r['mode']=='story' and int(r['beat'])==beat]
    if not rr:continue
    sample=[rr[round(i*(len(rr)-1)/7)] for i in range(8)]
    sheet('story_'+str(beat).zfill(2),sample,2,800)
    represent.append(rr[len(rr)//2])
sheet('story_overview',represent)
for actor in range(4):
    rr=[r for r in rows if r['file'].startswith('gallery_') and int(r['actor'])==actor]; entries=[]
    for action in dict.fromkeys(r['action'] for r in rr):
        aa=[r for r in rr if r['action']==action];entries.append(aa[len(aa)//2])
        sheet(f'action_{actor}_{action}',[aa[round(i*(len(aa)-1)/3)] for i in range(4)],2,640)
    if entries:sheet('actor_'+str(actor),entries)
    lens=[r for r in rows if r['file'].startswith('lens_'+str(actor)+'_')]
    if lens:
        sheet('lenses_'+str(actor),lens,3,640)
        sheet('lenses_overview_'+str(actor),lens[1::3],3,640)
print(root,len(rows),'frames /',len(list(out.glob('*.jpg'))),'sheets')
