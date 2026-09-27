from pathlib import Path
from PIL import Image,ImageDraw
import sys
root=Path(__file__).resolve().parents[1]
source=Path(sys.argv[1]) if len(sys.argv)>1 else root/'QA/Runtime'
out=root/'QA/Sheets';out.mkdir(exist_ok=True)
groups={'human_faces':sorted(source.glob('creator_h*_face.png')),'alien_faces':sorted(source.glob('creator_a*_face.png')),'human_sitting':sorted(source.glob('creator_h*_sit.png')),'alien_sitting':sorted(source.glob('creator_a*_sit.png')),'human_actions':sorted(source.glob('Human_action_*.png')),'alien_actions':sorted(source.glob('StandingAlien_action_*.png'))}
for name,files in groups.items():
 if not files:continue
 sheet=Image.new('RGB',(1200,330*((len(files)+3)//4)),(18,24,20));draw=ImageDraw.Draw(sheet)
 for i,path in enumerate(files):
  image=Image.open(path).convert('RGB');image.thumbnail((296,296));x=(i%4)*300;y=(i//4)*330
  sheet.paste(image,(x,y));draw.text((x+8,y+301),path.stem,fill=(232,235,219))
 sheet.save(out/(name+'.jpg'),quality=92)
print(out)
