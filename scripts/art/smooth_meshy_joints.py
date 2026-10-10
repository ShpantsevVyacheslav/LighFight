"""Local Meshy weight repair candidate. Does not alter mesh topology or source.
Run in Blender: -- source.blend output-directory
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
source,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers))
vertices=mesh.data.vertices;groups=list(mesh.vertex_groups)
weights=[{g.group:g.weight for g in v.groups} for v in vertices]
original=[w.copy() for w in weights]
regions=[(arm.data.bones[n].head_local.copy(),r) for n,r in [('LeftForeArm',14),('RightForeArm',14),('LeftLeg',13),('RightLeg',13),('LeftFoot',10)]]
mask=[max(max(0,1-(v.co-c).length/r)**2 for c,r in regions) for v in vertices]
clusters={}
for v in vertices:clusters.setdefault(tuple(round(c,4) for c in v.co),[]).append(v.index)
seams=[ids for ids in clusters.values() if len(ids)>1 and mask[ids[0]]>.001]
neighbors=[set() for v in vertices]
for edge in mesh.data.edges:
 a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
def roughness(ws):
 values=[]
 for edge in mesh.data.edges:
  a,b=edge.vertices
  if min(mask[a],mask[b])>.05:
   keys=ws[a].keys()|ws[b].keys();values.append(sum((ws[a].get(k,0)-ws[b].get(k,0))**2 for k in keys))
 return sum(values)/max(1,len(values))
before=roughness(weights)
def smoothstep(x):
 x=max(0,min(1,x));return x*x*(3-2*x)
# Keep the shaft of each forearm/shin rigid; blend only near its two joints.
# Broad thigh influence on calf vertices can distort the calf when the knee bends.
shaftCorrected=0
for bone,parent,end in [('LeftForeArm','LeftArm','LeftHand'),('RightForeArm','RightArm','RightHand'),('LeftLeg','LeftUpLeg','LeftFoot'),('RightLeg','RightUpLeg','RightFoot')]:
 start=arm.data.bones[bone].head_local;delta=arm.data.bones[end].head_local-start
 group,parentGroup,endGroup=[mesh.vertex_groups[n].index for n in [bone,parent,end]]
 for v in vertices:
  w=weights[v.index];t=(v.co-start).dot(delta)/delta.length_squared
  if w.get(group,0)<.05 or not 0<t<1:continue
  parentRemoved=w.get(parentGroup,0)*smoothstep(t/.25)
  endRemoved=w.get(endGroup,0)*(1-smoothstep((t-.75)/.25))
  if parentRemoved+endRemoved<1e-6:continue
  w[parentGroup]=w.get(parentGroup,0)-parentRemoved;w[endGroup]=w.get(endGroup,0)-endRemoved
  w[group]=w.get(group,0)+parentRemoved+endRemoved;shaftCorrected+=1

for iteration in range(8):
 result=[]
 for i,w in enumerate(weights):
  strength=.55*mask[i]
  if strength<.001 or not neighbors[i]:result.append(w.copy());continue
  average={}
  for j in neighbors[i]:
   for g,value in weights[j].items():average[g]=average.get(g,0)+value/len(neighbors[i])
  mixed={g:(1-strength)*w.get(g,0)+strength*average.get(g,0) for g in w.keys()|average.keys()}
  # Match Unity's four-influence skinning; normalize explicitly.
  mixed=dict(sorted(mixed.items(),key=lambda item:item[1],reverse=True)[:4]);total=sum(mixed.values())
  result.append({g:value/total for g,value in mixed.items() if value>1e-7})
 for ids in seams:
  mean={}
  for i in ids:
   for g,value in result[i].items():mean[g]=mean.get(g,0)+value/len(ids)
  mean=dict(sorted(mean.items(),key=lambda item:item[1],reverse=True)[:4]);total=sum(mean.values());mean={g:value/total for g,value in mean.items()}
  for i in ids:result[i]=mean.copy()
 weights=result
after=roughness(weights)
changed=0
for i,(old,new) in enumerate(zip(original,weights)):
 if old!=new:
  changed+=1
  for g in old:groups[g].remove([i])
  for g,value in new.items():groups[g].add([i],value,'REPLACE')
report={'shaftCorrectedVertices':shaftCorrected,'changedVertices':changed,'vertices':len(vertices),'weightRoughnessBefore':before,'weightRoughnessAfter':after,'maxWeightSumError':max(abs(sum(w.values())-1) for w in weights),'maxInfluences':max(len(w) for w in weights),'coincidentVertexGroups':len(seams),'maxSeamWeightDifference':max((max(abs(weights[i].get(g,0)-weights[ids[0]].get(g,0)) for g in weights[i].keys()|weights[ids[0]].keys()) for ids in seams for i in ids),default=0),'geometryChanged':False,'status':'candidate; review joint silhouettes before promotion'}
(out/'weight-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'joint-candidate.blend'))
# Fixed camera/light/pose comparisons using the same imported provider animation.
scene=bpy.context.scene;camera=scene.camera;scene.cycles.samples=8
scene.render.resolution_x=480;scene.render.resolution_y=520
for label,frame,focus,scale in [('elbows',1,Vector((0,0,1.27)),.88),('calf',21,Vector((.1,0,.35)),.75)]:
 scene.frame_set(frame);camera.location=focus+Vector((2,-4,.15));camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
 for variant,ws in [('before',original),('after',weights)]:
  for i,w in enumerate(ws):
   for group in groups:group.remove([i])
   for g,value in w.items():groups[g].add([i],value,'REPLACE')
  mesh.data.update();bpy.context.view_layer.update()
  scene.render.filepath=str(out/(label+'-'+variant+'.png'));bpy.ops.render.render(write_still=True)
