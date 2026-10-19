"""Explicit one-pose contour A; run only in an authorized CPU6 Blender window.
Pure math imports safely for tests. Original files/weights never saved or edited.
"""
import numpy as np

def compact_kernel(r):
 r=np.asarray(r,float);q=np.maximum(1-r,0);return np.where((r>=0)&(r<1),q**4*(4*r+1),0)

def interpolate_field(points,centers,values,radius):
 p=np.asarray(points,float);c=np.asarray(centers,float);v=np.asarray(values,float)
 if radius<=0 or not np.isfinite(p).all() or not np.isfinite(c).all() or not np.isfinite(v).all():raise ValueError('finite controls and positive support required')
 A=compact_kernel(np.linalg.norm(c[:,None]-c[None,:],axis=2)/radius)
 if np.linalg.cond(A)>1e6:raise ValueError('duplicate/ill-conditioned artist controls')
 coefficients=np.linalg.solve(A,v);out=np.zeros((len(p),v.shape[1]))
 for i in range(len(c)):out+=compact_kernel(np.linalg.norm(p-c[i],axis=1)/radius)[:,None]*coefficients[i]
 return out

def swept_volume(before,after,tri):
 a=np.asarray(before,float);b=np.asarray(after,float);t=np.asarray(tri);origin=(a.min(0)+a.max(0))*.5
 def volume(p):
  v=p[t]-origin;return np.einsum('ij,ij->i',v[:,0],np.cross(v[:,1],v[:,2])).sum()/6
 return float(volume(b)-volume(a))

def balanced_coefficient(evaluate,tolerance):
 lo,hi=-1.,1.;a,b=evaluate(lo),evaluate(hi)
 if abs(evaluate(0))<=tolerance:return 0.
 if not np.isfinite([a,b]).all() or a*b>0:raise ValueError('bounded volume compensation does not bracket zero; refuse target')
 for _ in range(60):
  mid=(lo+hi)/2;c=evaluate(mid)
  if abs(c)<=tolerance:return mid
  if a*c<=0:hi=mid;b=c
  else:lo=mid;a=c
 raise ValueError('volume residual exceeds fixed tolerance')

def garment_activation(clip,features,reference,phase,duration):
 if clip!='dance_01':return 0.
 distance=float(np.linalg.norm((np.asarray(features)-np.asarray(reference))/np.array([15.,20.])))
 fade=min(max(phase*duration/.35,0),max((1-phase)*duration/.35,0),1);fade=fade*fade*(3-2*fade)
 return float(compact_kernel(np.array([distance]))[0]*fade)

