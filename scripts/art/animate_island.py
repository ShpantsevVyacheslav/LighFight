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

def transition(name, t):
    if name == 'Walk':
        cycle=math.sin(t*math.tau)
        for side, sign in [('Left',1),('Right',-1)]:
            step=cycle*sign
            limb(side+'UpperLeg',side+'LowerLeg',(sign*.14,-.005+step*.13,.10+max(0,step)*.055),(sign*.15,-.16,.46))
            aim(side+'Foot',(0,-1,0))
    elif name == 'Crouch':
        # Lower the pelvis while keeping both soles planted.
        hips=arm.pose.bones['Hips']
        hips.location.y=-.22
        bpy.context.view_layer.update()
        for side, sign in [('Left',1),('Right',-1)]:
            limb(side+'UpperLeg',side+'LowerLeg',(sign*.14,-.005,.10),(sign*.15,-.28,.40))
            aim(side+'Foot',(0,-1,0))
        for side, sign in [('Left',1),('Right',-1)]:
            limb(side+'UpperArm',side+'LowerArm',(sign*.14,-.26,1.12),(sign*.33,-.10,.90))
            aim(side+'Hand',(0,-1,.1))
    elif name == 'Hit':
        recoil=math.sin(math.pi*t)*(1-t)
        arm.pose.bones['Spine'].rotation_quaternion=Quaternion((1,0,0),-.36*recoil)
        arm.pose.bones['Chest'].rotation_quaternion=Quaternion((1,0,0),-.25*recoil)
    elif name in ('Fall','GetUp'):
        # Shared continuous ground pose; the recovery reverses the fall blocking.
        u=t if name=='Fall' else 1-t
        u=u*u*(3-2*u)
        for side in ('Left','Right'):
            for part in ('UpperArm','LowerArm'):
                bone=arm.pose.bones[side+part]
                bone.rotation_quaternion=bone.rotation_quaternion.slerp(Quaternion(),u*.85)
        bpy.context.view_layer.update()
        hips=arm.pose.bones['Hips']
        pivot=hips.head.copy()
        rotation=Matrix.Rotation(-math.pi*.49*u,4,'X')
        hips.matrix=Matrix.Translation((0,.18*u,-.44*u)) @ Matrix.Translation(pivot) @ rotation @ Matrix.Translation(-pivot) @ hips.matrix
        bpy.context.view_layer.update()
        deps=bpy.context.evaluated_depsgraph_get()
        minimum=min((obj.matrix_world @ vertex.co).z
                    for obj in bpy.context.scene.objects if obj.type=='MESH'
                    for vertex in obj.evaluated_get(deps).data.vertices)
        if minimum < .018:
            hips.matrix=Matrix.Translation((0,0,.018-minimum)) @ hips.matrix
    bpy.context.view_layer.update()

motions = [('Walk',60),('Crouch',60),('Hit',30),('Fall',64),('GetUp',48),('Idle' ,90),('Guard',90),('jab',32),('frontKick',52),('cross',38),('hook',44),('uppercut',44)]
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
        transition(name,t)
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
    'status':'blocking drafts: combat hands, strikes, locomotion, recoil and floor-constrained fall/recovery; not release-ready'},indent=2))
