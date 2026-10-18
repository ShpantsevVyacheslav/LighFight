"""Build labeled sheets from actual renders. Does not alter individual images."""
from pathlib import Path
import json,math
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[4];G=R/'art/drafts/newrig-sailor-v1/gallery-v1';m=json.loads((G/'manifest.json').read_text())
# Quaternion signs represent same orientation; report shortest physical rotation.
for r in m['cases']:
 r['rotationErrorsDeg']={k:min(v,abs(360-v)) for k,v in r['rotationErrorsDeg'].items()};r['worldRotationMaxErrorDeg']=max(r['rotationErrorsDeg'].values())
m['rotationMetric']='shortest quaternion rotation; q and -q identical';m['nativeBox0IsAuthoredIdle']=False;m['sourceGuardLimitation']='box_02 frame0 is crouched/open-hand authoredboxingstart, not standalone idle';(G/'manifest.json').write_text(json.dumps(m,indent=2))
for kind in ['fullbody','shoulders']:
 rows=[]
 for clip in ['box_02','front_kick_01','hit_to_head','walk','dance_01']:
  cases=[r for r in m['cases'] if r['clip']==clip];w=320;h=400 if kind=='fullbody' else 320;sheet=Image.new('RGB',(w*len(cases),h+52),(242,242,242));draw=ImageDraw.Draw(sheet)
  for i,r in enumerate(cases):
   im=Image.open(G/r[kind]).convert('RGB').resize((w,h));sheet.paste(im,(i*w,52));draw.text((i*w+8,5),f"{clip} {r['phase']:.0%} / {r['sourceSeconds']:.3f}s",fill=(0,0,0));draw.text((i*w+8,25),'BLENDER B-BONE / NOT GAME PROOF',fill=(110,30,20))
  sheet.save(G/f'sheet-{clip}-{kind}.png');rows.append(sheet)
 width=max(x.width for x in rows);height=sum(x.height for x in rows);full=Image.new('RGB',(width,height),(242,242,242));y=0
 for row in rows:full.paste(row,(0,y));y+=row.height
 full.save(G/f'all-motion-{kind}-sheet.png')
print(json.dumps({'renderedImages':sum('fullbody' in r and 'shoulders' in r for r in m['cases'])*2,'maxRotationErrorDeg':max(r['worldRotationMaxErrorDeg'] for r in m['cases']),'lowestWholeMeshZ':min(r['meshWorldMin'][2] for r in m['cases'])},indent=2))
