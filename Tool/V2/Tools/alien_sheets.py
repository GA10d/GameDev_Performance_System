from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];src=root/'QA/AlienFinal';out=root/'QA/AlienSheets';out.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',19)
heads=['灰裔原型','阔吻鳃族','裂颚甲族','穹壳独眼','横翼四目','菌伞胞族','晶面硅族','喙面翼族','纵颅深潜者','花萼共生体']
crowns=['无冠','宽鳃扇','回卷角','背帆冠','月环骨冠','垂落头触须','菌褶伞冠','晶簇冠','感光触须','叠甲冠','叶瓣冠','横桥骨冠']
hair={22:'原包长发·短版',23:'原包长发·齐颈',27:'原包双髻',30:'原包分缝',31:'原包圆寸',32:'原包贴头短发'}
def sheet(name,items,cols=5,cell=290):
 h=cell+34;image=Image.new('RGB',(cols*cell,h*((len(items)+cols-1)//cols)),(14,23,19));d=ImageDraw.Draw(image)
 for i,(p,label) in enumerate(items):
  im=Image.open(p).convert('RGB');im.thumbnail((cell,cell));x=(i%cols)*cell;y=(i//cols)*h
  image.paste(im,(x,y));d.text((x+6,y+cell),label,font=font,fill=(235,239,221))
 image.save(out/(name+'.jpg'),quality=94)
for yaw in [0,70,180]:
 sheet('头型_'+str(yaw),[(src/f'head_{i:02}_{yaw:03}.png',n) for i,n in enumerate(heads)])
 sheet('颅冠_'+str(yaw),[(src/f'crest_{i:02}_{yaw:03}.png',n) for i,n in enumerate(crowns)],4)
sheet('全部头型颅冠组合',[(src/f'combo_{f:02}_{c:02}.png',f'{f:02} / {c:02}') for f in range(10) for c in range(12)],12,180)
for f in range(10):sheet('组合_'+heads[f],[(src/f'combo_{f:02}_{c:02}.png',crowns[c]) for c in range(12)],4,260)
sheet('原包发型',[(src/f'ubc_{i:02}_{yaw:03}.png',n+' / '+str(yaw)) for i,n in hair.items() for yaw in [0,70,180]],3)
sheet('原包发型动作',[(p,p.stem.replace('ubc_motion_','')) for p in sorted(src.glob('ubc_motion_*.png'))],3)
for start in [0,5]:sheet('动作极值_'+str(start),[(p,p.stem.replace('extreme_','')) for p in sorted(src.glob('extreme_*.png')) if start<=int(p.stem.split('_')[1])<start+5],6,240)
print(out)
