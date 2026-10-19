#!/usr/bin/env python3
"""Read-only V8 per-mode Greeting witness inspection. No tolerance acceptance."""
import argparse,json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--out',type=Path,required=True);a=p.parse_args();base='100k-2actors-jab-Greeting0';images=[base+'.png',base+'-repeat.png'];r={'status':'OBSERVATIONAL; no timing/dispatch acceptance','captures':[]}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def diff(x,y,path=''):
 if type(x)!=type(y):return [path]
 if isinstance(x,dict):
  return [z for k in sorted(x.keys()|y.keys()) if k not in ['stage','utc','uptime','frame','draws'] for z in (diff(x[k],y[k],path+'/'+k) if k in x and k in y else [path+'/'+k])]
 if isinstance(x,list):
  if len(x)!=len(y):return [path+'/length']
  return [z for i,(v,w) in enumerate(zip(x,y)) for z in diff(v,w,path+'/'+str(i))]
 return [] if x==y else [path]
for name in images:
 prefix=a.output/(name+'-witness');before=json.loads(Path(str(prefix)+'-before-state.json').read_text());png=json.loads(Path(str(prefix)+'-png-state.json').read_text());after=json.loads(Path(str(prefix)+'-after-state.json').read_text());c={'image':name,'requestAtPNGFrame':[],'beforeVsPNGChangedPaths':diff(before,png),'beforeVsAfterChangedPaths':diff(before,after),'actors':[]}
 for actor in range(2):
  h=json.loads(Path(str(prefix)+f'-before-a{actor}.json').read_text());c['requestAtPNGFrame'].append(h['requestFrame']==png['frame'] and h['requestDraws']==png['draws'])
  files=[Path(str(prefix)+f'-{stage}-a{actor}-pnt.bin') for stage in ['before','after']];vals=[np.frombuffer(f.read_bytes()[4:],dtype='<f4').reshape(-1,3,3).astype(float) for f in files];delta=abs(vals[0]-vals[1]);c['actors'].append({'actor':actor,'beforeAfterRawEqual':sha(Path(str(prefix)+f'-before-a{actor}.raw'))==sha(Path(str(prefix)+f'-after-a{actor}.raw')),'PNTMaxAbs':delta.max(axis=(0,2)).tolist(),'sourceFrame':h['requestFrame'],'completeFrame':h['completeFrame']})
 r['captures'].append(c)
x=np.asarray(Image.open(a.output/images[0]).convert('RGBA')).astype(int);y=np.asarray(Image.open(a.output/images[1]).convert('RGBA')).astype(int);delta=abs(x-y);r['PNGRepeat']={'maxRGB':int(delta[:,:,:3].max()),'RGBgt2Pixels':int((delta[:,:,:3].max(2)>2).sum()),'RGBgt16Pixels':int((delta[:,:,:3].max(2)>16).sum()),'alphaMismatchPixels':int((delta[:,:,3]>0).sum())}
a.out.write_text(json.dumps(r,indent=2)+'\n')
