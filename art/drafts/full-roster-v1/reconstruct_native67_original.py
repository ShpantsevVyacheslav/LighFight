"""Reconstruct Rose skin weights without changing source geometry or native binds.
Run with Blender 4.2 LTS: -b --factory-startup --python-exit-code 1 -P this.py -- ...
No input files are written. Outputs require a new/empty directory, or an owned manifest.
"""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import struct
import sys
import time

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

SCHEMA = 'full-roster-closed-bary-positive-preservation/v1'
STABLE_TERMS = ('head', 'neck', 'hand', 'finger', 'thumb', 'index', 'middle',
                'ring', 'pinky', 'little', 'foot', 'toe')


def log(stage, **values):
    print('PROGRESS ' + json.dumps({'stage': stage, **values}), flush=True)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def file_sha(path):
    h = hashlib.sha256()
    with open(path, 'rb') as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def array(data, prop, count, dtype=np.float32):
    result = np.empty(count, dtype)
    data.foreach_get(prop, result)
    return result


def coordinates(obj, evaluated=False):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get()) if evaluated else None
    mesh = ev.to_mesh() if ev else obj.data
    result = array(mesh.vertices, 'co', len(mesh.vertices) * 3).reshape(-1, 3)
    matrix = np.array(obj.matrix_world)
    result = result @ matrix[:3, :3].T + matrix[:3, 3]
    if ev:
        ev.to_mesh_clear()
    return result


def geometry_signature(obj):
    me = obj.data
    return {
        'vertices': len(me.vertices), 'polygons': len(me.polygons),
        'triangles': sum(len(p.vertices) - 2 for p in me.polygons), 'corners': len(me.loops),
        'positions_sha256': sha(array(me.vertices, 'co', len(me.vertices) * 3).tobytes()),
        'triangle_corner_indices_sha256': sha(array(me.loops, 'vertex_index', len(me.loops), np.int32).tobytes()),
        'polygon_sizes_sha256': sha(array(me.polygons, 'loop_total', len(me.polygons), np.int32).tobytes()),
        'material_indices_sha256': sha(array(me.polygons, 'material_index', len(me.polygons), np.int32).tobytes()),
        'uv_sha256': {uv.name: sha(array(uv.data, 'uv', len(uv.data) * 2).tobytes()) for uv in me.uv_layers},
        'object_world_matrix_sha256': sha(np.array(obj.matrix_world, np.float64).tobytes()),
    }


def bind_signature(arm):
    return {
        'bone_count': len(arm.data.bones),
        'local_rest_matrices_sha256': sha(np.array([list(row) for b in arm.data.bones for row in b.matrix_local], np.float64).tobytes()),
        'parent_names': {b.name: b.parent.name if b.parent else None for b in arm.data.bones},
        'armature_world_matrix_sha256': sha(np.array(arm.matrix_world, np.float64).tobytes()),
    }


def material_signature(obj):
    def value(x):
        if isinstance(x, (float, int, str, bool)):
            return x
        try:
            return list(x)
        except TypeError:
            return str(type(x).__name__)
    result = []
    for mat in obj.data.materials:
        nodes = []
        for node in mat.node_tree.nodes:
            item = {'name': node.name, 'type': node.bl_idname,
                    'inputs': [(s.identifier, value(s.default_value)) for s in node.inputs if hasattr(s, 'default_value')]}
            if node.type == 'TEX_IMAGE' and node.image:
                img = node.image
                item['image'] = {'name': img.name, 'size': list(img.size),
                                 'colorspace': img.colorspace_settings.name,
                                 'raw_sha256': sha(img.packed_file.data) if img.packed_file else file_sha(bpy.path.abspath(img.filepath))}
            nodes.append(item)
        result.append({'name': mat.name, 'nodes': nodes,
                       'links': [(l.from_node.name, l.from_socket.identifier, l.to_node.name, l.to_socket.identifier)
                                 for l in mat.node_tree.links]})
    return sha(json.dumps(result, sort_keys=True).encode())


