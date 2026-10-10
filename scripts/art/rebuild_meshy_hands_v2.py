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
 def surface(name,verts,faces):
  mesh=bpy.data.meshes.new(name);mesh.from_pydata([wrist+Vector((sign*v[0],v[1],v[2])) for v in verts],[],faces);mesh.update()
  part=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(part);parts.append(part)
 def capsule(a,b,r):
  # Explicit tapered elliptical cross-sections, rather than overlapping elongated spheres.
  a,b=Vector(a),Vector(b);axis=(b-a).normalized();across=Vector((-axis.y,axis.x,0)).normalized();up=axis.cross(across).normalized()
  verts=[];faces=[];sections=12;radial=16
  for k in range(sections+1):
   t=k/sections;center=a.lerp(b,t)
   radius=r*(1-.12*t)*(1+.065*math.sin(t*math.pi*2))
   # Rounded ends; middle sections define the actual phalanx contour.
   cap=min(1,.12+min(t,1-t)*8)
   for n in range(radial):
    theta=n/radial*math.tau;p=center+radius*cap*(across*math.cos(theta)+up*.82*math.sin(theta));verts.append(p)
  for k in range(sections):
   for n in range(radial):
    j=k*radial+n;q=k*radial+(n+1)%radial;faces.append((j,q,q+radial,j+radial))
  faces.append(tuple(reversed(range(radial))));faces.append(tuple(sections*radial+n for n in range(radial)))
  surface('Phalanx',verts,faces)
 # A flattened palm with a tapered wrist, not a single inflated oval.
 verts=[];faces=[];radial=24
 profiles=[(0,2.25,1.1),(1.5,2.45,1.12),(3.5,3.05,1.32),(5.5,3.55,1.4),(7.5,3.65,1.3),(9.2,3.4,1.15)]
 for u,width,depth in profiles:
  for n in range(radial):
   a=n/radial*math.tau;c=math.cos(a);z=math.sin(a)
   y=.55+width*math.copysign(abs(c)**.85,c);height=depth*math.copysign(abs(z)**.85,z)
   distal=max(0,min(1,(u-7.5)/1.7));verts.append((u-distal*.95*((y-.55)/3.65)**2,y,height))
 for k in range(len(profiles)-1):
  for n in range(radial):
   j=k*radial+n;q=k*radial+(n+1)%radial;faces.append((j,q,q+radial,j+radial))
 faces.append(tuple(reversed(range(radial))));faces.append(tuple((len(profiles)-1)*radial+n for n in range(radial)))
 surface('Palm',verts,faces)
 # Thenar pad anchors the thumb independently of the finger roots.
 sphere((4.4,-2.1,-.38),(2.2,1.65,1.22))
 digit=[]
 for name,y,length,r in [('Index',-1.85,7.4,.85),('Middle',.2,8.1,.92),('Ring',2.2,7.5,.86),('Pinky',3.95,5.9,.7)]:
  start=Vector(({'Index':9.0,'Middle':9.35,'Ring':9.1,'Pinky':8.1}[name],y,.18));direction=Vector((1,(y-.6)*.023,0)).normalized();points=[start+direction*length*t for t in [0,.46,.76,1]]
  for j in range(3):
   capsule(points[j],points[j+1],r*(1-.10*j))
   sphere(points[j],(r*.98,r*.98,r*.79))
  sphere(points[-1],(r*.67,r*.67,r*.55))
  digit.append((name,points,r))
 # Thumb leaves radial edge at a distinct angle, with two phalanges.
 pts=[Vector((4.3,-2.7,-.3)),Vector((6.6,-4.55,-.4)),Vector((9.0,-4.9,-.3))]
 for j in range(2):
  capsule(pts[j],pts[j+1],.94-.13*j);sphere(pts[j],(.94-.13*j,.94-.13*j,.77-.1*j))
 sphere(pts[-1],(.65,.65,.55))
 digit.append(('Thumb',pts,1.0))
 bpy.ops.object.select_all(action='DESELECT')
 for p in parts:p.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);bpy.ops.object.join();hand=parts[0];hand.name='Rebuilt'+side+'Hand'
 rem=hand.modifiers.new('UnionPalmDigits','REMESH');rem.mode='VOXEL';rem.voxel_size=.15;bpy.ops.object.modifier_apply(modifier=rem.name)
 sm=hand.modifiers.new('RelaxSurface','SMOOTH');sm.factor=.4;sm.iterations=2;bpy.ops.object.modifier_apply(modifier=sm.name)
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
    scores.append((distance,n,influence,name))
  scores.sort();digitName=scores[0][3];best=[entry for entry in scores if entry[3]==digitName][:2];influence=best[0][2]
  if influence<1:groups[side+'Hand'].add([v.index],1-influence,'REPLACE')
  den=sum(1/(s[0]+.15)**3 for s in best)
  for distance,n,_,_ in best:
   if influence>0:groups[n].add([v.index],influence/(distance+.15)**3/den,'REPLACE')
 hand.matrix_world=body.matrix_world.copy();mod=hand.modifiers.new('DigitSkin','ARMATURE');mod.object=arm;mod.use_deform_preserve_volume=True
 # Capture a closed pose as a shape, while retaining explicit finger bones.
 for name,points,chain,r in chains:
  for j,n in enumerate(chain):
   p=arm.pose.bones[n];p.rotation_mode='XYZ'
   # Rest bones point along local +Y; local X bends fingers under the palm.
   p.rotation_euler.x=math.radians(-[78,100,65][j] if name!='Thumb' else -[20,45][j])
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
