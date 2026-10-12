"""Convert a direct web GLB to Unity FBX with eight skin influences intact.
Verifies rest surface, all weights, UVs and evaluated animation samples.
Blender --background --python this.py -- input.glb output.fbx
"""
import bpy, json, sys, argparse
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from merge_visible_web_fbx import surface_snapshot
from native_fbx_compat import check_round_trip

def character():
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 meshes=[o for o in bpy.data.objects if o.type=='MESH' and len(o.data.vertices)>1000]
 assert len(meshes)==1
 return arm,meshes

def poses(arm,mesh,action):
 arm.animation_data_create();arm.animation_data.action=action
 lo,hi=action.frame_range;result=[];bone_samples=[];indices=range(0,len(mesh.data.vertices),31)
 for i in range(37):
  frame=lo+(hi-lo)*i/36;bpy.context.scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
  obj=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());evaluated=obj.to_mesh()
  result.append([list(mesh.matrix_world@evaluated.vertices[j].co) for j in indices]);obj.to_mesh_clear()
  bone_samples.append([list(arm.matrix_world@bone.matrix.translation) for bone in arm.pose.bones])
 return result,float((hi-lo)/bpy.context.scene.render.fps),bone_samples

def rest(arm,meshes):
 arm.animation_data_clear()
 for bone in arm.pose.bones:bone.matrix_basis.identity()
 bpy.context.view_layer.update();return surface_snapshot(meshes)

def main():
 parser=argparse.ArgumentParser();parser.add_argument('source',type=Path);parser.add_argument('target',type=Path);parser.add_argument('--step',type=float,default=0.0625);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);source,target=args.source,args.target;assert 0<args.step<=1
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(source));arm,meshes=character();actions=list(bpy.data.actions)
 assert len(actions)==1,'One direct native clip required';action=actions[0];name=action.name
 reference,duration,bone_samples=poses(arm,meshes[0],action);surface=rest(arm,meshes)
 bone_names=[bone.name for bone in arm.pose.bones];rest_bones=[list(arm.matrix_world@bone.matrix.translation) for bone in arm.pose.bones]
 # Exporter needs a live action binding; fake-user alone silently exports no motion.
 arm.animation_data_create();arm.animation_data.action=action
 bpy.context.scene.frame_start=int(action.frame_range[0]);bpy.context.scene.frame_end=int(action.frame_range[1])
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);meshes[0].select_set(True);bpy.context.view_layer.objects.active=arm
 target.parent.mkdir(parents=True,exist_ok=True)
 bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_step=args.step)
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(target));arm,meshes=character();actions=list(bpy.data.actions)
 assert len(actions)==1,'Export must retain exactly one action';actual,actual_duration,_=poses(arm,meshes[0],actions[0]);result=rest(arm,meshes)
 report={'clip':name,'triangles':result['triangles'],'vertices':len(result['positions']),'sampleCount':37,'bakeStep':args.step,'durationSeconds':duration,'durationError':abs(actual_duration-duration),'geometryModified':False,'retargeted':False}
 report.update(check_round_trip(surface,result));error=max(abs(x-y) for left,right in zip(reference,actual) for p,q in zip(left,right) for x,y in zip(p,q))
 report['maxAnimatedVertexErrorMeters']=error;report['animationPoseParityVerified']=error<0.0001 and report['durationError']<0.0001
 target.with_suffix('.reference.json').write_text(json.dumps({'duration':duration,'boneNames':bone_names,'rest':[{'x':p[0],'y':p[1],'z':p[2]} for p in rest_bones],'samples':[{'points':[{'x':p[0],'y':p[1],'z':p[2]} for p in frame]} for frame in bone_samples]},separators=(',',':')))
 target.with_suffix('.audit.json').write_text(json.dumps(report,indent=2));print(report);assert report['animationPoseParityVerified'],'FBX motion changed'
if __name__=='__main__':main()
