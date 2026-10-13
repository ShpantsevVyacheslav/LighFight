"""Prepare generated yard in game meters; flatten only the clear ground patch.
Original GLB is retained. No decimation, UV edits, or texture downscaling.
"""
import bpy,json,math,statistics,struct,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[2];out=root/'art/source/courtyard-tripo-v1/game-draft';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(root/'art/source/courtyard-tripo-v1/original.glb'));bpy.context.view_layer.update()
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.vertices)>1000)
source_triangles=sum(len(p.vertices)-2 for p in mesh.data.polygons)
scale=18/0.9792174100875854
# Blender Z up. Original Y becomes the horizontal combat X axis.
m=Matrix(((0,-scale,0,-.12*scale),(scale,0,0,-.05*scale),(0,0,scale,0),(0,0,0,1)))
mesh.data.transform(m@mesh.matrix_world);mesh.matrix_world=Matrix.Identity(4);mesh.data.update()
def tree():return BVHTree.FromPolygons([v.co for v in mesh.data.vertices],[p.vertices[:] for p in mesh.data.polygons],all_triangles=True)
bvh=tree()
def height(x,y):
 hit=bvh.ray_cast(Vector((x,y,20)),Vector((0,0,-1)),40)[0]
 if hit is None:raise RuntimeError('Ground missing at '+str((x,y)))
 return hit.z
sample_xy=[(x,y) for x in [-5,-2.5,0,2.5,5] for y in [-1.7,-.8,0,.8,1.7]]
samples=[height(x,y) for x,y in sample_xy]
plane=np.linalg.lstsq(np.array([[x,y,1] for x,y in sample_xy]),np.array(samples),rcond=None)[0]
residual=max(abs(z-(plane[0]*x+plane[1]*y+plane[2])) for (x,y),z in zip(sample_xy,samples))
if residual>.15:raise RuntimeError('Ground has obstacles, cannot align plane: '+str(residual))
rotation=Vector((-float(plane[0]),-float(plane[1]),1)).normalized().rotation_difference(Vector((0,0,1))).to_matrix().to_4x4()
mesh.data.transform(rotation);mesh.data.update();bvh=tree()
samples=[height(x,y) for x,y in sample_xy];ground=statistics.median(samples)
if max(samples)-min(samples)>.30:raise RuntimeError('Ground remained uneven after alignment: '+str(samples))
changed=0
for vertex in mesh.data.vertices:
 p=vertex.co;p.z-=ground
 # Blend outside the 11x4m footprint; retain all props and the outer terrain.
 if abs(p.x)<5.75 and abs(p.y)<2.25 and abs(p.z)<.32:
  weight=min(1,max(0,(5.75-abs(p.x))/.25),max(0,(2.25-abs(p.y))/.25))
  if weight>0:p.z*=1-weight;changed+=1
mesh.data.update();bvh=tree()
errors=[];obstacles=[]
for ix in range(23):
 for iy in range(9):
  x=-5.5+ix*.5;y=-2+iy*.5;z=height(x,y);errors.append(abs(z))
  hit=bvh.ray_cast(Vector((x,y,.05)),Vector((0,0,1)),2.8)[0]
  if hit is not None:obstacles.append([x,y,hit.z])
if max(errors)>.025 or obstacles:raise RuntimeError('Ground/clearance failed: '+str({'max':max(errors),'obstacles':obstacles[:10]}))
mesh.name='Courtyard native surface'
# Preserve embedded source maps with their exact stored resolution.
raw=(root/'art/source/courtyard-tripo-v1/original.glb').read_bytes();json_size=struct.unpack_from('<I',raw,12)[0];gltf=json.loads(raw[20:20+json_size]);binary=raw[28+json_size:];textures=[]
for item in gltf['images']:
 view=gltf['bufferViews'][item['bufferView']];data=binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']];path=out/Path(item['name']).name;path.write_bytes(data)
 image=bpy.data.images.get(item['name'])
 if image is None:raise RuntimeError('Source image not found '+item['name'])
 image.filepath_raw=str(path);textures.append({'path':path.name,'size':list(image.size),'sha256':hashlib.sha256(data).hexdigest(),'bytes_preserved':True})
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
bpy.ops.export_scene.fbx(filepath=str(out/'courtyard.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=False)
report={'source_triangles':source_triangles,'game_triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),'scale_to_meters':scale,'source_ground':ground,'ground_plane_fit_residual_m':residual,'ground_alignment_plane':plane.tolist(),'changed_ground_vertices':changed,'textures':textures,'ground_samples':len(errors),'ground_max_error_m':max(errors),'clearance_obstacles':obstacles,'clearance_height_m':2.85,'combat_area_m':[11,4],'decimation':False,'uv_modified':False,'device_verified':False}
(out/'layout-audit.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(out/'courtyard-game.blend'));print('ARENA_LAYOUT '+json.dumps(report))
