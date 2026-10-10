"""Stitch CC0 MakeHuman anatomical hands, using their actual proximal contour."""
import bpy,bmesh,sys,json,math
from pathlib import Path
from mathutils import Vector
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src));scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH' and o.name=='char1');hands=[bpy.data.objects['Rebuilt'+s+'Hand'] for s in ['Left','Right']]
# Match hand albedo to sampled source wrist pixels in linear space.
source_material=body.data.materials[0]
image=next(n.image for n in source_material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image.name=='texture_0')
pixels=list(image.pixels);width,height=image.size;colors=[];uvs=body.data.uv_layers.active.data
for polygon in body.data.polygons:
 for loop_index in polygon.loop_indices:
  vertex=body.data.vertices[body.data.loops[loop_index].vertex_index]
  if 67<abs(vertex.co.x)<71:
   uv=uvs[loop_index].uv;x=min(width-1,max(0,int(uv.x*width)));y=min(height-1,max(0,int(uv.y*height)))
   offset=(y*width+x)*4;colors.append(pixels[offset:offset+3])
if not colors:raise RuntimeError('No wrist albedo samples')
skin_color=[sum(c[channel] for c in colors)/len(colors) for channel in range(3)]
for hand in hands:
 hand.data.materials[0].node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*skin_color,1)
body.hide_render=False
for h in hands:h.hide_render=False
# All participants have identical shape names; each unaffected shape equals Basis.
names=['LeftElbow','RightElbow','LeftKnee','LeftCalfContour','ClosedFists']
for o in [body]+hands:
 for n in names:
  if n not in o.data.shape_keys.key_blocks:o.shape_key_add(name=n)
 for k in o.data.shape_keys.key_blocks:k.value=0
# Obtain the actual cross-section from a disposable MakeHuman hand mesh.
contours={}
for side,sign,hand in [('Left',1,hands[0]),('Right',-1,hands[1])]:
 w=arm.data.bones[side+'Hand'].head_local;probe=bmesh.new();probe.from_mesh(hand.data)
 cut=bmesh.ops.bisect_plane(probe,geom=list(probe.verts)+list(probe.edges)+list(probe.faces),dist=.00001,plane_co=(w.x+sign*5.3,0,0),plane_no=(sign,0,0))
 points=[v.co.copy() for v in probe.verts if abs(sign*(v.co.x-w.x)-5.3)<.001]
 center=sum(points,Vector())/len(points);rY=(max(p.y for p in points)-min(p.y for p in points))*.5;rZ=(max(p.z for p in points)-min(p.z for p in points))*.5
 contours[side]=(points,center,rY,rZ);probe.free()
for o in [body]+hands:
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001)
 sides=['Left','Right'] if o==body else ['Left' if 'Left' in o.name else 'Right']
 for side in sides:
  sign=1 if side=='Left' else -1;w=arm.data.bones[side+'Hand'].head_local
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(w.x+sign*(5.0 if o==body else 5.3),0,0),plane_no=(sign,0,0),clear_outer=o==body,clear_inner=o!=body)
 # Match source palm cross-section to the rebuilt proximal palm, keeping wrist untouched.
 if o==body:
  shape_layers=list(bm.verts.layers.shape.values())
  for side,sign in [('Left',1),('Right',-1)]:
   w=arm.data.bones[side+'Hand'].head_local
   ring=[v for v in bm.verts if abs(sign*(v.co.x-w.x)-5)<.001]
   ym=(min(v.co.y for v in ring)+max(v.co.y for v in ring))*.5;zm=(min(v.co.z for v in ring)+max(v.co.z for v in ring))*.5
   ry=(max(v.co.y for v in ring)-min(v.co.y for v in ring))*.5;rz=(max(v.co.z for v in ring)-min(v.co.z for v in ring))*.5
   for v in bm.verts:
    u=sign*(v.co.x-w.x)
    if 2<u<5.01:
     t=max(0,min(1,(u-2)/3));t=t*t*(3-2*t)
     a=math.atan2((v.co.z-zm)/rz,(v.co.y-ym)/ry)
     points,center,targetRY,targetRZ=contours[side]
     def error(p):
      angle=math.atan2((p.z-center.z)/targetRZ,(p.y-center.y)/targetRY)
      return abs(math.atan2(math.sin(angle-a),math.cos(angle-a)))
     angular=sorted((math.atan2((p.z-center.z)/targetRZ,(p.y-center.y)/targetRY),j) for j,p in enumerate(points))
     angular=[(theta-math.tau,j) for theta,j in angular]+angular+[(theta+math.tau,j) for theta,j in angular]
     pair=next((angular[j],angular[j+1]) for j in range(len(angular)-1) if angular[j][0]<=a<=angular[j+1][0])
     (a0,i0),(a1,i1)=pair;tAngular=(a-a0)/max(1e-8,a1-a0);nearest=points[i0].lerp(points[i1],tAngular)
     target=Vector((v.co.x,nearest.y,nearest.z))
     delta=(target-v.co)*t;v.co+=delta
     for layer in shape_layers:v[layer]+=delta
 bm.to_mesh(o.data);bm.free()
