"""Read-only audit of NEW autoheat/transfer fields, not old native rig."""
import bpy,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1'
bpy.ops.wm.open_mainfile(filepath=str(O/'stage02-bound-original-dense.blend'))
b=bpy.data.objects['Sailor_OriginalDense_UNTOUCHED'];co=np.empty(len(b.data.vertices)*3,np.float32);b.data.vertices.foreach_get('co',co);co=co.reshape(-1,3);mw=np.array(b.matrix_world);co=co@mw[:3,:3].T+mw[:3,3]
names=[g.name for g in b.vertex_groups];arm={i for i,n in enumerate(names) if any(s in n.lower() for s in ['shoulder','upper_arm','forearm','hand','palm','thumb','index','middle','ring','pinky'])};x=np.abs(co[:,0]);z=co[:,2]
regions={'collarCentral':(x<.09)&(z>.79)&(z<.85),'axillaryTorso':(x>.07)&(x<.13)&(z>.65)&(z<.77),'outerSleeveCap':(x>.12)&(x<.20)&(z>.72)&(z<.84),'sleeveLower':(x>.15)&(z>.55)&(z<.72)}
report={}
for region,mask in regions.items():
 ids=np.flatnonzero(mask);weights=[];groupMean={}
 for i in ids:
  v=b.data.vertices[int(i)];weights.append(sum(g.weight for g in v.groups if g.group in arm))
  for g in v.groups:groupMean[names[g.group]]=groupMean.get(names[g.group],0)+g.weight
 a=np.array(weights);report[region]={'vertices':len(ids),'armInfluenceMean':float(a.mean()),'armInfluenceP95':float(np.quantile(a,.95)),'armWeightOver05Vertices':int(np.count_nonzero(a>.05)),'meanGroups':sorted([(n,w/len(ids)) for n,w in groupMean.items()],key=lambda x:-x[1])[:12]}
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE' and o.name!='FreshSailor_CONTROL_METARIG_v1');report['gameExportDeformationAudit']={'segmentedDeformBones':{b.name:b.bbone_segments for b in rig.data.bones if b.use_deform and b.bbone_segments>1},'modifiers':[{ 'type':m.type,'preserveVolume':getattr(m,'use_deform_preserve_volume',None)} for m in b.modifiers]};(O/'newrig-partition-audit.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)
