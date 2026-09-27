"""Build labelled contact sheets from the actual executable's RenderTexture captures."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
r=Path(__file__).resolve().parents[1];src=r/'QA/OutfitFinal';out=r/'QA/OutfitSheets';out.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
names=['舱外加压','轨道驾驶','星表测绘','采矿动力','废船拆解','生化隔离','低温勘探','热区防护','舰队安保','轨道医疗','舰桥礼勤','远航行商']
for species in ['Human','StandingAlien']:
 for yaw in [0,90,180]:
  files=[src/f'outfit_{species}_{i:02d}_{yaw:03d}.png' for i in range(10,22)]
  if not all(f.exists() for f in files):continue
  sheet=Image.new('RGB',(1200,1560),(17,23,21));draw=ImageDraw.Draw(sheet)
  for k,f in enumerate(files):
   x=k%4*300;y=k//4*520
   picture=Image.open(f).convert('RGB').crop((250,35,750,960));picture.thumbnail((290,460))
   sheet.paste(picture,(x+(300-picture.width)//2,y+50));draw.text((x+12,y+8),f'{k+10:02d} {names[k]}',font=font,fill=(218,226,198))
  sheet.save(out/f'{species}_{yaw:03d}.jpg',quality=92)
 # Motion views: side-by-side kneel / seated talk / dance for each outfit.
 for start in [10,16]:
  sheet=Image.new('RGB',(1200,900),(17,23,21));draw=ImageDraw.Draw(sheet)
  for k,i in enumerate(range(start,start+6)):
   for a,action in enumerate([6,9,16]):
    f=src/f'motion_{species}_{i:02d}_{action:02d}.png'
    if not f.exists():continue
    x=(k%2)*600+a*200;y=(k//2)*300
    pic=Image.open(f).convert('RGB');pic.thumbnail((200,430));sheet.paste(pic,(x,y+65))
   draw.text(((k%2)*600+15,(k//2)*300+12),f'{i:02d} {names[i-10]} | 跪姿 / 坐姿 / 舞动',font=small,fill=(218,226,198))
  sheet.save(out/f'{species}_motion_{start:02d}.jpg',quality=94)
mix=Image.new('RGB',(1400,1200),(17,23,21));draw=ImageDraw.Draw(mix)
for i in range(22):
 f=src/f'mixed_{i:02d}.png'
 if not f.exists():continue
 pic=Image.open(f).convert('RGB').crop((240,30,760,970));pic.thumbnail((195,260));x=i%7*200;y=i//7*300;mix.paste(pic,(x+(200-pic.width)//2,y+28))
 draw.text((x+6,y+2),f'{i:02d}/{(i+7)%22:02d}/{(i+13)%22:02d}',font=small,fill=(218,226,198))
mix.save(out/'mixed_outfits.jpg',quality=92)
print('OUTFIT_SHEETS',len(list(out.glob('*.jpg'))))
