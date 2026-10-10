"""Clean photo-generated fist, straighten axial wrist centerline, reduce surface and fit both source wrist frames."""
import bpy,bmesh,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
fbx,source,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(fbx));o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
for v in o.data.vertices:v.co=o.matrix_world@v.co
o.matrix_world=Matrix.Identity(4)
original=len(o.data.polygons);bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,plane_co=(.12,0,0),plane_no=(1,0,0),clear_outer=True);boundary=[e for e in bm.edges if e.is_boundary];bmesh.ops.holes_fill(bm,edges=boundary,sides=0);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
# Cross-section center interpolation removes source bent forearm while preserving hand surface sections.
knots=[(-.6,.018,.287),(-.4,.003,.237),(-.2,-.012,.204),(0,-.034,.139),(.15,-.065,.062)]
def center(x):
 if x<=knots[0][0]:return knots[0][1:]
 for a,b in zip(knots,knots[1:]):
  if x<=b[0]:
   t=(x-a[0])/(b[0]-a[0]);return (a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t)
 return knots[-1][1:]
for v in o.data.vertices:
 cy,cz=center(v.co.x);v.co.y-=cy;v.co.z-=cz
reference_coords=[v.co.copy() for v in o.data.vertices];o.data.calc_loop_triangles();tree=BVHTree.FromPolygons(reference_coords,[tuple(t.vertices) for t in o.data.loop_triangles],all_triangles=True)
bpy.context.view_layer.objects.active=o;o.select_set(True);dec=o.modifiers.new('MobileFistSurface','DECIMATE');dec.ratio=min(1,16000/len(o.data.polygons));bpy.ops.object.modifier_apply(modifier=dec.name)
o.data.calc_loop_triangles();reduced_tree=BVHTree.FromPolygons([v.co.copy() for v in o.data.vertices],[tuple(t.vertices) for t in o.data.loop_triangles],all_triangles=True)
reverse_errors=[reduced_tree.find_nearest(p)[3] for p in reference_coords]
errors=[tree.find_nearest(v.co)[3] for v in o.data.vertices];bm=bmesh.new();bm.from_mesh(o.data);bad=sum(not e.is_manifold for e in bm.edges);bm.free()
if bad:raise RuntimeError('Reduced fist is not manifold')
if max(errors+reverse_errors)>.015:raise RuntimeError('Reduction changed silhouette too much')
verts=[tuple(v.co) for v in o.data.vertices];faces=[tuple(p.vertices) for p in o.data.polygons]
report={'sourceTriangles':original,'preparedTriangles':len(faces),'preparedVertices':len(verts),'nonManifoldEdges':bad,'maxVertexSurfaceErrorCm':max(errors)*12,'meanVertexSurfaceErrorCm':sum(errors)/len(errors)*12,'maxOriginalVertexToReducedSurfaceErrorCm':max(reverse_errors)*12,'sourceCapRemovedAtX':.12,'handScaleCmPerSourceUnit':12,'rig':'closed geometry rigidly weighted to existing wrist; no finger folding','creditsSpent':0}
bpy.ops.wm.save_as_mainfile(filepath=str(out/'prepared-fist.blend'));bpy.ops.export_scene.fbx(filepath=str(out/'prepared-fist.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False)
bpy.ops.wm.open_mainfile(filepath=str(source));arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=bpy.data.objects['char1'];action=arm.animation_data.action;arm.animation_data.action=None
for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
for side,sign in [('Left',1),('Right',-1)]:
 w=arm.data.bones[side+'Hand'].head_local;mesh=bpy.data.meshes.new('Meshy'+side+'Fist');mesh.from_pydata([w+Vector((-sign*(x+.1)*12,y*12,z*12)) for x,y,z in verts],[],faces if sign<0 else [tuple(reversed(f)) for f in faces]);mesh.update();hand=bpy.data.objects.new('Rebuilt'+side+'Hand',mesh);bpy.context.collection.objects.link(hand);hand.matrix_world=body.matrix_world.copy();hand.data.materials.append(body.data.materials[0].copy())
 for p in mesh.polygons:p.use_smooth=True
 uv=mesh.uv_layers.new(name='MeshyFistUV')
 for polygon in mesh.polygons:
  for i in polygon.loop_indices:
   p=mesh.vertices[mesh.loops[i].vertex_index].co;uv.data[i].uv=(sign*(p.x-w.x)/12,(p.y-w.y+5)/10)
 group=hand.vertex_groups.new(name=side+'Hand');group.add(list(range(len(verts))),1,'REPLACE');hand.shape_key_add(name='Basis');hand.shape_key_add(name='ClosedFists');mod=hand.modifiers.new('RigidWristBinding','ARMATURE');mod.object=arm
arm.animation_data.action=action;bpy.ops.wm.save_as_mainfile(filepath=str(out/'static-fist-study.blend'));(out/'preparation-audit.json').write_text(json.dumps(report,indent=2));print(report)
