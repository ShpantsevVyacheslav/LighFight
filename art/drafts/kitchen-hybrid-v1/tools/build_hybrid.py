"""Tripo furniture + manually reconstructed photo-based architecture, exploratory only.
No source mesh writes/remesh/decimation. World placement uses one uniform parent transform.
"""
import bpy,json,hashlib,math,struct
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
import numpy as np
ROOT=Path(__file__).resolve().parents[4];OUT=ROOT/'art/drafts/kitchen-hybrid-v1';SOURCE=ROOT/'art/source/kitchen-tripo-v1/original-v2.glb'
def digest(b):return hashlib.sha256(b).hexdigest()
def meshhash(mesh):
 mesh.calc_loop_triangles();p=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',p);t=np.empty(len(mesh.loop_triangles)*3,np.int32);mesh.loop_triangles.foreach_get('vertices',t);uv=np.empty(len(mesh.uv_layers.active.data)*2,np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);return {'positionSHA256':digest(p.tobytes()),'triangleSHA256':digest(t.tobytes()),'loopUVSHA256':digest(uv.tobytes()),'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles)}
def material(name,color,rough=.6,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;return m
ARCH=[];BLOCK=[]
def box(name,location,dimensions,mat,category='architecture'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.name=name;o.dimensions=dimensions;o.data.materials.append(mat);o['provenance']='Manually reconstructed photo-based blockout' if category=='blockout' else 'Manually reconstructed photo-based architecture';o['category']=category
 (BLOCK if category=='blockout' else ARCH).append(o);return o
def main():
 raw=SOURCE.read_bytes();sourceSHA=digest(raw);assert sourceSHA=='fce37d2145ef34dac5a39f7ddffcecb46c1d384162019b1948e3ea864640b2ff';length=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+length]);off=20+length;size=struct.unpack_from('<I',raw,off)[0];binary=raw[off+8:off+8+size];images=[]
 for im in g.get('images',[]):
  v=g['bufferViews'][im['bufferView']];start=v.get('byteOffset',0);images.append({'name':im.get('name'),'SHA256':digest(binary[start:start+v['byteLength']]),'bytes':v['byteLength']})
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(SOURCE));bpy.context.view_layer.update();furniture=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(furniture)==1;obj=furniture[0];originalHash=meshhash(obj.data);originalMaterials=[m.name for m in obj.data.materials];sourceMatrix=obj.matrix_world.copy();root=bpy.data.objects.new('UNCHANGED_TRIPO_TABLE_AND_4_CHAIRS',None);bpy.context.scene.collection.objects.link(root);obj.parent=root;obj.matrix_parent_inverse=Matrix.Identity(4);obj.matrix_basis=sourceMatrix;root.scale=(3.2,)*3;root.location=(1.5,0,.002);root['provenance']='Unchanged original-v2 mesh/UV/materials; parent uniform scale3.2 + translation only';bpy.context.view_layer.update()
 beige=material('PhotoBased_BeigeWall',(.62,.51,.37),.86);trim=material('PhotoBased_WhiteTrim',(.83,.80,.71),.55);white=material('BLOCKOUT_ApplianceWhite',(.78,.79,.77),.35);dark=material('BLOCKOUT_CoffeeMachine',(.035,.04,.045),.32);cupboard=material('BLOCKOUT_LightWood',(.45,.33,.20),.65);black=material('BLOCKOUT_MicrowaveGlass',(.025,.035,.04),.18)
 wood=material('Procedural_Oak_Lookdev_NOT_PHOTO_TEXTURE',(.32,.19,.085),.55);n=wood.node_tree.nodes;l=wood.node_tree.links;p=n.get('Principled BSDF');tex=n.new('ShaderNodeTexCoord');mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(45,2,3);l.new(tex.outputs['Generated'],mapping.inputs[0]);noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=4;noise.inputs['Detail'].default_value=2;l.new(mapping.outputs['Vector'],noise.inputs['Vector']);ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.15;ramp.color_ramp.elements[0].color=(.11,.052,.018,1);ramp.color_ramp.elements[1].position=.85;ramp.color_ramp.elements[1].color=(.46,.28,.12,1);l.new(noise.outputs['Fac'],ramp.inputs['Fac']);l.new(ramp.outputs['Color'],p.inputs['Base Color'])
 W,D,H=5.4,5.6,3.;x=W/2;y=D/2;th=.12;floor=box('Clean_Rectangular_Continuous_Floor',(0,0,-.06),(W,D,.12),wood);box('Plain_Back_Wall_NO_WINDOW',(0,y+th/2,H/2),(W+th,th,H),beige)
 # Exactly one right-wall opening; four disjoint structural wall panels surround it.
 start,end=-1.75,1.65;bottom,top=.88,2.65
 box('RightWall_BelowWindow',(x+th/2,0,bottom/2),(th,D,bottom),beige);box('RightWall_AboveWindow',(x+th/2,0,(top+H)/2),(th,D,H-top),beige)
 box('RightWall_FrontPier',(x+th/2,(-y+start)/2,(bottom+top)/2),(th,start+y,top-bottom),beige);box('RightWall_BackPier',(x+th/2,(end+y)/2,(bottom+top)/2),(th,y-end,top-bottom),beige)
 for name,loc,dim in [('WindowFrame_Sill',(x, (start+end)/2,bottom),(.16,end-start+.1,.065)),('WindowFrame_Head',(x,(start+end)/2,top),(.12,end-start+.1,.065)),('WindowFrame_Front',(x,start,(top+bottom)/2),(.12,.055,top-bottom)),('WindowFrame_Back',(x,end,(top+bottom)/2),(.12,.055,top-bottom))]:box(name,loc,dim,trim)
 glass=material('SingleRightWindow_Glass',(.45,.64,.76),.15);glass.node_tree.nodes.get('Principled BSDF').inputs['Transmission Weight'].default_value=.85
 box('ONE_RIGHT_WINDOW_Glass',(x+.035,(start+end)/2,(bottom+top)/2),(.008,end-start,top-bottom),glass)
 for i in range(19):
  yy=start+.08+i*(end-start-.16)/18;o=box(f'RightWindow_VerticalBlind_{i:02}',(x-.065,yy,(top+bottom)/2),(.009,.13,top-bottom-.10),trim);o.rotation_euler.z=math.radians(26)
 box('RightWindow_BlindRail',(x-.07,(start+end)/2,top-.015),(.04,end-start,.045),trim)
 box('Back_Skirting',(0,y-.017,.055),(W,.035,.11),cupboard);box('Right_Skirting',(x-.017,0,.055),(.035,D,.11),cupboard)
 # Explicit blockouts placed by photo semantics, not claimed finished appliances.
 box('BLOCKOUT_Fridge',(0,2.40,.99),(.70,.70,1.98),white,'blockout');box('BLOCKOUT_Fridge_FreezerSeam',(0,2.043,.64),(.70,.008,.015),dark,'blockout');box('BLOCKOUT_MicrowaveCupboard',(.70,2.40,.62),(.65,.70,1.24),cupboard,'blockout');box('BLOCKOUT_Microwave',(.70,2.38,1.43),(.56,.43,.34),white,'blockout');box('BLOCKOUT_Microwave_Front',(.70,2.156,1.43),(.44,.012,.24),black,'blockout')
 box('BLOCKOUT_CoffeeCupboard',(-2.35,1.50,.49),(.55,.80,.98),cupboard,'blockout');box('BLOCKOUT_CoffeeMachine',(-2.35,1.50,1.23),(.37,.43,.50),dark,'blockout')
 bpy.context.view_layer.update();pts=[obj.matrix_world@Vector(c) for c in obj.bound_box];flo=Vector([min(v[i] for v in pts) for i in range(3)]);fhi=Vector([max(v[i] for v in pts) for i in range(3)]);assert originalHash==meshhash(obj.data);assert originalMaterials==[m.name for m in obj.data.materials]
 # Full clean-floor component, no proxy approximation: original box12triangles.
 fmesh=floor.data;fmesh.calc_loop_triangles();tree=BVHTree.FromPolygons([floor.matrix_world@v.co for v in fmesh.vertices],[tuple(t.vertices) for t in fmesh.loop_triangles],all_triangles=True);hits=[]
 for yy in np.linspace(-y+.02,y-.02,101):
  for xx in np.linspace(-x+.02,x-.02,101):
   hit,normal,index,distance=tree.ray_cast(Vector((xx,yy,.05)),Vector((0,0,-1)),.1);assert hit is not None and abs(hit.z)<1e-6;hits.append(float(hit.z))
 clear={'minXY':[-1.9,-2.5],'maxXY':[.60,1.10],'width':2.5,'depth':3.6,'furnitureBoundsMin':list(flo),'furnitureBoundsMax':list(fhi),'furnitureWidth':fhi.x-flo.x,'clearWidthGreaterThanFurniture':2.5>fhi.x-flo.x,'scope':'expanded playable blockout, not photo-calibrated dimensions'};assert clear['clearWidthGreaterThanFurniture'] and fhi.x<x and flo.x>.60
 report={'status':'EXPLORATORY_HYBRID_NOT_PROMOTED','provenance':'Tripo furniture + manually reconstructed photo-based architecture','sourceFurniture':str(SOURCE.relative_to(ROOT)),'sourceSHA256':sourceSHA,'sourceFurnitureLocalGeometryHashes':originalHash,'sourceMaterialsUnchanged':originalMaterials,'sourceEmbeddedImages':images,'geometryUVIndicesMaterialsUnchanged':True,'furnitureUniformParentScale':3.2,'furnitureParentTranslation':list(root.location),'roomDimensions':[W,D,H],'dimensionsAssumedNotMeasured':True,'singleWindow':{'wall':'RIGHT +X','openingY':[start,end],'openingZ':[bottom,top],'backWallOpeningCount':0,'rightWallOpeningCount':1,'blinds':19},'clearFloor':clear,'floorGrid':{'rays':len(hits),'hits':len(hits),'minZ':min(hits),'maxZ':max(hits),'fullFloorComponentTriangles':12,'scope':'floor itself including beneath furniture; not a navigation collision test'},'architectureObjects':len(ARCH),'blockoutObjects':[o.name for o in BLOCK],'limits':['Not a fully generated Tripo room.','Floor/architecture materials are procedural lookdev, not texture reconstruction from the photograph.','Fridge/cupboard/coffee machine are explicitly unfinished blockouts.','Room expanded for clear play space, no photogrammetric metric claim.','No Unity/mobile/runtime/lighting acceptance.']};(OUT/'manifest.json').write_text(json.dumps(report,indent=2))
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.cycles.max_bounces=5;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.world.color=(.18,.18,.18);scene.view_settings.view_transform='AgX'
 target=Vector((0,0,1.15))
 for name,loc,power,size in [('WindowLight',(3.6,-.1,2.3),600,3.2),('RoomKey',(-3,-4,6),700,5),('RoomFill',(-2,3,4),350,3)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
 cd=bpy.data.cameras.new('HybridReviewCamera');camera=bpy.data.objects.new('HybridReviewCamera',cd);scene.collection.objects.link(camera);scene.camera=camera;cd.type='ORTHO';cd.ortho_scale=8.3
 for view,direction in [('interior-threequarter',(-2,-3,2.1)),('top',(0,0,4))]:
  camera.location=target+Vector(direction)*5.6;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/f'{view}.png');bpy.ops.render.render(write_still=True)
 assert digest(SOURCE.read_bytes())==sourceSHA and originalHash==meshhash(obj.data);report['sourcePreservedAfterRenders']=True;(OUT/'manifest.json').write_text(json.dumps(report,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kitchen-hybrid-v1.blend'));print('RESULT '+json.dumps({'status':report['status'],'sourcePreserved':True,'sourceTriangles':originalHash['triangles'],'floorHits':len(hits),'clearFloor':clear,'blend':str(OUT/'kitchen-hybrid-v1.blend')}),flush=True)
main()
