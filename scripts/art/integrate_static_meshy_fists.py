"""Stitch rigid photo-generated Meshy fists to original source wrists; copy body skin maps."""
import bpy,bmesh,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform,closest_point_on_tri
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
# Match surface centers: Meshy joint heads and anatomical skin centers differ.
alignment=[]
for side,sign,hand in [('Left',1,hands[0]),('Right',-1,hands[1])]:
 wrist=arm.data.bones[side+'Hand'].head_local
 centers=[]
 for obj,cut in [(body,-3),(hand,-.096)]:
  probe=bmesh.new();probe.from_mesh(obj.data);bmesh.ops.bisect_plane(probe,geom=list(probe.verts)+list(probe.edges)+list(probe.faces),dist=.00001,plane_co=(wrist.x+sign*cut,0,0),plane_no=(sign,0,0))
  points=[v.co.copy() for v in probe.verts if abs(sign*(v.co.x-wrist.x)-cut)<.001]
  centers.append(Vector((0,(min(p.y for p in points)+max(p.y for p in points))/2,(min(p.z for p in points)+max(p.z for p in points))/2)));probe.free()
 delta=centers[0]-centers[1]
 for key in hand.data.shape_keys.key_blocks:
  for vertex in key.data:vertex.co+=delta
 # Synchronize base coordinates with the shifted Basis.
 for v,p in zip(hand.data.vertices,hand.data.shape_keys.key_blocks['Basis'].data):v.co=p.co
 bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for bone in arm.data.edit_bones:
  if bone.name.startswith(side+'MH_'):bone.head+=delta;bone.tail+=delta
 bpy.ops.object.mode_set(mode='OBJECT');alignment.append({'side':side,'surfaceCenterShiftCm':list(delta)})

# Transfer the original skin maps using source surface coordinates, excluding the old watch.
body.data.calc_loop_triangles();source_triangles=[];source_uv=[]
for tri in body.data.loop_triangles:
 center=sum((body.data.vertices[i].co for i in tri.vertices),Vector())/3
 if not 55<abs(center.x)<65.5:continue
 triangle_uv=[body.data.uv_layers.active.data[i].uv.copy() for i in tri.loops]
 sample_colors=[]
 for uv in triangle_uv:
  x=min(width-1,max(0,int(uv.x*width)));y=min(height-1,max(0,int(uv.y*height)));offset=(y*width+x)*4;sample_colors.append(pixels[offset:offset+3])
 if any(sum(rgb)/3<.35 for rgb in sample_colors):continue
 source_triangles.append(list(tri.vertices));source_uv.append([body.data.uv_layers.active.data[i].uv.copy() for i in tri.loops])
source_vertices=[v.co.copy() for v in body.data.vertices]
source_surface=BVHTree.FromPolygons(source_vertices,source_triangles,all_triangles=True)
def skin_uv(point,reference=None):
 nearest,normal,index,distance=source_surface.find_nearest(point if reference is None else reference)
 if index is None:raise RuntimeError('Skin surface mapping failed')
 a,b,c=[source_vertices[i] for i in source_triangles[index]]
 ua,ub,uc=[Vector((uv.x,uv.y,0)) for uv in source_uv[index]]
 result=barycentric_transform(closest_point_on_tri(point,a,b,c),a,b,c,ua,ub,uc)
 def brightness(uv):
  x=min(width-1,max(0,int(uv.x*width)));y=min(height-1,max(0,int(uv.y*height)));k=(y*width+x)*4;return sum(pixels[k:k+3])/3
 if brightness(result)<.40:result=max([ua,ub,uc],key=brightness)
 return result.xy
# Blend texture samples, never atlas coordinates (interpolated UVs cross unrelated islands).
source_material=source_material.copy();source_material.name='UnifiedHandSkin';body.data.materials[0]=source_material
body.data.uv_layers.new(name='HandDetailUV');body_mask=body.data.color_attributes.new(name='HandSkinBlend',type='FLOAT_COLOR',domain='CORNER')
for value in body_mask.data:value.color=(0,0,0,1)
nodes=source_material.node_tree.nodes;links=source_material.node_tree.links
for normal_node in nodes:
 if normal_node.type=='NORMAL_MAP':normal_node.inputs['Strength'].default_value=.25