bpy.ops.object.select_all(action='DESELECT')
for o in [body]+hands:o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(body.data);seams=[];bridge_faces=[]
for side,sign in [('Left',1),('Right',-1)]:
 x=arm.data.bones[side+'Hand'].head_local.x+sign*5.0
 edges=[e for e in bm.edges if e.is_boundary and all(abs(v.co.x-x)<.31 for v in e.verts)]
 result=bmesh.ops.bridge_loops(bm,edges=edges)
 if not result.get('faces'):raise RuntimeError('Missing wrist bridge '+side+' edges '+str(len(edges)))
 for f in result['faces']:f.material_index=len(body.data.materials)-1
 bridge_faces.extend(result['faces']);seams.append({'side':side,'bridgeFaces':len(result['faces'])})
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));remaining=sum(e.is_boundary and any((v.co.x-arm.data.bones['LeftHand'].head_local.x>4.9) or (arm.data.bones['RightHand'].head_local.x-v.co.x>4.9) for v in e.verts) for e in bm.edges)
# Deliberately remove a bridge face on a disposable copy: the boundary audit must fail.
bm.faces.index_update();mutation=bm.copy();mutation.faces.ensure_lookup_table()
bmesh.ops.delete(mutation,geom=[mutation.faces[bridge_faces[0].index]],context='FACES_ONLY')
mutation_detected=any(e.is_boundary and any((v.co.x-arm.data.bones['LeftHand'].head_local.x>4.9) or (arm.data.bones['RightHand'].head_local.x-v.co.x>4.9) for v in e.verts) for e in mutation.edges)
mutation.free()
if not mutation_detected:raise RuntimeError('Wrist boundary audit missed removed bridge face')
bm.to_mesh(body.data);bm.free()
# Both ends of bridge have an identical wrist field; fingers retain their own weights.
for v in body.data.vertices:
 side='Left' if v.co.x>0 else 'Right';sign=1 if v.co.x>0 else -1;w=arm.data.bones[side+'Hand'].head_local;u=sign*(v.co.x-w.x)
 if abs(u-5.0)<.2:
  for g in body.vertex_groups:g.remove([v.index])
  body.vertex_groups[side+'Hand'].add([v.index],1,'REPLACE')
if remaining:raise RuntimeError('Open hand boundary edges '+str(remaining))
report={'stage':'MakeHuman anatomical hand candidate, visual/device review pending','bones':len(arm.data.bones),'digitsPerHand':5,'fingerAndPalmBonesAdded':38,'shapeNames':[k.name for k in body.data.shape_keys.key_blocks],'seams':seams,'openHandBoundaryEdges':remaining,'removedBridgeFaceMutationDetected':mutation_detected,'creditsSpent':0,'wristAlbedoSamples':len(colors),'handAlbedoLinear':skin_color}
body.data.shape_keys.key_blocks['ClosedFists'].value=1
bpy.ops.wm.save_as_mainfile(filepath=str(out/'stitched-fingers.blend'))
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'island-finger-candidate.fbx'),use_selection=True,use_mesh_modifiers=False,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
scene.cycles.samples=12;scene.render.resolution_x=800;scene.render.resolution_y=600
arm.data.pose_position='REST';scene.frame_set(1);bpy.context.view_layer.update();w=arm.data.bones['LeftHand'].head_local
focus=arm.matrix_world@(w+Vector((7,0,0)));cam=scene.camera;cam.location=focus+Vector((.2,-.9,.6));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.29
scene.render.filepath=str(out/'stitched-closed.png');bpy.ops.render.render(write_still=True)
arm.data.pose_position='POSE';scene.frame_set(10);bpy.context.view_layer.update();focus=arm.matrix_world@arm.pose.bones['LeftHand'].head;cam.location=focus+Vector((.35,-.75,.3));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.36
scene.render.filepath=str(out/'jab-closed.png');bpy.ops.render.render(write_still=True)
scene.frame_set(1);bpy.context.view_layer.update();focus=arm.matrix_world@arm.pose.bones['RightHand'].head;cam.location=focus+Vector((-.35,-.75,.3));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/'right-guard.png');bpy.ops.render.render(write_still=True)
(out/'hand-integration-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
