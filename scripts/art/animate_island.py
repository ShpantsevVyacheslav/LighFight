"""Author draft motion clips locally; all poses use the existing weighted rig.
blender -b island-combat-hands.blend --python animate_island.py -- output-directory
These are blocking animation drafts, not motion capture or release-ready clips.
"""
import bpy
import math
import sys
import json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

out = Path(sys.argv[sys.argv.index('--')+1]); out.mkdir(parents=True, exist_ok=True)
arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
bpy.context.view_layer.objects.active = arm
arm.animation_data_clear()
for action in list(bpy.data.actions):
    bpy.data.actions.remove(action)
scene = bpy.context.scene; scene.render.fps = 60

def aim(name, direction):
    p = arm.pose.bones[name]
    rest = p.bone.matrix_local
    swing = (rest.to_3x3() @ Vector((0,1,0))).rotation_difference(Vector(direction).normalized())
    p.matrix = Matrix.Translation(p.head) @ swing.to_matrix().to_4x4() @ rest.to_3x3().to_4x4()
    bpy.context.view_layer.update()

def limb(upper, lower, target, guide):
    start = arm.pose.bones[upper].head.copy()
    end = Vector(target); direction = end-start
    a = arm.data.bones[upper].length; b = arm.data.bones[lower].length
    d = min(direction.length, (a+b)*.985)
    direction.normalize(); end = start + direction*d
    along = (a*a-b*b+d*d)/(2*d)
    pole = Vector(guide)-start
    pole = (pole-direction*pole.dot(direction)).normalized()
    joint = start+direction*along+pole*math.sqrt(max(0,a*a-along*along))
    aim(upper, joint-start); aim(lower,end-joint)

def pose(strike=0, kick=0, breath=0, cross=0, hook=0, uppercut=0):
    for p in arm.pose.bones:
        p.rotation_mode='QUATERNION'; p.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    hips=arm.pose.bones['Hips']
    hips.matrix=Matrix.Translation((.012*cross-.01*strike,-.025*(strike+cross),0)) @ hips.bone.matrix_local
    yaw=math.radians(28*(cross-strike)+40*hook-22*uppercut)
    arm.pose.bones['Spine'].rotation_quaternion=Quaternion((0,1,0),yaw*.55) @ Quaternion((1,0,0),-.035*(strike+cross))
    arm.pose.bones['Chest'].rotation_quaternion=Quaternion((0,1,0),yaw*.45)
    arm.pose.bones['Head'].rotation_quaternion=Quaternion((0,1,0),-yaw*.3)
    bpy.context.view_layer.update()
    # Slightly stagger the feet; preserve left supporting foot during the kick.
    limb('LeftUpperLeg','LeftLowerLeg',(.14,-.09,.1),(.15,-.11,.46))
    limb('RightUpperLeg','RightLowerLeg',(-.14,.08-kick*.82,.10+kick*.68),(-.15,-.11-kick*.34,.46+kick*.50))
    aim('LeftFoot',(0,-1,0)); aim('RightFoot',(0,-1,-kick*.15))
    limb('LeftUpperArm','LeftLowerArm',(.14-hook*.30,-.27-strike*.21-hook*.12,1.34),(.33,-.10-hook*.17,1.03+hook*.19))
    limb('RightUpperArm','RightLowerArm',(-.14,-.25-cross*.24-uppercut*.08,1.34+uppercut*.16),(-.33,-.10-uppercut*.07,1.03))
    aim('LeftHand',(0,-1,.1)); aim('RightHand',(0,-1,.1))
    p=arm.pose.bones['Spine']; p.rotation_quaternion.rotate(Vector((0,0,1)).rotation_difference(Vector((0,-breath*.006,1))))
    bpy.context.view_layer.update()

motions = [('Idle',90),('Guard',90),('jab',32),('frontKick',52),('cross',38),('hook',44),('uppercut',44)]
for name, count in motions:
    action=bpy.data.actions.new(name); action.use_fake_user=True
    arm.animation_data_create(); arm.animation_data.action=action
    scene.frame_start=1; scene.frame_end=count
    for frame in range(1,count+1):
        t=(frame-1)/(count-1)
        # Anticipation, fast extension, brief contact, controlled recovery.
        envelope = max(0, min((t-.12)/.24, (1-t)/.40, 1))
        pose(envelope if name=='jab' else 0,envelope if name=='frontKick' else 0,
             math.sin(t*math.tau) if name in ('Idle','Guard') else 0,
             envelope if name=='cross' else 0,envelope if name=='hook' else 0,envelope if name=='uppercut' else 0)
        for p in arm.pose.bones:
            p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name)
            p.keyframe_insert(data_path='location',frame=frame,group=p.name)
    for curve in action.fcurves:
        for key in curve.keyframe_points: key.interpolation='LINEAR'
arm.animation_data.action=bpy.data.actions['Guard']
scene.frame_start=1; scene.frame_end=90; scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
for obj in bpy.context.scene.objects:
    if obj.type=='MESH':obj.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'island-motion-draft.blend'))
bpy.ops.export_scene.fbx(filepath=str(out/'island-motion-draft.fbx'),use_selection=True,
                         object_types={'ARMATURE','MESH'},add_leaf_bones=False,
                         bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
                         bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
(out/'motion-audit.json').write_text(json.dumps({'fps':60,'clips':dict(motions),
    'status':'blocking drafts with combat hand variant and torso rotation; visual review required'},indent=2))
