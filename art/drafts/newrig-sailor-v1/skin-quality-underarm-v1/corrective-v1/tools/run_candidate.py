"""Single dance50 right inner-sleeve corrective; owned copies only.
prepare-mask first, then build only with a garment-only visual review receipt.
"""
import argparse,json,sys
from pathlib import Path
FILE=Path(__file__).resolve();CO=FILE.parents[1];BASE=CO.parent/'tools/probe_source_off_on.py'
p=argparse.ArgumentParser();p.add_argument('--mode',choices=['prepare-mask','build'],default='prepare-mask');p.add_argument('--receipt',type=Path,default=CO/'garment-mask-review.json');opt=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
if opt.mode=='build':
 receipt=json.loads(opt.receipt.read_text());assert receipt.get('reviewed') is True and receipt.get('garmentOnly') is True,'Require actual anatomical garment mask review'
sys.argv=[sys.argv[0],'--','--stage','renders','--source-material','--output',str(CO/('mask-proposal' if opt.mode=='prepare-mask' else 'candidate-v1')),'--max-seconds','240']
s=BASE.read_text();s=s[:s.index("if args.stage=='masks':")];__file__=str(BASE)
try:exec(compile(s,str(BASE),'exec'))
finally:__file__=str(FILE)
sys.path.insert(0,str(FILE.parent));from corrective_core import compact_window,radial_contour_delta,inverse_lbs_delta
case={'name':'dance50','clip':'dance_01','phase':.5};setup_pose(case,True);surface,skin=world_surface();boneW=ref['armatureWorldBlender']@ref['DEFRestArmature'];index={n:i for i,n in enumerate(names)};S=boneW[index['DEF-upper_arm.R'],:3,3];E=boneW[index['DEF-forearm.R'],:3,3];neck=boneW[index['DEF-spine.004'],:3,3];chest=boneW[index['DEF-spine.003'],:3,3];up=(neck-chest);up/=np.linalg.norm(up);axis=(E-S);length=np.linalg.norm(axis);axis/=length;down=-up+axis*np.dot(up,axis);down/=np.linalg.norm(down);q=restWorld-S;t=q@axis/length;radial=q-np.outer(q@axis,axis);radius=np.linalg.norm(radial,axis=1);direction=radial/np.maximum(radius[:,None],1e-12);angle=direction@down;H=np.ptp(restWorld[:,2]);allowed=np.zeros(N,bool);allowed[regions['sleeve_armhole_R']]=True
# Spatial restrictions intentionally exclude actual neck/skin and distal anchors.
allowed&=(restWorld[:,2]<neck[2]-.018*H)&(t>-.08)&(t<.55)&(radius<.085*H)&(angle>.25)
lateral=boneW[index['DEF-upper_arm.L'],:3,3]-S;lateral/=np.linalg.norm(lateral);outward=-(restWorld-neck)@lateral;shoulderOut=float(-(S-neck)@lateral);medial=np.clip((outward-.65*shoulderOut)/(.25*shoulderOut),0,1);medial=medial*medial*(3-2*medial);angular=np.clip((angle-.25)/.60,0,1);angular=angular*angular*(3-2*angular);falloff=compact_window(t,-.08,.45)*angular*medial*allowed*.65;ids=np.flatnonzero(falloff>0);assert 50<len(ids)<40000,'Unexpected local mask extent'
protected=np.unique(np.concatenate([regions[n] for n in regions.files if 'anchor' in n]+[np.flatnonzero(restWorld[:,2]>=neck[2]-.018*H)]));falloff[protected]=0;ids=np.flatnonzero(falloff>0)
maskfile=CO/'garment-mask-proposal.npz'
if opt.mode=='prepare-mask':np.savez_compressed(maskfile,originalVertexIndices=ids,falloff=falloff[ids],protectedOriginalVertexIndices=protected)
else:
 assert sha(maskfile)==receipt['maskSHA256'],'Reviewed mask changed'
 for item in receipt.get('images',[]):
  image=(CO/item['path']).resolve();assert image.is_relative_to(CO) and sha(image)==item['sha256']
 assert len(receipt.get('images',[]))>=3,'Require front/back/side actual mask views'
 saved=np.load(maskfile);assert np.array_equal(saved['originalVertexIndices'],ids) and np.array_equal(saved['falloff'],falloff[ids]),'Recomputed mask differs'
