"""Read-only source geometry review; diagnostic axial-twist removal, not promotion."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
R=Path('/Users/macbook1/Documents/ChatGPT/LighFight');role=sys.argv[sys.argv.index('--')+1];row=next(r for r in json.loads((R/'art/drafts/full-roster-v1/inventory.json').read_text())['fighters'] if r['id']==role);O=R/('art/drafts/full-roster-v1/'+role+'-dense-positive');bpy.ops.wm.open_mainfile(filepath=str(O/'original-dense-native-spatial.blend'))
scene=bpy.context.scene;body=bpy.data.objects['OriginalDenseNative'];arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data_create();arm.animation_data.action=None;arm.data.pose_position='POSE'
for p in arm.pose.bones:p.matrix_basis.identity()
bpy.context.view_layer.update()
# Reload authored actions read-only: unlinked FBX actions may not survive blend save.
before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(R/row['nativePack']['path']))
for ob in set(bpy.data.objects)-before:ob.hide_render=True;ob.hide_set(True)
# Reference upper arms aligned horizontally from actual bind; geometry/bind untouched.
pairs=[]
for side in ['Left','Right']:
 p=arm.pose.bones[side+'_UpperArm'];c=arm.pose.bones[side+'_LowerArm'];delta=c.head-p.head;desired=Vector((1 if delta.x>0 else -1,0,0));p.matrix_basis.identity() # canonical computed below in armature space
 mat=p.matrix.copy();mat=delta.rotation_difference(desired).to_matrix().to_4x4()@mat;mat.translation=p.head;p.matrix=mat;bpy.context.view_layer.update();pairs.append((p,c,p.matrix_basis.to_quaternion(),c.location.copy(),p.matrix.inverted().to_3x3()@(c.head-p.head)))
for p in arm.pose.bones:p.matrix_basis.identity()
bpy.context.view_layer.update();pts=[body.matrix_world@Vector(c) for c in body.bound_box];lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)));h=hi.z-lo.z;target=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z+h*.7))
cd=bpy.data.cameras.new('DenseReviewCamera');cam=bpy.data.objects.new('DenseReviewCamera',cd);scene.collection.objects.link(cam);cam.location=target+Vector((0,-h*2.4,h*.04));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cd.type='ORTHO';cd.ortho_scale=h*.8;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True;scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.13,.13,.13);scene.view_settings.view_transform='AgX'
for name,offset,power in [('key',(-2,-3,4),600),('fill',(2,-2,2),350),('rim',(0,2,3),500)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=h*2;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=target+Vector(offset)*h/2;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
proof=[];modes={p.name:p.rotation_mode for p in arm.pose.bones}
for clip,phase in [('dance_01',.45),('dance_01',.5),('dance_01',.55),('box_02',.5),('front_kick_01',.5),('hit_to_head',.5)]:
 for bone in arm.pose.bones:bone.rotation_mode=modes[bone.name];bone.matrix_basis.identity()
 a=next(a for a in bpy.data.actions if a.name.split('|')[-1]==clip);arm.animation_data.action=a;f=a.frame_range[0]+phase*(a.frame_range[1]-a.frame_range[0]);scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update();arm.animation_data.action=None;label=f'{clip}-{int(phase*100)}';ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());bounds=[ev.matrix_world@Vector(v) for v in ev.bound_box];low=Vector(tuple(min(v[k] for v in bounds) for k in range(3)));high=Vector(tuple(max(v[k] for v in bounds) for k in range(3)));focus=(low+high)*.5;cam.location=focus+Vector((0,-h*2.4,h*.04));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=max(high.z-low.z,high.x-low.x)*1.14;scene.render.filepath=str(O/(label+'-native.png'));bpy.ops.render.render(write_still=True);rows=[]
 for p,c,canonical,unused,axis in pairs:
  axis.normalize();q=canonical.inverted()@p.matrix_basis.to_quaternion();v=Vector((q.x,q.y,q.z));proj=axis*v.dot(axis);tw=Quaternion((q.w,proj.x,proj.y,proj.z));assert tw.magnitude>1e-5;tw.normalize();swing=q@tw.inverted();elbow=c.head.copy();hand=c.children[0].head.copy();world=c.matrix.copy();p.rotation_mode='QUATERNION';p.rotation_quaternion=canonical@swing;bpy.context.view_layer.update();c.matrix=world;bpy.context.view_layer.update();e=(elbow-c.head).length;w=(hand-c.children[0].head).length;assert max(e,w)<1e-5,(e,w);rows.append({'bone':p.name,'removedTwistDeg':math.degrees(tw.angle),'elbowShiftM':e,'handShiftM':w})
 scene.render.filepath=str(O/(label+'-twist0-diagnostic.png'));bpy.ops.render.render(write_still=True);proof.append({'clip':clip,'phase':phase,'correction':rows})
(O/'twist-review.json').write_text(json.dumps({'status':'DIAGNOSTIC_NOT_PROMOTED','role':role,'geometryTriangles':sum(len(p.vertices)-2 for p in body.data.polygons),'cases':proof,'limits':['Bind-derived horizontal canonical reference; requires comparison with accepted Sailor canonical.', 'Pose-only render diagnostic. Original animation clips and bind matrices unchanged on disk.']},indent=2));print('RESULT master dense review complete')
