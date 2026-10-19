"""Fit CC0 MakeHuman hand topology and authored weights to Meshy wrists.
Blender -- Meshy-source.blend downloaded-MakeHuman-dir output-dir
"""
import bpy,bmesh,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
source,assets,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH' and o.data.shape_keys)
rig=json.loads((assets/'default.mhskel').read_text());weight_data=json.loads((assets/'default_weights.mhw').read_text())
assert rig['license']=='CC0' and weight_data['license']=='CC0'
verts=[];faces=[];face_uvs=[];texcoords=[];group=None
for line in (assets/'base.obj').read_text().splitlines():
 if line.startswith('v '):verts.append(Vector(map(float,line.split()[1:])))
 elif line.startswith('vt '):texcoords.append(tuple(map(float,line.split()[1:3])))
 elif line.startswith('g '):group=line.split()[1]
 elif line.startswith('f ') and group=='body':
  tokens=line.split()[1:];faces.append([int(x.split('/')[0])-1 for x in tokens]);face_uvs.append([texcoords[int(x.split('/')[1])-1] for x in tokens])
def joint(name):
 ids=rig['joints'][name];return sum((verts[i] for i in ids),Vector())/len(ids)
def bonepoint(name,key):return joint(rig['bones'][name][key])
action=arm.animation_data.action;arm.animation_data.action=None
for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
mat=bpy.data.materials.new('MakeHumanStudySkin');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.58,.36,.29,1);bs.inputs['Roughness'].default_value=.55
hands=[];audit=[]
for side,suffix,sign in [('Left','.L',1),('Right','.R',-1)]:
 original=bonepoint('wrist'+suffix,'head');u=(bonepoint('finger3-1'+suffix,'head')-original).normalized()
 v=bonepoint('finger5-1'+suffix,'head')-bonepoint('finger2-1'+suffix,'head');v=(v-u*v.dot(u)).normalized();z=u.cross(v).normalized()
 # Mirrored right frame must have the same dorsal direction as left.
 if sign<0:z=-z
 scale=11.4;wrist=arm.data.bones[side+'Hand'].head_local.copy()
 def local(p):
  d=p-original;return Vector((d.dot(u)*scale,d.dot(v)*scale,d.dot(z)*scale))
 def fitted(p):
  l=local(p);return wrist+Vector((sign*l.x,l.y,l.z))
 selected=[n for n in rig['bones'] if n.endswith(suffix) and n.startswith(('finger','metacarpal','wrist'))]
 per_vertex={}
 for n in selected:
  for index,value in weight_data['weights'].get(n,[]):per_vertex.setdefault(index,[]).append((n,value))
 support={i for i,weights in per_vertex.items() if sum(w for _,w in weights)>.01}
 chosen_indices=[j for j,f in enumerate(faces) if any(i in support for i in f) and all((verts[i]-original).length<2.6 for i in f)];chosen=[faces[j] for j in chosen_indices]
 ids=sorted({i for f in chosen for i in f});mapping={i:k for k,i in enumerate(ids)}
 mesh=bpy.data.meshes.new('MakeHuman'+side+'Hand');mesh.from_pydata([fitted(verts[i]) for i in ids],[],[[mapping[i] for i in f] for f in chosen]);mesh.update()
 uv_layer=mesh.uv_layers.new(name='MakeHumanHandUV')
 for polygon,source_face in zip(mesh.polygons,chosen_indices):
  for loop_index,uv in zip(polygon.loop_indices,face_uvs[source_face]):uv_layer.data[loop_index].uv=uv
 hand=bpy.data.objects.new('Rebuilt'+side+'Hand',mesh);bpy.context.collection.objects.link(hand);hand.matrix_world=body.matrix_world.copy();hand.data.materials.append(mat)
 for f in mesh.polygons:f.use_smooth=True
 # Copy source bone heads/tails and their authored bending planes, rather than guessing joint positions.
 names={n:side+'MH_'+n.split('.')[0].replace('-','_') for n in selected};names['wrist'+suffix]=side+'Hand'
 bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for n in selected:
  if n.startswith('wrist'):continue
  b=arm.data.edit_bones.new(names[n]);b.head=fitted(bonepoint(n,'head'));b.tail=fitted(bonepoint(n,'tail'))
  # Local X is the digit flex axis; local Z points dorsally.
  direction=(b.tail-b.head).normalized();b.align_roll(Vector((0,0,1)))
 for n in selected:
  if n.startswith('wrist'):continue
  b=arm.data.edit_bones[names[n]];parent=rig['bones'][n]['parent'];b.parent=arm.data.edit_bones[names.get(parent,side+'Hand')]
 bpy.ops.object.mode_set(mode='OBJECT')
 groups={n:hand.vertex_groups.new(name=names[n]) for n in selected}
 for src_id,i in mapping.items():
  pairs=per_vertex.get(src_id,[('wrist'+suffix,1)]);pairs=sorted(pairs,key=lambda p:-p[1])[:4];total=sum(w for _,w in pairs)
  for n,w in pairs:groups[n].add([i],w/total,'REPLACE')
 # Add radial volume around each authored phalanx axis; preserve digit lengths and joint centers.
 for vertex in mesh.vertices:
  radial=Vector((0,0,0))
  for membership in vertex.groups:
   name=hand.vertex_groups[membership.group].name
   if 'MH_finger' not in name or name.endswith('MH_finger1_1'):continue
   bone=arm.data.bones[name];axis=(bone.tail_local-bone.head_local).normalized()
   delta=vertex.co-bone.head_local;radial+=(delta-axis*delta.dot(axis))*membership.weight*.12
  vertex.co+=radial
 # Trim the extra forearm to a single proximal wrist ring.
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(wrist.x-sign*.1,0,0),plane_no=(sign,0,0),clear_inner=True);bm.to_mesh(mesh);bm.free()
 mod=hand.modifiers.new('SourceHandSkin','ARMATURE');mod.object=arm;mod.use_deform_preserve_volume=False
 # Subdivision follows deformation in final review; capture base closed shape before subdivision.
 for n in selected:
  if not n.startswith('finger'):continue
  digit,phalanx=map(int,n.split('.')[0].replace('finger','').split('-'));p=arm.pose.bones[names[n]];p.rotation_mode='XYZ'
  if digit==1:
   p.rotation_euler.x=math.radians([-65,40,80][phalanx-1]);p.rotation_euler.y=sign*math.radians([-30,0,0][phalanx-1]);p.rotation_euler.z=sign*math.radians([80,0,0][phalanx-1])
   if phalanx==1:p.location=arm.data.bones[names[n]].matrix_local.to_3x3().inverted() @ Vector((0,0,.35))
  else:
   p.rotation_euler.x=math.radians(-[75,95,55][phalanx-1])
   if phalanx==1:
    shift={2:.24,3:0,4:-.16,5:-.35}[digit]
    p.location=arm.data.bones[names[n]].matrix_local.to_3x3().inverted() @ Vector((0,shift,0))
 bpy.context.view_layer.update();ev=hand.evaluated_get(bpy.context.evaluated_depsgraph_get());posed=ev.to_mesh();closed=[v.co.copy() for v in posed.vertices];ev.to_mesh_clear()
 for n in selected:
  if not n.startswith('wrist'):arm.pose.bones[names[n]].matrix_basis=Matrix.Identity(4)
 hand.shape_key_add(name='Basis');k=hand.shape_key_add(name='ClosedFists')
 for i,p in enumerate(closed):k.data[i].co=p
 k.value=0;sub=hand.modifiers.new('HandSubdivision','SUBSURF');sub.levels=2;sub.render_levels=2
 hands.append(hand);audit.append({'side':side,'sourceHandBones':len(selected),'baseVertices':len(mesh.vertices),'baseFaces':len(mesh.polygons),'quads':sum(len(f.vertices)==4 for f in mesh.polygons)})
arm.animation_data.action=action;scene.frame_set(1);body.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(out/'makehuman-hand-study.blend'))
arm.data.pose_position='REST';hands[1].hide_render=True
scene.cycles.samples=16;scene.render.resolution_x=800;scene.render.resolution_y=600
focus=arm.matrix_world@(arm.data.bones['LeftHand'].head_local+Vector((8,0,0)));cam=scene.camera;cam.data.ortho_scale=.27
for pose,value in [('open',0),('closed',1)]:
 hands[0].data.shape_keys.key_blocks['ClosedFists'].value=value
 for view,offset in [('dorsal',(.1,-.5,.9)),('palm',(.1,-.5,-.9)),('side',(.1,-1,.2))]:
  cam.location=focus+Vector(offset);cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();bpy.context.view_layer.update();scene.render.filepath=str(out/(pose+'-'+view+'.png'));bpy.ops.render.render(write_still=True)
report={'source':'MakeHuman CC0 base.obj, default.mhskel and default_weights.mhw','sourceCommit':json.loads((assets/'provenance.json').read_text())['commit'],'hands':audit,'totalBones':len(arm.data.bones),'status':'anatomical-source study, closure and wrist integration not accepted','creditsSpent':0}
(out/'study-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