report['candidate']={'mode':opt.mode,'sourceCase':case,'baseline':'Fixed C1 ON','side':'R','originalAffectedCandidates':len(ids),'maskSHA256':sha(maskfile),'maxWorldDisplacementM':.003,'scope':'One local under-sleeve contour; no Laplacian, weights or original-rest edits','garmentStatus':'PROVISIONAL_REQUIRES_VISUAL_REVIEW' if opt.mode=='prepare-mask' else 'EXPLICIT_RECEIPT','boundary':'No whole broad-head zero assumption: this target excludes all neck/head by independent spatial limit; original C1 neck motion retained.'}
report['renders']=[];originalData=body.data
if opt.mode=='prepare-mask':
 body.data=originalData.copy();attr=body.data.color_attributes.new(name='OwnedCandidateMask',type='FLOAT_COLOR',domain='POINT');colors=np.tile([.1,.1,.1,1.],(N,1));colors[ids]=np.column_stack((np.ones(len(ids)),.1+falloff[ids],np.zeros(len(ids)),np.ones(len(ids))));attr.data.foreach_set('color',colors.astype(np.float32).ravel())
 for slot in body.material_slots:
  if slot.material is None:continue
  mat=slot.material.copy();slot.material=mat;nt=mat.node_tree
  for bs in [n for n in nt.nodes if n.type=='BSDF_PRINCIPLED']:
   inp=bs.inputs['Base Color'];link=inp.links[0].from_socket if inp.is_linked else None;old=tuple(inp.default_value);mix=nt.nodes.new('ShaderNodeMixRGB');mix.inputs[0].default_value=.55;mix.inputs[1].default_value=old;vc=nt.nodes.new('ShaderNodeVertexColor');vc.layer_name='OwnedCandidateMask'
   if link:nt.links.new(link,mix.inputs[1])
   nt.links.new(vc.outputs['Color'],mix.inputs[2]);nt.links.new(mix.outputs[0],inp)
 for view in ['front','backthreequarter','side']:
  item=render('garment-mask-'+view,surface,view,True);item['material']='diagnostic-source-albedo-candidate-mask';report['renders'].append(item)
 body.data=originalData;report['status']='MASK_ONLY_NO_CORRECTION';report['candidate']['restWorldBoundsM']=[restWorld[ids].min(0).tolist(),restWorld[ids].max(0).tolist()]
