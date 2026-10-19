"""Read-only full-density native pose diagnostic; never promotes or rewrites source."""
import json, hashlib, math, sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]

def array(collection, prop, width):
    result = np.empty(len(collection) * width, np.float32)
    collection.foreach_get(prop, result)
    return result.reshape(-1, width)

def main():
    role = sys.argv[sys.argv.index('--') + 1]
    source = ROOT / f'art/drafts/full-roster-v1/{role}-native67-bary/original-dense-native-spatial.blend'
    output = ROOT / f'art/drafts/full-roster-v1/{role}-native-normal-diagnostic'
    output.mkdir(exist_ok=True)
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(source))
    body = bpy.data.objects['OriginalDenseNative']
    arm = next(m.object for m in body.modifiers if m.type == 'ARMATURE')
    arm.data.pose_position = 'POSE'
    arm.animation_data_create()
    actions = {a.name.split('|')[-1]: a for a in bpy.data.actions}
    rest = array(body.data.vertices, 'co', 3)
    for ob in list(bpy.context.scene.objects):
        ob.hide_render = True
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.eevee.taa_render_samples = 8
    scene.render.resolution_x = scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.world = bpy.data.worlds.new('Diagnostic world')
    scene.world.color = (.12, .12, .12)
    material = bpy.data.materials.new('Diagnostic neutral clay')
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (.55, .55, .55, 1)
    bsdf.inputs['Roughness'].default_value = .9
    camera_data = bpy.data.cameras.new('Diagnostic camera')
    camera = bpy.data.objects.new('Diagnostic camera', camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = 'ORTHO'
    for name, location, energy in [('Key', (2,-3,4), 450), ('Fill',(-3,-1,2),200)]:
        data = bpy.data.lights.new(name, 'AREA')
        data.energy, data.shape, data.size = energy, 'DISK', 3
        light = bpy.data.objects.new(name, data)
        scene.collection.objects.link(light)
        light.location = location
        light.rotation_euler = (Vector((0,0,.7))-light.location).to_track_quat('-Z','Y').to_euler()
    records = []
    for label in ['dance_01', 'box_02', 'hit_to_head']:
        if label not in actions:
            continue
        action = actions[label]
        arm.animation_data.action = action
        frame = sum(action.frame_range) / 2
        scene.frame_set(math.floor(frame), subframe=frame % 1)
        bpy.context.view_layer.update()
        deps = bpy.context.evaluated_depsgraph_get()
        evaluated = body.evaluated_get(deps)
        mesh = bpy.data.meshes.new_from_object(evaluated, preserve_all_data_layers=True, depsgraph=deps)
        posed = array(mesh.vertices, 'co', 3)
        assert len(posed) == len(rest)
        displacement = float(np.linalg.norm(posed-rest, axis=1).max())
        assert displacement > 1e-4, ('Static pose witness', label)
        obj = bpy.data.objects.new('Owned diagnostic surface', mesh)
        scene.collection.objects.link(obj)
        obj.matrix_world = body.matrix_world.copy()
        mesh.materials.clear()
        mesh.materials.append(material)
        imported_normals = array(mesh.corner_normals, 'vector', 3)
        positions_hash = hashlib.sha256(posed.tobytes()).hexdigest()
        world = posed @ np.array(obj.matrix_world)[:3,:3].T + np.array(obj.matrix_world)[:3,3]
        low, high = world.min(0), world.max(0)
        h = float(high[2]-low[2])
        target = Vector(((low[0]+high[0])/2, (low[1]+high[1])/2, low[2]+h*.74))
        camera_data.ortho_scale = h*.72
        camera.location = target + Vector((0,-h*3,h*.03))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        paths = []
        for mode in ['transported-normals', 'geometric-normals']:
            if mode == 'geometric-normals' and mesh.has_custom_normals:
                bpy.context.view_layer.objects.active = obj
                obj.select_set(True)
                with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj], selected_editable_objects=[obj]):
                    bpy.ops.mesh.customdata_custom_splitnormals_clear()
                mesh.update()
            assert hashlib.sha256(array(mesh.vertices,'co',3).tobytes()).hexdigest() == positions_hash
            path = output / f'{label}-50-{mode}.png'
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            paths.append(path.name)
        geometric_normals = array(mesh.corner_normals, 'vector', 3)
        angles = np.degrees(np.arccos(np.clip((imported_normals*geometric_normals).sum(1),-1,1)))
        records.append({'clip':label,'phase':.5,'vertices':len(posed),'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),'poseDisplacementMaxObjectUnits':displacement,'normalAngleDegP50P95Max':np.quantile(angles,[.5,.95,1]).tolist(),'positionsIdenticalBetweenNormalModes':True,'captures':paths})
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(mesh)
    unchanged = hashlib.sha256(source.read_bytes()).hexdigest() == digest
    assert unchanged and records
    report = {'status':'DIAGNOSTIC_REQUIRES_VISUAL_REVIEW','blender':bpy.app.version_string,'role':role,'sourceSHA256':digest,'sourceUnchanged':unchanged,'cases':records,'scope':'Own native animations on full original mesh. Normal-only A/B; not the canonical retarget dance, not a shoulder fix or runtime acceptance.'}
    (output/'report.json').write_text(json.dumps(report,indent=2))
    print('RESULT '+json.dumps({'status':report['status'],'cases':len(records),'sourceUnchanged':unchanged}),flush=True)

if __name__ == '__main__':
    main()
