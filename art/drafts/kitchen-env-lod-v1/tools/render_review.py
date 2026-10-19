"""Render actual portable GLB, one state/process; same cameras/lights/material settings.
IndexOB masks provide per-furniture silhouette/hole evidence independent of beauty color.
"""
import bpy,sys,json,hashlib,time,gc
from pathlib import Path
from mathutils import Vector
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent));from glb_static import load
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/kitchen-env-lod-v1';args=sys.argv[sys.argv.index('--')+1:];state=args[0];assert state in ['master','lod-300000','lod-100000'];file=OUT/f'kitchen-{state}.glb';dest=OUT/'renders'/state;dest.mkdir(parents=True,exist_ok=True)
def main():
 g,b=load(file);triangles=sum(g['accessors'][p['indices']]['count']//3 for m in g['meshes'] for p in m['primitives']);digest=hashlib.sha256(file.read_bytes()).hexdigest();del g,b;gc.collect();bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(file));objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(objects)==36 and not any(o.type=='ARMATURE' for o in bpy.context.scene.objects);roleOf={}
 for o in objects:
  current=o;role=None
  while current is not None:
   if current.name.startswith('ROLE_'):role=current.name[5:].lower();break
   current=current.parent
  roleOf[o]=role;o.pass_index={'table':1,'fridge':2,'coffee':3}.get(role,0)
 assert sorted(r for r in roleOf.values() if r)==['coffee','fridge','table']
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.cycles.max_bounces=5;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.18,.18,.18);scene.view_settings.view_transform='AgX';scene.frame_set(1)
 target=Vector((0,0,1.15))
 for name,location,power,size in [('WindowLight',(3.6,-.1,2.3),600,3.2),('RoomKey',(-3,-4,6),700,5),('RoomFill',(-2,3,4),350,3)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=location;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
 cd=bpy.data.cameras.new('FixedReviewCamera');camera=bpy.data.objects.new('FixedReviewCamera',cd);scene.collection.objects.link(camera);scene.camera=camera;cd.type='ORTHO';bpy.context.view_layer.use_pass_object_index=True;scene.use_nodes=True;nodes=scene.node_tree.nodes;nodes.clear();layers=nodes.new('CompositorNodeRLayers');composite=nodes.new('CompositorNodeComposite');scene.node_tree.links.new(layers.outputs['Image'],composite.inputs['Image']);output=nodes.new('CompositorNodeOutputFile');output.base_path=str(dest);output.format.file_format='OPEN_EXR';output.format.color_mode='BW';output.format.color_depth='32';output.format.exr_codec='ZIP';scene.node_tree.links.new(layers.outputs['IndexOB'],output.inputs[0]);rows=[]
 views=[('whole',(0,0,1.15),(-11.2,-16.8,11.76),8.3,None),('table-legs',(1.5,0,.65),(-4.5,-6,3),3.9,'table'),('coffee-detail',(-2.35,1.5,.85),(-3,-4,2),2.25,'coffee')]
 for name,point,offset,scale,only in views:
  for o,role in roleOf.items():o.hide_render=bool(only and role and role!=only)
  target=Vector(point);camera.location=target+Vector(offset);camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();cd.ortho_scale=scale;output.file_slots[0].path=name+'-id-';scene.render.filepath=str(dest/(name+'.png'));started=time.time();bpy.ops.render.render(write_still=True);maskpath=dest/(name+'-id-0001.exr');assert maskpath.exists() and maskpath.stat().st_mtime>=started-1;image=bpy.data.images.load(str(maskpath),check_existing=False);w,h=image.size;pixels=np.asarray(image.pixels[:],np.float32).reshape(h,w,-1)[...,0];labels=np.rint(pixels).astype(np.uint8);unique=np.unique(labels);assert set(unique).issubset({0,1,2,3}) and np.any(labels>0),(name,unique);pgm=dest/(name+'-id.pgm');pgm.write_bytes(f'P5\n{w} {h}\n255\n'.encode()+np.flipud(labels).tobytes());rows.append({'view':name,'image':name+'.png','mask':pgm.name,'cameraWorldMatrix':[list(r) for r in camera.matrix_world],'orthoScale':scale,'visibleFurniture':'all' if only is None else only,'maskPixelsByRole':{str(i):int(np.count_nonzero(labels==i)) for i in unique}});(dest/'manifest.json').write_text(json.dumps({'state':state,'actualGLB':file.name,'actualGLBSHA256':digest,'trianglesIncludingArchitecture':triangles,'CPUthreads':6,'samples':16,'resolution':[1100,1000],'viewTransform':'AgX','images':rows,'scope':'Actual portableGLB image proof, not runtime/performance/phone acceptance.'},indent=2));bpy.data.images.remove(image);print('VIEW_DONE '+state+' '+name,flush=True)
 assert hashlib.sha256(file.read_bytes()).hexdigest()==digest;print('RESULT '+json.dumps({'state':state,'views':len(rows),'triangles':triangles,'sourceFileUnchanged':True}),flush=True)
main()