def main():
 import argparse,hashlib,json,sys,time
 from pathlib import Path
 file=Path(__file__).resolve();owned=file.parents[1];co=owned.parent;skinRoot=co.parent;base=skinRoot/'tools/probe_source_off_on.py'
 parser=argparse.ArgumentParser();parser.add_argument('--mode',choices=['prepare','build'],default='prepare');options=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 configuration=json.loads((owned/'design.json').read_text());receiptPath=owned/'garment-review.json'
 if options.mode=='build':
  receipt=json.loads(receiptPath.read_text());assert receipt.get('reviewed') is True and receipt.get('garmentOnly') is True,'Actual new mask must be inspected first'
 sys.argv=[sys.argv[0],'--','--stage','renders','--source-material','--output',str(owned/options.mode),'--max-seconds','300'];source=base.read_text();source=source[:source.index("if args.stage=='masks':")];needle="'backthreequarter':(.85,2,.12),'side':(2,0,.06)";assert needle in source;source=source.replace(needle,"'backthreequarter':(-.85,2,.12),'side':(-2,0,.06)");ns={'__file__':str(base),'__name__':'owned_source_definitions'};exec(compile(source,str(base),'exec'),ns)
 import bpy
 from mathutils import Vector
 from mathutils.bvhtree import BVHTree
 from mathutils.kdtree import KDTree
 sys.path.insert(0,str(co/'tools'));from corrective_core import inverse_lbs_delta
 R=ns['report'];OUT=ns['OUT'];N=ns['N'];tri=ns['tri'];raw=ns['raw'];rest=ns['restWorld'];regions=ns['regions'];body=ns['body'];rig=ns['rig'];cam=ns['cam'];scene=ns['scene'];sha=ns['sha'];render=ns['render'];ns['setup_pose']({'name':'dance50','clip':'dance_01','phase':.5},True);points,matrices=ns['world_surface']();R['artistDesign']=configuration;R['candidateStatus']='PREPARED_NOT_ACCEPTED';R['renders']=[]
 sourceView=co/'weight-witness-v1/renders/worst-edge-side.png';assert sha(sourceView)==configuration['sourceViewSHA256'];viewReport=json.loads((sourceView.parent/'report.json').read_text());view=next(x for x in viewReport['renders'] if x['file']=='worst-edge-side.png');cam.location=Vector(view['cameraWorld']);cam.rotation_euler=(Vector(view['cameraTarget'])-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=view['orthoScale'];bpy.context.view_layer.update();assert cam.location.x<view['cameraTarget'][0]
 frame=cam.data.view_frame(scene=scene);xmin,xmax=min(v.x for v in frame),max(v.x for v in frame);ymin,ymax=min(v.y for v in frame),max(v.y for v in frame);direction=cam.matrix_world.to_3x3()@Vector((0,0,-1));direction.normalize();toward=-np.array(direction);bvh=BVHTree.FromPolygons(points.tolist(),tri.tolist(),all_triangles=True)
 allowed=np.zeros(N,bool)
 for name in ['sleeve_armhole_R','torso_underarm_R']:allowed[regions[name]]=True
 protected=np.unique(np.concatenate([regions[n] for n in regions.files if 'anchor' in n]));allowed[protected]=False
 def pick(pixel):
  u=pixel[0]/configuration['dimensions'][0];v=1-pixel[1]/configuration['dimensions'][1];origin=cam.matrix_world@Vector((xmin+u*(xmax-xmin),ymin+v*(ymax-ymin),frame[0].z));location,normal,face,distance=bvh.ray_cast(origin,direction,10)
  assert face is not None and np.all(allowed[tri[face]]),('Artist handle did not hit allowed garment; inspect, do not snap',pixel,face)
  return np.array(location),int(face)
 hits=[pick(c['pixelTopLeft']) for c in configuration['creaseControls']];centers=np.array([x[0] for x in hits]);desired=toward[None,:]*np.array([c['towardCameraMM']/1000 for c in configuration['creaseControls']])[:,None];compCenters=np.array([pick(c)[0] for c in configuration['compensationControls']]);crease=interpolate_field(points,centers,desired,configuration['creaseSupportRadiusM']);initial=np.sum([compact_kernel(np.linalg.norm(points-c,axis=1)/configuration['compensationRadiusM']) for c in compCenters],axis=0)[:,None]*(-toward)
 atControls=np.sum([compact_kernel(np.linalg.norm(centers-c,axis=1)/configuration['compensationRadiusM']) for c in compCenters],axis=0)[:,None]*(-toward);comp=initial-interpolate_field(points,centers,atControls,configuration['creaseSupportRadiusM'])
 # Feather only mask boundaries; this does not smooth geometry or weights.
 flags=allowed[tri];boundaryFaces=flags.any(1)&~flags.all(1);boundary=np.unique(tri[boundaryFaces][flags[boundaryFaces]]);assert len(boundary)>0;tree=KDTree(len(boundary))
 for i,v in enumerate(boundary):tree.insert(Vector(points[v]),i)
 tree.balance();support=allowed&((np.linalg.norm(crease,axis=1)>1e-12)|(np.linalg.norm(comp,axis=1)>1e-12));ids=np.flatnonzero(support);feather=np.zeros(N)
 for i in ids:
  dist=tree.find(Vector(points[i]))[2];u=min(dist/configuration['maskFeatherM'],1);feather[i]=u*u*(3-2*u)
 for center in centers:assert tree.find(Vector(center))[2]>=configuration['maskFeatherM'],'Explicit handle too close to mask boundary; revise artist mask'
 crease*=feather[:,None];comp*=feather[:,None];comp*=configuration['compensationMaximumM']/max(np.linalg.norm(comp,axis=1).max(),1e-20);ids=np.flatnonzero((np.linalg.norm(crease,axis=1)>1e-12)|(np.linalg.norm(comp,axis=1)>1e-12));assert not np.intersect1d(ids,protected).size
 maskfile=owned/'artist-mask.npz';controls={'sourcePixels':[c['pixelTopLeft'] for c in configuration['creaseControls']],'sourceWorldM':centers.tolist(),'explicitTargetWorldM':(centers+desired).tolist(),'originalFaceIDs':[x[1] for x in hits],'compensationWorldM':compCenters.tolist(),'meaning':'Desired artist surface points, not corrected-render evidence'}
 if options.mode=='prepare':np.savez_compressed(maskfile,originalVertexIndices=ids,feather=feather[ids],protectedOriginalVertexIndices=protected)
 else:
  assert sha(maskfile)==receipt['maskSHA256'];saved=np.load(maskfile);assert np.array_equal(saved['originalVertexIndices'],ids) and np.array_equal(saved['feather'],feather[ids])
  assert len(receipt.get('images',[]))>=3
  for image in receipt['images']:
   path=(owned/image['path']).resolve();assert path.is_relative_to(owned) and sha(path)==image['sha256']
 (OUT/'explicit-artist-controls.json').write_text(json.dumps(controls,indent=2)+'\n');R['maskSHA256']=sha(maskfile);R['controls']=controls;originalData=body.data
 if options.mode=='prepare':
  body.data=originalData.copy();attribute=body.data.color_attributes.new(name='OwnedArtistMask',type='FLOAT_COLOR',domain='POINT');colors=np.tile([.1,.1,.1,1.],(N,1));colors[ids]=[1,.25,.02,1];attribute.data.foreach_set('color',colors.astype(np.float32).ravel())
  for slot in body.material_slots:
   if slot.material is None:continue
   material=slot.material.copy();slot.material=material;nt=material.node_tree
   for bs in [n for n in nt.nodes if n.type=='BSDF_PRINCIPLED']:
    inp=bs.inputs['Base Color'];old=inp.links[0].from_socket if inp.is_linked else None;value=tuple(inp.default_value);mix=nt.nodes.new('ShaderNodeMixRGB');mix.inputs[0].default_value=.5;mix.inputs[1].default_value=value;vc=nt.nodes.new('ShaderNodeVertexColor');vc.layer_name='OwnedArtistMask'
    if old:nt.links.new(old,mix.inputs[1])
    nt.links.new(vc.outputs['Color'],mix.inputs[2]);nt.links.new(mix.outputs[0],inp)
  for viewName in ['front','backthreequarter','side']:R['renders'].append(render('artist-mask-'+viewName,points,viewName,True,(np.array(view['cameraTarget']),view['orthoScale'])))
  body.data=originalData;R['candidateStatus']='NEW_ARTIST_MASK_ONLY_NO_CORRECTION'
 else:
  activeMask=np.zeros(N,bool);activeMask[ids]=True;faces=tri[np.any(activeMask[tri],axis=1)];edges=np.sort(np.concatenate([faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]]]),axis=1);unique,count=np.unique(edges,axis=0,return_counts=True);assert np.all(count<=2),'Nonmanifold patch: no valid closed swept-volume claim';patchBoundary=unique[count==1];assert not np.any(activeMask[patchBoundary]),'Open moving boundary: refuse volume claim'
  evaluate=lambda alpha:swept_volume(points,points+crease+alpha*comp,faces);alpha=balanced_coefficient(evaluate,configuration['volumeToleranceM3']);delta=crease+alpha*comp;assert np.linalg.norm(delta,axis=1).max()<=configuration['maximumTotalDeltaM'];assert np.array_equal(delta[protected],np.zeros((len(protected),3)));active=np.flatnonzero(np.any(delta!=0,axis=1));joint=ns['joint'];weight=ns['weight'];linear=np.zeros((len(active),3,3))
  for k in range(4):linear+=matrices[joint[active,k],:3,:3]*weight[active,k,None,None]
  local=inverse_lbs_delta(linear,delta[active],configuration['inverseConditionMaximum']);np.savez_compressed(OUT/'artist-target-deltas.npz',originalVertexIndices=active,deltaBlenderLocal=local,deltaPosedWorld=delta[active]);R['volume']={'sweptPatchDeltaM3':evaluate(alpha),'compensationAlpha':alpha,'claim':'Closed swept patch only; not a whole-body anatomical volume measurement'};R['maxDeltaM']=float(np.linalg.norm(delta,axis=1).max());R['additionalProtectedDeltaM']=0;target=points+delta
  measureMask=np.zeros(N,bool)
  for region in ['sleeve_armhole_R','torso_underarm_R']:measureMask[regions[region]]=True
  R['actualControlResidualM']=[]
  for center,displacement,hit in zip(centers,desired,hits):
   face=tri[hit[1]];bary=np.linalg.lstsq(np.vstack((points[face].T,np.ones(3))),np.r_[center,1.],rcond=None)[0];actualTarget=bary@target[face];residual=float(np.linalg.norm(actualTarget-(center+displacement)));R['actualControlResidualM'].append(residual);assert residual<=.0001,'Triangle-interpolated artist handle misses explicit target'
  sourceEdges=ns['canonical_edges'](tri[np.any(measureMask[tri],axis=1)]);R['metrics']={}
  for region in ['sleeve_armhole_R','torso_underarm_R']:
   mask=np.zeros(N,bool);mask[regions[region]]=True;before=ns['edge_metrics'](rest,points,sourceEdges,mask);after=ns['edge_metrics'](rest,target,sourceEdges,mask);R['metrics'][region]={'before':before,'after':after,'strictFence':ns['candidate_fences'](before,after,{'maxM':0},True),'scope':'Full original edges incident to changed patch; global archived strict RED remains retained separately.'}
  R['absoluteStretch']={}
  def stats(values):return dict(zip(['min','median','p95','p99','max'],map(float,np.quantile(values,[0,.5,.95,.99,1]))))
  for region in ['sleeve_armhole_R','torso_underarm_R']:
   mask=np.zeros(N,bool);mask[regions[region]]=True;e=sourceEdges[np.any(mask[sourceEdges],axis=1)];restLengths=np.linalg.norm(rest[e[:,1]]-rest[e[:,0]],axis=1);beforeLengths=np.linalg.norm(points[e[:,1]]-points[e[:,0]],axis=1);afterLengths=np.linalg.norm(target[e[:,1]]-target[e[:,0]],axis=1);R['absoluteStretch'][region]={}
   for threshold in [0,.1,1.]:
    use=restLengths*1000>threshold;R['absoluteStretch'][region][str(threshold)+'MM']={'originalEdgeCount':int(use.sum()),'beforePositiveExcessMM':stats(np.maximum(beforeLengths[use]-restLengths[use],0)*1000),'afterPositiveExcessMM':stats(np.maximum(afterLengths[use]-restLengths[use],0)*1000),'deltaPosedEdgeMM':stats((afterLengths[use]-beforeLengths[use])*1000),'ratioFenceNotWeakened':True}
   R['metrics'][region]['triangles']={label:ns['triangle_metrics'](rest,p,tri,mask) for label,p in [('before',points),('after',target)]}
  def features():
   jm=ns['joints_measure']()['R'];a=np.array(jm['elbow'])-jm['shoulder'];b=np.array(jm['wrist'])-jm['elbow'];flexion=np.degrees(np.arccos(np.clip(np.dot(a,b)/np.linalg.norm(a)/np.linalg.norm(b),-1,1)));return np.array([jm['humerusElevationDeg'],flexion])
  referenceFeatures=features();R['poseDriver']={'meaning':'Dance-only geometric humerus-elevation/elbow-flexion compact C2 driver; no bone motion change','referenceDegrees':referenceFeatures.tolist(),'bandwidthDegrees':[15,20],'actualSamples':[]}
  for clip,phase in [('dance_01',.45),('dance_01',.475),('dance_01',.5),('dance_01',.525),('dance_01',.55),('box_02',.5),('front_kick_01',.5),('neutral',0)]:
   ns['setup_pose']({'name':'neutral' if clip=='neutral' else 'driver-sample','clip':clip,'phase':phase},True);f=features();value=garment_activation(clip,f,referenceFeatures,phase,556/24);_,poseSkin=ns['world_surface']();newLinear=np.zeros((len(active),3,3))
   for k in range(4):newLinear+=poseSkin[joint[active,k],:3,:3]*weight[active,k,None,None]
   displacement=np.einsum('nij,nj->ni',newLinear,local)*value;R['poseDriver']['actualSamples'].append({'clip':clip,'phase':phase,'featuresDegrees':f.tolist(),'weight':value,'maxAddedWorldM':float(np.linalg.norm(displacement,axis=1).max()),'additionalProtectedDeltaM':0})
   if clip!='dance_01':assert value==0 and np.array_equal(displacement,np.zeros_like(displacement))
  ns['setup_pose']({'name':'dance50','clip':'dance_01','phase':.5},True);restored,_=ns['world_surface']();assert np.linalg.norm(restored-points,axis=1).max()<1e-6,'Driver validation failed to restore exact baseline'
  body.data=originalData.copy();assert body.data.shape_keys is None;body.shape_key_add(name='Basis');key=body.shape_key_add(name='OwnedArtistUnderarm');co=raw.copy();co[active]+=local;key.data.foreach_set('co',co.astype(np.float32).ravel());scene.cycles.samples=20;scene.render.film_transparent=True
  for materialName,override in [('source',None),('clay',ns['clay'])]:
   scene.view_layers[0].material_override=override
   for label,value,surface in [('before',0,points),('after',1,target)]:
    key.value=value
    for viewName in ['front','backthreequarter','side']:
     item=render(materialName+'-'+label+'-'+viewName,surface,viewName,True,(np.array(view['cameraTarget']),view['orthoScale']));item['material']='original-source-normal1' if override is None else 'diagnostic-flat-clay';R['renders'].append(item)
  body.data=originalData;R['candidateStatus']='ARTIST_CANDIDATE_RENDERED_NOT_ACCEPTED'
 assert ns['hash_arrays'](body)==ns['original']
 for path,digest in R['sourceHashes'].items():assert sha(path)==digest
 R.update(status=R['candidateStatus'],sourceAttributesExact=True,seconds=time.monotonic()-ns['started'],artistScriptSHA256=sha(file));ns['write']();print('RESULT '+R['status'],flush=True)

