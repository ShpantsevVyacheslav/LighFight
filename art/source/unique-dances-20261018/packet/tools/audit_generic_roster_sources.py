"""Read-only original FBX mesh/UV/weights/bind inventory; no DCC/runtime launch."""
import importlib.util,pathlib,hashlib,array,json,sys
sp=importlib.util.spec_from_file_location('fbx_bind',pathlib.Path(__file__).with_name('audit_fbx_bind_girdle.py'));fb=importlib.util.module_from_spec(sp);sp.loader.exec_module(fb)
ACTIONS=[66,77,63,67,82,70,75,65,71,74,64]
def numbers(a):
 kind='d' if any(isinstance(x,float) for x in a) else 'q';b=array.array(kind,a)
 if sys.byteorder!='little':b.byteswap()
 return kind.encode()+b.tobytes()
def meshhash(nodes):
 h=hashlib.sha256()
 def walk(n):
  if n.name in ['Vertices','PolygonVertexIndex','Normals','NormalsIndex','UV','UVIndex','Tangents','Binormals','Colors','ColorIndex']:
   h.update(n.name.encode());h.update(numbers(n.p[0]))
  for c in n.c:walk(c)
 for n in nodes:walk(n)
 return h.hexdigest()
def one(p):
 ns=fb.FBX(p).all();ob=next(n for n in ns if n.name=='Objects').c;co=next(n for n in ns if n.name=='Connections').c;models={n.p[0]:n.p[1].split('\0')[0] for n in ob if n.name=='Model'};clusters={n.p[0]:n for n in ob if n.name=='Deformer' and n.p[-1]=='Cluster'};geom=[n for n in ob if n.name=='Geometry' and n.p[-1]=='Mesh'];weights=hashlib.sha256();binds=hashlib.sha256();names=[]
 for name,c in sorted((models[n.p[1]],clusters[n.p[2]]) for n in co if n.name=='C' and n.p[0]=='OO' and n.p[1] in models and n.p[2] in clusters):
  names.append(name)
  for x in c.c:
   if x.name in ['Indexes','Weights']:weights.update(name.encode()+x.name.encode()+numbers(x.p[0]))
   if x.name=='TransformLink':binds.update(name.encode()+numbers(x.p[0]))
 verts=sum(len(next(x for x in g.c if x.name=='Vertices').p[0])//3 for g in geom);polys=sum(sum(i<0 for i in next(x for x in g.c if x.name=='PolygonVertexIndex').p[0]) for g in geom)
 return {'file':str(p),'rawBytes':p.stat().st_size,'rawSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'meshes':len(geom),'vertices':verts,'polygons':polys,'skinnedBones':len(names),'boneNames':names,'meshUvNormalsTopologyHash':meshhash(geom),'allOriginalWeightsIndicesHash':weights.hexdigest(),'allOriginalBindLinksHash':binds.hexdigest()}
if __name__=='__main__':
 root=pathlib.Path('art/source/unique-dances-20261018');rows=[]
 for i,a in enumerate(ACTIONS):
  r=one(root/f'action-{a}.fbx');r.update(actor=i,action=a);rows.append(r)
 report={'method':'Raw FBX binary arrays and all skin-cluster weights/binds, read-only; no sampled/sparse vertices','actors':rows,'sharedGeometryUVNormalsTopology':len({r['meshUvNormalsTopologyHash'] for r in rows})==1,'sharedOriginalWeights':len({r['allOriginalWeightsIndicesHash'] for r in rows})==1,'sharedOriginalBindLinks':len({r['allOriginalBindLinksHash'] for r in rows})==1,'limits':'Donor audit only. Each target requires its own imported true-bind calibration; no shared target rig/weight substitution.'};dest=root/'audit/generic-roster';dest.mkdir(parents=True,exist_ok=True);(dest/'offline-original-source-inventory.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='actors'}))
