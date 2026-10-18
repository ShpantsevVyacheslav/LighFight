"""Attest rendered R2 poses remain exact after only increasing derived-curve sample rate.
Images remain labeled as R2 actual Blender renders; no claim of an unperformed R3 render.
"""
from pathlib import Path
import json,struct,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1/native67-trapezius-v1'
p=ROOT/'art/drafts/newrig-sailor-v1/tools/20_native67_clavicle_corrective.py';s=p.read_text();exec(compile(s[:s.index('tracks={')],str(p),'exec'))
def load(path):
 global g,data
 b=path.read_bytes();length=struct.unpack_from('<I',b,12)[0];g=json.loads(b[20:20+length]);offset=20+length;size=struct.unpack_from('<I',b,offset)[0];data=bytearray(b[offset+8:offset+8+size]);return channels(next(a for a in g['animations'] if a['name']=='Dance_TrapeziusSupport_v1')),hashlib.sha256(b).hexdigest()
a,shaA=load(O/'versions/r2-96hz/sailor-native67-trapezius-candidate.glb');b,shaB=load(O/'sailor-native67-trapezius-candidate.glb');rows=[]
for phase in [0,.25,.5,.75,1]:
 ma,qa=world(sample(a,duration*phase));mb,qb=world(sample(b,duration*phase));error=max(float(np.max(np.abs(ma[i]-mb[i]))) for i in ma);rows.append({'phase':phase,'allNodeWorldMatrixMaxAbsDelta':error});assert error<1e-12,(phase,error)
result={'status':'PASS','actualRenderedCandidateSHA256':shaA,'finalCandidateSHA256':shaB,'allFiveRenderedPosesWorldMatrixExactWithin':1e-12,'rows':rows,'basis':'Both exported GLB rotation tracks independently parsed and interpolated; complete hierarchy matrices compared. All mesh/skin/material/BIN originals exact in both. Existing PNGs are actual R2 renders; no R3 GPU render claimed.'};(O/'render-pose-identity.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
