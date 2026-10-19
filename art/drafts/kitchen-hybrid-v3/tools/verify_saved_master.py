"""Read-only reopen validation of all saved Tripo components and packed source image bytes."""
import bpy,json,hashlib
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/kitchen-hybrid-v3';BLEND=OUT/'kitchen-hybrid-v3.blend'
def sha(data):return hashlib.sha256(data).hexdigest()
def meshhash(mesh):
 mesh.calc_loop_triangles();p=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',p);t=np.empty(len(mesh.loop_triangles)*3,np.int32);mesh.loop_triangles.foreach_get('vertices',t);uv=np.empty(len(mesh.uv_layers.active.data)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);return {'positionSHA256':sha(p.tobytes()),'triangleSHA256':sha(t.tobytes()),'loopUVSHA256':sha(uv.tobytes()),'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles)}
def main():
 bpy.ops.wm.open_mainfile(filepath=str(BLEND));specs=[('table','UNCHANGED_TRIPO_TABLE_AND_4_CHAIRS','kitchen-hybrid-v1','sourceFurnitureLocalGeometryHashes','sourceEmbeddedImages','sourceFurniture','sourceSHA256'),('fridge','UNCHANGED_TRIPO_FRIDGE_MICROWAVE_GROUP','kitchen-hybrid-v2','newSourceMeshHashes','newSourceEmbeddedImages','newSource','newSourceSHA256'),('coffee','UNCHANGED_TRIPO_COFFEE_GROUP','kitchen-hybrid-v3','newSourceMeshHashes','newSourceEmbeddedImages','newSource','newSourceSHA256')];rows=[]
 for label,root,folder,geometry,imageKey,sourceKey,shaKey in specs:
  m=json.loads((ROOT/'art/drafts'/folder/'manifest.json').read_text());obj=bpy.data.objects[root].children[0];actual=meshhash(obj.data);assert actual==m[geometry],label;assert not obj.modifiers,label;assert sha((ROOT/m[sourceKey]).read_bytes())==m[shaKey],label;images=[]
  for sourceImage in m[imageKey]:
   image=bpy.data.images.get(sourceImage['name']);assert image is not None and image.packed_file,sourceImage['name'];actualSHA=sha(bytes(image.packed_file.data));assert actualSHA==sourceImage['SHA256'],(sourceImage['name'],actualSHA,sourceImage['SHA256']);images.append({'name':sourceImage['name'],'packedBytesExact':True,'SHA256':actualSHA})
  rows.append({'component':label,'geometryExactAfterSaveReopen':True,'geometry':actual,'packedSourceImages':images,'rawMasterPreserved':True})
 assert not any(o.name.startswith('BLOCKOUT_') for o in bpy.data.objects);floor=bpy.data.objects['Clean_Rectangular_Continuous_Floor'];floor.data.calc_loop_triangles();assert len(floor.data.loop_triangles)==12;report={'status':'PASS','savedMasterSHA256':sha(BLEND.read_bytes()),'components':rows,'totalSourceTriangles':sum(r['geometry']['triangles'] for r in rows),'totalSourceVertices':sum(r['geometry']['vertices'] for r in rows),'floorTriangles':12,'remainingBlockouts':0,'sourceGeometryReduction':False,'sourceImageRecompression':False,'limits':'Saved Blender master read-only exact-data proof; no runtime/mobile acceptance.'};(OUT/'saved-master-proof.json').write_text(json.dumps(report,indent=2));print('RESULT '+json.dumps({'status':'PASS','sourceTriangles':report['totalSourceTriangles'],'sourceVertices':report['totalSourceVertices'],'exactPackedImages':sum(len(r['packedSourceImages']) for r in rows)}),flush=True)
main()