attribute=nodes.new('ShaderNodeAttribute');attribute.attribute_name='HandSkinBlend';separate=nodes.new('ShaderNodeSeparateColor');links.new(attribute.outputs['Color'],separate.inputs['Color'])
uv_node=nodes.new('ShaderNodeUVMap');uv_node.uv_map='HandDetailUV'
for texture in [node for node in list(nodes) if node.type=='TEX_IMAGE']:
 outgoing=list(texture.outputs['Color'].links)
 if not outgoing:continue
 detail=nodes.new('ShaderNodeTexImage');detail.image=texture.image;links.new(uv_node.outputs['UV'],detail.inputs['Vector'])
 mix=nodes.new('ShaderNodeMixRGB');links.new(separate.outputs['Red'],mix.inputs[0]);links.new(texture.outputs['Color'],mix.inputs[1]);links.new(detail.outputs['Color'],mix.inputs[2])
 for link in outgoing:target=link.to_socket;links.remove(link);links.new(mix.outputs[0],target)
# Select a skin-only patch on the source forearm atlas, avoiding watch/cloth pixels.
patch_half=80/max(width,height);patch_candidates=[]
for index in range(0,len(source_uv),max(1,len(source_uv)//160)):
 center=sum(source_uv[index],Vector((0,0)))/3;values=[]
 for iy in range(7):
  for ix in range(7):
   uv=center+Vector(((ix/6-.5)*patch_half*2,(iy/6-.5)*patch_half*2));x=int(uv.x*width);y=int(uv.y*height)
   if not (0<=x<width and 0<=y<height):values.append((0,0,0));continue
   k=(y*width+x)*4;values.append(pixels[k:k+3])
 if any(sum(color)/3<.36 or color[0]<color[1]*.95 for color in values):continue
 mean=[sum(color[channel] for color in values)/len(values) for channel in range(3)]
 score=sum((mean[channel]-skin_color[channel])**2 for channel in range(3));patch_candidates.append((score,center))
if not patch_candidates:raise RuntimeError('No clean forearm skin texture patch')
patch_center=min(patch_candidates,key=lambda item:item[0])[1]
# Crop and repeat the clean source skin patch so hand texel density matches the forearm.
for detail in [node for node in nodes if node.type=='TEX_IMAGE' and node.inputs['Vector'].is_linked and node.inputs['Vector'].links[0].from_node==uv_node]:
 original=detail.image;sw,sh=original.size;source_pixels=list(original.pixels);size=512;result=[]
 for row in range(size):
  for column in range(size):
   uv=patch_center+Vector((((column/size*3)%1-.5)*patch_half*2,((row/size*3)%1-.5)*patch_half*2))
   px=min(sw-1,max(0,int(uv.x*sw)));py=min(sh-1,max(0,int(uv.y*sh)));index=(py*sw+px)*4
   result.extend(source_pixels[index:index+4])
 cropped=bpy.data.images.new('HandDetail_'+original.name,width=size,height=size,alpha=True)
 cropped.colorspace_settings.name=original.colorspace_settings.name;cropped.pixels.foreach_set(result)
 sample_index=(int(patch_center.y*sh)*sw+int(patch_center.x*sw))*4
 filename='detail-skin.png' if original==image else ('detail-normal.png' if source_pixels[sample_index+2]>.75 and source_pixels[sample_index]<.7 else 'detail-mrao.png')
 if filename=='detail-normal.png':
  import numpy as np
  field=np.asarray(result,dtype=np.float32).reshape(size,size,4);smooth=field.copy()
  for axis in [0,1]:smooth=sum(np.roll(smooth,offset,axis=axis) for offset in range(-6,7))/13
  field[:,:,:2]=np.clip(.5+(field[:,:,:2]-smooth[:,:,:2])*.6,0,1)
  xy=field[:,:,:2]*2-1;field[:,:,2]=np.sqrt(np.maximum(0,1-(xy*xy).sum(axis=2)))*.5+.5
  cropped.pixels.foreach_set(field.reshape(-1))
 cropped.filepath_raw=str(out/filename);cropped.file_format='PNG';cropped.save();cropped.pack();detail.image=cropped


for hand in hands:
 hand.data.materials.clear();hand.data.materials.append(source_material)
 layer=hand.data.uv_layers.active or hand.data.uv_layers.new(name='TransferredSkinUV');authored=[loop.uv.copy() for loop in layer.data];layer.name=body.data.uv_layers.active.name
 minimum=Vector((min(uv.x for uv in authored),min(uv.y for uv in authored)));maximum=Vector((max(uv.x for uv in authored),max(uv.y for uv in authored)))
 side='Left' if 'Left' in hand.name else 'Right';sign=1 if side=='Left' else -1;wrist=arm.data.bones[side+'Hand'].head_local
 base_uv_name=layer.name;hand.data.uv_layers.new(name='HandDetailUV');hand.data.color_attributes.new(name='HandSkinBlend',type='FLOAT_COLOR',domain='CORNER')
 layer=hand.data.uv_layers[base_uv_name];detail_layer=hand.data.uv_layers['HandDetailUV'];mask=hand.data.color_attributes['HandSkinBlend']
 bpy.ops.object.select_all(action='DESELECT');hand.select_set(True);bpy.context.view_layer.objects.active=hand
 hand.data.uv_layers.active=detail_layer
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
 layer=hand.data.uv_layers[base_uv_name];detail_layer=hand.data.uv_layers['HandDetailUV'];mask=hand.data.color_attributes['HandSkinBlend']
 detail_coords=[loop.uv.copy() for loop in detail_layer.data]
 hand.data.uv_layers.active=layer
 for polygon in hand.data.polygons:
  for loop_index in polygon.loop_indices:
   loop=hand.data.loops[loop_index];point=hand.data.vertices[loop.vertex_index].co;uv=authored[loop_index]
   normalized=detail_coords[loop_index]
   detail_uv=patch_center+(normalized-Vector((.5,.5)))*patch_half*2
   t=max(0,min(1,sign*(point.x-wrist.x)/1.5));t=t*t*(3-2*t)
   layer.data[loop_index].uv=skin_uv(point,polygon.center);detail_layer.data[loop_index].uv=normalized;mask.data[loop_index].color=(t,0,0,1)

uv_counts=[len({(round(loop.uv.x,5),round(loop.uv.y,5)) for loop in hand.data.uv_layers[0].data}) for hand in hands]
if min(uv_counts)<100:raise RuntimeError('Skin UV transfer collapsed')
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
 cut=bmesh.ops.bisect_plane(probe,geom=list(probe.verts)+list(probe.edges)+list(probe.faces),dist=.00001,plane_co=(w.x+sign*-.096,0,0),plane_no=(sign,0,0))
 points=[v.co.copy() for v in probe.verts if abs(sign*(v.co.x-w.x)+.096)<.001]
 center=sum(points,Vector())/len(points);rY=(max(p.y for p in points)-min(p.y for p in points))*.5;rZ=(max(p.z for p in points)-min(p.z for p in points))*.5
 contours[side]=(points,center,rY,rZ);probe.free()
for o in [body]+hands:
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001)
 sides=['Left','Right'] if o==body else ['Left' if 'Left' in o.name else 'Right']
 for side in sides:
  sign=1 if side=='Left' else -1;w=arm.data.bones[side+'Hand'].head_local
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(w.x+sign*(-3.0 if o==body else -.096),0,0),plane_no=(sign,0,0),clear_outer=o==body,clear_inner=o!=body)
 # Match source palm cross-section to the rebuilt proximal palm, keeping wrist untouched.
 if o==body:
  shape_layers=list(bm.verts.layers.shape.values())
  for side,sign in [('Left',1),('Right',-1)]:
   w=arm.data.bones[side+'Hand'].head_local
   ring=[v for v in bm.verts if abs(sign*(v.co.x-w.x)+3)<.001]
   ym=(min(v.co.y for v in ring)+max(v.co.y for v in ring))*.5;zm=(min(v.co.z for v in ring)+max(v.co.z for v in ring))*.5
   ry=(max(v.co.y for v in ring)-min(v.co.y for v in ring))*.5;rz=(max(v.co.z for v in ring)-min(v.co.z for v in ring))*.5
   for v in bm.verts:
    u=sign*(v.co.x-w.x)
    if -12<u<-2.99:
     t=max(0,min(1,(u+12)/9));t=t*t*(3-2*t)
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
bpy.ops.wm.save_as_mainfile(filepath=str(out/'fitted-hands.blend'))
bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(body.data);seams=[];bridge_faces=[]
for side,sign in [('Left',1),('Right',-1)]:
 x=arm.data.bones[side+'Hand'].head_local.x+sign*-3.0
 edges=[e for e in bm.edges if e.is_boundary and all(abs(v.co.x-x)<3.01 for v in e.verts)]
 result=bmesh.ops.bridge_loops(bm,edges=edges)
 if not result.get('faces'):raise RuntimeError('Missing wrist bridge '+side+' edges '+str(len(edges)))
 for f in result['faces']:f.material_index=len(body.data.materials)-1
 uv_layer=bm.loops.layers.uv.active
 for face in result['faces']:
  for loop in face.loops:loop[uv_layer].uv=skin_uv(loop.vert.co,face.calc_center_median())
 bridge_faces.extend(result['faces']);seams.append({'side':side,'bridgeFaces':len(result['faces'])})
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));remaining=sum(e.is_boundary and any((v.co.x-arm.data.bones['LeftHand'].head_local.x>-3.1) or (arm.data.bones['RightHand'].head_local.x-v.co.x>-3.1) for v in e.verts) for e in bm.edges)
# Deliberately remove a bridge face on a disposable copy: the boundary audit must fail.
bm.faces.index_update();mutation=bm.copy();mutation.faces.ensure_lookup_table()
bmesh.ops.delete(mutation,geom=[mutation.faces[bridge_faces[0].index]],context='FACES_ONLY')
mutation_detected=any(e.is_boundary and any((v.co.x-arm.data.bones['LeftHand'].head_local.x>-3.1) or (arm.data.bones['RightHand'].head_local.x-v.co.x>-3.1) for v in e.verts) for e in mutation.edges)
mutation.free()
if not mutation_detected:raise RuntimeError('Wrist boundary audit missed removed bridge face')
bm.to_mesh(body.data);bm.free()
# Both ends of bridge have an identical wrist field; fingers retain their own weights.
for v in body.data.vertices:
 side='Left' if v.co.x>0 else 'Right';sign=1 if v.co.x>0 else -1;w=arm.data.bones[side+'Hand'].head_local;u=sign*(v.co.x-w.x)
 if abs(u+3.0)<.2:
  for g in body.vertex_groups:g.remove([v.index])
  body.vertex_groups[side+'ForeArm'].add([v.index],1,'REPLACE')
