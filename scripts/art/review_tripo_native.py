import bpy,json,sys
from pathlib import Path
from mathutils import Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');tag=sys.argv[sys.argv.index('--')+1];out=root/('art/previews/island-tripo-v1/local-'+tag);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(root/('art/source/island-tripo-v1/island-h31-'+tag+'-boxing01.glb')))
arm=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None)
if arm and arm.animation_data and arm.animation_data.action:
 a,b=arm.animation_data.action.frame_range;bpy.context.scene.frame_set(int(a+(b-a)*.35))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=900;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard'
scene.world=bpy.data.worlds.new('Review background');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.035,.04,.05,1)
meshes=[o for o in scene.objects if o.type=='MESH' and len(o.data.vertices)>1000];pts=[]
for o in scene.objects:
 if o.type=='MESH' and o not in meshes:o.hide_render=True
for o in meshes:
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();pts.extend(o.matrix_world@v.co for v in me.vertices);ev.to_mesh_clear();lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));h=hi.z-lo.z;center=(lo+hi)/2
# Unlit original base color: isolates texture and surface silhouette from lighting.
for m in bpy.data.materials:
 if not m.use_nodes:continue
 nodes=m.node_tree.nodes;pr=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None);output=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
 if pr and output:
  em=nodes.new('ShaderNodeEmission');em.inputs[0].default_value=pr.inputs['Base Color'].default_value
  if pr.inputs['Base Color'].is_linked:m.node_tree.links.new(pr.inputs['Base Color'].links[0].from_socket,em.inputs[0])
  m.node_tree.links.new(em.outputs[0],output.inputs['Surface'])
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO'
for name,vec,target,scale in [('front',(0,-2,0),center,h*1.1),('side',(2,0,0),center,h*1.1),('face',(0,-2,0),Vector((center.x,center.y,lo.z+h*.89)),h*.29),('hands',(0,-2,0),Vector((hi.x-h*.12,center.y,lo.z+h*.49)),h*.32)]:
 cam.location=target+Vector(vec)*h;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print('REVIEW_RESULT '+json.dumps(dict(version=bpy.app.version_string,bounds=[list(lo),list(hi)],outputs=str(out))))
