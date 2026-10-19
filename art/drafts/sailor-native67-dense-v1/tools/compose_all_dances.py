import json,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[4];base=R/'art/drafts/sailor-native67-dense-v1';out=base/'native67-all-dances-gallery-v1';out.mkdir(exist_ok=True)
names={66:'Boom Dance',65:'Bass Beats',64:'All Night Dance',67:'Bubble Dance',63:'Arm Circle Shuffle',74:'Gangnam Groove',77:'Magic Genie',75:'Indoor Swing',70:'Cardio Dance',71:'Denim Pop Dance',82:'Shake It Off Dance'}
folders=[base/'native67-alternate-dances-v2',base/'native67-alternate-dances-more-v1'];sources={a:folders[0 if i<8 else 1] for i,a in enumerate(names)}
for f,n in zip(folders,[40,15]):
 d=json.loads((f/'result.json').read_text());assert len(d['poses'])==n and not d['error'];assert d['poseAdjustment']=='NONE';assert all(e['samples']==5 and e['maxArmDirectionMismatchDeg']<.5 for e in d['alternativeDances'])
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',19);ids=list(names);phases=[10,30,50,70,90]
def cell(canvas,draw,a,p,view,x,y,w=384,h=320):
 im=Image.open(sources[a]/f'action-{a}-{p}-{view}-full-float.png').convert('RGB');im.thumbnail((w,h));canvas.paste(im,(x,y+32));draw.text((x+8,y+5),f'{names[a]} · {p}%',font=font,fill='white')
for view in ['front-shoulders','front-full','quarter-shoulders']:
 for group in range(3):
  subset=ids[group*4:(group+1)*4];c=Image.new('RGB',(1920,360*len(subset)),(15,18,22));dr=ImageDraw.Draw(c)
  for row,a in enumerate(subset):
   for col,p in enumerate(phases):cell(c,dr,a,p,view,col*384,row*360)
  c.save(out/f'dances-{group+1}-{view}.jpg',quality=94)
for a in ids:
 c=Image.new('RGB',(1920,720),(15,18,22));dr=ImageDraw.Draw(c)
 for row,v in enumerate(['front-shoulders','front-full']):
  for col,p in enumerate(phases):cell(c,dr,a,p,v,col*384,row*360)
 c.save(out/f'dance-{a}.jpg',quality=94)
c=Image.new('RGB',(1920,1296),(15,18,22));dr=ImageDraw.Draw(c)
for j,a in enumerate(ids):cell(c,dr,a,50,'front-shoulders',(j%4)*480,(j//4)*432,480,400)
c.save(out/'overview.jpg',quality=94)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
receipt={'poses':55,'dances':names,'phases':phases,'correction':'NONE','sourceResults':[{'path':str(f/'result.json'),'sha256':sha(f/'result.json')} for f in folders],'limits':'Generic Meshy clips retargeted to same native67 Tripo dense model. Not native Tripo animation exports. Arm direction agreement is not anatomical or twist certification. Numeric gates pass; framebuffer repeat remains RED. Editor synchronous readback, not mobile performance acceptance.','captures':[{'path':str(p),'sha256':sha(p)} for f in folders for p in sorted(f.glob('*-full-float.png'))],'sheets':[{'path':str(p),'sha256':sha(p)} for p in sorted(out.glob('*.jpg'))]};(out/'manifest.json').write_text(json.dumps(receipt,indent=2));print(out)
