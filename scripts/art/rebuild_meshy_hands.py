"""Rebuild separate five-digit hand surfaces; Blender source.blend output-dir.
Draft local retopology, source preserved. No provider API requests.
"""
import bpy,bmesh,sys,math,json
from pathlib import Path
from mathutils import Vector,Matrix
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src));scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH' and o.data.shape_keys)
action=arm.animation_data.action;arm.animation_data.action=None
for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
scene.frame_set(1);bpy.context.view_layer.update()
mat=bpy.data.materials.new('RebuiltHandSkin');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.58,.36,.29,1);bs.inputs['Roughness'].default_value=.57
hands=[];specs=[]
for side,sign in [('Left',1),('Right',-1)]:
 wrist=arm.data.bones[side+'Hand'].head_local.copy();parts=[]
 def sphere(c,sc):
  bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=12,location=wrist+Vector((sign*c[0],c[1],c[2])))
  p=bpy.context.object;p.scale=sc;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);parts.append(p)
 def capsule(a,b,r):
  a,b=Vector(a),Vector(b);d=b-a;mid=(a+b)*.5
  sphere(mid,(r,r,r));p=parts[-1];p.rotation_euler=(Vector((sign*d.x,d.y,d.z))).to_track_quat('Z','Y').to_euler();p.scale=(1,1,(d.length*.5+r)/r)
  bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 sphere((5.8,.6,0),(4.8,3.8,1.65));sphere((1.6,.2,0),(2.4,2.7,1.55))
 digit=[]
 for name,y,length,r in [('Index',-1.9,7.7,.88),('Middle',.15,8.4,.96),('Ring',2.25,7.7,.91),('Pinky',4.1,6.1,.76)]:
  start=Vector((9.4,y,0));direction=Vector((1,(y-.6)*.023,0)).normalized();points=[start+direction*length*t for t in [0,.46,.76,1]]
  for j in range(3):capsule(points[j],points[j+1],r*(1-.10*j))
  digit.append((name,points,r))
 # Thumb leaves radial edge at a distinct angle, with two phalanges.
 pts=[Vector((4.3,-2.7,-.25)),Vector((6.1,-4.8,-.35)),Vector((8.8,-5.4,-.25))]
 for j in range(2):capsule(pts[j],pts[j+1],1.0-.13*j)
 digit.append(('Thumb',pts,1.0))
 bpy.ops.object.select_all(action='DESELECT')
 for p in parts:p.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);bpy.ops.object.join();hand=parts[0];hand.name='Rebuilt'+side+'Hand'
 rem=hand.modifiers.new('UnionPalmDigits','REMESH');rem.mode='VOXEL';rem.voxel_size=.22;bpy.ops.object.modifier_apply(modifier=rem.name)
 sm=hand.modifiers.new('RelaxSurface','SMOOTH');sm.factor=.4;sm.iterations=3;bpy.ops.object.modifier_apply(modifier=sm.name)
 for f in hand.data.polygons:f.use_smooth=True
 hand.data.materials.clear();hand.data.materials.append(mat)
 # Add anatomically separated digit chains to the existing wrist bone.
 bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 chains=[]
 for name,points,r in digit:
  chain=[]
  for j in range(len(points)-1):
   bn=side+name+str(j+1);b=arm.data.edit_bones.new(bn);b.head=wrist+Vector((sign*points[j].x,points[j].y,points[j].z));b.tail=wrist+Vector((sign*points[j+1].x,points[j+1].y,points[j+1].z));b.parent=arm.data.edit_bones[chain[-1] if chain else side+'Hand'];b.use_connect=bool(chain);chain.append(bn)
  chains.append((name,points,chain,r))
 bpy.ops.object.mode_set(mode='OBJECT')
 groups={n:hand.vertex_groups.new(name=n) for n in [side+'Hand']+[n for _,_,c,_ in chains for n in c]}
 for v in hand.data.vertices:
  loc=v.co-wrist;loc.x*=sign;scores=[]
  for name,points,chain,r in chains:
   # Distal palm remains controlled by wrist; blend into digit root over 1.2 cm.
   if name=='Thumb':influence=max(0,min(1,(-loc.y-2.7)/1.5))
   else:influence=max(0,min(1,(loc.x-8.5)/1.4))
   for j,n in enumerate(chain):
    a,b=points[j:j+2];d=b-a;t=max(0,min(1,(loc-a).dot(d)/d.length_squared));distance=(loc-(a+d*t)).length
    scores.append((distance,n,influence))
  scores.sort();best=scores[:2];influence=best[0][2]
  if influence<1:groups[side+'Hand'].add([v.index],1-influence,'REPLACE')
  den=sum(1/(s[0]+.15)**3 for s in best)
  for distance,n,_ in best:
   if influence>0:groups[n].add([v.index],influence/(distance+.15)**3/den,'REPLACE')
 hand.matrix_world=body.matrix_world.copy();mod=hand.modifiers.new('DigitSkin','ARMATURE');mod.object=arm
 # Capture a closed pose as a shape, while retaining explicit finger bones.
 for name,points,chain,r in chains:
  for j,n in enumerate(chain):
   p=arm.pose.bones[n];p.rotation_mode='XYZ'
   # Rest bones point along local +Y; local X bends fingers under the palm.
   p.rotation_euler.x=math.radians(-[65,95,65][j] if name!='Thumb' else -[20,45][j])
   if name=='Thumb':p.rotation_euler.z=sign*math.radians([55,20][j])
 bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();ev=hand.evaluated_get(deps);posed=ev.to_mesh();closed=[v.co.copy() for v in posed.vertices];ev.to_mesh_clear()
 for _,_,chain,_ in chains:
  for n in chain:arm.pose.bones[n].matrix_basis=Matrix.Identity(4)
 hand.shape_key_add(name='Basis');key=hand.shape_key_add(name='ClosedFists')
 for i,p in enumerate(closed):key.data[i].co=p
 key.value=0;hands.append(hand);specs.append({'side':side,'digits':5,'fingerBones':sum(len(c) for _,_,c,_ in chains),'vertices':len(hand.data.vertices)})
# Save standalone hands first: integration follows after visual acceptance.
arm.animation_data.action=action;scene.frame_set(1);bpy.context.view_layer.update()
body.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(out/'separate-fingers.blend'))
scene.cycles.samples=12;scene.render.resolution_x=800;scene.render.resolution_y=600
arm.data.pose_position='REST';wrist=arm.data.bones['LeftHand'].head_local
focus=arm.matrix_world@(wrist+Vector((8,0,0)));camera=scene.camera;camera.location=focus+Vector((.2,-.9,.6));camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=.27
hands[1].hide_render=True
for label,val in [('open',0),('closed',1)]:
 hands[0].data.shape_keys.key_blocks['ClosedFists'].value=val;bpy.context.view_layer.update();scene.render.filepath=str(out/(label+'.png'));bpy.ops.render.render(write_still=True)
report={'stage':'standalone hand topology and digit rig, integration pending','hands':specs,'bones':len(arm.data.bones),'sourcePreserved':True,'creditsSpent':0}
(out/'hand-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
