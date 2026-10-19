"""Separate local packet, original geometry authority + full closed-triangle CSR."""
from pathlib import Path
import numpy as np,json,hashlib,shutil
R=Path(__file__).resolve().parents[4];O=R/'.tools/native67-bary-weight-packet-v1';O.mkdir(exist_ok=False);source=R/'.tools/native67-weight-packet-v1';candidate=R/'art/drafts/sailor-native67-dense-v1/full-bary-weights-v3';report=json.load(open(candidate/'report.json'));csr=candidate/'derived-full-bary-csr.npz';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();assert sha(csr)==report['csrSHA256'];z=np.load(csr);assert len(z['indptr'])==994453 and np.all(z['weights']>0);assert np.diff(z['indptr']).max()<=255
for p in source.iterdir():
 if p.name not in ['manifest.json','weights.bin','indices.bin','counts.bin','bones.txt']:shutil.copy2(p,O/p.name)
np.diff(z['indptr']).astype('u1').tofile(O/'counts.bin');z['weights'].astype('<f4').tofile(O/'weights.bin');z['indices'].astype('<i4').tofile(O/'indices.bin');(O/'bones.txt').write_text('\n'.join(z['boneNames'])+'\n');(O/'manifest.json').write_text(json.dumps({'kind':'DERIVED_FULL_CLOSED_TRIANGLE_BARY_NO_SPATIAL_AVERAGE','sourceCSR':str(csr),'sourceCSRSHA256':sha(csr),'originalPacketManifestSHA256':sha(source/'manifest.json'),'files':{p.name:{'bytes':p.stat().st_size,'sha256':sha(p)} for p in O.iterdir() if p.is_file()}},indent=2));print(O)
