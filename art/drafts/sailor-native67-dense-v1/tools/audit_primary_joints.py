"""Offline actual joint-vector correspondence, not weight diagnosis."""
import json,math
from pathlib import Path
import numpy as np
O=Path(__file__).resolve().parents[1];R=O.parents[2];A=O/'actual-primary-v1';r=json.loads((A/'report.json').read_text());meta=json.loads((O/'offline-source-audit.json').read_text());p=R/'art/drafts/newrig-sailor-v1/dense-def-fbx-packet-v1';npz=np.load(p/'original-indexed-attributes.npz');names=json.loads((p/'manifest.json').read_text())['boneNames'];mw=npz['armatureWorldBlender'];rests=npz['DEFRestArmature'];b=np.array(r['nativeTrueBindWitness']['fileGlobalToBlenderWorld']);native={n:(b@np.r_[v,1])[:3] for n,v in meta['nativeDonor']['boneBindHeadsFileUnits'].items()};rig={n:(mw@rests[i])[:3,3] for i,n in enumerate(names)}
def angle(a,b):return float(np.degrees(np.arccos(np.clip(np.dot(a,b)/(np.linalg.norm(a)*np.linalg.norm(b)),-1,1))))
# Original DEF heads span same joint locations as semantic ORG heads; proof not claimed for tail axes.
rmap={'UpperChest':'DEF-spine.003','Neck':'DEF-spine.004'}
for side in ['Right','Left']:
 suffix='R' if side=='Right' else 'L'
 for n,stem in [('Shoulder','shoulder'),('UpperArm','upper_arm'),('LowerArm','forearm'),('Hand','hand')]:rmap[side+'_'+n]='DEF-'+stem+'.'+suffix
out={'method':'Actual evaluated semantic head vectors; rest heads from raw native clusters and original Rigify DEF rest. Pose mismatch is not skin-weight error.','limits':['Raw callback equality proves no C1, not native-vsRigify pose equivalence.','Rest head vectors differ anatomically; angular delta here is absolute world direction, not local bone quaternion error.','World -X camera depth is X coordinate; camera follows shared target.'], 'poses':[]}
for row in r['poses']:
 c=row['captures'];nj=np.array(c[0]['worldJointPositions']);rj=np.array(c[3]['worldJointPositions']);d={'phase':row['phase'],'limbs':[],'neckToShoulder':[]};labels=c[0]['semanticLabels']
 for side,base in [('Right',2),('Left',6)]:
  for segment,i,j in [('upperarm',base+1,base+2),('forearm',base+2,base+3)]:
   nv=nj[j]-nj[i];rv=rj[j]-rj[i];nr=native[labels[j]]-native[labels[i]];rr=rig[rmap[labels[j]]]-rig[rmap[labels[i]]]
   d['limbs'].append({'side':side,'segment':segment,'nativeVectorM':nv.tolist(),'rigifyVectorM':rv.tolist(),'worldDirectionDeltaDeg':angle(nv,rv),'nativeLengthMm':float(np.linalg.norm(nv)*1000),'rigifyLengthMm':float(np.linalg.norm(rv)*1000),'nativePoseVsRestDeg':angle(nv,nr),'rigifyPoseVsRestDeg':angle(rv,rr),'nativeRestLengthMm':float(np.linalg.norm(nr)*1000),'rigifyRestLengthMm':float(np.linalg.norm(rr)*1000),'nativeDepthDeltaMm':float(nv[0]*1000),'rigifyDepthDeltaMm':float(rv[0]*1000)})
  d['neckToShoulder'].append({'side':side,'nativeMm':float(np.linalg.norm(nj[base]-nj[1])*1000),'rigifyMm':float(np.linalg.norm(rj[base]-rj[1])*1000),'nativeUpperarmMinusNeckMm':((nj[base+1]-nj[1])*1000).tolist(),'rigifyUpperarmMinusNeckMm':((rj[base+1]-rj[1])*1000).tolist()})
 out['poses'].append(d)
(A/'joint-correspondence.json').write_text(json.dumps(out,indent=2)+'\n')
for d in out['poses']:print(d['phase'],[(x['side'],x['segment'],round(x['worldDirectionDeltaDeg'],3)) for x in d['limbs']])
