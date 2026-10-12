"""Render native Tripo FBX clips without modifying geometry or skin weights."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight')
source=root/'art/source/island-tripo-v1/unity-pack'
out=root/'art/previews/island-tripo-v1/native-pack';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(next(source.glob('*.fbx'))))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=800;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard'
scene.world=bpy.data.worlds.new('Tripo review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.035,.04,.05,1)
meshes=[o for o in scene.objects if o.type=='MESH' and len(o.data.vertices)>1000]
arm=next(o for o in scene.objects if o.type=='ARMATURE')
actions=list(bpy.data.actions)
for m in bpy.data.materials:
 if not m.use_nodes:continue
 nodes=m.node_tree.nodes;pr=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None);output=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
 if pr and output:
  em=nodes.new('ShaderNodeEmission');em.inputs[0].default_value=pr.inputs['Base Color'].default_value
  if pr.inputs['Base Color'].is_linked:m.node_tree.links.new(pr.inputs['Base Color'].links[0].from_socket,em.inputs[0])
  m.node_tree.links.new(em.outputs[0],output.inputs['Surface'])
def bounds():
 pts=[]
 for o in meshes:
  ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();pts.extend(o.matrix_world@v.co for v in me.vertices);ev.to_mesh_clear()
 lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));return lo,hi
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO'
reports=[]
for action in actions:
 if "--" in sys.argv and action.name.split("|")[-1] not in sys.argv[sys.argv.index("--")+1:]:continue
 arm.animation_data.action=action;lo_f,hi_f=action.frame_range
 all_lo=[];all_hi=[]
 for phase in [0,.35,.7,1]:
  f=lo_f+(hi_f-lo_f)*phase;scene.frame_set(math.floor(f),subframe=f%1);lo,hi=bounds();all_lo.append(lo);all_hi.append(hi)
 lo=Vector(tuple(min(v[i] for v in all_lo) for i in range(3)));hi=Vector(tuple(max(v[i] for v in all_hi) for i in range(3)));h=hi.z-lo.z;center=(lo+hi)*.5
 cam.location=center+Vector((0,-2.5*h,0));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=max(h*1.15,(hi.x-lo.x)*scene.render.resolution_y/scene.render.resolution_x*1.15)
 name=action.name.split('|')[-1]
 for phase in [0,.35,.7,1]:
  f=lo_f+(hi_f-lo_f)*phase;scene.frame_set(math.floor(f),subframe=f%1);scene.render.filepath=str(out/(name+'-'+str(phase)+'.png'));bpy.ops.render.render(write_still=True)
 reports.append({'clip':name,'seconds':float((hi_f-lo_f)/scene.render.fps),'bounds':[list(lo),list(hi)]})
weights=[len([g for g in v.groups if g.weight>0]) for o in meshes for v in o.data.vertices]
report={'clips':reports,'unweighted':weights.count(0),'max_influences':max(weights),'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in meshes),'bones':len(arm.data.bones)}
(source/'pack-review.json').write_text(json.dumps(report,indent=2));print('REVIEW_RESULT '+json.dumps(report))
