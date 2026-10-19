"""Reviewed local graph scope, offline derived weight candidates; not rendered acceptance."""
import os
for key in ['OPENBLAS_NUM_THREADS','OMP_NUM_THREADS','MKL_NUM_THREADS']:os.environ[key]='1'
import sys,time,json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
HERE=Path(__file__).resolve();helper=HERE.parent/'prepare_connected_patch.py';ns={'__file__':str(helper),'__name__':'original_source_graph'};exec(compile(helper.read_text().split('view=next(')[0],str(helper),'exec'),ns);globals().update({k:v for k,v in ns.items() if not k.startswith('__')});O=HERE.parents[1]
sys.path.insert(0,str(HERE.parent));sys.path.insert(0,str(B/'tools'));from mixture_kernel import harmonic,mix
from skin_metrics import edge_metrics,triangle_metrics,canonical_edges
receipt=json.loads((O/'corridor-bone-audit.json').read_text());assert receipt['status']=='OFFLINE_CORRIDOR_BONE_REVIEW_NO_WEIGHTS_CHANGED'
cp=np.load(O/'corridor-proposal.npz');patch=cp['originalVertexIndices'];outer=cp['fixedOuterBoundary'];red=cp['fixedTorsoCore'];purple=cp['fixedSleeveCore'];assert not np.intersect1d(patch,protected).size
local=np.full(N,-1,int);local[patch]=np.arange(len(patch));ep=edges[np.all(local[edges]>=0,axis=1)];el=local[ep];length=np.linalg.norm(rest[ep[:,0]]-rest[ep[:,1]],axis=1);assert np.all(length>0)
# Explicit proposed families, supported by mapped anatomical chain. Existing
# conditional bone proportions are retained, never replacing forearm by upperarm.
armNames={'DEF-upper_arm.R','DEF-upper_arm.R.001','DEF-forearm.R','DEF-forearm.R.001'};torsoNames={'DEF-breast.R','DEF-spine.001','DEF-spine.002'}
armSlots=np.isin(ji[patch],[names.index(n) for n in armNames]);torsoSlots=np.isin(ji[patch],[names.index(n) for n in torsoNames]);original=w[patch].astype(float);total=original.sum(1);a=(original*armSlots).sum(1);t=(original*torsoSlots).sum(1);unknown=np.any((original>0)&~(armSlots|torsoSlots),axis=1);unmixable=(a<=0)|(t<=0)|unknown
fixed=np.isin(patch,np.r_[outer,red,purple])|unmixable;alpha=a/total
fit,solve=harmonic(len(patch),el,1/length,alpha,fixed,tolerance=1e-8,max_iterations=20000)
OUT=O/'offline-candidates-v1';OUT.mkdir(exist_ok=True);sourceSha=hashlib.sha256((P/'original-indexed-attributes.npz').read_bytes()).hexdigest();allEdges=canonical_edges(tri);reg=np.load(B/'regions-v1/region-proposals.npz')
view=next(x for x in json.loads((C/'weight-witness-v1/renders/report.json').read_text())['renders'] if x['file']=='worst-edge-side.png');cam=np.array(view['cameraWorld']);target=np.array(view['cameraTarget']);f=target-cam;f/=np.linalg.norm(f);right=np.cross(f,[0,0,1]);right/=np.linalg.norm(right);up=np.cross(right,f);W,H=640,768;scale=view['orthoScale']
def project(p):q=p-target;return np.c_[W/2+(q@right)*H/scale,H/2-(q@up)*H/scale]
xy=project(points);pix=np.rint(xy).astype(int);valid=(pix[:,0]>=0)&(pix[:,0]<W)&(pix[:,1]>=0)&(pix[:,1]<H);depth=(points-cam)@f;zb=np.full(W*H,np.inf);pid=pix[valid,1]*W+pix[valid,0];np.minimum.at(zb,pid,depth[valid]);vis=np.zeros(N,bool);vis[valid]=depth[valid]<=zb[pid]+.0008
background=Image.open(C/'artist-contour-v3/build-v3/source-before-side.png').convert('RGB');report={'status':'OFFLINE_DERIVED_WEIGHTS_NOT_ENGINE_VALIDATED','sourceIndexedSHA256':sourceSha,'corridorScopeApprovedByRoot':True,'sourceVertices':N,'sourceTriangles':len(tri),'patchVertices':len(patch),'fixedVertexCount':int(fixed.sum()),'unknownFamilyFixedVertices':int(unknown.sum()),'missingFamilyFixedVertices':int(unmixable.sum()),'armFamily':sorted(armNames),'torsoFamily':sorted(torsoNames),'solve':solve,'cases':[],'limits':['No actual Blender candidate render yet; plots are weight/displacement diagnostics over unchanged actual source render.','No torso/forearm anatomical error asserted.','Dirichlet mixture smoothing is a hypothesis, not a quality objective by itself.','Source geometry/UV/maps/binds unchanged; derived named weights intentionally change.']}
for amount in [.25,.5]:
 candidate64=mix(original,armSlots,fit,amount,fixed);stored=candidate64.astype(np.float32);assert np.array_equal(stored[fixed],w[patch][fixed]);assert np.array_equal(stored>0,original>0)
 # Existing4slots retained: no pruning/top4 projection. Float32 storage only.
 delta=stored.astype(float)-original;active=patch[np.any(delta!=0,axis=1)];assert not np.intersect1d(active,protected).size
 output=points.copy();hpPatch=hp[patch];part=np.zeros((len(patch),3))
 for k in range(4):part+=np.einsum('nij,nj->ni',skin[ji[patch,k]],hpPatch)[:,:3]*stored[:,k,None]
 output[patch]=part;disp=np.linalg.norm(output-points,axis=1);newalpha=(stored*armSlots).sum(1)/stored.sum(1)
 name='lambda'+str(int(amount*100));np.savez_compressed(OUT/(name+'-weights.npz'),originalVertexIndices=patch,jointIndices=ji[patch],originalWeights=w[patch],candidateWeights=stored,fixedOriginalVertexIndices=patch[fixed])
 row={'name':name,'lambda':amount,'changedVertices':len(active),'max4ProjectionResidual':0,'max4ProjectionMethod':'None: only existing positive4slots rescaled, no truncation/pruning/new influences.','float32StorageMaxComponentError':float(abs(stored-candidate64).max()),'storedMaxWeightSumDeviation':float(abs(stored.sum(1)-1).max()),'maxAbsoluteWeightChange':float(abs(delta).max()),'maxFamilyFractionChange':float(abs(newalpha-alpha).max()),'protectedChangedVertices':0,'fixedChangedVertices':0,'positiveSupportPreserved':True,'allPatchMaxInfluences':int(np.max((stored>0).sum(1))),'predictedDance50DisplacementM':dict(zip(['median','p95','max'],map(float,np.quantile(disp[patch],[.5,.95,1])))),'neutralRestSumOnlyDisplacementBoundM':float(np.max(np.linalg.norm(rest[patch],axis=1)*abs(stored.sum(1,dtype=float)-original.sum(1)))),'regions':{}}
 for region in ['sleeve_armhole_R','torso_underarm_R']:
  maskr=np.zeros(N,bool);maskr[reg[region]]=True;before=edge_metrics(rest,points,allEdges,maskr);after=edge_metrics(rest,output,allEdges,maskr);row['regions'][region]={'before':before,'after':after,'strictP99MaxNoWorse':bool(after['logStrainAbs']['p99']<=before['logStrainAbs']['p99']+1e-9 and after['logStrainAbs']['max']<=before['logStrainAbs']['max']+1e-9)}
 # Local witnesses prevent unchanged global tails from hiding patch regressions.
 pe=allEdges[np.any(np.isin(allEdges,active),axis=1)];ra=np.linalg.norm(rest[pe[:,1]]-rest[pe[:,0]],axis=1);bl=np.linalg.norm(points[pe[:,1]]-points[pe[:,0]],axis=1);al=np.linalg.norm(output[pe[:,1]]-output[pe[:,0]],axis=1)
 row['changedIncidentEdges']={'count':len(pe),'maxAbsolutePosedEdgeDeltaM':float(abs(al-bl).max()),'bins':{}}
 for threshold in [.0001,.001]:
  use=ra>threshold
  row['changedIncidentEdges']['bins'][str(threshold)]={'count':int(use.sum()),'beforeP99AbsoluteExcessM':float(np.quantile(np.maximum(bl[use]-ra[use],0),.99)),'afterP99AbsoluteExcessM':float(np.quantile(np.maximum(al[use]-ra[use],0),.99)),'beforeMaxRatio':float((bl[use]/ra[use]).max()),'afterMaxRatio':float((al[use]/ra[use]).max())}
 ptids=np.flatnonzero(np.any(np.isin(tri,active),axis=1));pt=tri[ptids];cross=lambda x:np.cross(x[pt[:,1]]-x[pt[:,0]],x[pt[:,2]]-x[pt[:,0]]);cb,ca=cross(points),cross(output)
 row['changedIncidentTriangles']={'count':len(pt),'areaBeforeM2':float(np.linalg.norm(cb,axis=1).sum()/2),'areaAfterM2':float(np.linalg.norm(ca,axis=1).sum()/2),'opposedBaselineNormals':int(np.sum(np.einsum('ij,ij->i',cb,ca)<0)),'collapsedAfterCount':int(np.sum(np.linalg.norm(ca,axis=1)<1e-18)),'oppositionWitnesses':[{'originalTriangleID':int(ptids[i]),'originalVertexIDs':pt[i].tolist(),'beforeAreaMm2':float(np.linalg.norm(cb[i])*5e5),'afterAreaMm2':float(np.linalg.norm(ca[i])*5e5),'maxVertexDisplacementMm':float(disp[pt[i]].max()*1000)} for i in np.flatnonzero(np.einsum('ij,ij->i',cb,ca)<0)]}
 # Annotated scalar diagnostics are not re-rendered candidate beauty.
 sheet=Image.new('RGB',(1280,800),(18,18,18))
 for col,(label,values) in enumerate([('Original arm-family mixture',alpha),(name+' derived mixture',newalpha)]):
  im=background.copy();arr=np.array(im)
  for j,v in enumerate(patch):
   if not vis[v]:continue
   x,y=pix[v];rgb=np.array([255*values[j],80,255*(1-values[j])]);arr[y,x]=(arr[y,x]*.25+rgb*.75).astype(np.uint8)
  im=Image.fromarray(arr);d=ImageDraw.Draw(im);d.rectangle((0,0,W,46),fill=(15,15,15));d.text((8,8),label,fill='white');d.text((8,26),'Weight heatmap only, NOT candidate beauty render',fill='white');sheet.paste(im,(col*640,32))
 d=ImageDraw.Draw(sheet);d.text((8,8),'Same original geometry/camera; red=arm family / blue=torso family',fill='white');sheet.save(OUT/(name+'-weight-heatmap.png'))
 report['cases'].append(row)
(OUT/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'solve':solve,'cases':[{k:c[k] for k in ['name','changedVertices','maxAbsoluteWeightChange','predictedDance50DisplacementM','storedMaxWeightSumDeviation']} for c in report['cases']]}))
