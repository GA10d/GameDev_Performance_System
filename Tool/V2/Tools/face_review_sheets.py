"""Assemble actual Unity QA captures; compare paint off/on without touching source images."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont,ImageChops

root=Path(__file__).resolve().parents[1]
qa=root/'QA/FaceTimeline';frames=qa/'Frames'
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
def sheet(name,items,columns=4,size=300):
 rows=(len(items)+columns-1)//columns
 canvas=Image.new('RGB',(columns*size,rows*(size+34)),(21,28,23));draw=ImageDraw.Draw(canvas)
 for i,(file,label) in enumerate(items):
  image=Image.open(frames/file).convert('RGB');image.thumbnail((size,size))
  x=i%columns*size;y=i//columns*(size+34);canvas.paste(image,(x,y))
  draw.text((x+8,y+size+5),label,font=font,fill=(234,233,215))
 canvas.save(qa/name,quality=92)

sheet('face-fixes.jpg',[(f'preset_{i:02d}_{angle}.png',f'预设 {i:02d} / {angle}°') for i in [3,4,7,9] for angle in [0,65]])
sheet('paint-options.jpg',[(f'paint_{species}_{i}_0.png',f'{label} / 纹样 {i}') for species,label in [('Human','人类'),('StandingAlien','灰裔')] for i in range(6)],6,230)
sheet('accessories.jpg',[(f'mask_{species}_{i}_65_0.png',f'{label} / '+('双滤呼吸器' if i==5 else '面罩')) for species,label in [('Human','人类'),('StandingAlien','灰裔')] for i in [5,7]]+
      [(f'headset_{species}_{i}_65.png',f'{label} / '+('接收器' if i==4 else '通讯器')) for species,label in [('Human','人类'),('StandingAlien','灰裔')] for i in [4,8]])
for prefix,columns in [('preset',5),('paint',6),('mask',6),('headset',6)]:
 items=[(p.name,p.stem.replace('StandingAlien','灰裔').replace('Human','人类')) for p in sorted(frames.glob(prefix+'_*.png'))]
 sheet('review-'+prefix+'.jpg',items,columns,230)
report=[]
for species in ['Human','StandingAlien']:
 for angle in [0,65,180]:
  base=Image.open(frames/f'paint_{species}_0_{angle}.png').convert('RGB')
  for mark in range(1,6):
   current=Image.open(frames/f'paint_{species}_{mark}_{angle}.png').convert('RGB')
   diff=ImageChops.difference(base,current);bounds=diff.getbbox()
   if angle==0:assert bounds is not None,(species,mark,'Paint invisible from front')
   if angle==180:assert bounds is None,(species,mark,'Paint leaked to back of head')
   report.append({'species':species,'angle':angle,'mark':mark,'changedBounds':bounds})
(qa/'paint-pixel-check.json').write_text(json.dumps({'status':'PASS','cases':report},indent=2),encoding='utf-8')
print('PASS: 30 paint on/off image comparisons; all five styles visible from front; back views unchanged.')