def extract_maps(source, output):
    if source.suffix.lower() == '.fbx':
        import shutil
        images=[]
        for i,path in enumerate(sorted(source.with_suffix('.fbm').iterdir())):
            if path.suffix.lower() not in ('.png','.jpg','.jpeg'):continue
            data=path.read_bytes();target=output/('original-image-'+str(i)+path.suffix.lower());target.write_bytes(data)
            images.append({'index':i,'original_name':path.name,'original_path':str(path),'path':str(target),'bytes':len(data),'mime':'image/png' if data.startswith(b'\x89PNG') else 'image/jpeg','sha256':sha(data)})
        assert images,'FBX original material images missing'
        return {'images':images,'source_material_metadata':[],'source_texture_metadata':[],'method':'Original FBM image bytes copied, material graph imported directly by Blender; no GLB conversion'}
    raw = source.read_bytes()
    magic, version, total = struct.unpack_from('<4sII', raw)
    assert magic == b'glTF' and version == 2 and total == len(raw), 'Invalid GLB header'
    offset, gltf, binary = 12, None, None
    while offset < len(raw):
        length, kind = struct.unpack_from('<I4s', raw, offset)
        chunk = raw[offset + 8:offset + 8 + length]
        if kind == b'JSON':
            gltf = json.loads(chunk)
        elif kind == b'BIN\x00':
            binary = chunk
        offset += 8 + length
    assert gltf is not None and binary is not None, 'Requires a binary GLB'
    images = []
    for index, image in enumerate(gltf.get('images', [])):
        if 'bufferView' in image:
            view = gltf['bufferViews'][image['bufferView']]
            assert view.get('buffer', 0) == 0, 'External image buffer unsupported'
            start = view.get('byteOffset', 0)
            blob = binary[start:start + view['byteLength']]
            assert len(blob) == view['byteLength']
        else:
            uri = image.get('uri', '')
            assert uri.startswith('data:') and ';base64,' in uri, 'Requires embedded images'
            blob = base64.b64decode(uri.split(',', 1)[1])
        mime = image.get('mimeType', 'image/png' if blob.startswith(b'\x89PNG') else 'image/jpeg')
        assert mime in ('image/png', 'image/jpeg'), 'Validated raw-image extraction supports PNG/JPEG only'
        ext = '.png' if mime == 'image/png' else '.jpg'
        target = output / ('original-image-' + str(index) + ext)
        target.write_bytes(blob)
        images.append({'index': index, 'original_name': image.get('name'), 'path': str(target),
                       'bytes': len(blob), 'mime': mime, 'sha256': sha(blob)})
    return {'images': images, 'source_material_metadata': gltf.get('materials', []),
            'source_texture_metadata': gltf.get('textures', [])}


def weight_matrix(obj, names):
    matrix = np.zeros((len(obj.data.vertices), len(names)), np.float32)
    columns = {name: i for i, name in enumerate(names)}
    for vertex in obj.data.vertices:
        for group in vertex.groups:
            name = obj.vertex_groups[group.group].name
            if name in columns:
                matrix[vertex.index, columns[name]] = group.weight
    return matrix


def assign_weights(obj, names, weights):
    for group in obj.vertex_groups[:]:
        obj.vertex_groups.remove(group)
    groups = [obj.vertex_groups.new(name=name) for name in names]
    for row, values in enumerate(weights):
        for column in np.flatnonzero(values):
            groups[int(column)].add([row], float(values[column]), 'REPLACE')


def weight_summary(obj):
    sums = np.array([sum(g.weight for g in v.groups) for v in obj.data.vertices])
    return {'unweighted': int(np.sum(sums == 0)),
            'max_influences': max(len(v.groups) for v in obj.data.vertices),
            'weight_sum_max_error': float(np.max(np.abs(sums - 1)))}


def nearest_summary(distances, height):
    quantiles = dict(zip(('p50', 'p95', 'p99', 'max'), np.quantile(distances, (.5, .95, .99, 1)).tolist()))
    return {'distance': quantiles, 'normalized_height': {k: v / height for k, v in quantiles.items()}}


def triangles(mesh):
    mesh.calc_loop_triangles()
    return [tuple(t.vertices) for t in mesh.loop_triangles]


