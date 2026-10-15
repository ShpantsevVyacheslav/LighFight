#!/usr/bin/env python3
"""Post-player gate. Does not run Unity. CPU is the corrected single-render reference."""
import argparse,csv,json,sys,struct
from pathlib import Path
import numpy as np
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('evidence',type=Path);a=p.parse_args();root=a.evidence
def matrices(path):
 data=path.read_bytes();offset=0
 count=struct.unpack_from('<i',data,offset)[0];offset+=4;out=[]
 for _ in range(count):
  n=struct.unpack_from('<i',data,offset)[0];offset+=4
  out.extend(np.frombuffer(data,dtype='<f4',count=n*16,offset=offset));offset+=n*16*4
 if offset!=len(data):raise ValueError('Invalid pose matrix fixture')
 return np.asarray(out)
rows=[];fail=[]
builds=root.parent/'DenseSkinningBuilds'
reference=(builds/'input-dependencies-CPU.txt').read_bytes()
for mode,value in [('CPU',0),('GPU',1),('GPUBatched',2)]:
 if (builds/f'input-dependencies-{mode}.txt').read_bytes()!=reference:fail.append(mode+': dependency SHA list differs')
 manifest=json.loads((root/mode/'build-manifest.json').read_text())
 if manifest['mode']!=mode or manifest['numericMode']!=value:fail.append(mode+': embedded build configuration mismatch')

for mode in ('CPU','GPU','GPUBatched'):
 folder=root/mode
 result=json.loads((folder/'runtime.json').read_text())
 if not result['completed'] or result['errors'] or result['api']!='Metal':fail.append(mode+': runtime error/backend')
 for path in folder.glob('*.csv'):
  if path.name in ('summary.csv','available-skin-mesh-markers.csv','capture-poses.csv'):continue
  records=list(csv.DictReader(path.open()))
  if len(records)!=200 or any(int(r['automaticCameraRenders'])!=1 for r in records):fail.append(str(path)+': measurement render/count')
 for path in sorted((root/'CPU').glob('*.png')):
  x=np.asarray(Image.open(path).convert('RGBA'),dtype=np.int16);y=np.asarray(Image.open(folder/path.name).convert('RGBA'),dtype=np.int16)
  if x.shape!=y.shape:fail.append(mode+': dimensions '+path.name);continue
  cpuPose=matrices(Path(str(path)+'.local-matrices.bin'));modePose=matrices(Path(str(folder/path.name)+'.local-matrices.bin'))
  poseError=float(np.max(np.abs(cpuPose-modePose))) if cpuPose.shape==modePose.shape else float('inf')
  if poseError>1e-5:fail.append(mode+': commanded/observed pose mismatch before raster attribution '+path.name)
  d=np.abs(x-y);rgb=d[:,:,:3];alpha=d[:,:,3]
  row=dict(mode=mode,image=path.name,maxLocalMatrixDelta=poseError,maxChannel=int(rgb.max()),meanChannel=float(rgb.mean()),over2Fraction=float(np.any(rgb>2,axis=2).mean()),alphaMismatchPixels=int(np.count_nonzero(alpha)))
  rows.append(row)
  # Strict review threshold, does not waive visual seam/tail review.
  if row['over2Fraction']>.001 or row['maxChannel']>16 or row['alphaMismatchPixels']:fail.append(mode+': framebuffer parity '+path.name)
 # Real changing attack framebuffers required; repeated snapshots cannot prove deformation.
 for scenario in ('100k-1actor','2m-1actor','2m-2actors'):
  for move in ('jab','frontKick'):
   frames=[(folder/f'{scenario}-{move}-phase{i}.png').read_bytes() for i in range(4)]
   if len(set(frames))<3:fail.append(mode+': missing dynamic deformation '+scenario+' '+move)
# Input dependency lists must be byte-identical between builds; compare outside this runtime folder.
out=dict(status='PASS_PENDING_VISUAL_REVIEW' if not fail else 'FAIL',failures=fail,images=rows,scope='Mac development player only; no phone or GPU execution inference from settings alone')
(root/'comparison.json').write_text(json.dumps(out,indent=2));print(json.dumps({k:v for k,v in out.items() if k!='images'},indent=2));sys.exit(bool(fail))
