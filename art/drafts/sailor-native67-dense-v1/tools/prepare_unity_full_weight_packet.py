"""Read-only original topology/native CSR packet for isolated Unity gate."""
from pathlib import Path
import numpy as np,json,hashlib
R=Path(__file__).resolve().parents[4];O=R/'.tools/native67-weight-packet-v1';O.mkdir(exist_ok=False)
a=R/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1/original-indexed-attributes.npz';c=R/'art/drafts/sailor-native67-dense-v1/spatial-candidate-v1/derived-native67-weights-csr.npz';o=R/'art/drafts/sailor-native67-dense-v1/native67-interchange-v2/original-source-offkey-oracle.npz';z=np.load(a);w=np.load(c);q=np.load(o)
arrays={'positions':z['positionsBlenderLocal'].astype('<f4'),'triangles':z['triangles'].astype('<i4'),'uv':z['loopUV'].astype('<f4'),'counts':np.diff(w['indptr']).astype('u1'),'indices':w['indices'].astype('<i4'),'weights':w['weights'].astype('<f4'),'rest':q['restWorld'].astype('<f4'),'meshWorld':q['meshWorld'].astype('<f4')}
for k,v in arrays.items():v.tofile(O/(k+'.bin'))
(O/'bones.txt').write_text('\n'.join(w['boneNames'])+'\n');manifest={'sourceFiles':{str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [a,c,o]},'files':{p.name:{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in O.iterdir() if p.is_file()},'scope':'No source edits; old Rigify joints/weights from indexed archive explicitly NOT used. Native67 CSR and native67 rest oracle only.'};(O/'manifest.json').write_text(json.dumps(manifest,indent=2));print(O)