def barycentric_baseline(source, donor, names, height):
    donor_points = [donor.matrix_world @ v.co for v in donor.data.vertices]
    faces = triangles(donor.data)
    tree = BVHTree.FromPolygons(donor_points, faces, all_triangles=True)
    donor_weights = [{donor.vertex_groups[g.group].name: g.weight for g in v.groups} for v in donor.data.vertices]
    assert all(sum(w.values()) > 0 for w in donor_weights), 'Donor has unweighted vertices'
    columns = {name: i for i, name in enumerate(names)}
    result = np.zeros((len(source.data.vertices), len(names)), np.float32)
    distances = np.empty(len(source.data.vertices), np.float32)
    for vertex in source.data.vertices:
        p = source.matrix_world @ vertex.co
        q, _, face_index, distance = tree.find_nearest(p)
        assert q is not None, 'Closest donor triangle missing'
        distances[vertex.index] = distance
        ids = faces[face_index]
        a, b, c = (donor_points[i] for i in ids)
        e, f, z = b - a, c - a, q - a
        ee, ef, ff, ez, fz = e.dot(e), e.dot(f), f.dot(f), e.dot(z), f.dot(z)
        denominator = ee * ff - ef * ef
        t = (ff * ez - ef * fz) / denominator if abs(denominator) > 1e-25 else 0
        u = (ee * fz - ef * ez) / denominator if abs(denominator) > 1e-25 else 0
        barycentric = np.maximum((1 - t - u, t, u), 0)
        barycentric /= sum(barycentric)
        weights = {}
        for index, factor in zip(ids, barycentric):
            for name, weight in donor_weights[index].items():
                weights[name] = weights.get(name, 0) + weight * factor
        ordered = sorted(((name, weight) for name, weight in weights.items() if weight > 0), key=lambda x: -x[1])
        total = sum(weight for _, weight in ordered)
        assert total > 0, 'Baseline vertex unweighted'
        for name, weight in ordered:
            result[vertex.index, columns[name]] = weight / total
        if vertex.index % 100000 == 0:
            log('barycentric baseline', vertices=vertex.index)
    return result, nearest_summary(distances, height)


def spatial_field(source, donor, names, baseline, height, args):
    points = coordinates(source)
    donor_points = np.array([donor.matrix_world @ v.co for v in donor.data.vertices], np.float32)
    donor_weights = weight_matrix(donor, names)
    tree = KDTree(len(donor_points))
    for index, point in enumerate(donor_points):
        tree.insert(point, index)
    tree.balance()
    stable = [i for i, name in enumerate(names) if any(term in name.lower() for term in STABLE_TERMS)]
    result = baseline.copy()
    sigma = args.sigma_height * height
    for start in range(0, len(points), args.batch_size):
        stop = min(start + args.batch_size, len(points))
        rows = [tree.find_n(point, args.neighbors) for point in points[start:stop]]
        ids = np.array([[r[1] for r in row] for row in rows], np.int32)
        distances = np.array([[r[2] for r in row] for row in rows], np.float32)
        radius = np.maximum(distances[:, -1:], 1e-8)
        factors = np.exp(-.5 * (distances / sigma) ** 2) * np.maximum(1 - (distances / radius) ** 2, 0) ** 2
        total = factors.sum(1)
        if np.any(total == 0):
            raise ValueError('Spatial field has no nonzero neighbor support; sigma too small or geometry mismatched')
        factors /= total[:, None]
        smooth = np.einsum('nk,nkg->ng', factors, donor_weights[ids])
        stable_weight = baseline[start:stop, stable].sum(1) if stable else np.zeros(stop - start)
        blend = np.clip((args.stable_high - stable_weight) / (args.stable_high - args.stable_low), 0, 1)[:, None]
        result[start:stop] = baseline[start:stop] * (1 - blend) + smooth * blend
        if start % 100000 == 0:
            log('continuous spatial field', vertices=start)
    result[result < args.min_weight] = 0
    total = result.sum(1)
    assert np.all(total > 0), 'Spatial field has unweighted vertices'
    result /= total[:, None]
    return result, [names[i] for i in stable]


def armature_modifier(obj, arm):
    mod = obj.modifiers.new('Native spatial skin weights', 'ARMATURE')
    mod.object = arm
    mod.use_deform_preserve_volume = False


def edge_setup(obj):
    edges = array(obj.data.edges, 'vertices', len(obj.data.edges) * 2, np.int32).reshape(-1, 2)
    points = coordinates(obj)
    lengths = np.linalg.norm(points[edges[:, 0]] - points[edges[:, 1]], axis=1)
    return edges, lengths


