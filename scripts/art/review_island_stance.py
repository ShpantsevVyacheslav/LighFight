"""Native Meshy stance review, original surface and animation, no retargeting."""
import bpy,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];out=root/'art/previews/source-quality/stance-review';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(root/'art/previews/source-quality/native-stance-for-audit.glb'))
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');action=arm.animation_data.action
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32;scene.render.resolution_x=800;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Neutral review');scene.world.color=(.18,.2,.23);scene.view_settings.view_transform='AgX'
bpy.ops.object.light_add(type='AREA',location=(3,-4,5));bpy.context.object.data.energy=450;bpy.context.object.data.size=5
bpy.ops.object.light_add(type='AREA',location=(-3,-2,3));bpy.context.object.data.energy=220;bpy.context.object.data.size=4
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=2.2;scene.camera=cam
for phase in [0,.5]:
 f=action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*phase;scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
 for name,angle in [('front',0),('side',math.pi/2)]:
  cam.location=Vector((math.sin(angle)*4,-math.cos(angle)*4,1.2));cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'-'+str(phase)+'.png'));bpy.ops.render.render(write_still=True)
