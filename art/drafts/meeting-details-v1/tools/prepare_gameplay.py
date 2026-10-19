import bpy,sys,json,hashlib,time,math,bmesh,os
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/meeting-details-v1';GAME=ROOT/'unity/Assets/LighFight/Art/Drafts/MeetingDetailsV1';GAME.mkdir(parents=True,exist_ok=True)
p=OUT/'meeting-master.glb';bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(p));print('BLENDER_VERSION',bpy.app.version_string,flush=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=4;scene.cycles.use_denoising=True;scene.cycles.max_bounces=3;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.resolution_x=960;scene.render.resolution_y=570;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.12,.14,.16,1);scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
def gp(p):return Vector((p[0],-p[2],p[1]))
for name,loc,t,power,size in [('Window',(3,2.4,1.5),(0,.8,2),500,2.5),('FrontFill',(1.5,4,-2),(0,1,2.5),500,4),('Ceiling',(0,2.9,3),(0,0,3),250,2)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=gp(loc);o.rotation_euler=(gp(t)-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',d);scene.collection.objects.link(cam);scene.camera=cam;d.lens=32
(OUT/'renders-fixed').mkdir(exist_ok=True)
def shot(name,loc,t):
 if os.getenv("LF_SKIP_RENDER")=="1":return
 cam.location=gp(loc);cam.rotation_euler=(gp(t)-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'renders-fixed'/f'{name}.png');bpy.ops.render.render(write_still=True);print('VIEW_DONE',name,flush=True)
shot('fight',(0,2.1,-6.4),(0,1.1,2.3));shot('details',(-1.8,1.75,.75),(0,.9,3))
# Bake authored world geometry, then reposition components into an 11x4m clear gameplay shell.
arch={'Warm plaster','Light oak','White frame','Window daylight','Radiator','Board','Ceiling grid','Ceiling panels','Right wall'}
props={'Black appliance and cables','Brushed metal','Glowing coals','Yellow ceramic','Red plastic','Paper','Slate grey beanbag','Network cabinet','Cabinet blue trim','Dark network glass','Terracotta pot','Vine leaves','Plant stems','Reference textile'}
objs=[o for o in scene.objects if o.type=='MESH']
for o in objs:
 mw=o.matrix_world.copy();verts=[mw@v.co for v in o.data.vertices];o.parent=None;o.matrix_world=Matrix.Identity(4)
 for v,w in zip(o.data.vertices,verts):
  x,y,z=w.x,w.z,-w.y
  if o.name in arch:x*=1.9;z*=1.35
  else:
   z+=1.8
   if o.name in props:
    if x>2.9:x+=2.75
    elif x< -2:x-=2.75
  v.co=gp((x,y,z))
 o.data.update();bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
 # Architectural material batches may span multiple walls. Separate disconnected boxes for truthful bounds validation.
 if o.name in arch and o.name!='Light oak':
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
 elif o.name=='Light oak':o.name='FightFloor';o.data.name='FightFloor'
bpy.context.view_layer.update()
# Batch disconnected architectural faces by material and safe spatial zone.
groups={}
for o in list(scene.objects):
 if o.type!='MESH' or o.name=='FightFloor':continue
 if not any(o.name==a or o.name.startswith(a+'.') for a in arch):continue
 corners=[o.matrix_world@Vector(c) for c in o.bound_box]
 mn=[min(q[k] for q in corners) for k in range(3)];mx=[max(q[k] for q in corners) for k in range(3)]
 if mn[0]>=5.5:zone='left'
 elif mx[0]<=-5.5:zone='right'
 elif mn[2]>=2.2:zone='ceiling'
 elif -mx[1]>=2:zone='rear'
 else:raise RuntimeError(('Unbatchable architecture',o.name,mn,mx))
 mat=o.data.materials[0].name;groups.setdefault((mat,zone),[]).append(o)
for (mat,zone),parts in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0]
 if len(parts)>1:bpy.ops.object.join()
 parts[0].name='Shell '+mat+' '+zone
bpy.context.view_layer.update()
# Inspect true world-space component bounds against unchanged gameplay volume.
checks=[]
for o in [o for o in scene.objects if o.type=='MESH']:
 v=[o.matrix_world@Vector(c) for c in o.bound_box];mins=[min(q[k] for q in v) for k in range(3)];maxs=[max(q[k] for q in v) for k in range(3)]
 # Blender axes map gameplay UnityY to Z, UnityZ to -Y (FBX import preserves Y-up).
 mn=[mins[0],mins[2],-maxs[1]];mx=[maxs[0],maxs[2],-mins[1]]
 if o.name=='FightFloor':assert mn[0]<=-5.5 and mx[0]>=5.5 and mn[2]<=-2 and mx[2]>=2 and abs(mx[1])<.05
 else:assert any(mx[k]<=[-5.5,0,-2][k] or mn[k]>=[5.5,2.2,2][k] for k in range(3)),('OBSTRUCTION',o.name,mn,mx)
 checks.append({'name':o.name,'min':mn,'max':mx})
# Material map extracts texture pixels; source GLB remains immutable. Export avoids GLB importer dependency.
matrows=[]
for mat in bpy.data.materials:
 if not mat.use_nodes:continue
 bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if not bs:continue
 row={'name':mat.name,'color':list(bs.inputs['Base Color'].default_value),'metallic':bs.inputs['Metallic'].default_value,'roughness':bs.inputs['Roughness'].default_value,'baseTexture':'','normalTexture':''}
 for n in mat.node_tree.nodes:
  if n.type!='TEX_IMAGE' or not n.image:continue
  role='normalTexture' if any(l.to_node.type=='NORMAL_MAP' for l in n.outputs['Color'].links) else 'baseTexture' if any(l.to_socket.name=='Base Color' for l in n.outputs['Color'].links) else ''
  if not role:continue
  filename='tex-'+str(len(matrows))+'-'+role+'.png';n.image.filepath_raw=str(GAME/filename);n.image.file_format='PNG';n.image.save();row[role]=filename
 matrows.append(row)
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
 if o.type=='MESH':o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(GAME/'meeting-gameplay.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
(GAME/'materials.json').write_text(json.dumps({'materials':matrows},indent=2));(OUT/'gameplay-layout.json').write_text(json.dumps({'sourceSHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'gameplayVolume':[11,2.2,4],'shellScaleXZ':[1.9,1.35],'furnitureTranslationZ':1.8,'furnitureScaleUnchanged':True,'checks':checks,'status':'BLENDER_BOUNDS_PASS_PENDING_UNITY'},indent=2))
shot('gameplay',(0,2.1,-6.4),(0,1.1,3.5));print('AGENT_RESULT '+json.dumps({'ok':True,'meshCount':len(checks),'export':str(GAME/'meeting-gameplay.fbx')}),flush=True)
