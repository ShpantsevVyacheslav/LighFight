"""Isolated source-space weight candidate; never changes vertices or animation."""
def apply(bpy):
 arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 report={'torso':0,'calf':0}
 def assign(obj,v,weights,blend):
  old={obj.vertex_groups[g.group].name:g.weight for g in v.groups}
  merged={n:old.get(n,0)*(1-blend)+weights.get(n,0)*blend for n in set(old)|set(weights)}
  total=sum(merged.values())
  for g in list(v.groups):obj.vertex_groups[g.group].remove([v.index])
  for n,w in merged.items():
   if w>1e-6:obj.vertex_groups.get(n).add([v.index],w/total,'REPLACE')
 for obj in bpy.context.scene.objects:
  if obj.type!='MESH' or len(obj.data.vertices)<1000:continue
  for v in obj.data.vertices:
   p=arm.matrix_world.inverted()@obj.matrix_world@v.co
   # API source has centimetre armature-local coordinates.
   if 101<p.z<137 and abs(p.x)<29:
    blend=min(1,(p.z-101)/6,(137-p.z)/6,(29-abs(p.x))/5)
    anchors=[('Hips',99.55),('Spine02',112.02),('Spine01',124.49),('Spine',137.33)]
    for (a,za),(b,zb) in zip(anchors,anchors[1:]):
     if za<=p.z<=zb:
      t=(p.z-za)/(zb-za);assign(obj,v,{a:1-t,b:t},blend);report['torso']+=1;break
   elif 20<p.z<46 and abs(p.x)>6:
    blend=min(1,(p.z-20)/6,(46-p.z)/6)
    assign(obj,v,{'LeftLeg' if p.x>0 else 'RightLeg':1},blend);report['calf']+=1
 return report
