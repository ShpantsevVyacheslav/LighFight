"""Prepared only: correct -X right-side source witness, no target changes.
Run as one headless CPU6 Blender after explicit heavy window handoff.
"""
from pathlib import Path
import sys,json
FILE=Path(__file__).resolve();CO=FILE.parents[1];BASE=CO.parent/'tools/probe_source_off_on.py';OUTPATH=CO/'weight-witness-v1/renders'
sys.argv=[sys.argv[0],'--','--stage','renders','--source-material','--output',str(OUTPATH),'--max-seconds','180']
s=BASE.read_text();s=s[:s.index("if args.stage=='masks':")];needle="'backthreequarter':(.85,2,.12),'side':(2,0,.06)";assert needle in s;s=s.replace(needle,"'backthreequarter':(-.85,2,.12),'side':(-2,0,.06)");__file__=str(BASE)
try:exec(compile(s,str(BASE),'exec'))
finally:__file__=str(FILE)
audit=json.loads((CO/'weight-witness-v1/audit.json').read_text());edge=audit['worstEdges'][0];ids=np.array(edge['originalVertexIDs']);case={'name':'dance50','clip':'dance_01','phase':.5};setup_pose(case,True);points,skin=world_surface();assert np.linalg.norm(points[ids]-np.array(edge['posedWorldM']),axis=1).max()<1e-6,'Original frozen pose mismatch'
report['weightWitness']=edge;report['sourceAuditSHA256']=sha(CO/'weight-witness-v1/audit.json');report['renders']=[]
def material(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;nt=m.node_tree;nt.nodes.clear();e=nt.nodes.new('ShaderNodeEmission');e.inputs['Color'].default_value=(*color,1);o=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(e.outputs[0],o.inputs['Surface']);return m
red=material('OwnedEndpointA',(1,.01,.01));green=material('OwnedEndpointB',(.01,1,.01));yellow=material('OwnedRatioEdge',(1,.8,.01));cyan=material('OwnedAbsoluteEdge',(.01,.8,1));magenta=material('OwnedAbsoluteEnd',(1,.01,1))
extra=audit['largestAbsoluteEdgeExcess'][0];report['absoluteExcessWitness']=extra;allIDs=[]
for label,witness,mats,lineMat in [('ratio',edge,[red,green],yellow),('absoluteExcess',extra,[cyan,magenta],cyan)]:
 pair=np.array(witness['originalVertexIDs']);allIDs.extend(pair.tolist());assert np.linalg.norm(points[pair]-np.array(witness['posedWorldM']),axis=1).max()<1e-6
 for i,mat in zip(pair,mats):
  bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.00055,location=points[i]);bpy.context.object.data.materials.append(mat)
 curve=bpy.data.curves.new('OwnedOriginalEdge-'+label,'CURVE');curve.dimensions='3D';curve.bevel_depth=.0002;sp=curve.splines.new('POLY');sp.points.add(1)
 for k,i in enumerate(pair):sp.points[k].co=(*points[i],1)
 ob=bpy.data.objects.new('OwnedOriginalEdge-'+label,curve);scene.collection.objects.link(ob);curve.materials.append(lineMat)
center=points[allIDs].mean(0)
for view in ['front','backthreequarter','side']:
 item=render('worst-edge-'+view,points,view,True,(center,.32));item['hemisphere']='correct R side -X' if view=='side' else ('right-back -X' if view=='backthreequarter' else 'front');item['marker']='red145536/green146384/yellow ratio edge; cyan290465/magenta300366 absolute-excess edge; natural occlusion'
 if view=='side':assert cam.location.x<center[0],'Wrong side camera regression'
 report['renders'].append(item)
report['renders'].append(render('worst-edge-fullbody-front',points,'front'))
assert hash_arrays(body)==original
for file,digest in report['sourceHashes'].items():assert sha(file)==digest
report.update(status='SOURCE_EDGE_WITNESS_NOT_CORRECTION',sourceAttributesExact=True,seconds=time.monotonic()-started,witnessScriptSHA256=sha(FILE));write();print('RESULT '+report['status'],flush=True)
