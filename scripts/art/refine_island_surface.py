"""Conservative seam-aware Taubin smoothing of skin shafts; keeps topology and weights."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector
src,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(src))
report=[]
for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
 mesh=obj.data;groups={g.index:g.name for g in obj.vertex_groups};coords=[v.co.copy() for v in mesh.vertices]
 clusters={}
 for v in mesh.vertices:clusters.setdefault(tuple(round(x,5) for x in v.co),[]).append(v.index)
 neighbors=[set() for _ in coords]
 for edge in mesh.edges:a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
 for ids in clusters.values():
  union=set().union(*(neighbors[i] for i in ids))-set(ids)
  for i in ids:neighbors[i]=union
 mask=[]
 for v in mesh.vertices:
  weights={groups.get(g.group,''):g.weight for g in v.groups}
  shaft=max(weights.get('LeftForeArm',0),weights.get('RightForeArm',0))
  # Gentle facial smoothing; do not smooth the integrated rigid fists or clothes.
  head=weights.get('Head',0)
  face=.65 if head>.95 and 146<v.co.z<165 else 0
  # Preserve the central nose/mouth ridge and eye sockets while relaxing cheek noise.
  if v.co.y < -7 and ((abs(v.co.x)<3 and 149<v.co.z<162) or 159<v.co.z<164):face*=.12
  mask.append(max(.8 if shaft>.85 else 0,face))
 current=[c.copy() for c in coords]
 for _ in range(14):
  for factor in [.45]:
   result=[]
   for i,co in enumerate(current):
    if not mask[i] or not neighbors[i]:result.append(co.copy());continue
    average=sum((current[j] for j in neighbors[i]),Vector())/len(neighbors[i]);result.append(co+(average-co)*factor*mask[i])
   current=result
 # Cap local sculpt displacement to 6 mm; skin object is imported in centimetres.
 current=[b+(a-b).normalized()*.6 if (a-b).length>.6 else a for a,b in zip(current,coords)]
 shifts=[(a-b).length for a,b in zip(current,coords)]
 for v,co in zip(mesh.vertices,current):v.co=co
 if mesh.shape_keys:
  for key in mesh.shape_keys.key_blocks:
   for i,v in enumerate(key.data):v.co+=current[i]-coords[i]
 for polygon in mesh.polygons:polygon.use_smooth=True
 report.append({'mesh':obj.name,'vertices':len(coords),'changed':sum(d>1e-8 for d in shifts),'maxDisplacementLocalUnits':max(shifts,default=0),'dimensions':list(obj.dimensions),'topologyChanged':False,'weightsChanged':False})
(out/"surface-deltas.json").write_text(json.dumps({"original":[list(v) for v in coords],"delta":[list(a-b) for a,b in zip(current,coords)]}));print(json.dumps(report));(out/'surface-audit.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'candidate.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'candidate.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='STRIP')
