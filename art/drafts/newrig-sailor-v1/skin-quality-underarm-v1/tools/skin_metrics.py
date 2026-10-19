"""Pure geometry diagnostics; no bpy, mutation, smoothing or inferred anatomy."""
import itertools
import numpy as np

def _points(p):
 p=np.asarray(p,dtype=np.float64)
 if p.ndim!=2 or p.shape[1]!=3 or not np.isfinite(p).all():raise ValueError('finite Nx3 points required')
 return p

def _stats(v):
 v=np.asarray(v);return {k:float(x) for k,x in zip(['min','median','p95','p99','max'],np.quantile(v,[0,.5,.95,.99,1]))} if len(v) else None

def canonical_edges(triangles):
 t=np.asarray(triangles)
 if t.ndim!=2 or t.shape[1]!=3 or t.dtype.kind not in 'iu' or np.any(t<0):raise ValueError('nonnegative integer triangles required')
 return np.unique(np.sort(np.concatenate([t[:,[0,1]],t[:,[1,2]],t[:,[2,0]]]),axis=1),axis=0)

def edge_metrics(rest,posed,edges,mask=None):
 a,b=_points(rest),_points(posed);e=np.asarray(edges);ids=np.arange(len(e))
 if a.shape!=b.shape:raise ValueError('same original vertex count required')
 if mask is not None:
  m=np.asarray(mask,dtype=bool);use=np.any(m[e],axis=1);e=e[use];ids=ids[use]
 la=np.linalg.norm(a[e[:,1]]-a[e[:,0]],axis=1);lb=np.linalg.norm(b[e[:,1]]-b[e[:,0]],axis=1);valid=la>1e-12;ratio=lb[valid]/la[valid];log=np.abs(np.log(np.maximum(ratio,1e-300)));worst=np.argsort(log)[-8:][::-1]
 return {'edgeCount':int(len(e)),'degenerateRestEdges':int((~valid).sum()),'ratio':_stats(ratio),'logStrainAbs':_stats(log),'worstEdgeCanonicalIDs':ids[valid][worst].tolist(),'worstEdgesVertexPairs':e[valid][worst].tolist(),'worstRatios':ratio[worst].tolist(),'boundaryPolicy':'any original endpoint in mask; no edge subsampling'}

def triangle_metrics(rest,posed,triangles,mask=None,expected_normals=None):
 a,b=_points(rest),_points(posed);t=np.asarray(triangles);ids=np.arange(len(t));use=np.ones(len(t),bool) if mask is None else np.any(np.asarray(mask,bool)[t],axis=1);t=t[use];ids=ids[use]
 cross=lambda p:np.cross(p[t[:,1]]-p[t[:,0]],p[t[:,2]]-p[t[:,0]])
 ca,cb=cross(a),cross(b);aa=np.linalg.norm(ca,axis=1)*.5;ab=np.linalg.norm(cb,axis=1)*.5;valid=aa>1e-18;ratios=ab[valid]/aa[valid];collapsed=(ab<=np.maximum(aa*1e-4,1e-18))&valid;opposed=[]
 if expected_normals is not None:
  normal=_points(expected_normals)[use];opposed=ids[np.einsum('ij,ij->i',cb,normal)<-1e-16].tolist()
 worst=np.argsort(np.abs(np.log(np.maximum(ratios,1e-300))))[-8:][::-1]
 return {'triangleCount':int(len(t)),'degenerateRestTriangles':int((~valid).sum()),'areaRatio':_stats(ratios),'collapsedCount':int(collapsed.sum()),'collapsedOriginalTriangleIDs':ids[collapsed][:16].tolist(),'worstOriginalTriangleIDs':ids[valid][worst].tolist(),'orientationAvailable':expected_normals is not None,'opposedTransportNormalCount':len(opposed) if expected_normals is not None else None,'opposedOriginalTriangleIDs':opposed[:16],'interpretation':'orientation witness; not a self-intersection proof'}

def section_segments(points,triangles,center,normal,epsilon=1e-9):
 p=_points(points);t=np.asarray(triangles);c=np.asarray(center,float);n=np.array(normal,dtype=float,copy=True);n/=np.linalg.norm(n);v=p[t];d=np.einsum('tij,j->ti',v-c,n);d[np.abs(d)<=epsilon]=0;use=(d.min(1)<=0)&(d.max(1)>=0)&np.any(d!=0,axis=1);v=v[use];d=d[use];segments=[]
 for verts,dist in zip(v,d):
  hits=[]
  for i,j in [(0,1),(1,2),(2,0)]:
   if dist[i]==0 and dist[j]==0:continue
   if dist[i]*dist[j]<=0:
    q=verts[i]+(verts[j]-verts[i])*dist[i]/(dist[i]-dist[j])
    if not any(np.linalg.norm(q-x)<=epsilon for x in hits):hits.append(q)
  if len(hits)==2 and np.linalg.norm(hits[0]-hits[1])>epsilon:segments.append(hits)
 return np.asarray(segments,float).reshape(-1,2,3)

