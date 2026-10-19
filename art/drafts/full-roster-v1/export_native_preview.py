import bpy,sys,json,hashlib,struct,math
from pathlib import Path
from mathutils import Vector
ROOT=Path('/Users/macbook1/Documents/ChatGPT/LighFight')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def preservation(meshes,arm):
 h=hashlib.sha256()
 for o in meshes:
  m=o.data
  for v in m.vertices:
   h.update(struct.pack('<3f',*v.co))
   for g in v.groups:h.update(struct.pack('<if',g.group,g.weight))
  for poly in m.polygons:h.update(struct.pack('<'+'I'*len(poly.vertices),*poly.vertices))
  for uv in m.uv_layers:
   for x in uv.data:h.update(struct.pack('<2f',*x.uv))
 for b in arm.data.bones:
  h.update(b.name.encode())
  for row in b.matrix_local:h.update(struct.pack('<4f',*row))
 return h.hexdigest()
def main():
 role=sys.argv[sys.argv.index('--')+1];inventory=json.loads((ROOT/'art/drafts/full-roster-v1/inventory.json').read_text());row=next(x for x in inventory['fighters'] if x['id']==role)
 source=ROOT/row['nativePack']['path'];out=ROOT/'prototypes/roster-portraits/models';out.mkdir(parents=True,exist_ok=True)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(source),use_anim=True,automatic_bone_orientation=False)
 scene=bpy.context.scene;meshes=[o for o in scene.objects if o.type=='MESH'];arms=[o for o in scene.objects if o.type=='ARMATURE'];assert len(arms)==1;arm=arms[0];actions=list(bpy.data.actions);assert len(actions)==8
 triangles=0
 for o in meshes:o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
 assert triangles in [99999,100000],triangles
 original=preservation(meshes,arm);fps=scene.render.fps/scene.render.fps_base;clips=[]
 for a in actions:
  arm.animation_data.action=a # FBX unused actions have id_root=0 until bound once in Blender4.2.
  assert a.id_root=='OBJECT'
  raw=a.name;a.name=raw.split('|')[-1];clips.append({'name':a.name,'sourceAction':raw,'firstFrame':float(a.frame_range[0]),'lastFrame':float(a.frame_range[1]),'durationSeconds':float((a.frame_range[1]-a.frame_range[0])/fps),'fcurves':len(a.fcurves)})
 # Imported native material nodes remain unchanged; original image files are read-only.
 inputs={str(source.relative_to(ROOT)):sha(source)}
 for f in sorted(source.with_suffix('.fbm').glob('*')):
  if f.is_file():inputs[str(f.relative_to(ROOT))]=sha(f)
 node_images=[]
 for o in meshes:
  for mat in o.data.materials:
   if mat and mat.node_tree:
    for n in mat.node_tree.nodes:
     if n.type=='TEX_IMAGE' and n.image:
      image=n.image;path=Path(bpy.path.abspath(image.filepath));assert path.is_file(),path;node_images.append({'name':image.name,'file':str(path.relative_to(ROOT)),'sha256':sha(path),'size':list(image.size),'colorSpace':image.colorspace_settings.name})
 arm.animation_data.action=None
 for pb in arm.pose.bones:pb.matrix_basis.identity()
 bpy.context.view_layer.update()
 for o in scene.objects:o.select_set(o in meshes or o==arm)
 bpy.context.view_layer.objects.active=arm
 target=out/f'{role}.glb'
 bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS',export_anim_single_armature=True,export_frame_range=False,export_force_sampling=True,export_frame_step=1,export_anim_slide_to_zero=True,export_optimize_animation_size=False,export_rest_position_armature=True,export_reset_pose_bones=True,export_skins=True,export_all_influences=True,export_apply=False,export_texcoords=True,export_normals=True,export_tangents=True,export_materials='EXPORT',export_image_format='AUTO',export_keep_originals=True,export_cameras=False,export_lights=False,export_extras=True)
 assert preservation(meshes,arm)==original,'Source geometry/UV/weights/binds changed during export'
 raw=target.read_bytes();magic,version,total=struct.unpack_from('<III',raw);assert magic==0x46546c67 and version==2 and total==len(raw);jl,jt=struct.unpack_from('<II',raw,12);g=json.loads(raw[20:20+jl]);bl,bt=struct.unpack_from('<II',raw,20+jl);binary=raw[28+jl:28+jl+bl]
 glbtris=sum(g['accessors'][p['indices']]['count']//3 for m in g['meshes'] for p in m['primitives']);assert glbtris==triangles,(glbtris,triangles)
 glbanim=[]
 for a in g.get('animations',[]):
  samplers=a['samplers'];duration=max(g['accessors'][s['input']]['max'][0] for s in samplers)-min(g['accessors'][s['input']]['min'][0] for s in samplers);glbanim.append({'name':a['name'],'durationSeconds':duration,'channels':len(a['channels'])})
 assert len(glbanim)==8,glbanim
 for c in clips:
  found=next(a for a in glbanim if a['name']==c['name']);assert abs(found['durationSeconds']-c['durationSeconds'])<1e-4
 embedded=[]
 for im in g.get('images',[]):
  view=g['bufferViews'][im['bufferView']];data=binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']];digest=hashlib.sha256(data).hexdigest();embedded.append({'name':im.get('name'),'sha256':digest,'bytes':len(data),'mimeType':im['mimeType'],'exactSourceFiles':[p for p,h in inputs.items() if h==digest]})
 report={'role':role,'blenderVersion':bpy.app.version_string,'sourceFBX':str(source.relative_to(ROOT)),'sourceSHA256':sha(source),'glb':str(target.relative_to(ROOT)),'glbSHA256':sha(target),'bytes':len(raw),'sourceTriangles':triangles,'glbTriangles':glbtris,'sourceControlVertices':sum(len(o.data.vertices) for o in meshes),'sourceBones':len(arm.data.bones),'glbSkinJoints':[len(s['joints']) for s in g['skins']],'sourcePreservationHashBeforeAfter':original,'framesPerSecond':fps,'sourceClips':clips,'animations':glbanim,'defaultAnimation':'box_02','defaultAnimationTimeSeconds':0,'defaultAnimationPlayback':'hold; no auto attack loop','firstFrameGuardCandidate':{'animation':'box_02','seconds':0,'sourceFrame':1,'poseValidity':'pending visual review; authored first pose, not standalone idle'},'danceAnimation':'dance_01','punchAnimation':'box_02','boxingIdleAnimation':None,'inputs':inputs,'sourceImages':node_images,'embeddedImages':embedded,'limits':['Original native FBX imported by Blender; glTF baked at source24fps, not original FBX tangent/curve serialization.','No standalone boxing idle in these8takes. box_02 is a punch loop, not guard.','Native source material scalar encoding retained; no guessed gamma correction, normalYflip, recolor or originaldense-atlas substitution.','Portable prototype export, not shipping Unity/native-phone parity proof.']}
 (out/f'{role}.manifest.json').write_text(json.dumps(report,indent=2))
 # PBR visual review of actual native actions; render-only camera/lights added AFTER export.
 scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.resolution_x=512;scene.render.resolution_y=640;scene.render.resolution_percentage=100
 scene.world.color=(.2,.2,.2);scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
 greet=next(a for a in actions if a.name=='greet_01');arm.animation_data.action=greet;scene.frame_set(round(greet.frame_range[0]));bpy.context.view_layer.update();corners=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector(tuple(min(p[i] for p in corners) for i in range(3)));hi=Vector(tuple(max(p[i] for p in corners) for i in range(3)));center=(lo+hi)*.5;height=hi.z-lo.z
 camera_data=bpy.data.cameras.new('QA camera');camera=bpy.data.objects.new('QA camera',camera_data);scene.collection.objects.link(camera);camera.location=center+Vector((0,-height*2.4,height*.05));camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=height*1.15;scene.camera=camera
 for name,pos,power,size in [('key',(-2,-3,4),650,3),('fill',(2,-2,2),300,3),('rim',(0,2,3),450,2)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=center+Vector(pos)*height/2;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
 for name,phase in [('greet_01',0),('box_02',0),('box_02',.45)]:
  a=next(a for a in actions if a.name==name);arm.animation_data.action=a;scene.frame_set(round(a.frame_range[0]+phase*(a.frame_range[1]-a.frame_range[0])));scene.render.filepath=str(out/f'{role}-review-{name}-{phase}.png');bpy.ops.render.render(write_still=True)
 assert all(sha(ROOT/p)==h for p,h in inputs.items()),'Source file changed'
 print('RESULT '+json.dumps({'role':role,'glb':str(target),'bytes':len(raw),'triangles':triangles,'animations':glbanim}))
main()
