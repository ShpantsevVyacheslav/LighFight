"""Contact sheets of actual PNG captures; resize/layout only, no crops or AI edits."""
import json,hashlib,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[4];O=R/'art/drafts/sailor-native67-dense-v1'/sys.argv[1];d=json.load(open(O/'result.json'));assert len(d['poses'])==40 and not d['error'];assert len(d['alternativeDances'])==8 and all(e['samples']==5 and e['maxArmDirectionMismatchDeg']<.5 for e in d['alternativeDances']);assert d['poseAdjustment']=='NONE' and d['skinningMethod']=='LinearBlend_all_positive_Float32'
names={66:'Boom Dance',65:'Bass Beats',64:'All Night Dance',67:'Bubble Dance',63:'Arm Circle Shuffle',74:'Gangnam Groove',77:'Magic Genie',75:'Indoor Swing'};ids=list(names);phases=[10,30,50,70,90];font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',23);small=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',19)
for view in ['front-shoulders','front-full','quarter-shoulders']:
 for sheet in range(2):
  canvas=Image.new('RGB',(1920,1440),(15,18,22));draw=ImageDraw.Draw(canvas)
  for row,action in enumerate(ids[sheet*4:(sheet+1)*4]):
   for col,phase in enumerate(phases):
    im=Image.open(O/f'action-{action}-{phase}-{view}-full-float.png').convert('RGB');im.thumbnail((384,320));x=col*384;y=row*360;canvas.paste(im,(x,y+36));draw.text((x+10,y+5),f'{names[action]} · {phase}%',fill=(240,240,240),font=small)
  canvas.save(O/f'other-dances-{sheet+1}-{view}.jpg',quality=94)
for action in ids:
 canvas=Image.new('RGB',(1920,720),(15,18,22));draw=ImageDraw.Draw(canvas)
 for row,view in enumerate(['front-shoulders','front-full']):
  for col,phase in enumerate(phases):
   im=Image.open(O/f'action-{action}-{phase}-{view}-full-float.png').convert('RGB');im.thumbnail((384,320));x=col*384;y=row*360;canvas.paste(im,(x,y+36));draw.text((x+10,y+5),f'{names[action]} · {phase}%',fill=(240,240,240),font=small)
 canvas.save(O/f'dance-{action}-detail-and-full.jpg',quality=94)
# One midphase from each dance for fast overview.
canvas=Image.new('RGB',(1920,856),(15,18,22));draw=ImageDraw.Draw(canvas)
for j,action in enumerate(ids):
 im=Image.open(O/f'action-{action}-50-front-shoulders-full-float.png').convert('RGB');im.thumbnail((480,400));x=(j%4)*480;y=(j//4)*428;canvas.paste(im,(x,y+28));draw.text((x+10,y+3),names[action]+' · 50%',fill=(240,240,240),font=font)
canvas.save(O/'eight-dances-overview.jpg',quality=94)
receipt={'sourceResultSHA256':hashlib.sha256((O/'result.json').read_bytes()).hexdigest(),'model':'Same original dense Sailor/native67/full-bary-positive weights','poseCorrection':'NONE','method':'Actual retargeted Generic source clips, not8nativeTripoexports. Truebind/virtualT calibration; source arm direction agreement verified in each JSON. No crop or retouch; input960x800PNG preserved. JPEG contact sheets derived for inspection.','poses':40,'dances':names,'phases':phases,'capturedViews':['front-full','front-shoulders','quarter-shoulders'],'files':[{'path':str(p.relative_to(R)),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(O.glob('*.jpg'))]};(O/'contact-sheet-manifest.json').write_text(json.dumps(receipt,indent=2));print('Composed',len(receipt['files']),'sheets')
