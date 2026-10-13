"""Audit and render untouched Tripo GLB sources; no geometry/UV/texture edits."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
tag=sys.argv[sys.argv.index('--')+1]
source=Path(sys.argv[sys.argv.index('--source')+1]).resolve() if '--source' in sys.argv else root/'art/source'/f'{tag}-tripo-v1'/'original.glb'
out=root/'art/previews'/f'{tag}-tripo-v1'/'source-local';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1000;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard'
scene.world=bpy.data.worlds.new('Source audit');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.04,.045,.055,1)
meshes=[o for o in scene.objects if o.type=='MESH' and len(o.data.vertices)>1000]
if not meshes:raise RuntimeError('No character mesh')
for material in bpy.data.materials:
 if not material.use_nodes:continue
 nodes=material.node_tree.nodes;p=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None);output=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
 if not p or not output:continue
 emission=nodes.new('ShaderNodeEmission');emission.inputs[0].default_value=p.inputs['Base Color'].default_value
 if p.inputs['Base Color'].is_linked:material.node_tree.links.new(p.inputs['Base Color'].links[0].from_socket,emission.inputs[0])
 material.node_tree.links.new(emission.outputs[0],output.inputs['Surface'])
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(lo+hi)*.5;h=hi.z-lo.z
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO'
for name,fraction,scale in [('front',.5,max(h*1.15,(hi.x-lo.x)*1.2*1.15)),('face',.90,h*.24)]:
 target=Vector((center.x,center.y,lo.z+h*fraction));camera.location=target+Vector((0,-h*3,0));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
audit={'source':str(source.relative_to(root)),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'textures':[{'name':i.name,'size':list(i.size)} for i in bpy.data.images if i.size[0]>0 and i.name!='Render Result'],'rigs':sum(o.type=='ARMATURE' for o in scene.objects),'geometry_modified':False}
(out/'audit.json').write_text(json.dumps(audit,indent=2));print('SOURCE_AUDIT '+json.dumps(audit))
