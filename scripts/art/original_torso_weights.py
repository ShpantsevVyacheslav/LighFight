"""Keep loose shirt waist attached to axial bones rather than kick-driven legs."""
def apply(body,arm=None):
 anchors=[('Hips',86),('Spine02',99),('Spine01',115)]
 if arm:anchors=[(n,arm.data.bones[n].head_local.z) for n,z in anchors]
 for v in body.data.vertices:
  if abs(v.co.x)>=22 or not 82<v.co.z<120:continue
  weights={min(anchors,key=lambda a:abs(v.co.z-a[1]))[0]:1}
  for a,b in zip(anchors,anchors[1:]):
   midpoint=(a[1]+b[1])/2
   if abs(v.co.z-midpoint)<2:
    t=(v.co.z-midpoint+2)/4;weights={a[0]:1-t,b[0]:t};break
  blend=max(0,min(1,(22-abs(v.co.x))/2))*max(0,min(1,(120-v.co.z)/5))
  previous={body.vertex_groups[g.group].name:g.weight for g in v.groups}
  for name,w in previous.items():weights[name]=weights.get(name,0)*blend+w*(1-blend)
  for name in list(weights):
   if name not in previous:weights[name]*=blend
  weights=dict(sorted(weights.items(),key=lambda p:p[1],reverse=True)[:4]);total=sum(weights.values());weights={n:w/total for n,w in weights.items()}
  for g in list(v.groups):body.vertex_groups[g.group].remove([v.index])
  for name,w in weights.items():body.vertex_groups[name].add([v.index],w,'REPLACE')
