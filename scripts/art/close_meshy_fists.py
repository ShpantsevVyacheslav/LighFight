"""EXPERIMENT ONLY: rejected hand-curl study on the connected Meshy surface.
Not suitable for Unity promotion: source finger topology folds/intersects.
Blender: -- source.blend output-directory
"""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
source,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE');obj=next(o for o in scene.objects if o.type=='MESH' and o.data.shape_keys)
key=obj.shape_key_add(name='ClosedFists');base=obj.data.shape_keys.key_blocks['Basis'];changed=0;wristMax=0;seams={}
def smooth(x):
 x=max(0,min(1,x));return x*x*(3-2*x)
for v in obj.data.vertices:
 p=base.data[v.index].co.copy();target=p.copy();side='Left' if p.x>0 else 'Right';sign=1 if p.x>0 else -1
 wrist=arm.data.bones[side+'Hand'].head_local;u=sign*(p.x-wrist.x);y=p.y-wrist.y;z=p.z-wrist.z
 hand=obj.vertex_groups[side+'Hand'].index;weight=next((g.weight for g in v.groups if g.group==hand),0)
 if weight>.2 and u>2:
  compressed=u-.18*max(0,min(u-2,7.5));fingerBase=10.5;compressedBase=fingerBase-.18*(fingerBase-2)
  x=compressed;newZ=z
  if u>fingerBase:
   angle=(u-fingerBase)/10.0*4.7;radius=2.7
   x=compressedBase+(radius+z+1.5)*math.sin(angle);newZ=-1.5-radius*(1-math.cos(angle))+(z+1.5)*math.cos(angle)
  newZ=z+.55*(newZ-z)
  fingers=Vector((x,y,newZ))
  # The thumb lies on the front side of the palm (negative local Y).
  pivot=Vector((4,0,0));d=Vector((compressed,y,z))-pivot;a=math.radians(65)
  thumb=pivot+Vector((math.cos(a)*d.x-math.sin(a)*d.y,math.sin(a)*d.x+math.cos(a)*d.y,d.z-.5*smooth((u-5)/5)))
  thumbBlend=smooth((-4.5-y)/2)*smooth((u-3)/3)
  local=fingers.lerp(thumb,thumbBlend)
  target=Vector((wrist.x+sign*local.x,wrist.y+local.y,wrist.z+local.z))
  target=p.lerp(target,smooth((u-2)/2))
 key.data[v.index].co=target;changed+=(target-p).length>1e-5
 if u<=2 and weight>.2:wristMax=max(wristMax,(target-p).length*.01)
 seams.setdefault(tuple(round(c,4) for c in p),[]).append(v.index)
for ids in seams.values():
 mean=sum((key.data[i].co for i in ids),Vector())/len(ids)
 for i in ids:key.data[i].co=mean
report={'changedVertices':changed,'wristBoundaryMaxDisplacementMeters':wristMax,'vertexCount':len(obj.data.vertices),'topologyPreserved':True,'sourceOpenHandsPreserved':True,'fingerBonesAdded':False,'status':'closed hand morph candidate; inspect thumb/finger intersections'}
(out/'fist-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));key.value=0
bpy.ops.wm.save_as_mainfile(filepath=str(out/'combat-fists.blend'))
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'island-combat-fists.fbx'),use_selection=True,use_mesh_modifiers=False,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
# Rest hand and guard close-ups, exact same camera for both variants.
scene.cycles.samples=8;scene.render.resolution_x=600;scene.render.resolution_y=500
for label,rest in [('rest',True),('guard',False)]:
 arm.data.pose_position='REST' if rest else 'POSE';scene.frame_set(1);bpy.context.view_layer.update()
 focus=arm.matrix_world@(arm.data.bones['LeftHand'].head_local if rest else arm.pose.bones['LeftHand'].head)+Vector((.045,0,0))
 camera=scene.camera;camera.location=focus+Vector((.3,-1,.25));camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=.29
 for variant,value in [('open',0),('closed',1)]:
  key.value=value;obj.data.update();bpy.context.view_layer.update();scene.render.filepath=str(out/(label+'-'+variant+'.png'));bpy.ops.render.render(write_still=True)
