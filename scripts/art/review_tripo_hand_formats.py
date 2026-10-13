"""Render the same native box_01 phase close to the wrist in GLB or FBX."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];kind=sys.argv[sys.argv.index('--')+1];folder=root/'art/source/island-tripo-v1';out=root/'art/previews/island-tripo-v1'/('hand-format-'+kind);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
if kind=='glb':bpy.ops.import_scene.gltf(filepath=str(folder/'island-h31-mixamo-boxing01.glb'))
else:bpy.ops.import_scene.fbx(filepath=str(next((folder/'unity-pack').glob('*.fbx'))))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.world=bpy.data.worlds.new('Native hand review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.04,.05,.06,1)
for obj in scene.objects:
 if obj.type=='MESH' and len(obj.data.vertices)<1000:obj.hide_render=True
for material in bpy.data.materials:
 if not material.use_nodes:continue
 nodes=material.node_tree.nodes;p=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None);output=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
 if not p or not output:continue
 e=nodes.new('ShaderNodeEmission');e.inputs[0].default_value=p.inputs['Base Color'].default_value
 if p.inputs['Base Color'].is_linked:material.node_tree.links.new(p.inputs['Base Color'].links[0].from_socket,e.inputs[0])
 material.node_tree.links.new(e.outputs[0],output.inputs['Surface'])
arm=next(o for o in scene.objects if o.type=='ARMATURE');action=next(a for a in bpy.data.actions if 'box_01' in a.name);arm.animation_data.action=action;frame=action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*.35;scene.frame_set(math.floor(frame),subframe=frame%1);bpy.context.view_layer.update()
hand=next(b for b in arm.pose.bones if b.name in ['mixamorig:RightHand','Right_Hand']);target=arm.matrix_world@hand.matrix.translation
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=.28
for name,offset in [('front',Vector((0,-.3,.08))),('side',Vector((.25,-.15,.06)))]:
 camera.location=target+offset;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print('HAND_FORMAT '+json.dumps({'format':kind,'clip':action.name,'phase':.35,'hand':hand.name,'target':list(target),'geometry_modified':False}))
