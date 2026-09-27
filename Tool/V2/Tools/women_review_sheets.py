"""Arrange unmodified Player camera captures for visual review (requires Pillow)."""
from pathlib import Path
from PIL import Image,ImageDraw
import shutil,json
root=Path(__file__).resolve().parents[1]
folder=root/'QA/WomenAlien';source=folder/'FinalRuntime'
def sheet(name,files,cols=5,size=260):
 rows=(len(files)+cols-1)//cols
 image=Image.new('RGB',(cols*size,rows*(size+30)),(18,25,21));draw=ImageDraw.Draw(image)
 for i,path in enumerate(files):
  picture=Image.open(path);picture.thumbnail((size,size));x=i%cols*size;y=i//cols*(size+30)
  image.paste(picture,(x,y));draw.text((x+5,y+size+6),path.stem,fill='white')
 image.save(folder/(name+'.jpg'),quality=92)
for shot in ['front','side','back','full','bow','dance']:
 sheet('women-'+('fronts' if shot=='front' else shot),[source/f'female_{i:02d}_{shot}.png' for i in range(10)])
sheet('women-accessories',sorted(source.glob('female_hat_*.png'))+sorted(source.glob('female_accessory_*.png')),4,280)
sheet('women-bald',[source/f'female_bald_{yaw}.png' for yaw in [0,70,150,180]],4,320)
for yaw in [0,70,150]:
 sheet(f'gray-mouths-{yaw}',[source/f'gray_{mouth}_{yaw}_{action}.png' for mouth in range(6) for action in ['Idle_Loop','Fixing_Kneeling']],4,280)
sheet('head-fixes',[source/'female_bald_0.png',source/'female_bald_70.png',source/'female_bald_180.png',source/'gray_0_0_Idle_Loop.png',source/'gray_0_70_Fixing_Kneeling.png',source/'gray_0_150_Fixing_Kneeling.png'],3,350)
(folder/'Runtime').mkdir(exist_ok=True)
shutil.copy2(source/'result.txt',folder/'Runtime/result.txt')
report={'pngFiles':len(list(source.glob('*.png'))),'contactSheets':12,'source':'FinalRuntime camera renders, final candidate Player','status':'READY_FOR_VISUAL_REVIEW'}
(folder/'screenshot-index.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
