"""Measure native shoulder placement and Guard motion; never alter source rigs."""
import bpy,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
source=root/'unity/Assets/LighFight/Art/Drafts/MeshyWebIsland/web-motions.fbx'
if '--' in sys.argv:source=Path(sys.argv[sys.argv.index('--')+1])
if source.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(source))
else:bpy.ops.import_scene.fbx(filepath=str(source))
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
names={b.name.split(':')[-1]:b.name for b in arm.data.bones}
selected=['Neck','Head','LeftShoulder','RightShoulder','LeftArm','RightArm','Spine2']
rest={n:arm.matrix_world@arm.data.bones[names[n]].head_local for n in selected}
up=(rest['Head']-rest['Spine2']).normalized()
def metrics(points):
 return {n:dict(position=list(points[n]),height_relative_neck_m=(points[n]-points['Neck']).dot(up)) for n in selected}
action=arm.animation_data.action if source.suffix=='.glb' else next(a for a in bpy.data.actions if a.name.split('|')[-1]=='Boxing_Guard_SmartRigArmature')
arm.animation_data_create()
for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
arm.animation_data.action=action
samples=[]
for phase in [0,.25,.5,.75,1]:
 f=action.frame_range[0]+(action.frame_range[1]-action.frame_range[0])*phase;bpy.context.scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
 points={n:arm.matrix_world@arm.pose.bones[names[n]].head for n in selected}
 samples.append(dict(phase=phase,joints=metrics(points),arm_height_change_m={n:(points[n]-points['Neck']-rest[n]+rest['Neck']).dot(up) for n in ['LeftArm','RightArm']}))
report=dict(source=str(source),joint_count=len(names),clip=action.name,rest=metrics(rest),samples=samples)
out=root/('art/previews/source-quality/island-shoulder-placement-glb.json' if source.suffix=='.glb' else 'art/previews/source-quality/island-shoulder-placement.json');out.parent.mkdir(exist_ok=True,parents=True);out.write_text(json.dumps(report,indent=2));print(json.dumps(report))
