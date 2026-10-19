"""Bake native FBX curves densely; preserve topology, UVs, weights and rig.

Every motion is checked against the untouched vendor FBX at 37 times.
Writes an independent Unity reference for each clip. No retargeting.
"""
import bpy, json, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_native_web_glb import character, poses, rest
from native_fbx_compat import check_round_trip

root = Path(__file__).resolve().parents[2]
tag = sys.argv[sys.argv.index('--') + 1]
folder = root / 'art/source' / f'{tag}-tripo-v1'
source = next((folder / 'unity-pack').glob('*.fbx'))
output = folder / 'baked-native'
output.mkdir(parents=True, exist_ok=True)
target = output / 'native-pack.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
arm, meshes = character()
actions = list(bpy.data.actions)
references = {}
for action in actions:
    name = action.name.split('|')[-1]
    if name in references:
        raise ValueError('Duplicate native motion: ' + name)
    frames, duration, bones = poses(arm, meshes[0], action)
    references[name] = (frames, duration, bones)
surface = rest(arm, meshes)
bone_names = [b.name for b in arm.pose.bones]
rest_bones = [list(arm.matrix_world @ b.matrix.translation) for b in arm.pose.bones]
indices = list(range(0, len(surface['positions']), 31))
uv = {}
for loop in meshes[0].data.loops:
    uv.setdefault(loop.vertex_index, list(meshes[0].data.uv_layers.active.data[loop.index].uv))
def vec(p):
    return dict(zip(('x', 'y', 'z'), p))
for name, (frames, duration, bones) in references.items():
    reference = {'duration': duration, 'boneNames': bone_names,
        'rest': [vec(p) for p in rest_bones],
        'samples': [{'points': [vec(p) for p in frame]} for frame in bones],
        'vertexIndices': indices, 'vertexRest': [vec(surface['positions'][i]) for i in indices],
        'vertexUV': [{'x': uv[i][0], 'y': uv[i][1]} for i in indices],
        'vertexSamples': [{'points': [vec(p) for p in frame]} for frame in frames]}
    (output / (name + '.reference.json')).write_text(json.dumps(reference, separators=(',', ':')))
arm.animation_data_create()
arm.animation_data.action = actions[0]
for action in actions:
    action.use_fake_user = True
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
meshes[0].select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=str(target), use_selection=True, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0, bake_anim_step=.0625)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(target))
arm, meshes = character()
actual_actions = {a.name.split('|')[-1]: a for a in bpy.data.actions}
if set(actual_actions) != set(references):
    raise ValueError('Native clips lost or added during export')
results = []
for name, (expected, duration, _) in references.items():
    actual, actual_duration, _ = poses(arm, meshes[0], actual_actions[name])
    error = max(abs(x-y) for left, right in zip(expected, actual)
                for p, q in zip(left, right) for x, y in zip(p, q))
    results.append({'clip': name, 'maxAnimatedVertexErrorMeters': error,
                    'durationError': abs(duration-actual_duration)})
    if error >= .0001 or abs(duration-actual_duration) >= .0001:
        raise ValueError('Native motion changed: ' + name)
result = rest(arm, meshes)
report = {'triangles': result['triangles'], 'geometryModified': False,
          'retargeted': False, 'bakeStep': .0625, 'clips': results}
report.update(check_round_trip(surface, result))
(output / 'pack-audit.json').write_text(json.dumps(report, indent=2) + '\n')
print('BAKED_PACK_RESULT ' + json.dumps(report))
