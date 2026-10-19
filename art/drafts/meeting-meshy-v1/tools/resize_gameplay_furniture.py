"""Resize only connected table component; translate chairs rigidly. Immutable source/LOD input."""
import runpy,contextlib,io,json,sys,hashlib
import numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];P=ROOT/'art/drafts/meeting-meshy-v1';sys.path.insert(0,str(ROOT/'art/drafts/kitchen-env-lod-v1/tools'));from glb_static import write
with contextlib.redirect_stdout(io.StringIO()):d=runpy.run_path(str(P/'tools/inspect_furniture_components.py'))
g=d['g'];raw=bytearray(d['b']);pos=d['P'];component=d['r'];scale=1.4/.3764801025390625;primitive=g['meshes'][0]['primitives'][0];a=g['accessors'][primitive['attributes']['POSITION']];v=g['bufferViews'][a['bufferView']];Q=np.ndarray((a['count'],3),dtype='<f4',buffer=raw,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',12),4));na=g['accessors'][primitive['attributes']['NORMAL']];nv=g['bufferViews'][na['bufferView']];N=np.ndarray((na['count'],3),dtype='<f4',buffer=raw,offset=nv.get('byteOffset',0)+na.get('byteOffset',0),strides=(nv.get('byteStride',12),4));anchors=json.loads((P/'chair-anchors.json').read_text());rows=[];tableMask=None;half=.0
for c in np.unique(component):
 mask=component==c;W=pos[mask]
 if len(W)<30:continue
 if W[:,1].max()<1.1 and W[:,0].min()<0 and W[:,0].max()>0:tableMask=mask;half=(W[:,0].max()-W[:,0].min())/2;break
assert tableMask is not None
Q[tableMask,0]*=1.35;Q[tableMask,2]*=1.15;N[tableMask,0]/=1.35;N[tableMask,2]/=1.15;N[tableMask]/=np.linalg.norm(N[tableMask],axis=1)[:,None]
newanchors=json.loads(json.dumps(anchors));tableDelta=half*.35
for c in np.unique(component):
 mask=component==c;W=pos[mask]
 if len(W)<30 or np.any(mask&tableMask):continue
 x,z=W.mean(0)[[0,2]];candidates=[r for r in newanchors['anchors'] if np.sign(r['gameplay']['floorRoot'][0])==np.sign(x)];row=min(candidates,key=lambda r:abs(r['gameplay']['floorRoot'][2]-z));old=row['gameplay']['floorRoot'];dx=np.sign(x)*tableDelta;dz=(old[2]-4.8)*.15;Q[mask,0]+=dx/scale;Q[mask,2]+=dz/scale
 for key in ['seatSurfaceCenter','pelvisTargetEstimate','floorRoot']:row['gameplay'][key][0]+=float(dx);row['gameplay'][key][2]+=float(dz)
 rows.append({'chairId':row['id'],'rigidTranslation':[float(dx),0,float(dz)]})
a['min']=Q.min(0).tolist();a['max']=Q.max(0).tolist();out=P/'lod/furniture-80k-wide.glb';write(out,g,raw);newanchors['status']='GAMEPLAY_TABLE_WIDENED_REQUIRES_CONTEXT_CAPTURE';(P/'chair-anchors-wide.json').write_text(json.dumps(newanchors,indent=2));report={'status':'RESIZED_DERIVATIVE_PENDING_ACTUAL_REVIEW','source':str(d['p']),'sourceSHA256':hashlib.sha256((ROOT/d['p']).read_bytes()).hexdigest(),'tableScaleXZ':[1.35,1.15],'tabletopY':float(pos[tableMask,1].max()),'tableWidthBefore':float(half*2),'tableWidthAfter':float(half*2*1.35),'chairTranslations':rows,'triangleCountUnchanged':True,'normalsInverseTransposeCorrected':True,'catalogPublished':False};(P/'furniture-wide.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
