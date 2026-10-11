"""Merge direct Meshy FBX exports only when their bind rigs match.

Usage: Blender --background --python this.py -- input_folder output.fbx
No remeshing, weight normalization, geometry edits or animation retargeting.
"""
import bpy, json, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from native_fbx_compat import require_same_rig, check_round_trip

def rig_signature(arm):
    result = {b.name: (b.parent.name if b.parent else None, list(sum((list(row) for row in b.matrix_local), []))) for b in arm.data.bones}
    result["__object_transform__"]=(None, list(sum((list(row) for row in arm.matrix_world), [])))
    return result

def surface_snapshot(meshes):
    assert len(meshes)==1, 'Round-trip gate currently requires one source surface'
    obj=meshes[0]
    group_names={g.index:g.name for g in obj.vertex_groups}
    return {
        'positions':[list(obj.matrix_world@v.co) for v in obj.data.vertices],
        'weights':[{group_names[g.group]:g.weight for g in v.groups} for v in obj.data.vertices],
        'uv':[list(loop.uv) for loop in obj.data.uv_layers.active.data],
        'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons)
    }


def main():
    args=sys.argv[sys.argv.index('--')+1:]; folder, target=map(Path,args)
    files=sorted(folder.rglob('*.fbx')); assert files, 'No direct web FBX exports'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    base=None; signature=None; actions=[]; meshes=[]
    names_path=folder/"clip-names.json"
    names=json.loads(names_path.read_text()) if names_path.exists() else {}
    for file in files:
        before=set(bpy.data.objects); old_actions=set(bpy.data.actions)
        bpy.ops.import_scene.fbx(filepath=str(file))
        added=set(bpy.data.objects)-before
        arms=[o for o in added if o.type=='ARMATURE']; assert len(arms)==1
        arm=arms[0]; current=rig_signature(arm)
        if base is None:
            base=arm; signature=current
            meshes=[o for o in added if o.type=='MESH' and len(o.data.vertices)>1000]
            assert sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)==103074
        else:
            require_same_rig(signature,current)
        new_actions=sorted(set(bpy.data.actions)-old_actions,key=lambda a:a.name)
        explicit=names.get(str(file.relative_to(folder)))
        if explicit is not None:
            assert len(explicit)==len(new_actions), 'Clip manifest does not match exported actions'
        elif len(new_actions)==1 and '_Animation_' in file.stem:
            explicit=[file.stem.split('_Animation_',1)[1].removesuffix('_withSkin').removesuffix('_withoutSkin')]
        else:
            raise ValueError('Explicit clip-names.json required: FBX may truncate clip names')
        for action, name in zip(new_actions,explicit):
            if name in [a.name for a in actions]:raise ValueError('Duplicate native clip: '+name)
            action.name=name
            # Provider FBX end bones and all finger curves are retained.
            action.use_fake_user=True; actions.append(action)
        if arm!=base:
            for obj in added:bpy.data.objects.remove(obj,do_unlink=True)
        else:
            for obj in added:
                if obj not in meshes and obj!=base:bpy.data.objects.remove(obj,do_unlink=True)
    base.animation_data_clear()
    for bone in base.pose.bones:bone.matrix_basis.identity()
    bpy.ops.object.select_all(action='DESELECT');base.select_set(True)
    for mesh in meshes:mesh.select_set(True)
    bpy.context.view_layer.objects.active=base
    target.parent.mkdir(parents=True,exist_ok=True)
    surface=surface_snapshot(meshes)
    bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0,path_mode='COPY')
    report={'triangles':103074,'bones':len(signature)-1,'actions':[a.name for a in actions],
            'files':[str(f) for f in files],'retargeted':False,'geometryEditsRequested':False,'weightEditsRequested':False,'roundTripVerified':False}
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(target))
    result_meshes=[o for o in bpy.data.objects if o.type=='MESH' and len(o.data.vertices)>1000]
    report.update(check_round_trip(surface,surface_snapshot(result_meshes)))
    report['roundTripVerified']=True
    target.with_suffix('.audit.json').write_text(json.dumps(report,indent=2));print(report)
if __name__=='__main__':main()
