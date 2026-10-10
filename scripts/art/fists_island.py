"""Closed combat-hand mesh variant; source/open hands remain untouched.
Load the original 21-bone rig, then run this script with output directory.
"""
import bpy, bmesh, sys, json, math
from pathlib import Path
from mathutils import Vector
out=Path(sys.argv[sys.argv.index('--')+1]);out.mkdir(parents=True,exist_ok=True)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
# Remove the open-hand section below the wrist, preserving watch/wrist geometry.
bm=bmesh.new();bm.from_mesh(body.data)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000005)
remove=[v for v in bm.verts if abs(v.co.x)>.30 and v.co.z<.80 and v.co.z>.64]
bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(body.data);bm.free()
color=(.68,.51,.43,1)
skin=bpy.data.materials.new('FistSkin');skin.diffuse_color=color;skin.use_nodes=True
skin.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=color
skin.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65
fists=[]
for side,sign in [('Left',1),('Right',-1)]:
    parts=[]
    def shape(center,scale):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=(sign*center[0],center[1],center[2]))
        o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);parts.append(o)
    # Palm volume, wrist bridge, four knuckles and folded finger pad.
    shape((.382,-.087,.760),(.043,.032,.046))
    shape((.377,-.085,.797),(.030,.029,.027))
    for x,z in [(.354,.737),(.372,.727),(.392,.729),(.409,.739)]:
        shape((x,-.109,z),(.013,.017,.014))
        shape((x,-.064,z+.021),(.013,.015,.020))
    shape((.343,-.087,.768),(.018,.030,.019))
    shape((.352,-.113,.754),(.025,.013,.014))
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:p.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();fist=parts[0];fist.name='CombatFist'+side
    # Fuse component volumes so the result has a continuous surface.
    remesh=fist.modifiers.new('ClosedHandSurface','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.003
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=fist.modifiers.new('Smooth','SMOOTH');smooth.factor=.5;smooth.iterations=3;bpy.ops.object.modifier_apply(modifier=smooth.name)
    for face in fist.data.polygons:face.use_smooth=True
    fist.data.materials.append(skin)
    # Bake object transform before applying existing wrist bone bind pose.
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    group=fist.vertex_groups.new(name=side+'Hand');group.add(list(range(len(fist.data.vertices))),1,'REPLACE')
    mod=fist.modifiers.new('Skeleton','ARMATURE');mod.object=arm
    fist.parent=arm
    fists.append(fist)
    # Open the wrist cap so the skin can be stitched to the forearm.
    bm=bmesh.new();bm.from_mesh(fist.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,
                          plane_co=(0,0,.790),plane_no=(0,0,1),clear_outer=True)
    bm.to_mesh(fist.data);bm.free()
# Join, then stitch both open wrist rings with shared vertices.
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
for fist in fists:fist.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(body.data)
seams=[]
for side,sign in [('Left',1),('Right',-1)]:
    edges=[e for e in bm.edges if e.is_boundary and all(sign*v.co.x>.30 and .775<v.co.z<.825 for v in e.verts)]
    result=bmesh.ops.bridge_loops(bm,edges=edges)
    if not result.get('faces'):raise RuntimeError('No wrist bridge '+side)
    for face in result['faces']:face.material_index=len(body.data.materials)-1
    seams.append({'side':side,'bridgeFaces':len(result['faces'])})
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
remaining=sum(e.is_boundary and all(abs(v.co.x)>.30 and .775<v.co.z<.825 for v in e.verts) for e in bm.edges)
if remaining:raise RuntimeError('Unstitched wrist boundary edges: '+str(remaining))
bm.to_mesh(body.data);bm.free()
# Identical weight field on both sides of each seam; UVs outside the bridge remain intact.
for vertex in body.data.vertices:
    if abs(vertex.co.x)>.30 and vertex.co.z<.88:
        for group in body.vertex_groups:group.remove([vertex.index])
        side='Left' if vertex.co.x>0 else 'Right'
        t=max(0,min(1,(.865-vertex.co.z)/.075));t=t*t*(3-2*t)
        body.vertex_groups[side+'Hand'].add([vertex.index],t,'REPLACE')
        body.vertex_groups[side+'LowerArm'].add([vertex.index],1-t,'REPLACE')
for face in body.data.polygons:face.use_smooth=True
print('WRIST AUDIT',json.dumps({'seams':seams,'openWristEdges':remaining}))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'island-combat-hands.blend'))
(out/'hand-variant.json').write_text(json.dumps({'method':'stitched hand/forearm mesh with continuous wrist weights','sourceOpenHandsPreserved':True,
    'seams':seams,'openWristEdges':remaining,'bones':len(arm.data.bones),'skinColor':color,'status':'draft seam and silhouette review required'},indent=2))
