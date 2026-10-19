"""High->low PBR rebake after geometry weld/decimation. Original source never altered.
Used when preserving the old atlas on collapsed topology stretches source UVs.
blender ... -P rebake_prop_lod.py -- HIGH.glb LOW.glb OUT.glb [SIZE]
"""
import bpy,sys,json,hashlib,math,time,bmesh
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];highPath=Path(args[0]).resolve();lowPath=Path(args[1]).resolve();out=Path(args[2]).resolve();size=int(args[3]) if len(args)>3 else 2048;before=hashlib.sha256(highPath.read_bytes()).hexdigest();out.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def load_one(p):
 old=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(p));new=[o for o in bpy.data.objects if o not in old and o.type=='MESH'];assert len(new)==1;return new[0]
high=load_one(highPath);low=load_one(lowPath);high.name='Original high source';low.name='Watertight LOD with new atlas'
assert len(high.data.materials)==1 and len(low.data.materials)==1,'Single material source expected'
sourceMaterial=high.data.materials[0];sourceCopy=sourceMaterial.copy();high.data.materials[0]=sourceCopy;nodes=sourceCopy.node_tree.nodes;links=sourceCopy.node_tree.links;bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL');originalSurface=output.inputs['Surface'].links[0].from_socket
# Replace only the low UVs. Source UVs and pixels are untouched.
# glTF splits positions at UV seams during export; reconnect them before island generation.
bm=bmesh.new();bm.from_mesh(low.data);beforeVertices=len(bm.verts);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);rebakeWelded=beforeVertices-len(bm.verts);bm.to_mesh(low.data);bm.free();low.data.update()
while low.data.uv_layers:low.data.uv_layers.remove(low.data.uv_layers[0])
low.data.uv_layers.new(name='RebakedLODAtlas');bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.01,area_weight=0,correct_aspect=True,scale_to_bounds=False);bpy.ops.object.mode_set(mode='OBJECT')
mat=bpy.data.materials.new('Rebaked '+out.stem);mat.use_nodes=True;low.data.materials.clear();low.data.materials.append(mat);tn=mat.node_tree.nodes;tl=mat.node_tree.links;tbs=tn.get('Principled BSDF');images={};textureNodes={}
for name,space in [('base','sRGB'),('rm','Non-Color'),('normal','Non-Color')]:
 image=bpy.data.images.new(out.stem+'-'+name,width=size,height=size,alpha=True);image.colorspace_settings.name=space;node=tn.new('ShaderNodeTexImage');node.image=image;images[name]=image;textureNodes[name]=node
emission=nodes.new('ShaderNodeEmission');emission.inputs['Strength'].default_value=1;combine=nodes.new('ShaderNodeCombineRGB');combine.inputs['R'].default_value=1
for sourceSocket,targetSocket in [(bs.inputs['Roughness'],combine.inputs['G']),(bs.inputs['Metallic'],combine.inputs['B'])]:
 if sourceSocket.is_linked:links.new(sourceSocket.links[0].from_socket,targetSocket)
 else:targetSocket.default_value=sourceSocket.default_value
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=1;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.02;scene.render.bake.max_ray_distance=.05;scene.render.bake.margin=12;scene.render.bake.normal_space='TANGENT';scene.render.bake.use_clear=True
bpy.ops.object.select_all(action='DESELECT');high.select_set(True);low.select_set(True);bpy.context.view_layer.objects.active=low;times={}
for name in ['base','rm','normal']:
 for n in tn:n.select=False
 textureNodes[name].select=True;tn.active=textureNodes[name]
 if name=='normal':links.new(originalSurface,output.inputs['Surface'])
 else:
  links.new(emission.outputs[0],output.inputs['Surface'])
  if name=='base':
   if bs.inputs['Base Color'].is_linked:links.new(bs.inputs['Base Color'].links[0].from_socket,emission.inputs['Color'])
   else:emission.inputs['Color'].default_value=bs.inputs['Base Color'].default_value
  else:links.new(combine.outputs[0],emission.inputs['Color'])
 started=time.time();bpy.ops.object.bake(type='NORMAL' if name=='normal' else 'EMIT');times[name]=time.time()-started;images[name].filepath_raw=str(out.with_name(out.stem+'-'+name+'.png'));images[name].file_format='PNG';images[name].save();print('BAKE_DONE',name,times[name],flush=True)
# The new material samples the new atlas only; never reattach the old atlas to collapsed UVs.
tl.new(textureNodes['base'].outputs['Color'],tbs.inputs['Base Color']);sep=tn.new('ShaderNodeSeparateRGB');tl.new(textureNodes['rm'].outputs['Color'],sep.inputs[0]);tl.new(sep.outputs['G'],tbs.inputs['Roughness']);tl.new(sep.outputs['B'],tbs.inputs['Metallic']);nm=tn.new('ShaderNodeNormalMap');tl.new(textureNodes['normal'].outputs['Color'],nm.inputs['Color']);tl.new(nm.outputs[0],tbs.inputs['Normal']);bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low;bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',use_selection=True,export_materials='EXPORT',export_normals=True,export_texcoords=True,export_tangents=True)
assert hashlib.sha256(highPath.read_bytes()).hexdigest()==before;report={'status':'REBAKED_DERIVATIVE_REQUIRES_ACTUAL_RENDER','sourceSHA256':before,'sourceUnchanged':True,'inputLOD':str(lowPath),'triangles':sum(len(p.vertices)-2 for p in low.data.polygons),'atlasSize':size,'newUVAtlas':True,'lowExportSeamsReweldedBeforeUnwrap':rebakeWelded,'derivedBaseRMNormalMaps':True,'bakeSeconds':times,'scope':'Environment only. Original4K maps and original geometry remain in immutable high source. This low asset has a new UV atlas and derived2K maps.'};out.with_suffix('.json').write_text(json.dumps(report,indent=2));print('AGENT_RESULT '+json.dumps(report),flush=True)
