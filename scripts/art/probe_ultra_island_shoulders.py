import bpy,json,sys
from pathlib import Path
from mathutils import Vector
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'art/previews/island-ultra-shoulder-probe';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(root/'art/source/island-ultra-v2/jab.glb'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=max((o for o in bpy.context.scene.objects if o.type=='MESH'),key=lambda o:len(o.data.vertices));action=arm.animation_data.action
for o in bpy.context.scene.objects:
 if o.type=='MESH' and o!=body:o.hide_render=True
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=900;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.world=bpy.data.worlds.new('Background');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.035,.04,.05,1)
for m in body.data.materials:
 if not m.use_nodes:continue
 nodes=m.node_tree.nodes;pr=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL');em=nodes.new('ShaderNodeEmission');em.inputs[0].default_value=pr.inputs['Base Color'].default_value
 if pr.inputs['Base Color'].is_linked:m.node_tree.links.new(pr.inputs['Base Color'].links[0].from_socket,em.inputs[0])
 m.node_tree.links.new(em.outputs[0],output.inputs['Surface'])
# Fixed world framing shared by all variants/frames; imported rig is1.8m tall.
bpy.ops.object.camera_add(location=(0,-4,.94));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.15;scene.camera=cam
original=[{body.vertex_groups[g.group].name:g.weight for g in v.groups} for v in body.data.vertices]
report={'frames':[],'affected_vertices':0,'removed_arm_weight':0}
for variant in ['source','axial']:
 if variant=='axial':
  anchors=[('Hips',99.55),('Spine02',112.02),('Spine01',124.49),('Spine',137.33)]
  for v in body.data.vertices:
   p=arm.matrix_world.inverted()@body.matrix_world@v.co
   if not (102<p.z<137 and abs(p.x)<25):continue
   old=original[v.index];report['removed_arm_weight']+=sum(w for n,w in old.items() if 'Arm' in n or 'Shoulder' in n)
   blend=min(1,(p.z-102)/4,(137-p.z)/4,(25-abs(p.x))/4)
   weights={}
   for (a,za),(b,zb) in zip(anchors,anchors[1:]):
    if za<=p.z<=zb:t=(p.z-za)/(zb-za);weights={a:1-t,b:t};break
   if not weights:continue
   for g in list(v.groups):body.vertex_groups[g.group].remove([v.index])
   for n in set(old)|set(weights):
    w=old.get(n,0)*(1-blend)+weights.get(n,0)*blend
    if w>1e-6:body.vertex_groups[n].add([v.index],w,'REPLACE')
   report['affected_vertices']+=1
 for phase in [0,.35,.7,1]:
  a,b=action.frame_range;frame=a+(b-a)*phase;scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
  report['frames'].append({'variant':variant,'phase':phase,'joints':{n:list(arm.matrix_world@arm.pose.bones[n].head) for n in ['neck','LeftShoulder','LeftArm','RightShoulder','RightArm']}})
  scene.render.filepath=str(out/(variant+'-'+str(phase)+'.png'));bpy.ops.render.render(write_still=True)
(root/'art/source/island-ultra-v2/shoulder-probe.json').write_text(json.dumps(report,indent=2));print('PROBE_RESULT',report['affected_vertices'],report['removed_arm_weight'])
