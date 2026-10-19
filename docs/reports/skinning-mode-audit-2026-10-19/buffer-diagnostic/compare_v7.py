#!/usr/bin/env python3
"""Offline matched V7 observations. Never emits a timing/dispatch acceptance."""
import argparse,json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('builds',type=Path);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
r={'status':'OBSERVATIONAL ONLY; no original PNT tolerance/dispatch/performance acceptance','identity':{},'modes':{},'cross':[]}
for mode in ['CPU','GPU']:
 d=a.builds/'CurrentBufferEvidence'/f'{mode}-correctness'
 manifest=json.loads((a.builds/f'manifest-{mode}.json').read_text());r['identity'][mode]=manifest
 layout=json.loads((d/'calibrated-layout.json').read_text())
 startup=json.loads((d/'startup-fence.json').read_text())['observations']
 if startup[-1]['stage']!='settled' or not all(x['splashFinished'] for x in startup[1:]):raise SystemExit('Missing startup completion gate '+mode)
 if startup[-1]['uptime']-startup[-2]['uptime']<2:raise SystemExit('Insufficient real settle '+mode)
 data={'layout':layout,'startup':startup,'repeat':[]}
 for actor in range(2):
  for suffix in ['.raw','-decoded-pnt.bin','-world-oracle.bin']:
   hashes=[hashlib.sha256((d/f'actual-phase1-a{actor}-r{i}{suffix}').read_bytes()).hexdigest() for i in range(3)]
   data['repeat'].append({'actor':actor,'suffix':suffix,'byteIdentical':len(set(hashes))==1,'sha256':hashes})
 r['modes'][mode]=data
for key in ['sourceSnapshotSHA','dependencySHA256','unity','api','width','height','warmup','frames','development','automaticSingleCamera','frameTimingStats']:
 if r['identity']['CPU'][key]!=r['identity']['GPU'][key]:raise SystemExit('Identity mismatch '+key)
assembly={}
for mode in ['CPU','GPU']:
 assembly[mode]={line.split(' ',1)[0]:line.split(' ',1)[1] for line in (a.builds/f'compiled-assemblies-{mode}.sha256').read_text().splitlines()}
r['identity']['compiledAssemblyMapsIdentical']=assembly['CPU']==assembly['GPU']
for actor in range(2):
 vals=[]
 for mode in ['CPU','GPU']:
  f=a.builds/'CurrentBufferEvidence'/f'{mode}-correctness'/f'actual-phase1-a{actor}-r0-decoded-pnt.bin';raw=f.read_bytes();n=int.from_bytes(raw[:4],'little');x=np.frombuffer(raw[4:],dtype='<f4').reshape(n,3,3).astype(np.float64)
  if not np.isfinite(x).all():raise SystemExit('Nonfinite '+mode)
  vals.append(x)
 if vals[0].shape!=vals[1].shape:raise SystemExit('PNT shape mismatch')
 diff=abs(vals[0]-vals[1]);r['cross'].append({'actor':actor,'vertices':len(vals[0]),'outputSpacePNTMaxAbsComponents':diff.max(axis=(0,2)).tolist(),'outputSpacePNTMaxVectorDistance':np.linalg.norm(vals[0]-vals[1],axis=2).max(axis=0).tolist(),'note':'Output-space comparison; interpret only after matched calibrated conventions/matrices.'})
for name in ['100k-2actors-jab-Greeting0','100k-2actors-jab-phase1']:
 ims=[]
 for mode in ['CPU','GPU']:
  d=a.builds/'CurrentBufferEvidence'/f'{mode}-correctness';x=np.array(Image.open(d/(name+'.png')).convert('RGBA')).astype(int);y=np.array(Image.open(d/(name+'-repeat.png')).convert('RGBA')).astype(int);delta=abs(x-y)
  r['modes'][mode].setdefault('PNG',[]).append({'name':name,'repeatMaxRGBA':int(delta.max()),'repeatDifferingPixels':int((delta.max(2)>0).sum())});ims.append(x)
 delta=abs(ims[0]-ims[1]);r['cross'].append({'image':name,'maxRGB':int(delta[:,:,:3].max()),'RGBgt2Pixels':int((delta[:,:,:3].max(2)>2).sum()),'alphaMismatchPixels':int((delta[:,:,3]>0).sum())})
a.out.write_text(json.dumps(r,indent=2)+'\n')
