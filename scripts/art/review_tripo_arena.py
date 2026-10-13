"""Render the untouched generated courtyard and measure candidate ground footprint."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];source=root/'art/source/courtyard-tripo-v1/original.glb';out=root/'art/previews/courtyard-tripo-v1/source-local';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard'
scene.world=bpy.data.worlds.new('Courtyard audit');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.08,.09,1)
meshes=[o for o in scene.objects if o.type=='MESH' and len(o.data.vertices)>1000]
for material in bpy.data.materials:
 if not material.use_nodes:continue
 nodes=material.node_tree.nodes;p=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None);output=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
 if not p or not output:continue
 e=nodes.new('ShaderNodeEmission');e.inputs[0].default_value=p.inputs['Base Color'].default_value
 if p.inputs['Base Color'].is_linked:material.node_tree.links.new(p.inputs['Base Color'].links[0].from_socket,e.inputs[0])
 material.node_tree.links.new(e.outputs[0],output.inputs['Surface'])
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(lo+hi)*.5;size=hi-lo
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO'
for name,offset in [('front',Vector((0,-3,1))),('overview',Vector((2,-3,2))),('top',Vector((0,0,4)))]:
 camera.location=center+offset*max(size);camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=max(size)*1.1;scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
# Exact source coordinates; these measurements do not certify a playable area.
report={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),'bounds':[list(lo),list(hi)],'textures':[{'name':i.name,'size':list(i.size)} for i in bpy.data.images if i.size[0]>0 and i.name!='Render Result'],'geometry_modified':False,'playable_area_validated':False}
(out/'audit.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(root/'art/source/courtyard-tripo-v1/source-review.blend'));print('ARENA_SOURCE '+json.dumps(report))
