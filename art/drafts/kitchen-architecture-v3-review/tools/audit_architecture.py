"""Read-only source-geometry audit + bounded CPU architecture review. Blender4.2."""
import bpy,sys,json,hashlib,struct,math
from pathlib import Path
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/kitchen-architecture-v3-review';SOURCE=ROOT/'art/source/kitchen-tripo-v1/architecture-original-v3.glb'
def sha(data):return hashlib.sha256(data).hexdigest()
def main():
 raw=SOURCE.read_bytes();sourceSHA=sha(raw);length=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+length]);offset=20+length;size=struct.unpack_from('<I',raw,offset)[0];binary=raw[offset+8:offset+8+size]
 attrs=[]
 for mesh in g['meshes']:
  for prim in mesh['primitives']:
   for key,index in list(prim['attributes'].items())+[('INDICES',prim['indices'])]:
    a=g['accessors'][index];v=g['bufferViews'][a['bufferView']];start=v.get('byteOffset',0);attrs.append({'attribute':key,'count':a['count'],'componentType':a['componentType'],'type':a['type'],'bufferViewSHA256':sha(binary[start:start+v['byteLength']])})
 imageHashes=[]
 for im in g.get('images',[]):
  v=g['bufferViews'][im['bufferView']];start=v.get('byteOffset',0);imageHashes.append({'name':im.get('name'),'mimeType':im.get('mimeType'),'byteLength':v['byteLength'],'SHA256':sha(binary[start:start+v['byteLength']])})
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(SOURCE));objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(objects)==1
 obj=objects[0];mesh=obj.data;mesh.calc_loop_triangles();positions=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',positions);positions=positions.reshape(-1,3);tri=np.empty(len(mesh.loop_triangles)*3,np.int32);mesh.loop_triangles.foreach_get('vertices',tri);tri=tri.reshape(-1,3);uv=np.empty(len(mesh.uv_layers.active.data)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);importHashes={'positionsSHA256':sha(positions.tobytes()),'triangleIndicesSHA256':sha(tri.tobytes()),'loopUVSHA256':sha(uv.tobytes())};wm=np.asarray(obj.matrix_world);world=np.c_[positions,np.ones(len(positions))]@wm.T;world=world[:,:3];lo=world.min(0);hi=world.max(0);extent=hi-lo
 # Independent full-triangle BVH; no decimation/proxy. Interior grid excludes only5% perimeter.
 tree=BVHTree.FromPolygons([Vector(p) for p in world],tri.tolist(),all_triangles=True);N=101;rows=[];heights=[];miss=[];high=[]
 for iy,y in enumerate(np.linspace(lo[1]+.05*extent[1],hi[1]-.05*extent[1],N)):
  for ix,x in enumerate(np.linspace(lo[0]+.05*extent[0],hi[0]-.05*extent[0],N)):
   hit,normal,index,distance=tree.ray_cast(Vector((x,y,lo[2]+extent[2]*.15)),Vector((0,0,-1)),float(extent[2]*.2))
   if hit is None:miss.append([ix,iy]);continue
   relative=(hit.z-lo[2])/extent[2]
   if relative>.08:high.append([ix,iy,float(relative)]);continue
   heights.append(float(hit.z));rows.append({'x':float(x),'y':float(y),'floorZ':float(hit.z),'normalZ':float(normal.z)})
 grid={'grid':N,'interiorMarginFraction':.05,'rays':N*N,'lowBandOriginHeightFraction':.15,'floorAcceptHeightFraction':.08,'floorHits':len(heights),'missing':len(miss),'highBandHits':len(high),'missGridCoordinates':miss,'highBandCoordinates':high,'floorMinZ':min(heights) if heights else None,'floorMaxZ':max(heights) if heights else None,'floorP01P50P99':np.quantile(heights,[.01,.5,.99]).tolist() if heights else None,'allInteriorGridHasLowFloor':not miss and not high};(OUT/'floor-grid.json').write_text(json.dumps({'summary':grid,'samples':rows},indent=2))
 report={'status':'VISUAL_REVIEW_REQUIRED','source':str(SOURCE.relative_to(ROOT)),'sourceSHA256':sourceSHA,'BlenderVersion':bpy.app.version_string,'meshes':len(objects),'actualVertices':len(mesh.vertices),'actualTriangles':len(mesh.loop_triangles),'boundsMinXYZ_BlenderZUp':lo.tolist(),'boundsMaxXYZ_BlenderZUp':hi.tolist(),'extentsXYZ':extent.tolist(),'sourceAttributes':attrs,'embeddedImages':imageHashes,'importedGeometryHashes':importHashes,'floorGrid':grid,'sourceGeometryModified':False,'sourceFilePreserved':True,'units':'Provider normalized units; real room scale not inferred.'};(OUT/'audit.json').write_text(json.dumps(report,indent=2))
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.14,.14,.14);scene.view_settings.view_transform='AgX';scene.render.film_transparent=False
 center=Vector((lo+hi)/2);span=float(max(extent));cameraData=bpy.data.cameras.new('ArchitectureReviewCamera');camera=bpy.data.objects.new('ArchitectureReviewCamera',cameraData);scene.collection.objects.link(camera);scene.camera=camera;cameraData.type='ORTHO';cameraData.ortho_scale=span*1.35;cameraData.lens=50
 for name,direction,power in [('ReviewKey',(-1,-2,3),120),('ReviewFill',(2,1,2),70)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=span*2;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=center+Vector(direction)*span;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
 for view,direction in [('front',(0,-3,1.1)),('threequarter',(2,-3,2.1)),('top',(0,0,4))]:
  camera.location=center+Vector(direction)*span;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/f'{view}.png');bpy.ops.render.render(write_still=True)
 assert sha(SOURCE.read_bytes())==sourceSHA,'Source bytes changed'
 check=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',check);assert sha(check.tobytes())==importHashes['positionsSHA256'];check=np.empty(len(mesh.uv_layers.active.data)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',check);assert sha(check.tobytes())==importHashes['loopUVSHA256'];check=np.empty(len(mesh.loop_triangles)*3,np.int32);mesh.loop_triangles.foreach_get('vertices',check);assert sha(check.tobytes())==importHashes['triangleIndicesSHA256']
 report['sourceAndImportedGeometryPreservedAfterRendering']=True;(OUT/'audit.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({'sourceSHA256':sourceSHA,'triangles':len(mesh.loop_triangles),'vertices':len(mesh.vertices),'floorGrid':{k:v for k,v in grid.items() if k not in ['missGridCoordinates','highBandCoordinates']},'renders':3,'sourcePreserved':True}),flush=True)
main()