if remaining:raise RuntimeError('Open hand boundary edges '+str(remaining))
report={'stage':'Photo-generated rigid closed fist candidate, visual/device review pending','bones':len(arm.data.bones),'digitsPerHand':5,'fingerAndPalmBonesAdded':0,'shapeNames':[k.name for k in body.data.shape_keys.key_blocks],'seams':seams,'openHandBoundaryEdges':remaining,'removedBridgeFaceMutationDetected':mutation_detected,'creditsSpent':0,'handLengthScale':1,'handWidthThicknessScale':1,'extraProximalThicknessScale':1,'surfaceAlignment':alignment,'skinMapsTransferred':True,'cleanSkinPatchUV':list(patch_center),'skinPatchPixels':160,'wristAlbedoSamples':len(colors),'handAlbedoLinear':skin_color}
# The source jab bends the wrist at impact. Align its rest palm axis to the forearm.
arm.animation_data.action=arm.animation_data.action.copy();arm.animation_data.action.name='JabNeutralImpactWrist'
cached=[]
for frame in range(scene.frame_start,scene.frame_end+1):
 scene.frame_set(frame);bpy.context.view_layer.update();p=arm.pose.bones['LeftHand'];forearm=arm.pose.bones['LeftForeArm']
 axis=p.matrix.to_3x3()@arm.data.bones['LeftHand'].matrix_local.to_3x3().inverted()@Vector((1,0,0));target=(p.head-forearm.head).normalized()
 strength=max(0,min(1,(frame-3)/4,(18-frame)/4))
 neutral=forearm.matrix.to_3x3()@arm.data.bones['LeftForeArm'].matrix_local.to_3x3().inverted()@arm.data.bones['LeftHand'].matrix_local.to_3x3()
 neutral_axis=neutral@arm.data.bones['LeftHand'].matrix_local.to_3x3().inverted()@Vector((1,0,0))
 neutral=neutral_axis.normalized().rotation_difference(target).to_matrix()@neutral
 rotation=p.matrix.to_quaternion().slerp(neutral.to_quaternion(),strength);matrix=rotation.to_matrix().to_4x4();matrix.translation=p.matrix.translation
 cached.append((frame,matrix.copy()))
for frame,matrix in cached:
 scene.frame_set(frame);arm.pose.bones['LeftHand'].matrix=matrix;bpy.context.view_layer.update();arm.pose.bones['LeftHand'].keyframe_insert('rotation_quaternion',frame=frame)
scene.frame_set(10);bpy.context.view_layer.update();p=arm.pose.bones['LeftHand'];forearm=arm.pose.bones['LeftForeArm'];axis=p.matrix.to_3x3()@arm.data.bones['LeftHand'].matrix_local.to_3x3().inverted()@Vector((1,0,0))
report['impactWristAxisDegrees']=math.degrees(axis.angle(p.head-forearm.head))
if report['impactWristAxisDegrees']>1:raise RuntimeError('Impact wrist remains bent')

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
