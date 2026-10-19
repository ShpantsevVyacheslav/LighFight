"""Full-positive CSR bounds and raw-FBX interchange audit; no DCC side effects."""
import numpy as np

def source_id_uv(loop_indices,total):
 ids=np.asarray(loop_indices);assert 0<total<=1048576 and np.issubdtype(ids.dtype,np.integer) and ids.min()>=0 and ids.max()<total
 out=np.column_stack((ids.astype(np.float32)/np.float32(1048576),np.zeros(len(ids),np.float32)));assert np.array_equal(out[:,0]*np.float32(1048576),ids.astype(np.float32));return out

def csr_support_bounds(positions,ptr,indices,weights,bones):
 p=np.asarray(positions,dtype=float);assert ptr[0]==0 and ptr[-1]==len(weights) and np.all(np.diff(ptr)>0) and np.all(weights>0)
 rows=np.repeat(np.arange(len(p)),np.diff(ptr));lo=np.full((bones,3),np.inf);hi=np.full((bones,3),-np.inf)
 np.minimum.at(lo,indices,p[rows]);np.maximum.at(hi,indices,p[rows]);active=np.isfinite(lo).all(1);lo[~active]=0;hi[~active]=0;bits=np.array([[x,y,z] for x in (0,1) for y in (0,1) for z in (0,1)]);xyz=np.where(bits[None],hi[:,None],lo[:,None]);return np.concatenate([xyz,np.ones((*xyz.shape[:2],1))],2),active,float(np.add.reduceat(weights.astype(float),ptr[:-1]).max())

def error_bound(source,candidate,corners,active,maxsum):
 delta=np.einsum('bij,bkj->bki',candidate-source,corners)[...,:3];return float(np.linalg.norm(delta[active],axis=-1).max(initial=0)*maxsum)

def raw_fbx_attributes(path,positions,loop_indices,uv,ptr,indices,weights,names,reader_module,original_uvs=None,review_aux=None):
 a=reader_module;a.WANTED|={'UV','UVIndex'};nodes=a.Reader(path).all();objs=next(n for n in nodes if n.name=='Objects').c;conn=next(n for n in nodes if n.name=='Connections').c;geos=[n for n in objs if n.name=='Geometry' and n.p[-1]=='Mesh'];assert len(geos)==1;g=geos[0];raw=a.child(g,'Vertices').p[0].reshape(-1,3);assert np.array_equal(raw,positions),'Raw FBX positions/order changed';pol=a.child(g,'PolygonVertexIndex').p[0].copy();assert np.all(pol.reshape(-1,3)[:,2]<0);pol[pol<0]=-pol[pol<0]-1;assert np.array_equal(pol,loop_indices),'Raw oriented topology changed';layer=a.child(g,'LayerElementUV');vals=a.child(layer,'UV').p[0].reshape(-1,2);ind=a.child(layer,'UVIndex');corners=vals[ind.p[0]] if ind else vals;assert np.array_equal(corners,uv),'Raw corner UV changed'
 layers=[n for n in g.c if n.name=='LayerElementUV'];layer_info=[]
 for layer in layers:
  lname=a.child(layer,'Name').p[0];values=a.child(layer,'UV').p[0].reshape(-1,2);ui=a.child(layer,'UVIndex');cu=values[ui.p[0]] if ui else values;layer_info.append({'name':lname,'index':int(layer.p[0])})
  if original_uvs is not None and lname in original_uvs:assert np.array_equal(cu,original_uvs[lname]),'Original UV layer changed: '+lname
  if review_aux is not None and lname==review_aux['name']:assert np.array_equal(cu,review_aux['coordinates']),'Review ID UV coordinates changed'
 if original_uvs is not None:assert [x['name'] for x in layer_info[:len(original_uvs)]]==list(original_uvs),'Original UV layer order/name changed'
 if review_aux is not None:assert layer_info[-1]['name']==review_aux['name'],'Review UV not last original-preserving layer'
 models={n.p[0]:n for n in objs if n.name=='Model'};clusters={n.p[0]:n for n in objs if n.name=='Deformer' and n.p[-1]=='Cluster'};actual={};expected={}
 rows=np.repeat(np.arange(len(positions)),np.diff(ptr))
 for col,name in enumerate(names):
  keep=indices==col;expected[name]=(rows[keep],weights[keep].astype(float))
 for c in conn:
  if c.name=='C' and c.p[0]=='OO' and c.p[1] in models and c.p[2] in clusters:
   cl=clusters[c.p[2]];name=a.clean(models[c.p[1]].p[1]);ii=a.child(cl,'Indexes');ww=a.child(cl,'Weights');actual[name]=(ii.p[0],ww.p[0]) if ii and ww else (np.array([],int),np.array([],float))
 assert set(actual)==set(names),'Cluster bone-name set differs'
 for name in names:
  ii,ww=actual[name];order=np.argsort(ii);ei,ew=expected[name];assert np.array_equal(ii[order],ei) and np.array_equal(ww[order],ew),'Raw positive named weights changed: '+name
 return {'rawPositionsExact':True,'rawOrientedTrianglesExact':True,'rawCornerUVExact':True,'rawAllPositiveNamedWeightsExact':True,'positiveWeights':len(weights),'bones':len(names),'UVLayers':layer_info,'allOriginalUVLayersExact':original_uvs is not None,'reviewUVExact':review_aux is not None,'rawTangentLayerPresent':a.child(g,'LayerElementTangent') is not None,'rawBinormalLayerPresent':a.child(g,'LayerElementBinormal') is not None}
