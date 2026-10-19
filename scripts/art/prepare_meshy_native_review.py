"""Preserve provider mesh, weights and bind pose; combine only matching native clips."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];src=root/'art/source/original-island';out=root/'unity/Assets/LighFight/Art/Drafts/MeshyNativeIsland';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(src/'meshy-guard.fbx'))
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');meshes=[o for o in bpy.data.objects if o.type=='MESH'];guard=arm.animation_data.action;guard.name='MeshyGuard';guard.use_fake_user=True
before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(src/'meshy-boxing.fbx'));added=set(bpy.data.objects)-before;other=next(o for o in added if o.type=='ARMATURE');boxing=other.animation_data.action;boxing.name='MeshyBoxing';boxing.use_fake_user=True
errors=[]
for bone in arm.data.bones:
 assert bone.name in other.data.bones
 errors.append(max(abs(bone.matrix_local[r][c]-other.data.bones[bone.name].matrix_local[r][c]) for r in range(4) for c in range(4)))
assert max(errors)<.0001,'Provider animations use different bind poses'
for obj in added:bpy.data.objects.remove(obj,do_unlink=True)
for mesh in meshes:
 mat=mesh.data.materials[0];mat.use_nodes=True;bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(src/'albedo.png'));mat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.65
arm.animation_data.action=None
for bone in arm.pose.bones:bone.matrix_basis.identity()
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
for mesh in meshes:mesh.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(out/'native-motions.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY')
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.render.resolution_x=900;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Review');scene.world.color=(.08,.08,.08)
bpy.ops.object.camera_add(location=(2,-4,1.2));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.88))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2;scene.camera=camera
for loc,power in [((2,-3,4),450),((-3,-1,2),300),((0,3,3),400)]:
 bpy.ops.object.light_add(type='AREA',location=loc);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=4;lamp.rotation_euler=(Vector((0,0,1))-lamp.location).to_track_quat('-Z','Y').to_euler()
previews=root/'art/previews/meshy-native';previews.mkdir(parents=True,exist_ok=True)
report={'bindMatrixError':max(errors),'sourceGeometryModified':False,'sourceWeightsModified':False,'clips':[]}
for action in [guard,boxing]:
 arm.animation_data.action=action;first,last=action.frame_range;report['clips'].append({'name':action.name,'first':float(first),'last':float(last)})
 for phase in [0,.5]:
  scene.frame_set(round(first+(last-first)*phase));bpy.context.view_layer.update();scene.render.filepath=str(previews/(action.name+'-'+str(phase)+'.png'));bpy.ops.render.render(write_still=True)
(src/'native-review-audit.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(src/'native-review.blend'));print(report)
