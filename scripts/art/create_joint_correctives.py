"""Bake localized bend correctives for linear skinning; keep the source intact.
Blender: -- source.blend output-directory
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix,kdtree
source,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE')
obj=next(o for o in scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers));mod=next(m for m in obj.modifiers if m.type=='ARMATURE');mod.use_deform_preserve_volume=False
base=[v.co.copy() for v in obj.data.vertices];clusters={}
for i,v in enumerate(base):clusters.setdefault(tuple(round(c,4) for c in v),[]).append(i)
ids=list(clusters.values());lookup={v:i for i,vs in enumerate(ids) for v in vs};neighbors=[set() for _ in ids]
for edge in obj.data.edges:
 a,b=map(lookup.get,edge.vertices)
 if a!=b:neighbors[a].add(b);neighbors[b].add(a)
obj.shape_key_add(name='Basis');report=[]
for name,parent,end,radius in [('LeftElbow','LeftArm','LeftHand',14),('RightElbow','RightArm','RightHand',14),('LeftKnee','LeftUpLeg','LeftFoot',13)]:
 joint={'LeftElbow':'LeftForeArm','RightElbow':'RightForeArm','LeftKnee':'LeftLeg'}[name]
 samples=[]
 for frame in range(1,43):
  scene.frame_set(frame);a,b,c=[arm.pose.bones[n].head.copy() for n in [parent,joint,end]]
  angle=180-math.degrees((a-b).angle(c-b));samples.append((angle,frame))
 angle,frame=max(samples);scene.frame_set(frame)
 evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());em=evaluated.to_mesh();posed=[v.co.copy() for v in em.vertices];evaluated.to_mesh_clear()
 points=[sum((posed[i] for i in vs),Vector())/len(vs) for vs in ids]
 center=arm.data.bones[joint].head_local;mask=[max(0,1-(base[vs[0]]-center).length/radius)**2 for vs in ids]
 # Low-pass the bend crease, using welded adjacency across UV seams.
 for _ in range(6):
  for strength in [.5,-.53]:
   result=[]
   for i,p in enumerate(points):
    if not neighbors[i]:result.append(p);continue
    mean=sum((points[j] for j in neighbors[i]),Vector())/len(neighbors[i]);result.append(p+(mean-p)*strength*mask[i])
   points=result
 matrices={g.index:arm.pose.bones[g.name].matrix@arm.data.bones[g.name].matrix_local.inverted() for g in obj.vertex_groups if g.name in arm.data.bones}
 key=obj.shape_key_add(name=name);maximum=0;changed=0
 for v in obj.data.vertices:
  delta=points[lookup[v.index]]-posed[v.index]
  if mask[lookup[v.index]]<.001:delta=Vector()
  blend=Matrix(((0,0,0,0),)*4)
  for g in v.groups:
   if g.group in matrices:blend+=matrices[g.group]*g.weight
  correction=blend.to_3x3().inverted_safe()@delta
  # Never let automatic relaxation make a large sculpt change.
  if correction.length>1.5:correction*=1.5/correction.length
  key.data[v.index].co=base[v.index]+correction
  maximum=max(maximum,correction.length*.01);changed+=correction.length>1e-5
 # Synchronize the corrective on coincident UV vertices, too.
 for vs in ids:
  mean=sum((key.data[i].co for i in vs),Vector())/len(vs)
  for i in vs:key.data[i].co=mean
 report.append({'shape':name,'referenceBendDegrees':angle,'referenceFrame':frame,'changedVertices':changed,'maxDeltaMeters':maximum})
 key.value=0
# Mild left-calf symmetry target, confined away from knee, ankle and shoes.
right=[v for v in obj.data.vertices if v.co.x<-4 and 12<v.co.z<45];tree=kdtree.KDTree(len(right))
for i,v in enumerate(right):tree.insert(Vector((-v.co.x,v.co.y,v.co.z)),i)
tree.balance();key=obj.shape_key_add(name='LeftCalfContour');maximum=0
for vs in ids:
 i=vs[0];p=base[i];delta=Vector()
 if p.x>4 and 15<p.z<42:
  target,index,distance=tree.find(p);factor=math.sin(math.pi*(p.z-15)/27)**2*.5;delta=(target-p)*factor
  if delta.length>1.2:delta*=1.2/delta.length
 for j in vs:key.data[j].co=base[j]+delta
 maximum=max(maximum,delta.length*.01)
deltas=[key.data[vs[0]].co-base[vs[0]] for vs in ids]
for _ in range(10):
 deltas=[d*.3+sum((deltas[j] for j in neighbors[i]),Vector())/len(neighbors[i])*.7 if neighbors[i] and 15<base[ids[i][0]].z<42 and base[ids[i][0]].x>4 else Vector() for i,d in enumerate(deltas)]
maximum=0
for index,vs in enumerate(ids):
 delta=deltas[index];maximum=max(maximum,delta.length*.01)
 for i in vs:key.data[i].co=base[i]+delta
report.append({'shape':'LeftCalfContour' ,'maxDeltaMeters':maximum,'runtimeWeight':100,'status':'mild mirrored surface candidate; not anatomical certification'})
(out/'corrective-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'joint-correctives.blend'))
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'island-joint-correctives.fbx'),use_selection=True,use_mesh_modifiers=False,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
