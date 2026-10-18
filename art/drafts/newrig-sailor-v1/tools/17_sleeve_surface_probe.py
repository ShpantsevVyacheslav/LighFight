"""Locate visible sleeve peak in source/rest coordinates before changing corrective mask."""
import bpy,json,hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1'
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
for b in rig.data.bones:
 if b.use_deform:b.bbone_segments=1
retarget('dance_01',.75);center=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);center.z+=.04;cam.location=center+Vector((0,-2,.05));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=.65;bpy.context.view_layer.update()
N=len(body.data.vertices);rest=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',rest);rest=rest.reshape(-1,3);mw=np.array(body.matrix_world);rest=rest@mw[:3,:3].T+mw[:3,3];ob=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ob.to_mesh();posed=np.empty(N*3,np.float32);me.vertices.foreach_get('co',posed);posed=posed.reshape(-1,3)@mw[:3,:3].T+mw[:3,3];ob.to_mesh_clear();camera=np.array(cam.matrix_world.inverted());cc=posed@camera[:3,:3].T+camera[:3,3];pixels=np.c_[768*(.5+cc[:,0]/.65),768*(.5-cc[:,1]/.65)]
mask=(pixels[:,0]>580)&(pixels[:,0]<650)&(pixels[:,1]>260)&(pixels[:,1]<315);ids=np.flatnonzero(mask);groups={}
for i in ids:
 for g in body.data.vertices[int(i)].groups:groups[body.vertex_groups[g.group].name]=groups.get(body.vertex_groups[g.group].name,0)+g.weight
metrics={}
for part in ['upper_arm','forearm']:
 b=rig.data.bones['ORG-'+part+'.L'];h=np.array(rig.matrix_world@b.head_local);t=np.array(rig.matrix_world@b.tail_local);axis=t-h;L=np.linalg.norm(axis);axis/=L;a=(rest[ids]-h)@axis/L;metrics[part]={'restAlongQuantiles':np.quantile(a,[0,.1,.5,.9,1]).tolist()}
li=np.empty(len(body.data.loops),np.int32);body.data.loops.foreach_get('vertex_index',li);uv=np.empty(len(li)*2,np.float32);body.data.uv_layers.active.data.foreach_get('uv',uv);uv=uv.reshape(-1,2);first=np.full(N,len(li),np.int32);np.minimum.at(first,li,np.arange(len(li),dtype=np.int32));vuv=uv[first]
P=ROOT/'.tools/portrait-native-glb/newrig-sailor-surface-probe.npz';np.savez_compressed(P,rest=rest.astype(np.float32),posed=posed.astype(np.float32),pixels=pixels.astype(np.float32),uv=vuv,peakIndices=ids)
report={'pixelBox':[580,260,650,315],'vertices':len(ids),'restMetrics':metrics,'meanGroups':sorted([(k,v/len(ids)) for k,v in groups.items()],key=lambda x:-x[1])[:15],'localProbeCache':str(P.relative_to(ROOT))};(O/'anatomy-surface-probe.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps(report),flush=True)
