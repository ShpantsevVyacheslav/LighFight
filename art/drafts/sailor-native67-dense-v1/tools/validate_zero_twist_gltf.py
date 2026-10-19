from pathlib import Path
import numpy as np,json,ast,hashlib,sys
R=Path(__file__).resolve().parents[4];S=R/'art/drafts/sailor-native67-dense-v1'/(sys.argv[1] if len(sys.argv)>1 else 'native67-zero-twist-web-packet-v2');O=R/'prototypes/roster-portraits/twist-review'/(sys.argv[2] if len(sys.argv)>2 else '');P=R/'.tools/native67-bary-weight-packet-v1';r=json.loads((S/'export.json').read_text());g=json.loads((O/'sailor.gltf').read_text());data=(O/'sailor.bin').read_bytes()
# Use only raw TRS matrix math from builder; no export side effects.
tree=ast.parse((Path(__file__).parent/'build_zero_twist_gltf_v2.py').read_text());exec(compile(ast.Module(body=[v for v in tree.body if isinstance(v,ast.FunctionDef) and v.name in ['qmat','matrix','worlds']],type_ignores=[]),'<math-only>','exec'))
def read(i):
 a=g['accessors'][i];v=g['bufferViews'][a['bufferView']];n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']];dtype={5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']];return np.frombuffer(data,dtype,count=a['count']*n,offset=v.get('byteOffset',0)+a.get('byteOffset',0)).reshape(a['count'],n)
prim=g['meshes'][0]['primitives'][0];attrs=prim['attributes'];pos=read(attrs['POSITION']);j=np.c_[read(attrs['JOINTS_0']),read(attrs['JOINTS_1'])];w=np.c_[read(attrs['WEIGHTS_0']),read(attrs['WEIGHTS_1'])];srcp=np.fromfile(S/'positions.bin','<f4').reshape(-1,3);ids=np.fromfile(S/'originalIds.bin','<i4');c=np.fromfile(P/'counts.bin','u1');ptr=np.r_[0,np.cumsum(c,dtype=np.int64)];si=np.fromfile(P/'indices.bin','<i4');sw=np.fromfile(P/'weights.bin','<f4');srcnames=(P/'bones.txt').read_text().splitlines();dstnames=(S/'boneNames.txt').read_text().splitlines();mapping=np.array([dstnames.index(n) for n in srcnames]);
for k in range(8):
 on=c[ids]>k;at=ptr[ids[on]]+k;assert np.array_equal(w[on,k],sw[at]) and np.array_equal(j[on,k],mapping[si[at]]);assert np.all(w[~on,k]==0)
assert len(pos)==1305574 and g['accessors'][prim['indices']]['count']//3==1988914
bind=np.fromfile(S/'bindposes.bin','<f4').reshape(67,4,4).transpose(0,2,1);ibm=read(g['skins'][0]['inverseBindMatrices']).reshape(67,4,4).transpose(0,2,1);select=np.unique(np.r_[np.arange(0,len(pos),319),np.random.default_rng(19).integers(0,len(pos),4096)]);ref=np.diag([1,1,-1,1]);rows=[];parents={child:i for i,node in enumerate(g['nodes']) for child in node.get('children',[])}
for clip,anim in zip(r['clips'],g['animations']):
 raw=np.fromfile(S/clip['file'],'<f4').reshape(clip['frames'],len(r['nodes']),10);tracks={}
 for ch in anim['channels']:tracks[ch['target']['node'],ch['target']['path']]=read(anim['samplers'][ch['sampler']]['output'])
 for f in sorted(set([0,clip['frames']//4,clip['frames']//2,3*clip['frames']//4,clip['frames']-1])):
  sourceWorld=worlds(raw[f]);loc=[]
  for i,n in enumerate(g['nodes']):
   components=[]
   for path,default in [('translation',[0,0,0]),('rotation',[0,0,0,1]),('scale',[1,1,1])]:components.extend(tracks[i,path][f] if (i,path) in tracks else n.get(path,default))
   loc.append(matrix(np.array(components)))
  glob={}
  def world(i):
   if i not in glob:glob[i]=world(parents[i])@loc[i] if i in parents else loc[i]
   return glob[i]
  expected=np.zeros((len(select),3));actual=expected.copy()
  for k in range(8):
   weights=w[select,k];bone=j[select,k];srcmat=np.array([sourceWorld[r['joints'][int(b)]]@bind[int(b)] for b in range(67)]);dstmat=np.array([world(node)@ibm[i] for i,node in enumerate(g['skins'][0]['joints'])]);sp=srcp[select];tp=pos[select];expected+=np.einsum('nij,nj->ni',srcmat[bone,:3,:3],sp)*weights[:,None]+srcmat[bone,:3,3]*weights[:,None];actual+=np.einsum('nij,nj->ni',dstmat[bone,:3,:3],tp)*weights[:,None]+dstmat[bone,:3,3]*weights[:,None]
  expected[:,2]*=-1;err=float(np.linalg.norm(expected-actual,axis=1).max());assert err<1e-6,(clip['name'],f,err);rows.append({'clip':clip['name'],'frame':f,'sampledVertices':len(select),'worldPositionMaxErrorM':err})
report={'status':'ROUNDTRIP_POSITION_GATE_PASS_PENDING_BROWSER_VISUAL','vertices':len(pos),'triangles':1988914,'allPositiveWeightsExact':True,'noInfluencePruning':True,'rows':rows,'maxErrorM':max(x['worldPositionMaxErrorM'] for x in rows),'limits':'Sampled full-positive CPU raw Unity-local hierarchy vs decoded GLTF hierarchy at exact baked keys. Does not verify runtime shading, between-key motion or phone performance.'};(O/'roundtrip.json').write_text(json.dumps(report,indent=2));print(report['status'],report['maxErrorM'])
