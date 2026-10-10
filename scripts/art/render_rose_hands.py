import bpy
from pathlib import Path
from mathutils import Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'art/previews/rose-hands-v1'
bpy.ops.wm.open_mainfile(filepath=str(root/'art/source/rose-hands-v1/candidate.blend'));scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Studio');scene.world.color=(.25,.25,.25)
bpy.ops.object.light_add(type='AREA',location=(0,-2,3));bpy.context.object.data.energy=350;bpy.context.object.data.size=4
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1100;scene.render.resolution_y=800;scene.render.resolution_percentage=100
for name,target,direction,size in [('whole',(0,0,0),(1,-3,.3),2.3),('left-side',(.74,.025,.55),(0,-1,.3),.34),('left-back',(.74,.025,.55),(0,.2,1),.34),('right-side',(-.74,.025,.55),(0,-1,.3),.34)]:
 target=Vector(target);cam.location=target+Vector(direction);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size;scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
