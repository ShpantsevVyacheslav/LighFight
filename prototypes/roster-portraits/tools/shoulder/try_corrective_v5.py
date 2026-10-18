import bpy,json,math,hashlib,sys
from pathlib import Path
from mathutils import Vector,kdtree
R=Path(__file__).resolve().parents[4];O=R/'.tools/portrait-native-glb/shoulder-fix/v5';M=R/'prototypes/roster-portraits/models';role=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'sailor';O=O if role=='sailor' else O/role;O.mkdir(exist_ok=True);manifest=json.loads((M/f'{role}.manifest.json').read_text());source=R/manifest['sourceFBX'];sourceHash=hashlib.sha256(source.read_bytes()).hexdigest();glbHash=hashlib.sha256((M/f'{role}.glb').read_bytes()).hexdigest()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(M/f'{role}.glb'));go=list(bpy.context.scene.objects);ga=list(bpy.data.actions);gar=next(o for o in go if o.type=='ARMATURE');gm=[o for o in go if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)];before=set(bpy.data.objects);abefore=set(bpy.data.actions);bpy.ops.import_scene.fbx(filepath=str(source),use_anim=True,automatic_bone_orientation=False);so=[o for o in bpy.data.objects if o not in before];sa=[a for a in bpy.data.actions if a not in abefore];sar=next(o for o in so if o.type=='ARMATURE');sm=[o for o in so if o.type=='MESH'];assert len(sm)==len(gm)==1
scene=bpy.context.scene;gar.animation_data.use_nla=False;sar.animation_data.use_nla=False
for o in go:
 if o.type=='MESH' and o not in gm:o.hide_render=True

def points(meshes):
 deps=bpy.context.evaluated_depsgraph_get();result=[]
 for o in meshes:
  ob=o.evaluated_get(deps);me=ob.to_mesh();result.extend(ob.matrix_world@v.co for v in me.vertices);ob.to_mesh_clear()
 return result

def pose(arm,actions,name,phase):
 a=next(a for a in actions if name in a.name);arm.animation_data.action=a;frame=a.frame_range[0]+phase*(a.frame_range[1]-a.frame_range[0]);scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update();return frame
for arm in [gar,sar]:arm.animation_data.action=None;arm.data.pose_position='REST'
bpy.context.view_layer.update();rest=points(sm);gr=points(gm);tree=kdtree.KDTree(len(rest))
for i,p in enumerate(rest):tree.insert(p,i)
tree.balance();mapping=[tree.find(v)[1] for v in gr];restErr=max((gr[i]-rest[j]).length for i,j in enumerate(mapping));assert restErr<.0001,restErr
for arm in [gar,sar]:arm.data.pose_position='POSE'
mesh=sm[0];region=set();groups={g.index:g.name for g in mesh.vertex_groups}
for v in mesh.data.vertices:
 if any(g.weight>.05 and any(k in groups[g.group].lower().replace('_','') for k in ['upperarm','shoulder']) for g in v.groups):region.add(v.index)
