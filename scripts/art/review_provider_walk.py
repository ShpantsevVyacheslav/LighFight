import bpy,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(root/'art/source/original-island/meshy-rig/walking.glb'));scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE')
scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=900;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Review');scene.world.color=(.08,.08,.08)
bpy.ops.object.camera_add(location=(2,-4,1.2));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.88))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2.0;scene.camera=camera
for loc,power,size in [((2,-3,4),450,4),((-3,-1,2),300,3),((0,3,3),400,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size;lamp.rotation_euler=(Vector((0,0,1))-lamp.location).to_track_quat('-Z','Y').to_euler()
out=root/'art/previews/original-island';out.mkdir(parents=True,exist_ok=True)
scene.frame_set(30);bpy.context.view_layer.update();scene.render.filepath=str(out/'provider-walk.png');bpy.ops.render.render(write_still=True)
