"""Retain every accepted source adaptive key/value Float64; no new simplification."""
from pathlib import Path
from fractions import Fraction
import os,json,struct,hashlib
os.environ.setdefault('OPENBLAS_NUM_THREADS','1');os.environ.setdefault('OMP_NUM_THREADS','1')
import numpy as np
repo=Path(__file__).resolve().parents[6];packet=repo/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1';out=Path(__file__).resolve().parent
fitpath=packet/'adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH.json';cachepath=packet/'adaptive-dance_01-FULL_ADAPTIVE_TIGHT008_EXACT_BATCH-cache.npz'
fit=json.load(open(fitpath));assert fit['status']=='PASS_DIAGNOSTIC_NOT_EXPORT' and len(fit['acceptedKeys'])==2535
cache=np.load(cachepath);assert json.loads(str(cache['sourceConfig']))==fit['sourceConfig'];lookup={Fraction(int(t[0]),int(t[1])):i for i,t in enumerate(cache['times'])};keys=[Fraction(*t) for t in fit['acceptedKeys']];indices=np.array([lookup[t] for t in keys]);alltrs=cache['localTRS'];trs=alltrs[indices].astype('<f8');assert trs.shape==(2535,136,10)
path=Path('/tmp/lighfight-dense-adaptive-dance.nda')
with path.open('wb') as f:
 f.write(b'NDA1');f.write(struct.pack('<iii',1,len(keys),136));f.write(struct.pack('<d',556/24))
 for name in cache['boneNames']:
  b=str(name).encode();f.write(struct.pack('<i',len(b)));f.write(b)
 for values in [cache['parents'].astype('<i4'),cache['rest'].astype('<f8'),cache['armatureWorld'].astype('<f8'),cache['meshWorld'].astype('<f8'),np.array([float(t) for t in keys],dtype='<f8'),trs]:f.write(values.tobytes())
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  while b:=f.read(8*1024*1024):h.update(b)
 return h.hexdigest()
manifest={'status':'SOURCE_KEY_PACKET_NOT_RUNTIME_ACCEPTANCE','sourceFitSHA256':sha(fitpath),'sourceCacheSHA256':sha(cachepath),'keys':2535,'bones':136,'sourceTRSFloat64SHA256':hashlib.sha256(trs.tobytes()).hexdigest(),'timeFloat64SHA256':hashlib.sha256(np.array([float(t) for t in keys],dtype='<f8').tobytes()).hexdigest(),'rawKeyRationals':fit['acceptedKeys'],'interpolation':'linear position/scale; individually normalized quaternions; hemisphere shortest normalized linear interpolation; normalized again in compose; FULL parent-first rest FK, exactly curve_core.interpolate/Skeleton.world_pose','sourceGeometryWeightsMapsModified':False,'output':str(path),'bytes':path.stat().st_size,'SHA256':sha(path)}
(out/'source-key-packet.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps({k:v for k,v in manifest.items() if k!='rawKeyRationals'},indent=2))
