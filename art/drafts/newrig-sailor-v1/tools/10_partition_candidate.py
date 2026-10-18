"""NEW game-LBS sleeve/torso partition candidate. No geometric sculpt/blur.
Clavicle is capped progressively in central collar, not erased globally.
"""
import bpy,json,hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[4];O=ROOT/'art/drafts/newrig-sailor-v1';D=O/'partition-v1';D.mkdir(exist_ok=True)
s=(O/'tools/05_full_motion_gallery.py').read_text();exec(compile(s[:s.index('reports=[]')],str(O/'tools/05_full_motion_gallery.py'),'exec'))
for b in rig.data.bones:
 if b.use_deform:b.bbone_segments=1
assert not body.modifiers[0].use_deform_preserve_volume
N=len(body.data.vertices);co=np.empty(N*3,np.float32);body.data.vertices.foreach_get('co',co);co=co.reshape(-1,3);mw=np.array(body.matrix_world);rw=co@mw[:3,:3].T+mw[:3,3];x=np.abs(rw[:,0]);z=rw[:,2]
def sm(t):t=np.clip(t,0,1);return t*t*(3-2*t)
collar=sm((z-.76)/.04)*(1-sm((z-.835)/.020))*(1-sm((x-.05)/.075))
torso=sm((z-.62)/.05)*(1-sm((z-.76)/.025))*(1-sm((x-.07)/.06))
mask=np.maximum(collar,torso);roi=np.flatnonzero(mask>1e-7);names=[g.name for g in body.vertex_groups];arm=[i for i,n in enumerate(names) if any(t in n.lower() for t in ['shoulder','upper_arm','forearm','hand','palm','thumb','index','middle','ring','pinky'])]
w=np.zeros((len(roi),len(names)),np.float32)
for k,i in enumerate(roi):
 for g in body.data.vertices[int(i)].groups:w[k,g.group]=g.weight
before=w.copy();at=w[:,arm].sum(1);cap=np.where(collar[roi]>=torso[roi],.20,.05);desired=at*(1-mask[roi])+np.minimum(at,cap)*mask[roi];factor=np.divide(desired,at,out=np.ones_like(at),where=at>0);w[:,arm]*=factor[:,None];removed=at-desired;chest=sm((z[roi]-.70)/.07)
w[:,names.index('DEF-spine.002')]+=removed*(1-chest);w[:,names.index('DEF-spine.003')]+=removed*chest
top=np.argpartition(w,-4,axis=1)[:,-4:];limited=np.zeros_like(w);np.put_along_axis(limited,top,np.take_along_axis(w,top,axis=1),axis=1);w=limited/np.maximum(limited.sum(1,keepdims=True),1e-20)
assert np.min(w.sum(1))>.99999 and np.max(np.count_nonzero(w>0,axis=1))<=4
# Visual mask and actual original influence on REST geometry; debugmaterial temporary.
R.reset_pose(rig);oldMaterials=list(body.data.materials);attr=body.data.color_attributes.new(name='PartitionAudit',type='FLOAT_COLOR',domain='POINT');colors=np.ones((N,4),np.float32);colors[:,:3]=.22;colors[roi,0]=mask[roi];colors[roi,1]=1-mask[roi];colors[roi,2]=.05;attr.data.foreach_set('color',colors.ravel());material=bpy.data.materials.new('DEBUG_PARTITION_MASK');material.use_nodes=True;nt=material.node_tree;nt.nodes.clear();out=nt.nodes.new('ShaderNodeOutputMaterial');em=nt.nodes.new('ShaderNodeEmission');a=nt.nodes.new('ShaderNodeAttribute');a.attribute_name=attr.name;nt.links.new(a.outputs['Color'],em.inputs['Color']);nt.links.new(em.outputs[0],out.inputs['Surface']);body.data.materials.clear();body.data.materials.append(material)
scene.render.resolution_x=768;scene.render.resolution_y=768;target=Vector((0,0,.76));cam.location=target+Vector((0,-2,.03));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=.56;scene.render.filepath=str(D/'rest-mask-red-treatment-green-feather.png');bpy.ops.render.render(write_still=True)
body.data.materials.clear()
for m in oldMaterials:body.data.materials.append(m)
body.data.color_attributes.remove(attr)
def capture(label):
 for phase in [.5,.75]:
  retarget('dance_01',phase);target=rig.matrix_world@((rig.pose.bones['ORG-upper_arm.L'].head+rig.pose.bones['ORG-upper_arm.R'].head)*.5);target.z+=.025;cam.location=target+Vector((.8,-2,.14));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=.64;scene.render.filepath=str(D/f'{label}-dance-{phase}.png');bpy.ops.render.render(write_still=True)
capture('baseline-trueLBS')
for group in body.vertex_groups:group.remove(roi.tolist())
for k,i in enumerate(roi):
 for j in np.flatnonzero(w[k]>0):body.vertex_groups[int(j)].add([int(i)],float(w[k,j]),'REPLACE')
capture('partition-trueLBS')
np.savez_compressed(D/'partition-v1-weights.npz',indices=roi,names=np.array(names),before=before,after=w,mask=mask[roi])
report={'status':'CANDIDATE_VISUAL_REVIEW_NOT_ACCEPTED','method':'lateral torso/sleeve partition; progressive central collar arm/claviclecap20%, axillary torso5%, smooth regionfeather, removedweight tospine002/003; nogeometrysmoothing','treatmentVertices':int(len(roi)),'maxWeightL1Change':float(np.abs(w-before).sum(1).max()),'meanArmInfluenceBefore':float(at.mean()),'meanArmInfluenceAfter':float(w[:,arm].sum(1).mean()),'headAbove0855ExactWeightPreservation':bool(np.all(mask[z>=.855]==0)),'distalLateralAbove013ExactWeightPreservation':bool(np.all(mask[x>=.13]==0)),'maxInfluences':4,'unweighted':0,'blenderOnlyBBones':False,'note':'Measured arm influence is a hypothesis, not blanket anatomicalleak. No globalclaviclezeroing.'}
(D/'report.json').write_text(json.dumps(report,indent=2));R.reset_pose(rig);bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(O/'stage02-game-lbs-partition-v1.blend'),compress=True);print('RESULT '+json.dumps(report),flush=True)
