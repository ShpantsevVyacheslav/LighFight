"""Render the stitched candidate from fist/palm and whole-character views."""
import bpy,bmesh,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];src,out=map(Path,args[:2]);selected=args[2:];out.mkdir(parents=True,exist_ok=True);bpy.ops.wm.open_mainfile(filepath=str(src))
scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');body=bpy.data.objects['char1'];camera=scene.camera
scene.cycles.samples=16;scene.render.resolution_x=900;scene.render.resolution_y=700
for name,frame,value,offset in [('impact-palm',10,1,(.1,0,-1)),('impact-front',10,1,(1,-.15,.1)),('impact-back',10,1,(.1,0,1)),('impact-side',10,1,(0,-1,0)),('rest-palm',1,0,(.1,0,-1))]:
 if selected and name not in selected:continue
 scene.frame_set(frame);body.data.shape_keys.key_blocks['ClosedFists'].value=value;bpy.context.view_layer.update()
 # Isolate the lower left arm so other limbs cannot hide the palm in close views.
 evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=bpy.context.evaluated_depsgraph_get())
 if len(mesh.vertices)!=len(body.data.vertices):raise RuntimeError('Review vertex correspondence changed')
 probe=bmesh.new();probe.from_mesh(mesh);probe.faces.ensure_lookup_table()
 discard=[probe.faces[p.index] for p in mesh.polygons if any(body.data.vertices[i].co.x<52 for i in p.vertices)]
 bmesh.ops.delete(probe,geom=discard,context='FACES_ONLY');probe.to_mesh(mesh);probe.free()
 isolated=bpy.data.objects.new('IsolatedHandReview',mesh);scene.collection.objects.link(isolated);isolated.matrix_world=body.matrix_world.copy();body.hide_render=True
 transform=arm.pose.bones['LeftHand'].matrix@arm.data.bones['LeftHand'].matrix_local.inverted();wrist=arm.data.bones['LeftHand'].head_local
 focus=arm.matrix_world@transform@(wrist+Vector((10,0,1)))
 direction=(arm.matrix_world.to_3x3()@transform.to_3x3()@Vector(offset)).normalized()
 camera.location=focus+direction*.9;camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=.40
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 bpy.data.objects.remove(isolated,do_unlink=True);body.hide_render=False
scene.frame_set(10);body.data.shape_keys.key_blocks['ClosedFists'].value=1;bpy.context.view_layer.update();focus=Vector((0,0,.90));camera.location=focus+Vector((.5,-3,.2));camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=2.5;scene.render.filepath=str(out/'whole-character.png');bpy.ops.render.render(write_still=True)
