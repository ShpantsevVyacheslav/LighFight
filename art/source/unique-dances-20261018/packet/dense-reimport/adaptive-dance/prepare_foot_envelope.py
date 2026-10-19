"""Read-only original typed attributes -> contiguous offline FBM1, no reduction."""
from pathlib import Path
import hashlib,json,struct,os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
import numpy as np
repo=Path(__file__).resolve().parents[6]
src=repo/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1/original-indexed-attributes.npz'
a=np.load(src);p=a['positionsBlenderLocal'];j=a['jointIndices'];w=a['weights']
assert p.shape==(994452,3) and j.shape==w.shape==(994452,4)
assert np.isfinite(p).all() and np.isfinite(w).all() and (w>=0).all() and (w.sum(1)>0).all()
# NPZ joint IDs use exporter manifest order; NDA1 is parent-first and may differ.
source_names=json.loads((src.parent/'manifest.json').read_text())['boneNames']
with open('/tmp/lighfight-dense-adaptive-dance.nda','rb') as f:
 assert f.read(4)==b'NDA1'
 version,keys,bones=struct.unpack('<iii',f.read(12));f.read(8)
 target_names=[f.read(struct.unpack('<i',f.read(4))[0]).decode() for _ in range(bones)]
assert len(source_names)==len(set(source_names))==bones and set(source_names)==set(target_names)
remap=np.array([target_names.index(name) for name in source_names],dtype=np.int32)
foot={source_names.index(x) for x in ('DEF-foot.L','DEF-toe.L','DEF-foot.R','DEF-toe.R')}
ids=np.nonzero(np.any(np.isin(j,list(foot))&(w>0),axis=1))[0]
assert len(ids)==143168
p=p[ids];w=w[ids];j=remap[j[ids]]
out=Path('/tmp/lighfight-dense-foot-envelope.mesh')
with out.open('wb') as f:
 f.write(b'FBM1');f.write(struct.pack('<i',len(p)))
 for x,typ in [(p,'<f4'),(j,'<i4'),(w,'<f4')]:f.write(x.astype(typ).tobytes())
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  while b:=f.read(8388608):h.update(b)
 return h.hexdigest()
Path(str(out)+'.json').write_text(json.dumps(dict(source=str(src),sourceSHA256=sha(src),outputSHA256=sha(out),vertices=len(p),influences=int(np.count_nonzero(w)),sourceModified=False,boneNameOrderVerified=True,namedIndexRemap=remap.tolist()),indent=2))
print(out)
