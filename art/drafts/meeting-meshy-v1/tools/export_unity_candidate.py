"""Headless Blender adapter for an assembled GLB. Candidate only; no catalog edits.
blender -b --factory-startup --python-exit-code 1 -P ... -- INPUT.glb OUTPUT_FOLDER
"""
import bpy,sys,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
args=sys.argv[sys.argv.index('--')+1:];source=Path(args[0]).resolve();out=Path(args[1]).resolve();out.mkdir(parents=True,exist_ok=True)
assert source.suffix=='.glb';before=hashlib.sha256(source.read_bytes()).hexdigest();bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(source));bpy.context.view_layer.update();rows=[];bounds=[]
# Only the authored gameplay shell needs bounds-safe splitting/batching.
arch={'Warm plaster','White frame','Window daylight','Radiator','Board','Ceiling grid','Ceiling panels','Right wall'}
if '--gameplay-shell' in args:
 for obj in list(bpy.context.scene.objects):
  if obj.type!='MESH' or obj.name not in arch:continue
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
 bpy.context.view_layer.update();groups={}
 for obj in list(bpy.context.scene.objects):
  if obj.type!='MESH' or not any(obj.name==a or obj.name.startswith(a+'.') for a in arch):continue
  c=[obj.matrix_world@Vector(p) for p in obj.bound_box];mn=[min(q[k] for q in c) for k in range(3)];mx=[max(q[k] for q in c) for k in range(3)]
  if mn[0]>=5.5:zone='left'
  elif mx[0]<=-5.5:zone='right'
  elif mn[2]>=2.2:zone='ceiling'
  elif -mx[1]>=2:zone='rear'
  else:raise RuntimeError(('Shell intersects combat volume',obj.name,mn,mx))
  groups.setdefault((obj.data.materials[0].name,zone),[]).append(obj)
 for (mat,zone),parts in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for obj in parts:obj.select_set(True)
  bpy.context.view_layer.objects.active=parts[0]
  if len(parts)>1:bpy.ops.object.join()
  parts[0].name='Shell '+mat+' '+zone
 bpy.context.view_layer.update()
for obj in bpy.context.scene.objects:
 if obj.type!='MESH':continue
 points=[obj.matrix_world@Vector(c) for c in obj.bound_box];bounds.append({'name':obj.name,'minBlender':[min(p[k] for p in points) for k in range(3)],'maxBlender':[max(p[k] for p in points) for k in range(3)],'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons)})
for material in bpy.data.materials:
 if not material.use_nodes:continue
 bs=next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if not bs:continue
 row={'name':material.name,'color':list(bs.inputs['Base Color'].default_value),'metallic':bs.inputs['Metallic'].default_value,'roughness':bs.inputs['Roughness'].default_value,'baseTexture':'','normalTexture':''}
 for n in material.node_tree.nodes:
  if n.type!='TEX_IMAGE' or not n.image:continue
  role='normalTexture' if any(l.to_node.type=='NORMAL_MAP' for l in n.outputs['Color'].links) else 'baseTexture' if any(l.to_socket.name=='Base Color' for l in n.outputs['Color'].links) else ''
  if not role:continue
  filename='texture-'+str(len(rows))+'-'+role+'.png';n.image.filepath_raw=str(out/filename);n.image.file_format='PNG';n.image.save();row[role]=filename
 rows.append(row)
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.context.scene.objects:
 if obj.type=='MESH':obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'meshy-meeting-candidate.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
assert hashlib.sha256(source.read_bytes()).hexdigest()==before
(out/'materials.json').write_text(json.dumps({'materials':rows},indent=2));receipt={'status':'EXPORTED_PENDING_UNITY_IMPORT_AND_ACTUAL_CAPTURE','source':str(source),'sourceSHA256':before,'sourceUnchanged':True,'blenderVersion':bpy.app.version_string,'bounds':bounds,'catalogPublished':False};(out/'export-receipt.json').write_text(json.dumps(receipt,indent=2));print('AGENT_RESULT '+json.dumps({'ok':True,'meshes':len(bounds),'candidate':str(out/'meshy-meeting-candidate.fbx'),'catalogPublished':False}),flush=True)
