import json,collections,sys
from pathlib import Path
from PIL import Image,ImageDraw
src=Path(sys.argv[1]);out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
stats=collections.defaultdict(lambda:dict(frames=0,maxPosition=0,maxRotation=0,defenderMaxPosition=0,defenderMaxRotation=0,frameMismatch=0))
for f in src.glob('*.json'):
 a=json.loads(f.read_text()); key=a['actorId']+'/'+a['variant'];s=stats[key];s['frames']+=1
 for k in ['maxPosition','maxRotation','defenderMaxPosition','defenderMaxRotation']:s[k]=max(s[k],a[k])
 s['frameMismatch']+=not(a['sampleFrame']==a['preFrame']==a['eofFrame'])
(out/'summary.json').write_text(json.dumps(dict(stats),indent=2))
actors=sorted({k.split('/')[0] for k in stats})
for actor in actors:
 sheet=Image.new('RGB',(1000,5*305),'#18202a');d=ImageDraw.Draw(sheet)
 for row,(scenario,gap,tick) in enumerate([('Crouch','1.20','017'),('WalkBack','1.20','017'),('CrouchUppercut','0.90','025'),('CrouchUppercut','0.90','040'),('GuardBack','1.20','017')]):
  for col,variant in enumerate(['original','candidate']):
   p=src/f'{actor}-{variant}-{scenario}-{gap}-{tick}.png'
   if not p.exists():continue
   im=Image.open(p);im.thumbnail((500,280));x=col*500;y=row*305;sheet.paste(im,(x,y));d.text((x+4,y+282),f'{actor} {variant} {scenario} frame{tick}',fill='white')
 sheet.save(out/f'{actor}.jpg',quality=91)
print(json.dumps(dict(actors=len(actors),frames=sum(s['frames'] for s in stats.values()),maxPosition=max(s['maxPosition'] for s in stats.values()),maxRotation=max(s['maxRotation'] for s in stats.values()))))
