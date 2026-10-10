"""Blender review of a downloaded Meshy GLB; preserves provider skin weights.

blender -b --python review_meshy_animation.py -- model.glb output-directory
"""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector

model, output = map(Path, sys.argv[sys.argv.index('--') + 1:])
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(model))
scene = bpy.context.scene
meshes = [o for o in scene.objects if o.type == 'MESH']
rigs = [o for o in scene.objects if o.type == 'ARMATURE']
actions = list(bpy.data.actions)
if not rigs or not actions:
    raise RuntimeError('Expected a skinned model with a provider animation')
action = max(actions, key=lambda a: a.frame_range[1] - a.frame_range[0])
rig = rigs[0]
rig.animation_data_create()
for track in rig.animation_data.nla_tracks:
    track.mute = True
rig.animation_data.action = action
scene.frame_start = int(action.frame_range[0])
scene.frame_end = int(action.frame_range[1])
scene.frame_set(scene.frame_start)
points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
height = max(p.z for p in points) - min(p.z for p in points)
report = {'model': model.name, 'bones': len(rig.data.bones),
          'triangles': sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
          'restMeshBoundsHeightMeters': height, 'actions': [a.name for a in actions],
          'selectedAction': action.name, 'frames': list(action.frame_range),
          'importFps': scene.render.fps}
(output / 'audit.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report))
for o in meshes:
    for face in o.data.polygons:
        face.use_smooth = True
scene.render.engine = 'CYCLES'
scene.cycles.samples = 12
scene.render.resolution_x = 600
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.world.color = (.12, .12, .12)
scene.view_settings.view_transform = 'AgX'
bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
mat = bpy.data.materials.new('Review floor')
mat.diffuse_color = (.055, .065, .08, 1)
floor.data.materials.append(mat)
for loc, power, size in [((2,-3,4),220,4),((-2,-1,2),100,3),((0,3,3),200,3)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    light = bpy.context.object
    light.data.energy = power
    light.data.size = size
    light.rotation_euler = (Vector((0,0,.9))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-4,1.2))
camera = bpy.context.object
scene.camera = camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 2.25
camera.rotation_euler = (Vector((0,0,.85))-camera.location).to_track_quat('-Z','Y').to_euler()
# Sample 24 images across the complete clip; preserves source timing for review.
for i in range(24):
    frame = scene.frame_start + (scene.frame_end-scene.frame_start) * i / 23
    scene.frame_set(int(frame), subframe=frame % 1)
    scene.render.filepath = str(output / ('jab-%03d.png' % i))
    bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'review.blend'))
