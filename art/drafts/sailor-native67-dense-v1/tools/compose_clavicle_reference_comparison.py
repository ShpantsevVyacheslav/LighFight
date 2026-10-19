from pathlib import Path
import json,sys
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[4];B=R/'art/drafts/sailor-native67-dense-v1';O=B/(sys.argv[1] if len(sys.argv)>1 else 'native67-dance-source-clavicle-render-v1');d=json.loads((O/'result.json').read_text());assert len(d['poses'])==6 and not d['error']
f=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',19)
for group in range(2):
 c=Image.new('RGB',(1440,864),(16,19,23));dr=ImageDraw.Draw(c)
 for col,p in enumerate(d['poses'][group*3:group*3+3]):
  a=int(p['clip'].split('-')[1]);phase=round(p['phase']*100);old=B/('native67-surface-strain-v1' if ('delta-mush' in O.name or 'damping' in O.name or 'twist-match' in O.name or 'arm-twist' in O.name) else 'native67-collar-matched-baseline-v1' if 'collar' in O.name else 'native67-alternate-dances-more-v1' if a==82 else 'native67-alternate-dances-v2')
  for row,folder in enumerate([old,O]):
   im=Image.open(folder/f'action-{a}-{phase}-front-shoulders-full-float.png').convert('RGB');im.thumbnail((480,400));x=col*480;y=row*432;c.paste(im,(x,y+32));dr.text((x+8,y+6),f'{a} / {phase}% / '+('Native rest' if row==0 else ('Upper-arm twist gain0.5' if 'arm-twist-half' in O.name else 'Upper-arm twist gain0' if 'arm-twist-zero' in O.name else 'Source-matched clavicle twist' if 'twist-match' in O.name else 'Clavicle motion gain0.65' if 'damping' in O.name else 'Local delta mush' if 'delta-mush' in O.name else 'Clavicle collar weights' if 'collar-clavicle' in O.name else 'Collar weights' if 'collar' in O.name else 'Adaptive clavicle support' if 'adaptive' in O.name else 'Source clavicle reference')),font=f,fill='white')
 c.save(O/f'comparison-{group+1}.jpg',quality=95)
print('comparison ready')
