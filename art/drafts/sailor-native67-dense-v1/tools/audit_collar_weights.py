import os
os.environ['OPENBLAS_NUM_THREADS']='6'
import numpy as np,json,hashlib,shutil,sys
from pathlib import Path
mode='clavicle' if '--clavicle' in sys.argv else 'upperarm'
R=Path(__file__).resolve().parents[4];S=R/'.tools/native67-bary-weight-packet-v1';O=R/('art/drafts/sailor-native67-dense-v1/native67-collar-clavicle-weight-v1' if mode=='clavicle' else 'art/drafts/sailor-native67-dense-v1/native67-collar-weight-v1');O.mkdir(exist_ok=False)
names=(S/'bones.txt').read_text().splitlines();p=np.fromfile(S/'positions.bin','<f4').reshape(-1,3);m=np.fromfile(S/'meshWorld.bin','<f4').reshape(4,4);p=p@m[:3,:3].T+m[:3,3];rest=np.fromfile(S/'rest.bin','<f4').reshape(67,4,4);center=rest[names.index('UpperChest'),0,3];neck=rest[names.index('Neck'),2,3];arm=rest[names.index('Left_UpperArm'),2,3]
counts=np.fromfile(S/'counts.bin','u1');ix=np.fromfile(S/'indices.bin','<i4');w=np.fromfile(S/'weights.bin','<f4');rows=np.repeat(np.arange(len(p)),counts);W=np.zeros((len(p),67),np.float32);W[rows,ix]=w
x=np.abs(p[:,0]-center);z=p[:,2]
def smooth(a,b,v):
 t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
mask=smooth(.012,.032,x)*(1-smooth(.075,.115,x))*smooth(arm-.008,arm+.02,z)*(1-smooth(neck-.004,neck+.022,z))
region=mask>.25;arms=[names.index(n) for n in (['Left_Shoulder','Right_Shoulder'] if mode=='clavicle' else ['Left_UpperArm','Right_UpperArm'])];mass=W[:,arms].sum(1);before=W.copy();transfer=np.zeros(len(p),np.float32)
for b in arms:
 delta=W[:,b]*(.6*mask);W[:,b]-=delta;transfer+=delta
W[:,names.index('UpperChest')]+=transfer
assert np.all(W>=0);assert np.max(abs(W.sum(1)-before.sum(1)))<3e-7;assert np.all(W[before>0]>0);assert np.array_equal(W[mask==0],before[mask==0])
P=R/('.tools/native67-collar-clavicle-weight-packet-v1' if mode=='clavicle' else '.tools/native67-collar-weight-packet-v1');P.mkdir(exist_ok=False)
for f in S.iterdir():
 if f.name not in ['counts.bin','indices.bin','weights.bin','manifest.json']:shutil.copy2(f,P/f.name)
a,b=np.nonzero(W);newcounts=np.count_nonzero(W,axis=1).astype('u1');newcounts.tofile(P/'counts.bin');b.astype('<i4').tofile(P/'indices.bin');W[a,b].astype('<f4').tofile(P/'weights.bin')
report={'mode':mode,'status':'EXPERIMENT_PENDING_SURFACE_REVIEW','vertices':len(p),'regionVertices':int(region.sum()),'regionSelectedBoneMassQuantiles':np.quantile(mass[region],[0,.5,.9,.99,1]).tolist(),'regionVerticesSelectedMassAbove025':int(np.sum(region&(mass>.25))),'changedVertices':int(np.sum(transfer>0)),'maxTransferredMass':float(transfer.max()),'originalPositiveSupportPreserved':True,'outsideMaskByteEqual':True,'method':'Bounded smooth rest-coordinate collar ROI; transfer at most60% of selected influence (see mode) to UpperChest. Original positive influences retained. Not anatomical segmentation, not accepted rig. No mesh/bind/animation edits.','packet':str(P)}
(O/'audit.json').write_text(json.dumps(report,indent=2));np.savez_compressed(O/'mask.npz',positions=p,mask=mask,armMass=mass,transferred=transfer)
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest();(P/'manifest.json').write_text(json.dumps({'kind':'EXPERIMENT_COLLAR_UPPERARM_TO_CHEST','parent':str(S),'files':{f.name:sha(f) for f in P.iterdir() if f.is_file()}},indent=2));print(json.dumps(report,indent=2))
