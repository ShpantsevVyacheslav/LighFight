from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[4];B=R/'art/drafts/sailor-native67-dense-v1';S=B/'native67-arm-twist-zero-all55-v1';O=R/'docs/reports/native67-zero-twist-dances-2026-10-19';O.mkdir(exist_ok=True);d=json.loads((S/'result.json').read_text());assert len(d['poses'])==55 and not d['error'];assert all(len(p['armTwist'])==2 and all(v['gain']==0 and v['elbowShiftM']<=1e-6 and v['handShiftM']<=1e-6 for v in p['armTwist']) for p in d['poses'])
names={66:'Boom',65:'Bass Beats',64:'All Night',67:'Bubble',63:'Arm Circle Shuffle',74:'Gangnam Groove',77:'Magic Genie',75:'Indoor Swing',70:'Cardio',71:'Denim Pop',82:'Shake It Off'};ids=list(names);font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',19)
for group in range(3):
 sub=ids[group*4:(group+1)*4];c=Image.new('RGB',(960,len(sub)*432),(16,19,23));dr=ImageDraw.Draw(c)
 for row,a in enumerate(sub):
  old=B/('native67-alternate-dances-v2' if row+group*4<8 else 'native67-alternate-dances-more-v1')
  for col,folder in enumerate([old,S]):
   im=Image.open(folder/f'action-{a}-50-front-shoulders-full-float.png').convert('RGB');im.thumbnail((480,400));x=col*480;y=row*432;c.paste(im,(x,y+32));dr.text((x+8,y+6),names[a]+(' / 100%' if col==0 else ' / 0%'),font=font,fill='white')
 c.save(O/f'overview-{group+1}.jpg',quality=95)
for a in ids:
 c=Image.new('RGB',(1920,720),(16,19,23));dr=ImageDraw.Draw(c);old=B/('native67-alternate-dances-v2' if ids.index(a)<8 else 'native67-alternate-dances-more-v1')
 for row,folder in enumerate([old,S]):
  for col,phase in enumerate([10,30,50,70,90]):
   im=Image.open(folder/f'action-{a}-{phase}-front-shoulders-full-float.png').convert('RGB');im.thumbnail((384,320));x=col*384;y=row*360;c.paste(im,(x,y+32));dr.text((x+8,y+6),f'{names[a]} {phase}% / '+('100%' if row==0 else '0%'),font=font,fill='white')
 c.save(O/f'dance-{a}.jpg',quality=94)
receipt={'poses':55,'dances':names,'note':'User-preferred dance candidate0%; same original dense geometry, raw maps, no weight or clavicle change. Compensation retains authored forearm world orientation. Existing baseline captures may have tiny raster differences. Not mobile/runtime acceptance.','resultSHA256':hashlib.sha256((S/'result.json').read_bytes()).hexdigest(),'maxHandShiftM':max(v['handShiftM'] for p in d['poses'] for v in p['armTwist']),'maxElbowShiftM':max(v['elbowShiftM'] for p in d['poses'] for v in p['armTwist'])};(O/'gallery.json').write_text(json.dumps(receipt,indent=2));print(receipt)