def closed_section(segments,center,normal,epsilon=1e-7):
 s=np.asarray(segments,float);unavailable=lambda why:{'available':False,'reason':why,'segmentCount':len(s),'volumeClaim':False}
 if s.size==0:return unavailable('no_intersection')
 if s.shape[1:]!=(2,3) or not np.isfinite(s).all() or len(s)>20000:return unavailable('invalid_or_over_budget')
 points=[];grid={};edges=set();neighbors=list(itertools.product([-1,0,1],repeat=3))
 def vertex(q):
  cell=tuple(np.floor(q/epsilon).astype(np.int64))
  for off in neighbors:
   for i in grid.get(tuple(c+o for c,o in zip(cell,off)),[]):
    if np.linalg.norm(points[i]-q)<=epsilon:return i
  i=len(points);points.append(q.copy());grid.setdefault(cell,[]).append(i);return i
 for a,b in s:
  i,j=vertex(a),vertex(b)
  if i!=j:edges.add(tuple(sorted((i,j))))
 adj={i:[] for i in range(len(points))}
 for i,j in edges:adj[i].append(j);adj[j].append(i)
 if not edges or any(len(v)!=2 for v in adj.values()):return unavailable('open_or_branched_contour')
 order=[0];previous=-1;current=0
 for _ in range(len(points)+1):
  nxt=next(i for i in adj[current] if i!=previous)
  if nxt==0:break
  if nxt in order:return unavailable('invalid_cycle')
  order.append(nxt);previous,current=current,nxt
 else:return unavailable('walk_budget')
 if len(order)!=len(points):return unavailable('multiple_components')
 n=np.array(normal,dtype=float,copy=True);n/=np.linalg.norm(n);axis=np.eye(3)[np.argmin(np.abs(n))];u=np.cross(n,axis);u/=np.linalg.norm(u);v=np.cross(n,u);q=np.asarray(points)[order]-np.asarray(center);xy=np.column_stack((q@u,q@v));cross2=lambda a,b:a[...,0]*b[...,1]-a[...,1]*b[...,0]
 for i in range(len(xy)):
  j=np.arange(i+2,len(xy));j=j[j!=(i-1)%len(xy)]
  if not len(j):continue
  a,b=xy[i],xy[(i+1)%len(xy)];c,e=xy[j],xy[(j+1)%len(xy)];r=b-a;z=e-c;den=cross2(r,z);good=np.abs(den)>epsilon**2;safe=np.where(good,den,1);t=cross2(c-a,z)/safe;w=cross2(c-a,r)/safe
  if np.any(good&(t>0)&(t<1)&(w>0)&(w<1)):return unavailable('self_crossing_contour')
 area=abs(float(cross2(xy,np.roll(xy,-1,axis=0)).sum()))*.5
 if area<=epsilon**2:return unavailable('zero_area')
 return {'available':True,'areaM2':area,'diametersM':np.ptp(xy,axis=0).tolist(),'segmentCount':len(s),'orderedContourWorld':(np.asarray(points)[order]).tolist(),'volumeClaim':False,'interpretation':'one simple closed cross-section; not closed-garment volume'}

def anchor_metrics(baseline,candidate,indices):
 a,b=_points(baseline),_points(candidate);ids=np.asarray(indices,int);d=np.linalg.norm(b[ids]-a[ids],axis=1);worst=np.argsort(d)[-8:][::-1]
 return {'count':len(ids),'maxM':float(d.max(initial=0)),'p99M':float(np.quantile(d,.99)) if len(d) else None,'worstOriginalVertexIDs':ids[worst].tolist()}

def candidate_fences(before,after,anchors,masks_reviewed=False,anchor_limit=1e-6):
 failures=[]
 if not masks_reviewed:failures.append('masks_not_visually_reviewed')
 for key in ['p99','max']:
  if after['logStrainAbs'][key]>before['logStrainAbs'][key]+1e-9:failures.append('worst_strain_worsened' if key=='max' else 'p99_strain_worsened')
 if after.get('collapsedCount',0)>before.get('collapsedCount',0):failures.append('new_collapse')
 if anchors['maxM']>anchor_limit:failures.append('anchor_moved')
 return {'pass':not failures,'failures':failures,'meaning':'numeric regression fence only; visual shape acceptance remains required'}

def transport_normals(linear,rest_normals):
 a=np.asarray(linear,float);n=_points(rest_normals);cofactor=np.stack([np.cross(a[:,:,1],a[:,:,2]),np.cross(a[:,:,2],a[:,:,0]),np.cross(a[:,:,0],a[:,:,1])],axis=2);return np.einsum('nij,nj->ni',cofactor,n)

def projected_envelope(points,indices,origin,right,up,height_bounds,bins=32):
 p=_points(points);ids=np.asarray(indices,int);q=p[ids]-np.asarray(origin);x=q@np.asarray(right);y=q@np.asarray(up);lo,hi=height_bounds
 if hi<=lo:raise ValueError('nonzero fixed projection height interval required')
 which=np.clip(np.floor((y-lo)/(hi-lo)*bins).astype(int),0,bins-1);rows=[]
 for i in range(bins):
  local=np.flatnonzero(which==i)
  if not len(local):rows.append(None);continue
  low=local[np.argmin(x[local])];high=local[np.argmax(x[local])];rows.append({'count':len(local),'minX':float(x[low]),'maxX':float(x[high]),'minOriginalVertexID':int(ids[low]),'maxOriginalVertexID':int(ids[high])})
 return {'heightBoundsM':[float(lo),float(hi)],'bins':rows,'interpretation':'all-point projected envelope; not an occlusion-qualified raster silhouette or hole proof'}
