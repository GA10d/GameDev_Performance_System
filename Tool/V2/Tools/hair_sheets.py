from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];src=root/'QA/HairFinal';out=root/'QA/HairSheets';out.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',21)
names={10:'短寸 · 修复',12:'圆寸',13:'平头',14:'短碎发',15:'前刺短发',16:'后梳短发',17:'左侧分',18:'右侧分',19:'中分短发',20:'齐刘海',21:'斜刘海',22:'短波波头',23:'齐颈直发',24:'低马尾',25:'高马尾',26:'盘发',27:'双髻',28:'短卷发',29:'宽莫西干'}
def sheet(name,items,cols=5):
 cell=300;h=332;image=Image.new('RGB',(cols*cell,h*((len(items)+cols-1)//cols)),(15,23,19));d=ImageDraw.Draw(image)
 for i,(p,label) in enumerate(items):
  im=Image.open(p).convert('RGB');im.thumbnail((cell,cell));x=(i%cols)*cell;y=(i//cols)*h
  image.paste(im,(x,y));d.text((x+8,y+300),label,font=font,fill=(235,239,221))
 image.save(out/(name+'.jpg'),quality=94)
for yaw in [0,65,180]:sheet('发型_'+str(yaw),[(src/f'hair_{i:02}_{yaw:03}.png',n) for i,n in names.items()])
sheet('短寸头顶回归',[(p,p.stem.replace('crop_face_','脸型 ')) for p in sorted(src.glob('crop_*.png'))],4)
sheet('动作回归',[(p,p.stem) for p in sorted(src.glob('motion_*.png'))],5)
print(out)
