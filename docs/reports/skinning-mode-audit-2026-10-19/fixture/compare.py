#!/usr/bin/env python3
"""Offline correctness gate only. Strict prior thresholds unchanged; no engines."""
import hashlib,json,struct,sys
from pathlib import Path
import numpy as np
from PIL import Image
repo=Path(__file__).resolve().parents[4];owned=repo/'.tools/skinning-current100k-review';root=owned/'CurrentSkinningBuilds/CurrentSkinningEvidence';fail=[];rows=[]
def matrices(p):
 data=p.read_bytes();offset=0;count=struct.unpack_from('<i',data,offset)[0];offset+=4;parts=[]
 for _ in range(count):
  n=struct.unpack_from('<i',data,offset)[0];offset+=4;parts.append(np.frombuffer(data,dtype='<f4',count=n*16,offset=offset));offset+=n*64
 if offset!=len(data):raise ValueError('Malformed matrix fixture')
 return np.concatenate(parts)
def pair(a,b,label,repeat=False):
 x=np.array(Image.open(a).convert('RGBA'),dtype=np.int16);y=np.array(Image.open(b).convert('RGBA'),dtype=np.int16)
 if x.shape!=y.shape:fail.append(label+': dimensions');return
 ma=matrices(Path(str(a)+'.local-matrices.bin'));mb=matrices(Path(str(b)+'.local-matrices.bin'));delta=float(np.max(abs(ma-mb))) if ma.shape==mb.shape else float('inf')
 d=abs(x-y);row={'pair':label,'maxMatrixDelta':delta,'maxChannel':int(d[:,:,:3].max()),'over2Fraction':float(np.any(d[:,:,:3]>2,axis=2).mean()),'alphaMismatch':int(np.count_nonzero(d[:,:,3]))};rows.append(row)
 if delta>1e-5 or row['maxChannel']>16 or row['over2Fraction']>.001 or row['alphaMismatch']:fail.append(label+': unchanged parity threshold')
 if repeat and np.any(d):fail.append(label+': same-mode frozen framebuffer not exactly repeatable')
sha=hashlib.sha256((owned/'current-input-snapshot.json').read_bytes()).hexdigest()
for mode,value in [('CPU',0),('GPU',1)]:
 folder=root/(mode+'-correctness');runtime=json.loads((folder/'runtime.json').read_text());manifest=json.loads((folder/'build-manifest.json').read_text())
 if not runtime['completed'] or runtime['errors'] or runtime['api']!='Metal':fail.append(mode+': runtime')
 if manifest['sourceSnapshotSHA']!=sha or manifest['numericMode']!=value:fail.append(mode+': build provenance')
 for file in folder.glob('*.png'):
  if '-repeat.png' in file.name:continue
  repeat=folder/(file.stem+'-repeat.png');pair(file,repeat,mode+' repeat '+file.name,True)
 for phase in ['jab']:
  images=[(folder/f'100k-2actors-{phase}-phase{i}.png').read_bytes() for i in range(4)]
  if len(set(images))<3:fail.append(mode+': attack not dynamically deformed')
for file in (root/'CPU-correctness').glob('*.png'):
 if '-repeat.png' not in file.name:pair(file,root/'GPU-correctness'/file.name,'CPU/GPU '+file.name)
a=(owned/'CurrentSkinningBuilds/input-dependencies-CPU.txt').read_bytes();b=(owned/'CurrentSkinningBuilds/input-dependencies-GPU.txt').read_bytes()
if a!=b:fail.append('Asset dependency SHA listings differ')
result={'status':'FAIL' if fail else 'PASS numeric; visual/dispatch review still required','sourceSnapshotSHA':sha,'failures':fail,'pairs':rows};(root/'comparison.json').write_text(json.dumps(result,indent=2)+'\n')
# Permit is issued only with explicit human/parent review flag after numeric pass;
# dispatcher/capture proof is separate and no setting alone proves GPU execution.
if not fail and '--review-approved' in sys.argv:
 proofPath=root/'dispatch-and-visual-review.json'
 if not proofPath.is_file():raise SystemExit('Numeric PASS, but dispatch/actual GPU-buffer + visual proof missing; no timing permit')
 proof=json.loads(proofPath.read_text())
 if proof.get('sourceSnapshotSHA')!=sha or proof.get('actualGPUBufferParity')!='PASS' or proof.get('metalComputeDispatchObserved')!=True or proof.get('visualReview')!='PASS':raise SystemExit('Incomplete dispatch/buffer/visual gate; no timing permit')
 for artifact in proof.get('artifacts',[]):
  f=Path(artifact['path'])
  if not f.is_file() or hashlib.sha256(f.read_bytes()).hexdigest()!=artifact['sha256']:raise SystemExit('Proof artifact SHA mismatch')
 if not proof.get('artifacts'):raise SystemExit('Real proof artifact required')
 (root/'profile-permit.json').write_text(json.dumps({'sourceSnapshotSHA':sha,'numericCorrectness':'PASS','reviewApproval':'explicit reviewed artifact proof'})+'\n')
print(result['status']);sys.exit(bool(fail))