assert region,groups
edges=[(e.vertices[0],e.vertices[1]) for e in mesh.data.edges if e.vertices[0] in region and e.vertices[1] in region and (rest[e.vertices[0]]-rest[e.vertices[1]]).length>.0001]
# Fixed same camera/light framing; source FBX vs finalglTF; no edits to rig/material/weights.
pose(sar,sa,'greet_01',0);p=points(sm);lo=Vector(tuple(min(v[i] for v in p) for i in range(3)));hi=Vector(tuple(max(v[i] for v in p) for i in range(3)));height=hi.z-lo.z;center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z+height*.73));cameraData=bpy.data.cameras.new('audit camera');camera=bpy.data.objects.new('audit camera',cameraData);scene.collection.objects.link(camera);camera.location=center+Vector((0,-height*2.5,height*.03));camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();cameraData.type='ORTHO';cameraData.ortho_scale=height*.70;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.13,.13,.13);scene.view_settings.view_transform='AgX'
for name,pos,power in [('key',(-2,-3,3),500),('fill',(2,-2,1),250),('rim',(0,2,2),350)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=3;l=bpy.data.objects.new(name,d);scene.collection.objects.link(l);l.location=center+Vector(pos)*height/2;l.rotation_euler=(center-l.location).to_track_quat('-Z','Y').to_euler()

import numpy as np
base=sm[0];candidate=base.copy();candidate.data=base.data.copy();scene.collection.objects.link(candidate);candidate.name='ShoulderWeightCandidate';original=np.zeros((len(base.data.vertices),len(base.vertex_groups)),dtype=np.float32)
for v in base.data.vertices:
 for g in v.groups:original[v.index,g.group]=g.weight
coords=np.array([list(v) for v in rest],dtype=np.float32);names=[g.name for g in base.vertex_groups];eligible=[i for i,n in enumerate(names) if any(k in n.lower().replace('_','') for k in ['spine','chest','shoulder','upperarm','lowerarm'])];protected=[i for i,n in enumerate(names) if any(k in n.lower() for k in ['head','neck','hand'])]
mask=np.zeros(len(coords),dtype=np.float32);centers=[]
for side in ['Left','Right']:
 bone=sar.data.bones[side+'_UpperArm'];center=np.array(sar.matrix_world@bone.head_local);length=(sar.matrix_world@bone.tail_local-sar.matrix_world@bone.head_local).length;centers.append((center,length));distance=np.linalg.norm(coords-center,axis=1);t=np.clip((length*1.15-distance)/(length*.60),0,1);mask=np.maximum(mask,t*t*(3-2*t))
mask[original[:,protected].sum(axis=1)>.45]=0
# Collar/central upperchest are Dirichlet anchors; only lateral deltoid/sleeve band is reconstructed.
armLength=sum(x[1] for x in centers)/2;centerX=sum(float(x[0][0]) for x in centers)/2;neck=np.array(sar.matrix_world@sar.data.bones['Neck'].head_local);lateral=np.abs(coords[:,0]-centerX);t=np.clip((lateral-armLength*.52)/(armLength*.28),0,1);lateralFeather=t*t*(3-2*t);z=np.clip((coords[:,2]-(neck[2]-armLength*.55))/(armLength*.22),0,1);neckBand=z*z*(3-2*z);mask*=1-neckBand*(1-lateralFeather)
neckGroups=[i for i,n in enumerate(names) if 'neck' in n.lower()];neckWeight=original[:,neckGroups].sum(axis=1);outer=(lateral>armLength*.8)&(mask>.1);leak={'outerShoulderVertices':int(outer.sum()),'outerNeckWeightMean':float(neckWeight[outer].mean()),'outerNeckWeightP95':float(np.quantile(neckWeight[outer],.95)),'outerNeckWeightOver01':int(np.count_nonzero(neckWeight[outer]>.1))};print('NECK_DIAG '+json.dumps(leak),flush=True)
roi=np.where(mask>0)[0];local=np.full(len(coords),-1,dtype=np.int32);local[roi]=np.arange(len(roi));e=np.array([tuple(e.vertices) for e in base.data.edges],dtype=np.int32);dst=np.r_[e[:,0],e[:,1]];src=np.r_[e[:,1],e[:,0]];keep=local[dst]>=0;dst=local[dst[keep]];src=src[keep];degree=np.bincount(dst,minlength=len(roi));X=original[:,eligible].copy();steps=0;results=[]
# Baseline render pair in same source rig, camera and material.
def follow(phase):
 pose(sar,sa,'dance_01',phase);head=next(b for b in sar.pose.bones if b.name=='Head');chest=next(b for b in sar.pose.bones if b.name=='UpperChest');c=sar.matrix_world@((head.head+chest.head)*.5);c.z-=height*.08;camera.location=c+Vector((0,-height*2.5,height*.03));camera.rotation_euler=(c-camera.location).to_track_quat('-Z','Y').to_euler()
for o in gm:o.hide_render=True
for phase in [.5,.75]:
 follow(phase);candidate.hide_render=True;base.hide_render=False;scene.render.filepath=str(O/f'baseline-{phase}.png');bpy.ops.render.render(write_still=True)


# Diagnostic only: localized rest-preserving correction, not an export modifier.
vg=candidate.vertex_groups.new(name='LocalizedShoulderCorrective')
for i in roi: vg.add([int(i)],float(mask[i]),'REPLACE')
bpy.context.view_layer.objects.active=candidate
candidate.select_set(True)
mod=candidate.modifiers.new('LocalizedCorrectiveDiagnostic','CORRECTIVE_SMOOTH')
mod.vertex_group=vg.name
mod.rest_source='BIND'
mod.smooth_type='LENGTH_WEIGHTED'
mod.iterations=10
mod.factor=.5
sar.data.pose_position='REST';bpy.context.view_layer.update()
bpy.ops.object.correctivesmooth_bind(modifier=mod.name)
sar.data.pose_position='POSE';bpy.context.view_layer.update()
assert mod.is_bind
for factor,it in [(.5,10),(1.,20)]:
 mod.factor=factor;mod.iterations=it;base.hide_render=True;candidate.hide_render=False
 for phase in [.5,.75]:
  follow(phase);scene.render.filepath=str(O/f'corrective-{factor}-{it}-{phase}.png');bpy.ops.render.render(write_still=True)
print('CORRECTIVE_DIAGNOSTIC_COMPLETE',flush=True)
