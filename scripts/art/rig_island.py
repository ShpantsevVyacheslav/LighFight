"""Blender background rig draft. Outputs stay outside the playable roster.

Run: blender --background --python rig_island.py -- source.fbx output-directory
Landmarks are specific to the approved Island A-pose; not a general autorigger.
"""
import bpy
import sys
import json
from pathlib import Path
from mathutils import Vector

source, destination = map(Path, sys.argv[sys.argv.index('--') + 1:])
destination.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
if not meshes:
    raise RuntimeError('No source meshes')
# Bake imported transforms before defining metric-space landmarks.
for obj in meshes:
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
points = [obj.matrix_world @ v.co for obj in meshes for v in obj.data.vertices]
lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
height = hi.z - lo.z
if height <= 0:
    raise RuntimeError('Invalid height')
center = Vector(((lo.x + hi.x)/2, (lo.y + hi.y)/2, lo.z))
for obj in meshes:
    for vertex in obj.data.vertices:
        vertex.co = (vertex.co - center) * (1.7 / height)

bpy.ops.object.select_all(action='DESELECT')
arm_data = bpy.data.armatures.new('IslandSkeleton')
arm = bpy.data.objects.new('IslandRigDraft', arm_data)
bpy.context.collection.objects.link(arm)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')

def bone(name, head, tail, parent=None):
    b = arm_data.edit_bones.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = arm_data.edit_bones[parent]
    return b

bone('Hips', (0,0,.86), (0,0,.99))
bone('Spine', (0,0,.99), (0,0,1.15), 'Hips')
bone('Chest', (0,0,1.15), (0,0,1.36), 'Spine')
bone('Neck', (0,0,1.36), (0,0,1.47), 'Chest')
bone('Head', (0,0,1.47), (0,0,1.65), 'Neck')
for side, sign in [('Left',1), ('Right',-1)]:
    bone(side+'Shoulder', (sign*.07,0,1.35),(sign*.20,0,1.32),'Chest')
    bone(side+'UpperArm',(sign*.20,0,1.32),(sign*.30,0,1.08),side+'Shoulder')
    bone(side+'LowerArm',(sign*.30,0,1.08),(sign*.39,-.015,.84),side+'UpperArm')
    bone(side+'Hand',(sign*.39,-.015,.84),(sign*.40,-.015,.72),side+'LowerArm')
    bone(side+'UpperLeg',(sign*.11,0,.88),(sign*.13,0,.48),'Hips')
    bone(side+'LowerLeg',(sign*.13,0,.48),(sign*.14,0,.10),side+'UpperLeg')
    bone(side+'Foot',(sign*.14,0,.10),(sign*.14,-.15,.055),side+'LowerLeg')
    bone(side+'Toes',(sign*.14,-.15,.055),(sign*.14,-.22,.055),side+'Foot')
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:
    obj.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

unweighted = sum(1 for obj in meshes for v in obj.data.vertices if sum(g.weight for g in v.groups) < .001)
report = {'source':source.name, 'heightMetres':1.7, 'bones':len(arm_data.bones),
          'vertices':sum(len(obj.data.vertices) for obj in meshes),
          'unweightedVertices':unweighted, 'status':'draft; visual deformation review required'}
(destination/'rig-audit.json').write_text(json.dumps(report,indent=2))
if unweighted:
    bpy.ops.wm.save_as_mainfile(filepath=str(destination/'island-rig-failed.blend'))
    raise RuntimeError(f'Automatic weights failed: {unweighted} unweighted vertices')
bpy.ops.wm.save_as_mainfile(filepath=str(destination/'island-rig-draft.blend'))
bpy.ops.export_scene.fbx(filepath=str(destination/'island-rig-draft.fbx'),use_selection=True,
                         object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,
                         axis_forward='-Z',axis_up='Y')
print(json.dumps(report))
