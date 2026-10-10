"""Transfer Meshy's verified Rose rig onto the repaired local mesh; rigid closed fists."""
import bpy,json
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
root=Path('/Users/macbook1/Documents/ChatGPT/LighFight');out=root/'art/source/rose-hands-v1'
bpy.ops.wm.open_mainfile(filepath=str(out/'game-candidate.blend'));target=bpy.data.objects['RoseBody'];before=set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=str(root/'.tools/meshy/motion-expansion/rose-rig-rigged_character_glb_url.glb'));added=set(bpy.data.objects)-before;arm=next(o for o in added if o.type=='ARMATURE');src=next(o for o in added if o.type=='MESH' and o.vertex_groups)
# Match the provider's 1.7m, floor-origin coordinate system in its centimeter mesh space.
lo=min(v.co.z for v in target.data.vertices);hi=max(v.co.z for v in target.data.vertices);scale=170/(hi-lo)
for v in target.data.vertices:v.co=Vector((v.co.x*scale,v.co.y*scale,(v.co.z-lo)*scale))
target.matrix_world=src.matrix_world.copy();target.parent=arm;target.matrix_parent_inverse=arm.matrix_world.inverted();target.matrix_world=src.matrix_world.copy()
src.data.calc_loop_triangles();verts=[v.co.copy() for v in src.data.vertices];tris=[tuple(t.vertices) for t in src.data.loop_triangles];tree=BVHTree.FromPolygons(verts,tris,all_triangles=True)
weights=[{src.vertex_groups[g.group].name:g.weight for g in v.groups} for v in src.data.vertices]
for g in src.vertex_groups:target.vertex_groups.new(name=g.name)
missing=0
for v in target.data.vertices:
 if abs(v.co.x)>.700*scale:
  target.vertex_groups['LeftHand' if v.co.x>0 else 'RightHand'].add([v.index],1,'REPLACE');continue
 q,n,i,d=tree.find_nearest(v.co);ids=tris[i];a,b,c=[verts[k] for k in ids];bary=barycentric_transform(q,a,b,c,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)));result={}
 for k,factor in zip(ids,bary):
  for name,w in weights[k].items():result[name]=result.get(name,0)+max(0,factor)*w
 total=sum(result.values())
 if total<1e-6:missing+=1;continue
 for name,w in result.items():
  if w/total>.0001:target.vertex_groups[name].add([v.index],w/total,'REPLACE')
if missing:raise RuntimeError('Unweighted vertices: '+str(missing))
mod=target.modifiers.new('MeshyRoseRig','ARMATURE');mod.object=arm
for o in added:
 if o!=arm:bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT');target.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
(out/'rig-audit.json').write_text(json.dumps({'bones':len(arm.data.bones),'vertices':len(target.data.vertices),'unweighted':missing,'height_meters':1.7,'fists':'Rigid Hand bone; no finger articulation','weight_source':'Meshy Rose rig','device_verified':False},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'rigged.blend'));bpy.ops.export_scene.fbx(filepath=str(out/'rose-rigged.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)
