"""Bake the review shader into a standard base-color atlas for game export."""
import bpy
from pathlib import Path
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'art/source/rose-hands-v1'
bpy.ops.wm.open_mainfile(filepath=str(out/'candidate.blend'))
body=bpy.data.objects['RoseBody'];bpy.context.view_layer.objects.active=body;body.select_set(True)
mat=body.data.materials[0];body.data.materials.clear();body.data.materials.append(mat);nodes=mat.node_tree.nodes;links=mat.node_tree.links
original=body.data.uv_layers[0].name
# Keep every existing texture on its explicit UV set while baking a new atlas.
for n in list(nodes):
 if n.type=='TEX_IMAGE' and not n.inputs['Vector'].is_linked:
  uv=nodes.new('ShaderNodeUVMap');uv.uv_map=original;links.new(uv.outputs['UV'],n.inputs['Vector'])
body.data.uv_layers.new(name='GameAtlasUV');body.data.uv_layers.active=body.data.uv_layers['GameAtlasUV'];body.data.uv_layers['GameAtlasUV'].active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15192,island_margin=.003);bpy.ops.object.mode_set(mode='OBJECT')
bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');color=bsdf.inputs['Base Color'].links[0].from_socket
output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL');emission=nodes.new('ShaderNodeEmission');links.new(color,emission.inputs['Color']);links.new(emission.outputs[0],output.inputs['Surface'])
img=bpy.data.images.new('RoseGameAlbedo',width=4096,height=4096);target=nodes.new('ShaderNodeTexImage');target.image=img;nodes.active=target
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.bake.margin=12
body.data.update();bpy.ops.object.bake(type='EMIT',uv_layer='GameAtlasUV');img.filepath_raw=str(out/'skin-albedo.png');img.file_format='PNG';img.save();img.pack()
# A single conventional material, with the baked UV atlas, works in GLB/Unity.
nodes.clear();p=nodes.new('ShaderNodeBsdfPrincipled');p.inputs['Roughness'].default_value=.65;t=nodes.new('ShaderNodeTexImage');t.image=img;uv=nodes.new('ShaderNodeUVMap');uv.uv_map='GameAtlasUV';o=nodes.new('ShaderNodeOutputMaterial');links.new(uv.outputs['UV'],t.inputs['Vector']);links.new(t.outputs['Color'],p.inputs['Base Color']);links.new(p.outputs[0],o.inputs['Surface'])
coords=[v.uv.copy() for v in body.data.uv_layers['GameAtlasUV'].data]
baseuv=body.data.uv_layers[0]
for i,co in enumerate(coords):baseuv.data[i].uv=co
name=baseuv.name
for layer in list(body.data.uv_layers)[1:]:body.data.uv_layers.remove(layer)
for attribute in list(body.data.color_attributes):body.data.color_attributes.remove(attribute)
uv.uv_map=name
bpy.ops.wm.save_as_mainfile(filepath=str(out/'game-candidate.blend'));bpy.ops.export_scene.gltf(filepath=str(out/'candidate.glb'),export_format='GLB')