def raster_contours(folder):
 from pathlib import Path
 import json
 from PIL import Image
 folder=Path(folder);result={'scope':'Actual local-window alpha contours, not full-character frame acceptance. RGB differences are diagnostic, not an artistic acceptance score.','views':{}}
 for view in ['front','backthreequarter','side']:
  images=[np.asarray(Image.open(folder/('source-'+variant+'-'+view+'.png')).convert('RGBA')) for variant in ['before','after']];masks=[im[:,:,3]>128 for im in images];rows=[]
  for mask in masks:
   values=[]
   for row in mask:
    x=np.flatnonzero(row);values.append([int(x[0]),int(x[-1])] if len(x) else None)
   rows.append(values)
  delta=[abs(b[j]-a[j]) for a,b in zip(*rows) if a is not None and b is not None for j in [0,1]];common=masks[0]&masks[1];rgb=np.abs(images[0][:,:,:3].astype(float)-images[1][:,:,:3]).mean(2)
  result['views'][view]={'rowContoursBeforeAfter':rows,'maxAlphaContourDisplacementPixels':max(delta,default=None),'foregroundPixelCounts':[int(m.sum()) for m in masks],'touchesImageBorder':[bool(m[0].any() or m[-1].any() or m[:,0].any() or m[:,-1].any()) for m in masks],'meanRGBDifferenceOnCommonForeground':float(rgb[common].mean()) if common.any() else None}
 (folder/'raster-contours.json').write_text(json.dumps(result,indent=2)+'\n')

if __name__=='__main__':
 import sys
 if len(sys.argv)>1 and sys.argv[1]=='--raster':raster_contours(sys.argv[2])
 else:main()

