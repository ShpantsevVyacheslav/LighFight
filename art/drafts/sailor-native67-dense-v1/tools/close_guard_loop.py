from pathlib import Path
import numpy as np,json,shutil
R=Path(__file__).resolve().parents[4];S=R/'art/drafts/sailor-native67-dense-v1/native67-zero-twist-web-guard-v2';O=S.parent/'native67-zero-twist-web-guard-loop-v1';O.mkdir(exist_ok=False)
for f in S.iterdir():
 if f.is_file():shutil.copy2(f,O/f.name)
r=json.loads((S/'export.json').read_text());clip=r['clips'][0];a=np.fromfile(S/clip['file'],'<f4').reshape(clip['frames'],len(r['nodes']),10);k=24;out=a[k:].copy()
for i in range(k+1):
 t=i/k;w=t*t*(3-2*t);idx=len(out)-k-1+i;tail=out[idx].copy();first=a[i]
 # TRS channels: no linear quaternion blend without normalization/hemisphere fix.
 out[idx,:,:3]=(1-w)*tail[:,:3]+w*first[:,:3];out[idx,:,7:]=(1-w)*tail[:,7:]+w*first[:,7:]
 q=tail[:,3:7];b=first[:,3:7].copy();dot=(q*b).sum(1);b[dot<0]*=-1;dot=np.clip(abs(dot),0,1);theta=np.arccos(dot);den=np.sin(theta);near=den<1e-5;v=(1-w)*q+w*b;ok=~near;v[ok]=(np.sin((1-w)*theta[ok])/den[ok])[:,None]*q[ok]+(np.sin(w*theta[ok])/den[ok])[:,None]*b[ok];v/=np.linalg.norm(v,axis=1)[:,None];out[idx,:,3:7]=v
# Same physical pose at both ends; quaternion sign is irrelevant.
assert np.max(abs(out[0,:,:3]-out[-1,:,:3]))<1e-8;assert np.min(abs((out[0,:,3:7]*out[-1,:,3:7]).sum(1)))>.999999
out.astype('<f4').tofile(O/'Guard_Loop.bin');clip.update(file='Guard_Loop.bin',duration=clip['duration']*(len(out)-1)/(len(a)-1),frames=len(out));r['guardScope']='Authored Meshy Boxing Guard retargeted to native67, derived24-key cyclic seam overlap (~250ms). Original guard FBX untouched.';(O/'export.json').write_text(json.dumps(r,indent=2));(O/'loop-derivation.json').write_text(json.dumps({'source':str(S),'sourceFrames':len(a),'overlapKeys':k,'loopFrames':len(out),'duration':clip['duration'],'positionEndpointMax':float(np.max(abs(out[0,:,:3]-out[-1,:,:3]))),'quaternionHemisphereInvariantEndpoint':True,'method':'Tail overlaps original first24 keys; loop starts at source key24, making endpoint pose/derivative path cyclic. Local TRS interpolation, not source-curve edit.'},indent=2));print(O)
