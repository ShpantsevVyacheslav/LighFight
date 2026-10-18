import bpy,sys,json,math,time
import numpy as np
from pathlib import Path
R=Path(__file__).resolve().parents[4];role=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'sailor';version=sys.argv[sys.argv.index('--')+2] if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+2 else 'v1';O=R/f'.tools/portrait-native-glb/shoulder-fix/{version}';O=O if role=='sailor' else O/role;manifest=json.loads((R/f'prototypes/roster-portraits/models/{role}.manifest.json').read_text());bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(R/manifest['sourceFBX']),automatic_bone_orientation=False);arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');actions=list(bpy.data.actions);scene=bpy.context.scene;arm.animation_data.use_nla=False
choices=[40,75] if version=='v4' else [65,100] if version=='v3' else [100,400];packs={steps:np.load(O/f'candidate-{steps}-weights.npz') for steps in choices};original=packs[choices[0]]['sourceWeights'];mask=packs[choices[0]]['mask'];weights={0:original,**{k:v['weights'].copy() for k,v in packs.items()}}
for k,w in weights.items():
 if k:w[mask==0]=original[mask==0]
coords=np.array([tuple(v.co) for v in mesh.data.vertices],dtype=np.float64);T=np.array(arm.matrix_world.inverted()@mesh.matrix_world);coords=np.c_[coords,np.ones(len(coords))]@T.T;edges=np.array([tuple(e.vertices) for e in mesh.data.edges],dtype=np.int32);selection=(mask[edges[:,0]]>0)|(mask[edges[:,1]]>0);edges=edges[selection];local=np.unique(edges);lookup=np.full(len(coords),-1,dtype=np.int32);lookup[local]=np.arange(len(local));edges=lookup[edges];coords=coords[local];restWorld=(coords@np.array(arm.matrix_world).T)[:,:3];restLen=np.linalg.norm(restWorld[edges[:,0]]-restWorld[edges[:,1]],axis=1);keep=restLen>.0001;edges=edges[keep];restLen=restLen[keep];edgeWeight=restLen/restLen.sum();names=packs[choices[0]]['names'].tolist();bind=[arm.data.bones[n].matrix_local.inverted() for n in names];sparse={}
for k,w in weights.items():
 idx=np.argpartition(w[local],-4,axis=1)[:,-4:];ws=np.take_along_axis(w[local],idx,axis=1);sparse[k]=(idx,ws)

def deformed(matrices,key):
 idx,ws=sparse[key];p=np.zeros((len(coords),3),dtype=np.float64)
 for column in range(4):p+=np.einsum('nij,nj->ni',matrices[idx[:,column],:3,:],coords)*ws[:,column,None]
 return p

def metric(p):
 length=np.linalg.norm(p[edges[:,0]]-p[edges[:,1]],axis=1);ratio=length/restLen;log=np.log(np.maximum(ratio,1e-9));return float(np.sum(log*log*edgeWeight)),float(np.mean((ratio<.5)|(ratio>1.6))),float(np.quantile(ratio,.01)),float(np.quantile(ratio,.99))
keys=[0]+choices;rows=['clip,frame,'+','.join(str(k)+name for name in ['Energy','OutsideBand','P01','P99'] for k in keys)];summaries=[];start=time.time();cpuCheck=None
for action in actions:
 arm.animation_data.action=action;records={k:[] for k in keys}
 for frame in range(math.ceil(action.frame_range[0]),math.floor(action.frame_range[1])+1):
  scene.frame_set(frame);bpy.context.view_layer.update();matrices=np.array([np.array(arm.matrix_world@arm.pose.bones[n].matrix@b) for n,b in zip(names,bind)])
  positions={k:deformed(matrices,k) for k in weights}
  if cpuCheck is None:
   deps=bpy.context.evaluated_depsgraph_get();obj=mesh.evaluated_get(deps);me=obj.to_mesh();actual=np.array([tuple(obj.matrix_world@me.vertices[int(i)].co) for i in local]);obj.to_mesh_clear();cpuCheck=float(np.max(np.linalg.norm(actual-positions[0],axis=1)));assert cpuCheck<1e-5,cpuCheck
  current={k:metric(p) for k,p in positions.items()}
  for k,v in current.items():records[k].append(v)
  rows.append(','.join(map(str,[action.name,frame]+[current[k][j] for j in range(4) for k in keys])))
 record={'clip':action.name,'frames':len(records[0]),'candidates':{}}
 for k,items in records.items():
  a=np.array(items);record['candidates'][str(k)]={'meanLogStrainEnergy':float(a[:,0].mean()),'p95LogStrainEnergy':float(np.quantile(a[:,0],.95)),'meanOutsideBandFraction':float(a[:,1].mean()),'worstP01':float(a[:,2].min()),'worstP99':float(a[:,3].max()),'worstFrameLogStrainEnergy':float(a[:,0].max()),'worstFrameOutsideBandFraction':float(a[:,1].max())}
 summaries.append(record);print('CLIP '+json.dumps(record),flush=True)
(O/'all-eight-frames.csv').write_text('\n'.join(rows));anchorGroups={'neckHead':[i for i,n in enumerate(names) if 'neck' in n.lower() or 'head' in n.lower()],'distalArms':[i for i,n in enumerate(names) if 'lowerarm' in n.lower().replace('_','') or 'hand' in n.lower()]};anchorProof={}
for label,groups in anchorGroups.items():
 anchor=original[:,groups].sum(axis=1)>.5;anchorProof[label]={'vertices':int(anchor.sum()),'candidateMaxWeightL1Change':{str(k):float(np.max(np.abs(w[anchor]-original[anchor]).sum(axis=1))) for k,w in weights.items()},'poseDisplacementProof':'Identical weights+same source matrices imply exactly identical LBS positions for everyframe; no approximate pose cache'}
result={'role':role,'anchorProof':anchorProof,'scope':'every integer sourceframe across all8nativeclips; staticgeometry/weights only, sourcebones/curves unchanged; descriptive strain not aestheticacceptance','frames':len(rows)-1,'cpuLbsVsBlenderMaxM':cpuCheck,'relevantVertices':len(local),'edges':len(edges),'seconds':time.time()-start,'clips':summaries};(O/'all-eight-gate.json').write_text(json.dumps(result,indent=2));print('RESULT '+json.dumps(result))