else:
 # Rest-informed hand-defined compact radius curve, not smoothing vertices.
 knots=np.array([-.08,.04,.20,.34,.44]);radii=[]
 for knot in knots:
  ring=(falloff>0)&(np.abs(t-knot)<.09)&(angle>.25);assert ring.sum()>30,'Insufficient source support for contour knot';radii.append(float(np.median(radius[ring])))
 ps=np.array(joints_measure()['R']['shoulder']);pe=np.array(joints_measure()['R']['elbow']);pa=pe-ps;plen=np.linalg.norm(pa);pa/=plen;pu=np.array(rig.matrix_world.to_3x3()@rig.pose.bones['ORG-spine.003'].matrix.col[1].xyz);pu/=np.linalg.norm(pu);pd=-pu+pa*np.dot(pu,pa);assert np.linalg.norm(pd)>.05,'Unsupported near-vertical arm';pd/=np.linalg.norm(pd);pq=surface-ps;pt=pq@pa/plen;pr=pq-np.outer(pq@pa,pa);prn=np.linalg.norm(pr,axis=1);pdot=pr@pd/np.maximum(prn,1e-12);weightsMask=falloff*np.clip((pdot-.1)/.6,0,1)
 interval=np.clip(np.searchsorted(knots,pt)-1,0,len(knots)-2);u=np.clip((pt-knots[interval])/(knots[interval+1]-knots[interval]),0,1);u=u*u*(3-2*u);radii=np.asarray(radii);target=radii[interval]*(1-u)+radii[interval+1]*u
 deltaWorld=radial_contour_delta(surface,ps,pa,pd,target,weightsMask,.003);active=np.flatnonzero(np.any(deltaWorld!=0,axis=1));assert len(active)>10,'No meaningful contour deficit; do not invent target'
 assert np.array_equal(deltaWorld[protected],np.zeros((len(protected),3)));linear=np.zeros((len(active),3,3))
 for k in range(4):linear+=skin[joint[active,k],:3,:3]*weight[active,k,None,None]
 deltaLocal=inverse_lbs_delta(linear,deltaWorld[active],condition_limit=50);targetWorld=surface+deltaWorld;np.savez_compressed(OUT/'target-deltas.npz',originalVertexIndices=active,deltaBlenderLocal=deltaLocal,deltaPosedWorld=deltaWorld[active],sourceIndexedSHA256=np.array(regionInfo['sourceIndexedSHA256']))
 report['candidate'].update(actualTargetVertices=len(active),actualMaxWorldM=float(np.linalg.norm(deltaWorld,axis=1).max()),conditionMax=float(np.linalg.cond(linear).max()),profileKnotsT=knots.tolist(),profileRadiiSourceM=radii.tolist(),targetSHA256=sha(OUT/'target-deltas.npz'))
 report['metrics']={};edges=canonical_edges(tri)
 for name in ['sleeve_armhole_R','torso_underarm_R','medial_trapezius_R','collar_proposal','head_anchor_proposal','elbow_distal_anchor_R']:
  mask=regionMasks[name];report['metrics'][name]={label:{'edge':edge_metrics(restWorld,pts,edges,mask),'triangle':triangle_metrics(restWorld,pts,tri,mask)} for label,pts in [('before',surface),('after',targetWorld)]}
 report['numericFences']={}
 for name,comparison in report['metrics'].items():
  before=dict(comparison['before']['edge'],collapsedCount=comparison['before']['triangle']['collapsedCount']);after=dict(comparison['after']['edge'],collapsedCount=comparison['after']['triangle']['collapsedCount']);report['numericFences'][name]=candidate_fences(before,after,{'maxM':float(np.linalg.norm(deltaWorld[protected],axis=1).max(initial=0))},True)
 report['projectedContours']={};origin=np.array(rig.matrix_world@rig.pose.bones['ORG-spine.004'].head);contourIDs=regions['sleeve_armhole_R']
 for view,offset in [('front',(0,-2,.06)),('backthreequarter',(.85,2,.12)),('side',(2,0,.06))]:
  forward=-np.array(offset,float);forward/=np.linalg.norm(forward);right=np.cross(forward,[0,0,1.]);right/=np.linalg.norm(right);vup=np.cross(right,forward);height=(surface[contourIDs]-origin)@vup;bounds=[float(height.min()),float(height.max())];report['projectedContours'][view]={label:projected_envelope(points,contourIDs,origin,right,vup,bounds) for label,points in [('before',surface),('after',targetWorld)]}
 body.data=originalData.copy();body.shape_key_add(name='Basis');key=body.shape_key_add(name='OwnedDance50UnderSleeveTarget');co=raw.copy();co[active]+=deltaLocal;key.data.foreach_set('co',co.astype(np.float32).ravel())
 for label,value,points in [('before',0,surface),('after',1,targetWorld)]:
  key.value=value
  for view in ['front','backthreequarter','side']:report['renders'].append(render(label+'-'+view,points,view,True))
 body.data=originalData;report['status']='CANDIDATE_RENDERED_NOT_ACCEPTED'
assert hash_arrays(body)==original
for f,digest in report['sourceHashes'].items():assert sha(f)==digest
report.update(sourceAttributesExact=True,seconds=time.monotonic()-started,candidateScriptSHA256=sha(FILE));write();print('RESULT '+json.dumps({'status':report['status'],'seconds':report['seconds']}),flush=True)
