"""Separate static-prop geometry derivative; characters are forbidden inputs.
Original Meshy GLB never overwritten. Original stored PBR images are reattached byte-exactly.
blender ... -P make_prop_lod.py -- SOURCE OUTPUT TARGET_TRIANGLES
Acceptance still requires actual screen-space source/LOD comparisons.
"""
import bpy,sys,json,hashlib,bmesh
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];src=Path(args[0]).resolve();out=Path(args[1]).resolve();target=int(args[2]);ROOT=Path(__file__).resolve().parents[4];sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'));from glb_static import load,attach_original_materials,audit_images
assert 'meeting-meshy-props-v1' in str(src) or src==ROOT/'art/source/meeting-tripo-v1/original-v1.glb','Only approved static meeting environment sources allowed';assert src!=out and target>=1000;g,_=load(src);assert len(g['materials'])==1 and not g.get('skins') and not g.get('animations'),'Static single-material source required';before=hashlib.sha256(src.read_bytes()).hexdigest();bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(src));meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes);ratio=min(1,target/tris)
welded=0
for obj in meshes:
 high=obj.copy();high.data=obj.data.copy();bpy.context.scene.collection.objects.link(high);high.hide_render=True;
 bm=bmesh.new();bm.from_mesh(obj.data);beforeVerts=len(bm.verts);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);welded+=beforeVerts-len(bm.verts);bm.to_mesh(obj.data);bm.free();obj.data.update()
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;mod=obj.modifiers.new('Static prop screen-space LOD','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 transfer=obj.modifiers.new('Preserve original surface normals','DATA_TRANSFER');transfer.object=high;transfer.use_loop_data=True;transfer.data_types_loops={'CUSTOM_NORMAL'};transfer.loop_mapping='POLYINTERP_NEAREST';bpy.ops.object.modifier_apply(modifier=transfer.name);bpy.data.objects.remove(high,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
out.parent.mkdir(parents=True,exist_ok=True);geom=out.with_name(out.stem+'-geometry.glb');bpy.ops.export_scene.gltf(filepath=str(geom),export_format='GLB',use_selection=True,export_materials='NONE',export_normals=True,export_texcoords=True,export_tangents=True)
attach_original_materials(geom,src,out);lod,_=load(out);actual=sum(lod['accessors'][p['indices']]['count']//3 for m in lod['meshes'] for p in m['primitives']);assert actual<=target+max(100,int(target*.03));assert audit_images(src)==audit_images(out);assert hashlib.sha256(src.read_bytes()).hexdigest()==before
receipt={'status':'LOD_GEOMETRY_CREATED_REQUIRES_SCREEN_SPACE_COMPARISON','sourceSHA256':before,'originalUnchanged':True,'sourceTriangles':tris,'targetTriangles':target,'actualTriangles':actual,'storedImagesExact':True,'sourceSurfaceNormalsTransferred':True,'exactPositionUVSeamDuplicatesWelded':welded,'LODType':'Static prop geometry derivative only, no character assets, no texture reduction','catalogPublished':False};out.with_suffix('.json').write_text(json.dumps(receipt,indent=2));print('AGENT_RESULT '+json.dumps(receipt),flush=True)
