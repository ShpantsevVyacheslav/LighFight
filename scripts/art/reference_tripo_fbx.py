"""Create an independent Blender reference for a native vendor FBX clip."""
import bpy,json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from export_native_web_glb import character,poses,rest
root=Path(__file__).resolve().parents[2];path=next((root/'art/source/island-tripo-v1/unity-pack').glob('*.fbx'))
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path));arm,meshes=character();action=next(a for a in bpy.data.actions if 'box_01' in a.name)
vertex_samples,duration,bone_samples=poses(arm,meshes[0],action);surface=rest(arm,meshes);indices=list(range(0,len(surface['positions']),31));uv={}
for loop in meshes[0].data.loops:uv.setdefault(loop.vertex_index,list(meshes[0].data.uv_layers.active.data[loop.index].uv))
def vec(p):return dict(zip(['x','y','z'],p))
report={'duration':duration,'boneNames':[b.name for b in arm.pose.bones],'rest':[vec(arm.matrix_world@b.matrix.translation) for b in arm.pose.bones],'samples':[{'points':[vec(p) for p in frame]} for frame in bone_samples],'vertexIndices':indices,'vertexRest':[vec(surface['positions'][i]) for i in indices],'vertexUV':[{'x':uv[i][0],'y':uv[i][1]} for i in indices],'vertexSamples':[{'points':[vec(p) for p in frame]} for frame in vertex_samples]}
path=root/'.tools/tripo-review-unity/Assets/LighFight/Art/Drafts/TripoIsland/native-pack.reference.json';path.write_text(json.dumps(report,separators=(',',':')));print('REFERENCE '+str(path)+' '+str(len(indices))+' vertices x37frames')
