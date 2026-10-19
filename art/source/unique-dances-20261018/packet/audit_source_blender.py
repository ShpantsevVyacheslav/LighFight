import bpy, json, pathlib, hashlib, sys
root=pathlib.Path(sys.argv[sys.argv.index('--')+1]);out=root/'audit';out.mkdir(exist_ok=True)
clips=[];inventory=[]
print('BLENDER_VERSION',bpy.app.version_string,flush=True)
for path in sorted(root.glob('action-*.fbx')):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(path),use_anim=True)
 arms=[o for o in bpy.data.objects if o.type=='ARMATURE']
 if len(arms)!=1:raise RuntimeError('Expected one donor armature '+str(path))
 arm=arms[0];action=arm.animation_data.action
 if not action:raise RuntimeError('No donor action '+str(path))
 lo,hi=action.frame_range;scene=bpy.context.scene
 names=[b.name for b in arm.pose.bones if any(s.lower() in b.name.lower() for s in ['Spine','Head','Arm','ForeArm','UpLeg','Leg','Foot','Hand']) and not any(s in b.name.lower() for s in ['thumb','index','middle','ring','pinky'])]
 samples=[];positions=[]
 for i in range(32):
  t=float(lo+(hi-lo)*i/32);scene.frame_set(int(t),subframe=t-int(t));bpy.context.view_layer.update()
  samples.append({n:list(arm.pose.bones[n].matrix_basis.to_quaternion())[1:]+[arm.pose.bones[n].matrix_basis.to_quaternion().w] for n in names})
  positions.append({n:list(arm.matrix_world@arm.pose.bones[n].head) for n in names})
 name=path.stem;clips.append({'name':name,'samples':samples})
 meshes=[o for o in bpy.data.objects if o.type=='MESH']
 inventory.append({'name':name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size,'action':action.name,'frame_range':[lo,hi],'fps':scene.render.fps/scene.render.fps_base,'duration_seconds':(hi-lo)/(scene.render.fps/scene.render.fps_base),'bone_count':len(arm.data.bones),'bones':[b.name for b in arm.data.bones],'sampled_joints':names,'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)} for o in meshes]})
 (out/(name+'-positions.json')).write_text(json.dumps(positions))
 print('ACTION_AUDITED',name,hi-lo,len(names),flush=True)
(out/'motion-samples.json').write_text(json.dumps({'clips':clips}))
(out/'inventory.json').write_text(json.dumps({'blender':bpy.app.version_string,'actions':inventory},indent=2))
print('AGENT_RESULT',json.dumps({'actions':len(clips),'output':str(out)}),flush=True)