def strain(obj, setup):
    edges, rest = setup
    points = coordinates(obj, evaluated=True)
    ratio = np.linalg.norm(points[edges[:, 0]] - points[edges[:, 1]], axis=1) / np.maximum(rest, 1e-9)
    ratio = ratio[rest > 1e-7]
    return {'strain_p50_p95_p99_max': np.quantile(ratio, (.5, .95, .99, 1)).tolist(),
            'edges_over_5': int(np.sum(ratio > 5)), 'edge_count': len(ratio)}


def pose_metrics(candidate, baseline, donor, arm, poses, height, samples):
    result = {}
    setups = {label: edge_setup(obj) for label, obj in (('spatial', candidate), ('rejected_baseline', baseline), ('donor', donor))}
    arm.data.pose_position = 'POSE'
    arm.animation_data_create()
    for clip, frame in poses:
        matches = [a for a in bpy.data.actions if a.name.endswith('|' + clip) or a.name == clip]
        assert len(matches) == 1, 'Missing or ambiguous native action: ' + clip
        arm.animation_data.action = matches[0]
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        case = {label: {'strain': strain(obj, setups[label])} for label, obj in
                (('spatial', candidate), ('rejected_baseline', baseline), ('donor', donor))}
        deps = bpy.context.evaluated_depsgraph_get()
        ev = donor.evaluated_get(deps)
        me = ev.to_mesh()
        donor_vertices = [donor.matrix_world @ v.co for v in me.vertices]
        tree = BVHTree.FromPolygons(donor_vertices, triangles(me), all_triangles=True)
        ev.to_mesh_clear()
        for label, obj in (('spatial', candidate), ('rejected_baseline', baseline)):
            points = coordinates(obj, evaluated=True)
            step = max(1, len(points) // samples)
            distances = np.array([tree.find_nearest(Vector(p))[3] for p in points[::step]])
            case[label]['donor_surface_distance'] = {'samples': len(distances), **nearest_summary(distances, height)}
        result[clip + ':' + str(frame)] = case
        log('pose metrics', clip=clip, frame=frame)
    arm.data.pose_position = 'REST'
    arm.animation_data_clear()
    bpy.context.view_layer.update()
    return result


def prepare_output(args):
    source, donor, output = (Path(p).expanduser().resolve() for p in (args.source, args.donor, args.out))
    assert source.is_file() and donor.is_file(), 'Input files missing'
    assert source.suffix.lower() in ('.glb','.fbx') and donor.suffix.lower() == '.fbx', 'Requires original GLB/FBX and donor FBX'
    assert not source.is_relative_to(output) and not donor.is_relative_to(output), 'Output must not contain input files'
    if output.exists() and any(output.iterdir()):
        manifest = output / 'manifest.json'
        owned = manifest.is_file() and json.loads(manifest.read_text()).get('schema') == SCHEMA
        assert args.overwrite and owned, 'Nonempty output requires --overwrite and a manifest owned by this script'
        assert not any(p.is_symlink() or (p.is_file() and p.stat().st_nlink > 1)
                       for p in output.rglob('*')), 'Output contains linked files; refuse potential writes outside owned output'
    output.mkdir(parents=True, exist_ok=True)
    return source, donor, output


def export_rig_fbx(candidate, arm, maps, output):
    # Export-specific material references are changed in memory, never in the saved
    # blend. Load owned raw filenames instead of unpacking arbitrary GLB image paths.
    by_sha = {item['sha256']: item['path'] for item in maps['images']}
    external_images = {}
    for material in candidate.data.materials:
        for node in material.node_tree.nodes:
            if node.type != 'TEX_IMAGE' or not node.image or not node.image.packed_file:
                continue
            original = node.image
            raw_hash = sha(original.packed_file.data)
            if raw_hash in by_sha:
                key = (raw_hash, original.colorspace_settings.name)
                if key not in external_images:
                    image = bpy.data.images.load(by_sha[raw_hash], check_existing=False)
                    image.colorspace_settings.name = original.colorspace_settings.name
                    external_images[key] = image
                node.image = external_images[key]
    bpy.ops.object.select_all(action='DESELECT')
    candidate.hide_set(False)
    candidate.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = candidate
    target = output / 'rose-original-dense-native-spatial-rig.fbx'
    bpy.ops.export_scene.fbx(filepath=str(target), use_selection=True, object_types={'MESH', 'ARMATURE'},
                             use_mesh_modifiers=False, add_leaf_bones=False, use_armature_deform_only=False,
                             bake_anim=False, axis_forward='-Z', axis_up='Y', mesh_smooth_type='OFF',
                             use_tspace=True, path_mode='COPY', embed_textures=False)
    return {'path': str(target), 'bytes': target.stat().st_size, 'sha256': file_sha(target),
            'animation': 'rig-only; actions remain in blend/provider FBX',
            'material_limit': 'Original packed RM map must be assigned explicitly; G=roughness, B=metallic'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True)
    parser.add_argument('--donor', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--overwrite', action='store_true')
    parser.add_argument('--sigma-height', type=float, default=.007)
    parser.add_argument('--neighbors', type=int, default=48)
    parser.add_argument('--min-weight', type=float, default=0)
    parser.add_argument('--stable-low', type=float, default=.75)
    parser.add_argument('--stable-high', type=float, default=.95)
    parser.add_argument('--batch-size', type=int, default=5000)
    parser.add_argument('--max-align-p99-height', type=float, default=.001)
    parser.add_argument('--max-align-max-height', type=float, default=.002)
    parser.add_argument('--metrics-samples', type=int, default=50000)
    parser.add_argument('--pose', action='append', help='Native action:frame; may repeat')
    parser.add_argument('--export-fbx', action='store_true', help='Rig-only FBX; original actions remain in blend/provider FBX')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    assert args.sigma_height > 0 and args.neighbors >= 2 and args.batch_size > 0
    assert 0 <= args.stable_low < args.stable_high <= 1 and 0 <= args.min_weight < 1
    assert args.max_align_p99_height > 0 and args.max_align_max_height > 0 and args.metrics_samples > 0
    assert bpy.app.background, 'Run headless with --factory-startup'
    assert bpy.app.version[:2] == (4, 2), 'Validated for Blender 4.2 LTS; other native-action APIs require separate review'
    source_path, donor_path, output = prepare_output(args)
    started = time.time()
    input_hashes = {'source': file_sha(source_path), 'donor': file_sha(donor_path)}
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    maps = extract_maps(source_path, output)
    if source_path.suffix.lower()=='.fbx':
        bpy.ops.import_scene.fbx(filepath=str(source_path),use_anim=False,automatic_bone_orientation=False)
    else:
        bpy.ops.import_scene.gltf(filepath=str(source_path))
    source_meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert len(source_meshes) == 1, 'Requires exactly one source mesh; no implicit joining'
    candidate = source_meshes[0]
    before_objects = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(donor_path))
    imported = [o for o in bpy.data.objects if o not in before_objects]
    meshes, arms = [o for o in imported if o.type == 'MESH'], [o for o in imported if o.type == 'ARMATURE']
    assert len(meshes) == 1 and len(arms) == 1, 'Requires exactly one native donor mesh and armature'
    donor, arm = meshes[0], arms[0]
    arm.data.pose_position = 'REST'
    arm.animation_data_clear()
    bpy.context.view_layer.update()
    height = float(np.ptp(coordinates(candidate)[:, 2]))
    assert height > 0 and candidate.data.uv_layers, 'Source height/UVs invalid'
    assert not candidate.modifiers and not candidate.vertex_groups, 'Requires untouched, unrigged source mesh'
    assert donor.find_armature() == arm, 'Donor is not bound to the imported native armature'
    source_geometry, source_material = geometry_signature(candidate), material_signature(candidate)
    native_bind = bind_signature(arm)
    donor_geometry = geometry_signature(donor)
    names = [g.name for g in donor.vertex_groups]
    assert names and all(name in arm.data.bones for name in names), 'Donor groups do not match native bones'
    assert args.neighbors <= len(donor.data.vertices), 'Too many neighbors'
    baseline_weights, alignment = barycentric_baseline(candidate, donor, names, height)
    assert alignment['normalized_height']['p99'] <= args.max_align_p99_height, 'Rest alignment p99 gate failed; no pose/shape fitting attempted'
    assert alignment['normalized_height']['max'] <= args.max_align_max_height, 'Rest alignment maximum gate failed; no pose/shape fitting attempted'
    log('rest alignment accepted', **alignment['normalized_height'])
    assign_weights(candidate, names, baseline_weights)
    baseline = candidate.copy()
    baseline.data = candidate.data.copy()  # deform weights live on mesh; cannot share this data
    baseline.name = 'RejectedBarycentricBaseline'
    bpy.context.scene.collection.objects.link(baseline)
    armature_modifier(baseline, arm)
    spatial_weights, stable_groups = baseline_weights, []  # Match Sailor guard-v3 full closed-triangle bary authority.
    assign_weights(candidate, names, spatial_weights)
    armature_modifier(candidate, arm)
    candidate.name = 'OriginalDenseNative'
    summary = weight_summary(candidate)
    assert summary['unweighted'] == 0 and summary['weight_sum_max_error'] < 1e-5
    assert geometry_signature(candidate) == source_geometry, 'Source geometry/UV/topology/transform changed'
    assert geometry_signature(donor) == donor_geometry, 'Donor rest geometry changed'
    assert bind_signature(arm) == native_bind, 'Native rest matrices changed'
    assert material_signature(candidate) == source_material, 'Original source material/image bytes changed'
    poses = []
    for item in args.pose or ('box_02:24', 'box_02:44', 'front_kick_01:24', 'front_kick_01:34'):
        clip, frame = item.rsplit(':', 1)
        poses.append((clip, int(frame)))
    metrics = pose_metrics(candidate, baseline, donor, arm, poses, height, args.metrics_samples)
    assert geometry_signature(candidate) == source_geometry and bind_signature(arm) == native_bind
    baseline_mesh = baseline.data
    bpy.data.objects.remove(baseline, do_unlink=True)
    bpy.data.meshes.remove(baseline_mesh)
    donor.hide_render = True
    donor.hide_set(True)
    candidate.hide_render = False
    blend_path = output / 'original-dense-native-spatial.blend'
    for action in bpy.data.actions:
        action.use_fake_user = True  # Preserve all authored takes, including inactive clips.
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.wm.open_mainfile(filepath=str(blend_path))
    candidate = bpy.data.objects['OriginalDenseNative']
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    assert geometry_signature(candidate) == source_geometry, 'Saved geometry preservation failed'
    assert bind_signature(arm) == native_bind, 'Saved native-bind preservation failed'
    assert material_signature(candidate) == source_material, 'Saved original-material preservation failed'
    summary = weight_summary(candidate)
    assert summary['unweighted'] == 0 and summary['weight_sum_max_error'] < 1e-5
    report = {'schema': SCHEMA, 'blender_version': bpy.app.version_string,
              'inputs': {'source': str(source_path), 'donor': str(donor_path), 'sha256': input_hashes},
              'parameters': vars(args), 'height': height, 'source_preservation': source_geometry,
              'source_material_sha256': source_material, 'native_bind_preservation': native_bind,
              'rest_alignment': alignment, 'stable_groups': stable_groups, 'weights': summary,
              'original_maps': maps, 'poses': metrics, 'blend': str(blend_path),
              'visual_status': 'Parameter reproduction only; consult reviewed SPATIAL_REPORT.md. No automatic visual acceptance.',
              'elapsed_seconds': time.time() - started}
    if args.export_fbx:
        report['fbx'] = export_rig_fbx(candidate, arm, maps, output)
    assert file_sha(source_path) == input_hashes['source'] and file_sha(donor_path) == input_hashes['donor'], 'Input file bytes changed'
    assert all(file_sha(item['path']) == item['sha256'] for item in maps['images']), 'Extracted image bytes changed'
    report['elapsed_seconds'] = time.time() - started
    (output / 'manifest.json').write_text(json.dumps(report, indent=2))
    print('RESULT ' + json.dumps({'manifest': str(output / 'manifest.json'), 'blend': str(blend_path),
                                'weights': summary, 'preservation': 'passed', 'elapsed_seconds': report['elapsed_seconds']}), flush=True)


if __name__ == '__main__':
    main()
