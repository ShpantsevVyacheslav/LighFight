"""Controlled source / provider rig comparison, with identical unlit texture."""
import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];folder=root/'art/source/island-web-visible';out=root/'art/previews/source-quality';out.mkdir(parents=True,exist_ok=True)
for filename,pose in [('model','source'),('rig','bind'),('rig','frame0'),('jab','jab-0'),('jab','jab-50'),('greeting','greeting-0'),('greeting','greeting-50'),('web-corrected-running','web-bind'),('web-corrected-running','web-run-50'),('web-fbx','web-greeting-50')]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if filename=='web-fbx':bpy.ops.import_scene.fbx(filepath=str(next((folder/'web-greeting-fbx').glob('*.fbx'))))
 else:bpy.ops.import_scene.gltf(filepath=str(folder/(filename+'.glb')))
 arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
 if arm and pose in ('bind','web-bind'):
  arm.animation_data_clear()
  for bone in arm.pose.bones:bone.matrix_basis.identity()
 else:
  action=arm.animation_data.action if arm and arm.animation_data else None
  if action:
   first,last=action.frame_range;sceneFrame=round(first+(last-first)*(.5 if pose.endswith('-50') else 0));bpy.context.scene.frame_set(sceneFrame)
  else:bpy.context.scene.frame_set(1)
 for obj in list(bpy.context.scene.objects):
  if obj.type=='MESH' and len(obj.data.vertices)<1000:bpy.data.objects.remove(obj,do_unlink=True)
 scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=700;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Background');scene.world.color=(.03,.04,.06);scene.view_settings.view_transform='Standard'
 texture=bpy.data.images.load(str(folder/'model_textures/base_color.png'));material=bpy.data.materials.new('Original texture only');material.use_nodes=True;nodes=material.node_tree.nodes;nodes.clear();image=nodes.new('ShaderNodeTexImage');image.image=texture;emission=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial');material.node_tree.links.new(image.outputs['Color'],emission.inputs['Color']);material.node_tree.links.new(emission.outputs[0],output.inputs[0])
 points=[]
 for obj in scene.objects:
  if obj.type=='MESH':
   obj.data.materials.clear();obj.data.materials.append(material);mesh=obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh();points.extend(obj.matrix_world@v.co for v in mesh.vertices)
 lo=min(p.z for p in points);hi=max(p.z for p in points);height=hi-lo;center=Vector((0,0,(hi+lo)/2))
 bpy.ops.object.camera_add(location=center+Vector((0,-height*2,0)));camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=height*1.15;scene.camera=camera
 scene.render.filepath=str(out/('controlled-'+pose+'.png'));bpy.ops.render.render(write_still=True)
