import os
os.environ['OPENBLAS_NUM_THREADS']='6'
from pathlib import Path
import numpy as np,json,hashlib,sys
R=Path(__file__).resolve().parents[4];O=R/'art/drafts/sailor-native67-dense-v1/native67-surface-strain-v1';N=994452
S=O;O=O.parent/'native67-local-delta-mush-v1' if '--corrective' in sys.argv else O
p=np.fromfile(S/'original-world-rest.bin','<f4').reshape(N,3).astype(float);tri=np.fromfile(R/'.tools/native67-bary-weight-packet-v1/triangles.bin','<i4').reshape(-1,3);z=np.load(R/'art/drafts/sailor-native67-dense-v1/native67-collar-weight-v1/mask.npz');mask=z['mask'];region=mask[tri].mean(1)>.25
# Metric is orientation-independent; identical original IDs/topology, no moving alignment fit.
e1=p[tri[:,1]]-p[tri[:,0]];e2=p[tri[:,2]]-p[tri[:,0]];restarea=np.linalg.norm(np.cross(e1,e2),axis=1);valid=restarea>1e-9;restedge=np.linalg.norm(e1,axis=1);validedge=restedge>.0002
report={'scope':'Triangle area ratio and first indexed edge stretch. Collar region is empirical rest mask. Real cloth folds may compress; not anatomical acceptance. Tiny triangles/edges excluded for conditioning. No posed fit.','restSHA256':hashlib.sha256((S/'original-world-rest.bin').read_bytes()).hexdigest(),'regionTriangles':int(region.sum()),'validRegionTriangles':int((region&valid).sum()),'poses':[]}
for file in sorted(O.glob('action-*-world-positions.bin')):
 q=np.fromfile(file,'<f4').reshape(N,3).astype(float);a=q[tri[:,1]]-q[tri[:,0]];b=q[tri[:,2]]-q[tri[:,0]];area=np.linalg.norm(np.cross(a,b),axis=1);ratio=area[region&valid]/restarea[region&valid];stretch=np.linalg.norm(a,axis=1)[region&validedge]/restedge[region&validedge];report['poses'].append({'name':file.stem,'areaRatioQuantiles':np.quantile(ratio,[0,.01,.1,.5,.9,.99,1]).tolist(),'areaBelowHalfFraction':float(np.mean(ratio<.5)),'edgeStretchQuantiles':np.quantile(stretch,[.01,.1,.5,.9,.99]).tolist()})
assert len(report['poses'])==6;(O/'strain-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
