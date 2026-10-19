"""Owned read-only render witness for original vertex664129 and adjacent face.
Run after the source OFF/ON process exits, one Blender CPU6 process only.
"""
from pathlib import Path
import sys,json
FILE=Path(__file__).resolve();BASE=FILE.with_name('probe_source_off_on.py');OUTPATH=FILE.parents[1]/'boundary-marker-v1'
sys.argv=[sys.argv[0],'--','--stage','renders','--source-material','--output',str(OUTPATH),'--max-seconds','180']
s=BASE.read_text();s=s[:s.index("if args.stage=='masks':")];exec(compile(s,str(BASE),'exec'))
vertex=664129;faceIDs=np.flatnonzero(np.any(tri==vertex,axis=1));assert len(faceIDs)>0;face=int(faceIDs[0]);ids=tri[face]
marker=bpy.data.materials.new('OwnedBoundaryMarkerEmission');marker.use_nodes=True;nt=marker.node_tree;nt.nodes.clear();em=nt.nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(1,.015,.015,1);em.inputs['Strength'].default_value=1;out=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(em.outputs[0],out.inputs['Surface'])
report['marker']={'originalVertexIndex':vertex,'adjacentOriginalTriangleIndex':face,'adjacentOriginalTriangleVertexIndices':ids.tolist(),'allAdjacentOriginalTriangleIndices':faceIDs.tolist(),'positionBlenderLocal':raw[vertex].tolist(),'originalNamedWeights':[{'bone':names[int(j)],'weight':float(w)} for j,w in zip(joint[vertex],weight[vertex]) if w>0],'interpretation':'Geometric/anatomical counterexample only; bone names do not establish a leakage diagnosis. Red 3D sphere radius2mm marks exact world position; red edges mark its first original adjacent triangle.'}
owned=[];report['renders']=[];case={'name':'dance50','clip':'dance_01','phase':.5}
for on in [False,True]:
 for ob in owned:bpy.data.objects.remove(ob,do_unlink=True)
 owned=[];setup_pose(case,on);points,skin=world_surface();label='ON' if on else 'OFF'
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.002,location=points[vertex]);ob=bpy.context.object;ob.name='OwnedOriginalVertex664129';ob.data.materials.append(marker);owned.append(ob)
 curve=bpy.data.curves.new('OwnedAdjacentTriangle','CURVE');curve.dimensions='3D';curve.bevel_depth=.00045;curve.bevel_resolution=0;sp=curve.splines.new('POLY');sp.points.add(3)
 for k,i in enumerate([*ids,ids[0]]):sp.points[k].co=(*points[i],1)
 ob=bpy.data.objects.new('OwnedAdjacentTriangle',curve);scene.collection.objects.link(ob);curve.materials.append(marker);owned.append(ob)
 for view in ['front','backthreequarter','side']:
  entry=render('vertex664129-'+label+'-'+view,points,view,True,(np.array(rig.matrix_world@rig.pose.bones['ORG-spine.004'].head)+[0,0,.015],.34));entry['markerWorldM']=points[vertex].tolist();entry['variant']=label;report['renders'].append(entry)
assert hash_arrays(body)==original
for f,digest in report['sourceHashes'].items():assert sha(f)==digest
report.update(status='COUNTEREXAMPLE_RENDERED_NOT_LEAK_DIAGNOSIS',sourceAttributesExact=True,seconds=time.monotonic()-started,markerScriptSHA256=sha(FILE));write();print('RESULT '+json.dumps({'status':report['status'],'seconds':report['seconds']}),flush=True)
