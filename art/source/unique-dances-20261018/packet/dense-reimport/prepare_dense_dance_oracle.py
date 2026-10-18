"""Lossless frozen original-source oracle transport. No Blender/Unity/source edits."""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1');os.environ.setdefault('OMP_NUM_THREADS','1')
from pathlib import Path
from fractions import Fraction
import json,hashlib,struct
import numpy as np
repo=Path(__file__).resolve().parents[5];packet=repo/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1'
cachepath=packet/'adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH-cache.npz'
a=np.load(cachepath);names=list(a['boneNames']);lookup={Fraction(int(t[0]),int(t[1])):i for i,t in enumerate(a['times'])}
worldPose=a['worldPose'];worldSkin=a['worldSkin'];report=json.loads((packet/'adaptive-dance-tspace-fbx-offkey.json').read_text());keyed=json.loads((packet/'full-dance-keyed-original-source-reference.json').read_text());assert report['status']=='PASS_ORIGINAL_HOLDOUTS_PENDING_UNITY'
# Source cache order is preserved and matched by bone NAME in Unity, never assumed.
rows=[(float(s['phase']),Fraction(*s['secondsRational']),True) for s in keyed['samples']]
rows += [(-1.,Fraction(*s['timeRational']),False) for s in report['rows']]
out=Path('/tmp/lighfight-dense-dance-original.dnc')
with out.open('wb') as f:
 f.write(b'DNC1');f.write(struct.pack('<iii',len(names),len(rows),len(keyed['samples'])));f.write(struct.pack('<dd',556/24,float(a['maxWeightSum'])))
 for name in names:
  b=str(name).encode();f.write(struct.pack('<i',len(b)));f.write(b)
 for array in [a['boundCorners'].astype('<f8'),a['boundActive'].astype('u1'),a['meshWorld'].astype('<f8'),(a['armatureWorld']@a['rest']).astype('<f8')]:f.write(array.tobytes())
 for phase,t,is_key in rows:
  i=lookup[t];f.write(struct.pack('<ddB',phase,float(t),int(is_key)));f.write(worldPose[i].astype('<f8').tobytes());f.write(worldSkin[i].astype('<f8').tobytes())
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  while b:=f.read(8*1024*1024):h.update(b)
 return h.hexdigest()
manifest={'scope':'Lossless original frozen-source matrices/bounds from verified fullDance cache, no sampling/reconstruction','sourceCache':str(cachepath),'sourceCacheSHA256':sha(cachepath),'originalReferenceJSONSHA256':sha(packet/'full-dance-keyed-original-source-reference.json'),'offkeyReportSHA256':sha(packet/'adaptive-dance-tspace-fbx-offkey.json'),'boneCount':len(names),'poses':len(rows),'keyedAndEndpoints':5,'independentHolds':7848,'output':str(out),'bytes':out.stat().st_size,'SHA256':sha(out)}
(repo/'art/source/unique-dances-20261018/audit/dense-reimport/dance-oracle-transport.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest,indent=2))
