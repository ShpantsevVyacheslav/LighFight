import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[4];O=R/'art/drafts/sailor-native67-dense-v1/native67-dance-joint-audit-v1';d=json.loads((O/'result.json').read_text());assert not d.get('error') and len(d['poses'])==66
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',20)
# Fixed projection/scale in normalized body coordinates; no pose fit.
edges=[(0,1),(1,2),(2,3),(3,4),(4,5),(3,6),(6,7),(7,8),(8,9),(3,10),(10,11),(11,12),(12,13)]
C=Image.new('RGB',(1600,960),(16,19,23));dr=ImageDraw.Draw(C)
for col,(action,phaseIndex) in enumerate([('action-66',3),('action-65',3),('action-67',3),('action-82',3)]):
 p=[p for p in d['poses'] if p['action']==action][phaseIndex]
 for row,key in enumerate(['source','target']):
  x=col*400;y=row*480;color=(90,210,235) if row==0 else (240,192,100);dr.text((x+10,y+10),f'{action} 50% / {key}',font=font,fill=color)
  def xy(v):return x+200+v['x']*270,y+420-v['y']*270
  for a,b in edges:dr.line([xy(p[key][a]),xy(p[key][b])],fill=color,width=4)
  for v in p[key][:14]:
   xx,yy=xy(v);dr.ellipse((xx-4,yy-4,xx+4,yy+4),fill='white')
C.save(O/'source-target-shoulders.jpg',quality=95)
rows=[]
for p in d['poses']:
 rows.append({'action':p['action'],'seconds':p['seconds'],'sourceDropMean':(p['sourceLeftShoulderNeckDrop']+p['sourceRightShoulderNeckDrop'])/2,'targetDropMean':(p['targetLeftShoulderNeckDrop']+p['targetRightShoulderNeckDrop'])/2})
(O/'summary.json').write_text(json.dumps(rows,indent=2))
print('66 samples; saved skeleton sheet')
