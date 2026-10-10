"""Reuse photo-derived closed fists; static review candidate, no rig or animation."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'art/source/rose-hands-v1'
bpy.ops.wm.open_mainfile(filepath=str(root/'.tools/meshy/user-fist-25/prepared/prepared-fist.blend'))
donor=next(o for o in bpy.context.scene.objects if o.type=='MESH');dv=[v.co.copy() for v in donor.data.vertices];df=[tuple(p.vertices) for p in donor.data.polygons]
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(root/'art/source/rose-premium-v2/model.glb'));body=next(o for o in bpy.context.scene.objects if o.type=='MESH');body.name='RoseBody'
for v in body.data.vertices:v.co=body.matrix_world@v.co
body.matrix_world=Matrix.Identity(4)
# Preserve high-res texture projection reference before reducing mesh.
body.data.calc_loop_triangles();sv=[v.co.copy() for v in body.data.vertices];st=[tuple(t.vertices) for t in body.data.loop_triangles];su=[[body.data.uv_layers.active.data[i].uv.copy() for i in t.loops] for t in body.data.loop_triangles];tree=BVHTree.FromPolygons(sv,st,all_triangles=True)
def project_uv(p):
 q,n,i,d=tree.find_nearest(p);a,b,c=[sv[j] for j in st[i]];u=[Vector((v.x,v.y,0)) for v in su[i]];return barycentric_transform(q,a,b,c,*u).xy
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bm.to_mesh(body.data);bm.free()
bpy.context.view_layer.objects.active=body;body.select_set(True);dec=body.modifiers.new('MobileReview','DECIMATE');dec.ratio=80000/350168;bpy.ops.object.modifier_apply(modifier=dec.name)
# Cut original palms at a skin-only wrist section, leaving sleeve intact.
bm=bmesh.new();bm.from_mesh(body.data)
for sign in [1,-1]:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,plane_co=(sign*.700,0,0),plane_no=(sign,0,0),clear_outer=True)
bm.to_mesh(body.data);bm.free()
reports=[]
for sign,side in [(1,'Left'),(-1,'Right')]:
 bm=bmesh.new();bm.from_mesh(body.data);pts=[v.co.copy() for v in bm.verts if abs(v.co.x-sign*.700)<1e-5];bm.free()
 cy=(min(v.y for v in pts)+max(v.y for v in pts))/2;cz=(min(v.z for v in pts)+max(v.z for v in pts))/2
 mouth=[v for v in dv if abs(v.x-.12)<1e-5];my=(min(v.y for v in mouth)+max(v.y for v in mouth))/2;mz=(min(v.z for v in mouth)+max(v.z for v in mouth))/2
 def fit(v):
  depth=(.12-v.x)*.15;t=max(0,1-depth/.035);sy=(max(v.y for v in pts)-min(v.y for v in pts))/(max(v.y for v in mouth)-min(v.y for v in mouth));sz=(max(v.z for v in pts)-min(v.z for v in pts))/(max(v.z for v in mouth)-min(v.z for v in mouth));return Vector((sign*(.702+depth),cy+(v.y-my)*(.15*(1-t)+sy*t),cz+(v.z-mz)*(.15*(1-t)+sz*t)))
 mesh=bpy.data.meshes.new(side+'PhotoFist');mesh.from_pydata([fit(v) for v in dv],[],df if sign<0 else [tuple(reversed(f)) for f in df]);mesh.update();hand=bpy.data.objects.new(side+'PhotoFist',mesh);bpy.context.collection.objects.link(hand)
 bm=bmesh.new();bm.from_mesh(mesh)
 # Remove the donor wrist cap to obtain one open attachment loop.
 faces=[f for f in bm.faces if all(abs(v.co.x-sign*.702)<.0001 for v in f.verts)];bmesh.ops.delete(bm,geom=faces,context='FACES');bm.to_mesh(mesh);bm.free()
 mat=body.data.materials[0].copy();mat.name=side+'SkinFromRoseAtlas';mesh.materials.append(mat)
 # Reuse Rose atlas, not previous character's color. Remove donor-specific normal projection.
 for node in mat.node_tree.nodes:
  if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=0
 # Copy a clean skin-only source texture patch, avoiding UV-island seams.
 image=next(n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image.colorspace_settings.name=='sRGB');w,h=image.size;pixels=list(image.pixels);candidates=[]
 for tri,uvs in zip(st[::256],su[::256]):
  center=sum((sv[i] for i in tri),Vector())/3
  if not (center.z>.72 and abs(center.x)<.10):continue
  u=sum(uvs,Vector((0,0)))/3;x=int(u.x*w);y=int(u.y*h)
  if not 32<x<w-32 or not 32<y<h-32:continue
  samples=[pixels[((y+dy)*w+x+dx)*4:((y+dy)*w+x+dx)*4+3] for dy in range(-32,32) for dx in range(-32,32)]
  if any(c[0]<c[1]*1.03 or c[0]<.35 or c[2]>c[0] for c in samples):continue
  mean=[sum(c[k] for c in samples)/len(samples) for k in range(3)];score=sum((c[k]-mean[k])**2 for c in samples for k in range(3));candidates.append((score,x,y))
 if not candidates:raise RuntimeError('No clean skin patch')
 _,px,py=min(candidates);patch=bpy.data.images.new(side+'RoseSkinPatch',width=64,height=64);patch.pixels=[q for y in range(py-32,py+32) for x in range(px-32,px+32) for q in pixels[(y*w+x)*4:(y*w+x)*4+4]];patch.pack()
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image==image:node.image=patch;node.extension='REPEAT'
 uv=mesh.uv_layers.new(name='UVMap')
 for p in mesh.polygons:
  p.use_smooth=True
  for i in p.loop_indices:
   v=mesh.vertices[mesh.loops[i].vertex_index].co;uv.data[i].uv=(.1+(sign*v.x-.700)*5,.5+(v.z-cz)*5)
 # Stitch rings on joined mesh with angle-ordered zipper; no overlapping capped objects.
 bpy.ops.object.select_all(action='DESELECT');body.select_set(True);hand.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
 bm=bmesh.new();bm.from_mesh(body.data);rings=[]
 for cut in [.700,.702]:
  ring=[v for v in bm.verts if abs(v.co.x-sign*cut)<1e-5 and any(e.is_boundary for e in v.link_edges)]
  ring.sort(key=lambda v:math.atan2(v.co.z-cz,v.co.y-cy));rings.append(ring)
 a,b=rings
 if not a or not b:raise RuntimeError('Missing wrist attachment boundary')
 # Advance around rings by normalized index; both rings wrap exactly once.
 i=j=0;faces=[]
 while i<len(a) or j<len(b):
  if j==len(b) or (i<len(a) and (i+1)/len(a)<=(j+1)/len(b)):
   verts=[a[i%len(a)],a[(i+1)%len(a)],b[j%len(b)]];i+=1
  else:verts=[a[i%len(a)],b[(j+1)%len(b)],b[j%len(b)]];j+=1
  try:faces.append(bm.faces.new(verts))
  except ValueError:raise RuntimeError('Duplicate seam triangle')
 layer=bm.loops.layers.uv.active
 face_set=set(faces);skin_index=len(body.data.materials)-1
 for f in bm.faces:
  center=f.calc_center_median()
  if f in face_set:
   f.material_index=skin_index;f.smooth=True
   for loop in f.loops:loop[layer].uv=(.1+(sign*loop.vert.co.x-.700)*5,.5+(loop.vert.co.z-cz)*5)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));boundary=sum(e.is_boundary for e in bm.edges if abs(e.verts[0].co.x-sign*.7)<.03 and abs(e.verts[0].co.z-cz)<.15)
 reports.append({'side':side,'body_ring':len(a),'fist_ring':len(b),'seam_triangles':len(faces),'wrist_boundary_edges':boundary})
 if boundary:
  print('BOUNDARY',side,len(a),len(b),[(list(e.verts[0].co),list(e.verts[1].co)) for e in bm.edges if e.is_boundary and abs(e.verts[0].co.x-sign*.7)<.03 and abs(e.verts[0].co.z-cz)<.15][:8]);raise RuntimeError('Wrist seam remains open')
 bm.to_mesh(body.data);bm.free()
bm=bmesh.new();bm.from_mesh(body.data)
seamverts=[v for v in bm.verts if .696<abs(v.co.x)<.725]
for _ in range(8):bmesh.ops.smooth_vert(bm,verts=seamverts,factor=.35,use_axis_x=False,use_axis_y=True,use_axis_z=True)
bm.to_mesh(body.data);bm.free()
# Blend original wrist atlas into copied skin patch in shader, never interpolate atlas islands.
mat=body.data.materials[0].copy();mat.name='RoseUnifiedSkin';nodes=mat.node_tree.nodes;links=mat.node_tree.links
base=next(n for n in nodes if n.type=='TEX_IMAGE' and n.image and n.image.colorspace_settings.name=='sRGB');outgoing=list(base.outputs['Color'].links)
attr=nodes.new('ShaderNodeAttribute');attr.attribute_name='HandSkinBlend';split=nodes.new('ShaderNodeSeparateColor');links.new(attr.outputs['Color'],split.inputs['Color'])
uvnode=nodes.new('ShaderNodeUVMap');uvnode.uv_map='HandDetailUV'
patchnodes=[]
for name in ['LeftRoseSkinPatch','RightRoseSkinPatch']:
 n=nodes.new('ShaderNodeTexImage');n.image=bpy.data.images[name];n.extension='EXTEND';links.new(uvnode.outputs['UV'],n.inputs['Vector']);patchnodes.append(n)
side=nodes.new('ShaderNodeMixRGB');links.new(split.outputs['Green'],side.inputs[0]);links.new(patchnodes[0].outputs['Color'],side.inputs[1]);links.new(patchnodes[1].outputs['Color'],side.inputs[2])
mix=nodes.new('ShaderNodeMixRGB');links.new(split.outputs['Red'],mix.inputs[0]);links.new(base.outputs['Color'],mix.inputs[1]);links.new(side.outputs[0],mix.inputs[2])
for link in outgoing:target=link.to_socket;links.remove(link);links.new(mix.outputs[0],target)
normalweight=nodes.new('ShaderNodeMath');normalweight.operation='SUBTRACT';normalweight.inputs[0].default_value=1;links.new(split.outputs['Red'],normalweight.inputs[1])
for n in nodes:
 if n.type=='NORMAL_MAP':links.new(normalweight.outputs[0],n.inputs['Strength'])
body.data.materials[0]=mat;detail=body.data.uv_layers.new(name='HandDetailUV');mask=body.data.color_attributes.new(name='HandSkinBlend',type='FLOAT_COLOR',domain='CORNER')
detail=body.data.uv_layers['HandDetailUV'];mask=body.data.color_attributes['HandSkinBlend']
for poly in body.data.polygons:
 poly.material_index=0
 for i in poly.loop_indices:
  v=body.data.vertices[body.data.loops[i].vertex_index].co;sign=1 if v.x>0 else -1;t=max(0,min(1,(abs(v.x)-.685)/.015));t=t*t*(3-2*t);mask.data[i].color=(t,0 if sign>0 else 1,0,1)
  detail.data[i].uv=(.1+(abs(v.x)-.700)*5,.5+(v.z-.55)*5)
body.data.calc_loop_triangles();report={'triangles':len(body.data.loop_triangles),'wrists':reports,'rigged':False,'credits':0,'status':'static candidate, requires visual acceptance','donor':'prepared photo fist from previous character','skin':'Clean skin-only patch copied from Rose atlas; body atlas on seam','limitations':['No finger opening','No animated deformation verification','Skin seam requires further close-up acceptance']}
(out/'audit.json').write_text(json.dumps(report,indent=2)+'\n');bpy.ops.wm.save_as_mainfile(filepath=str(out/'candidate.blend'));# Shader blend remains Blender review source until baked to a standard game material.

