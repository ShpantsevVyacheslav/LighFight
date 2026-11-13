#!/usr/bin/env python3
"""
Rostov-on-Don 3D World Generator - Embankment & Voroshilovsky Bridge
Generates a realistic 3D Wavefront OBJ + MTL model of the Rostov Embankment
from OpenStreetMap geodata, including terrain elevation, road ribbons,
quay wall, procedural buildings with roofs, and the Voroshilovsky Bridge.
"""

import json
import math
import os
import sys

# Reference origin (Intersection of Beregovaya st. and Voroshilovsky Bridge)
LAT_ORIGIN = 47.2138
LON_ORIGIN = 47.2138  # Will be set to 39.7214
LON_ORIGIN = 39.7214

# Meters per degree
METERS_PER_LAT = 111320.0
METERS_PER_LON = 111320.0 * math.cos(math.radians(LAT_ORIGIN))  # ~75610 m

def geo_to_local(lat, lon):
    x = (lon - LON_ORIGIN) * METERS_PER_LON
    z = (lat - LAT_ORIGIN) * METERS_PER_LAT
    return x, z

def terrain_elevation(x, z):
    """
    Rostov right-bank elevation profile:
    z < -10: River Don (water level y=0.5m, quay parapet at y=2.5m)
    -10 <= z <= 35: Beregovaya street & promenade (y=2.5m - 3.8m)
    z > 35: Right-bank slope ascending towards Donskaya, Stanislavskogo, Moskovskaya
    """
    if z < -10:
        return 0.5  # Water plane
    elif z <= 35:
        # Promenade and Beregovaya street
        return 2.5 + (z + 10) * 0.03
    else:
        # Ascending slope towards north
        dz = z - 35
        return 3.85 + 38.0 * math.pow(min(dz / 300.0, 1.0), 1.25)

def area_2d(poly):
    return 0.5 * sum(poly[i][0] * poly[(i+1)%len(poly)][1] - poly[(i+1)%len(poly)][0] * poly[i][1] for i in range(len(poly)))

def is_point_in_triangle(p, a, b, c):
    def sign(p1, p2, p3):
        return (p1[0] - p3[0]) * (p2[1] - p3[1]) - (p2[0] - p3[0]) * (p1[1] - p3[1])
    d1 = sign(p, a, b)
    d2 = sign(p, b, c)
    d3 = sign(p, c, a)
    has_neg = (d1 < 0) or (d2 < 0) or (d3 < 0)
    has_pos = (d1 > 0) or (d2 > 0) or (d3 > 0)
    return not (has_neg and has_pos)

def triangulate_poly(poly):
    """Ear-clipping triangulation for 2D polygon."""
    n = len(poly)
    if n < 3:
        return []
    if n == 3:
        return [(0, 1, 2)]
    indices = list(range(n))
    if area_2d(poly) < 0:
        indices.reverse()
    
    triangles = []
    max_iter = len(indices) * 4
    while len(indices) > 3 and max_iter > 0:
        max_iter -= 1
        ear_found = False
        for i in range(len(indices)):
            prev_i = indices[(i - 1) % len(indices)]
            curr_i = indices[i]
            next_i = indices[(i + 1) % len(indices)]
            a, b, c = poly[prev_i], poly[curr_i], poly[next_i]
            cross = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
            if cross <= 1e-7:
                continue
            inside = False
            for j in range(len(indices)):
                test_i = indices[j]
                if test_i in (prev_i, curr_i, next_i):
                    continue
                if is_point_in_triangle(poly[test_i], a, b, c):
                    inside = True
                    break
            if not inside:
                triangles.append((prev_i, curr_i, next_i))
                indices.pop(i)
                ear_found = True
                break
        if not ear_found:
            for k in range(1, len(indices) - 1):
                triangles.append((indices[0], indices[k], indices[k+1]))
            break
    if len(indices) == 3:
        triangles.append((indices[0], indices[1], indices[2]))
    return triangles

class ObjWriter:
    def __init__(self):
        self.vertices = []
        self.normals = []
        self.uvs = []
        self.groups = {}  # mat_name -> list of face tuples (v_indices)

    def add_vertex(self, x, y, z):
        self.vertices.append((x, y, z))
        return len(self.vertices)

    def add_normal(self, nx, ny, nz):
        self.normals.append((nx, ny, nz))
        return len(self.normals)

    def add_uv(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_face(self, mat_name, v_ids, n_id=None):
        if mat_name not in self.groups:
            self.groups[mat_name] = []
        self.groups[mat_name].append((v_ids, n_id))

    def write_obj(self, filepath, mtl_filename):
        with open(filepath, 'w') as f:
            f.write(f"# Rostov-on-Don Embankment 3D Model\n")
            f.write(f"mtllib {mtl_filename}\n\n")
            for v in self.vertices:
                f.write(f"v {v[0]:.3f} {v[1]:.3f} {v[2]:.3f}\n")
            for n in self.normals:
                f.write(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}\n")
            f.write("\n")
            for mat_name, faces in self.groups.items():
                f.write(f"usemtl {mat_name}\n")
                f.write(f"s 1\n")
                for v_ids, n_id in faces:
                    if n_id:
                        f.write("f " + " ".join(f"{vid}//{n_id}" for vid in v_ids) + "\n")
                    else:
                        f.write("f " + " ".join(str(vid) for vid in v_ids) + "\n")
                f.write("\n")

def write_mtl(filepath):
    content = """# Rostov Embankment Materials Library
newmtl Don_Water
Kd 0.12 0.32 0.44
Ks 0.85 0.92 0.98
Ns 128
d 0.90
illum 2

newmtl Promenade_Granite
Kd 0.78 0.75 0.70
Ks 0.12 0.12 0.12
Ns 12
illum 2

newmtl Quay_Wall
Kd 0.60 0.58 0.56
Ks 0.08 0.08 0.08
Ns 8
illum 2

newmtl Parapet_Granite
Kd 0.62 0.32 0.28
Ks 0.35 0.35 0.35
Ns 45
illum 2

newmtl Road_Asphalt
Kd 0.22 0.22 0.24
Ks 0.05 0.05 0.05
Ns 6
illum 2

newmtl Road_Sidewalk
Kd 0.70 0.68 0.65
Ks 0.08 0.08 0.08
Ns 8
illum 2

newmtl Building_Historic_Brick
Kd 0.72 0.40 0.28
Ks 0.10 0.10 0.10
Ns 10
illum 2

newmtl Building_Stucco_Light
Kd 0.86 0.82 0.74
Ks 0.08 0.08 0.08
Ns 8
illum 2

newmtl Building_FiveSeas_Glass
Kd 0.18 0.36 0.52
Ks 0.92 0.95 1.00
Ns 110
d 0.88
illum 2

newmtl Building_Roof
Kd 0.32 0.34 0.36
Ks 0.15 0.15 0.15
Ns 18
illum 2

newmtl Bridge_Concrete
Kd 0.82 0.84 0.85
Ks 0.15 0.15 0.15
Ns 15
illum 2

newmtl Bridge_Steel
Kd 0.30 0.38 0.45
Ks 0.55 0.58 0.62
Ns 65
illum 2

newmtl Greenery_Park
Kd 0.28 0.48 0.20
Ks 0.02 0.02 0.02
Ns 4
illum 2
"""
    with open(filepath, 'w') as f:
        f.write(content)

def main():
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
    cache_path = os.path.join(root, "unity/Assets/LighFight/Art/Arenas/RostovEmbankment/osm_rostov_cache.json")
    out_obj = os.path.join(root, "unity/Assets/LighFight/Art/Arenas/RostovEmbankment/RostovEmbankment.obj")
    out_mtl = os.path.join(root, "unity/Assets/LighFight/Art/Arenas/RostovEmbankment/RostovEmbankment.mtl")

    if not os.path.exists(cache_path):
        print("Error: Cached data not found:", cache_path)
        sys.exit(1)

    with open(cache_path) as f:
        osm_data = json.load(f)

    elements = osm_data['elements']
    nodes = {e['id']: (e['lat'], e['lon']) for e in elements if e['type'] == 'node'}
    ways = [e for e in elements if e['type'] == 'way']

    writer = ObjWriter()

    # Pre-add standard normals
    n_up = writer.add_normal(0, 1, 0)
    n_down = writer.add_normal(0, -1, 0)
    n_south = writer.add_normal(0, 0, -1)
    n_north = writer.add_normal(0, 0, 1)

    print("Generating Rostov Embankment 3D Model...")

    # 1. DON RIVER WATER PLANE
    # Broad water expanse south of the embankment
    w_min_x, w_max_x = -450.0, 450.0
    w_min_z, w_max_z = -220.0, -10.0
    w_y = 0.5

    v1 = writer.add_vertex(w_min_x, w_y, w_min_z)
    v2 = writer.add_vertex(w_max_x, w_y, w_min_z)
    v3 = writer.add_vertex(w_max_x, w_y, w_max_z)
    v4 = writer.add_vertex(w_min_x, w_y, w_max_z)
    writer.add_face("Don_Water", (v1, v2, v3), n_up)
    writer.add_face("Don_Water", (v1, v3, v4), n_up)

    # 2. PROMENADE (Pedestrian waterfront)
    # Between river quay edge (z = -10) and Beregovaya street (z = 15)
    prom_min_x, prom_max_x = -450.0, 450.0
    p1 = writer.add_vertex(prom_min_x, 2.5, -10.0)
    p2 = writer.add_vertex(prom_max_x, 2.5, -10.0)
    p3 = writer.add_vertex(prom_max_x, 2.8, 15.0)
    p4 = writer.add_vertex(prom_min_x, 2.8, 15.0)
    writer.add_face("Promenade_Granite", (p1, p2, p3), n_up)
    writer.add_face("Promenade_Granite", (p1, p3, p4), n_up)

    # Quay vertical wall (from water level 0.5m to promenade 2.5m)
    qw1 = writer.add_vertex(prom_min_x, 0.0, -10.0)
    qw2 = writer.add_vertex(prom_max_x, 0.0, -10.0)
    qw3 = writer.add_vertex(prom_max_x, 2.5, -10.0)
    qw4 = writer.add_vertex(prom_min_x, 2.5, -10.0)
    writer.add_face("Quay_Wall", (qw1, qw2, qw3), n_south)
    writer.add_face("Quay_Wall", (qw1, qw3, qw4), n_south)

    # Granit parapet railing along the water line (height 1.05m, thickness 0.35m)
    par_h = 3.55
    par_z_front = -10.0
    par_z_back = -9.65
    pr1 = writer.add_vertex(prom_min_x, 2.5, par_z_front)
    pr2 = writer.add_vertex(prom_max_x, 2.5, par_z_front)
    pr3 = writer.add_vertex(prom_max_x, par_h, par_z_front)
    pr4 = writer.add_vertex(prom_min_x, par_h, par_z_front)
    writer.add_face("Parapet_Granite", (pr1, pr2, pr3), n_south)
    writer.add_face("Parapet_Granite", (pr1, pr3, pr4), n_south)

    # Top of parapet
    pt1 = writer.add_vertex(prom_min_x, par_h, par_z_front)
    pt2 = writer.add_vertex(prom_max_x, par_h, par_z_front)
    pt3 = writer.add_vertex(prom_max_x, par_h, par_z_back)
    pt4 = writer.add_vertex(prom_min_x, par_h, par_z_back)
    writer.add_face("Parapet_Granite", (pt1, pt2, pt3), n_up)
    writer.add_face("Parapet_Granite", (pt1, pt3, pt4), n_up)

    # 3. ROADS & STREETS FROM OSM
    highways = [w for w in ways if 'highway' in w.get('tags', {})]
    road_count = 0
    for h in highways:
        tags = h.get('tags', {})
        h_type = tags.get('highway', '')
        way_nodes = [nodes[nid] for nid in h.get('nodes', []) if nid in nodes]
        if len(way_nodes) < 2:
            continue

        width = 10.0 if h_type in ('primary', 'secondary', 'trunk') else (6.0 if h_type in ('residential', 'service') else 4.0)
        half_w = width * 0.5
        mat_name = "Road_Asphalt"

        pts_3d = []
        for lat, lon in way_nodes:
            lx, lz = geo_to_local(lat, lon)
            if -460 <= lx <= 460 and -50 <= lz <= 320:
                ly = terrain_elevation(lx, lz) + 0.08
                pts_3d.append((lx, ly, lz))

        if len(pts_3d) < 2:
            continue

        for i in range(len(pts_3d) - 1):
            p_a = pts_3d[i]
            p_b = pts_3d[i+1]
            dx = p_b[0] - p_a[0]
            dz = p_b[2] - p_a[2]
            seg_len = math.hypot(dx, dz)
            if seg_len < 0.1:
                continue
            # Normal perpendicular vector in XZ
            nx = -dz / seg_len * half_w
            nz = dx / seg_len * half_w

            rv1 = writer.add_vertex(p_a[0] + nx, p_a[1], p_a[2] + nz)
            rv2 = writer.add_vertex(p_b[0] + nx, p_b[1], p_b[2] + nz)
            rv3 = writer.add_vertex(p_b[0] - nx, p_b[1], p_b[2] - nz)
            rv4 = writer.add_vertex(p_a[0] - nx, p_a[1], p_a[2] - nz)

            writer.add_face(mat_name, (rv1, rv2, rv3), n_up)
            writer.add_face(mat_name, (rv1, rv3, rv4), n_up)
            road_count += 1

    print(f"Generated {road_count} road segments.")

    # 4. BUILDINGS FROM OSM
    buildings = [w for w in ways if 'building' in w.get('tags', {})]
    bldg_count = 0
    for b in buildings:
        tags = b.get('tags', {})
        way_nodes = [nodes[nid] for nid in b.get('nodes', []) if nid in nodes]
        if len(way_nodes) < 3:
            continue

        # If closed polygon, remove duplicate last point for processing
        if way_nodes[0] == way_nodes[-1] and len(way_nodes) > 3:
            poly_nodes = way_nodes[:-1]
        else:
            poly_nodes = way_nodes

        poly_2d = []
        for lat, lon in poly_nodes:
            lx, lz = geo_to_local(lat, lon)
            poly_2d.append((lx, lz))

        # Check bounds
        avg_x = sum(p[0] for p in poly_2d) / len(poly_2d)
        avg_z = sum(p[1] for p in poly_2d) / len(poly_2d)
        if not (-450 <= avg_x <= 450 and -20 <= avg_z <= 320):
            continue

        base_y = sum(terrain_elevation(p[0], p[1]) for p in poly_2d) / len(poly_2d)

        # Height determination
        levels_str = tags.get('building:levels')
        name = tags.get('name', '')
        bldg_id = b.get('id', 0)

        # Special landmark: Business Center "Five Seas" (Пять морей)
        is_five_seas = "Пять морей" in name or bldg_id == 87075821
        if is_five_seas:
            height = 52.0
            wall_mat = "Building_FiveSeas_Glass"
        elif levels_str and levels_str.isdigit():
            levels = int(levels_str)
            height = levels * 3.5
            wall_mat = "Building_Historic_Brick" if levels <= 4 else "Building_Stucco_Light"
        elif "shop" in tags or "cafe" in tags or "restaurant" in tags:
            height = 5.0
            wall_mat = "Building_Stucco_Light"
        else:
            height = 10.5  # Standard 3 levels
            wall_mat = "Building_Historic_Brick" if (avg_z > 40 and hash(name or str(bldg_id)) % 2 == 0) else "Building_Stucco_Light"

        roof_y = base_y + height

        # Create vertices for base and roof
        base_vids = []
        roof_vids = []
        for p in poly_2d:
            b_vid = writer.add_vertex(p[0], base_y, p[1])
            r_vid = writer.add_vertex(p[0], roof_y, p[1])
            base_vids.append(b_vid)
            roof_vids.append(r_vid)

        # Build walls (quads for each edge)
        num_pts = len(poly_2d)
        for i in range(num_pts):
            next_i = (i + 1) % num_pts
            # Compute outward normal
            edx = poly_2d[next_i][0] - poly_2d[i][0]
            edz = poly_2d[next_i][1] - poly_2d[i][1]
            elen = math.hypot(edx, edz)
            if elen > 1e-5:
                wn_id = writer.add_normal(-edz / elen, 0, edx / elen)
            else:
                wn_id = None

            wv1 = base_vids[i]
            wv2 = base_vids[next_i]
            wv3 = roof_vids[next_i]
            wv4 = roof_vids[i]

            writer.add_face(wall_mat, (wv1, wv2, wv3), wn_id)
            writer.add_face(wall_mat, (wv1, wv3, wv4), wn_id)

        # Triangulate and create roof
        triangles = triangulate_poly(poly_2d)
        for t in triangles:
            # Reorder for upward normal
            writer.add_face("Building_Roof", (roof_vids[t[0]], roof_vids[t[1]], roof_vids[t[2]]), n_up)

        bldg_count += 1

    print(f"Generated {bldg_count} 3D buildings.")

    # 5. VOROSHILOVSKY BRIDGE (Iconic Twin-Span Bridge)
    # Spans from Z = -220 to Z = +80 at X ~ 0
    br_x_left = -11.0
    br_x_right = 11.0
    br_z_start = -220.0
    br_z_end = 80.0
    br_deck_y = 24.0
    br_thickness = 2.2

    # Bridge Deck (Roadway)
    bd1 = writer.add_vertex(br_x_left, br_deck_y, br_z_start)
    bd2 = writer.add_vertex(br_x_right, br_deck_y, br_z_start)
    bd3 = writer.add_vertex(br_x_right, br_deck_y, br_z_end)
    bd4 = writer.add_vertex(br_x_left, br_deck_y, br_z_end)
    writer.add_face("Road_Asphalt", (bd1, bd2, bd3), n_up)
    writer.add_face("Road_Asphalt", (bd1, bd3, bd4), n_up)

    # Deck bottom (Steel structure)
    bb1 = writer.add_vertex(br_x_left, br_deck_y - br_thickness, br_z_start)
    bb2 = writer.add_vertex(br_x_right, br_deck_y - br_thickness, br_z_start)
    bb3 = writer.add_vertex(br_x_right, br_deck_y - br_thickness, br_z_end)
    bb4 = writer.add_vertex(br_x_left, br_deck_y - br_thickness, br_z_end)
    writer.add_face("Bridge_Steel", (bb1, bb3, bb2), n_down)
    writer.add_face("Bridge_Steel", (bb1, bb4, bb3), n_down)

    # Deck side beams
    writer.add_face("Bridge_Steel", (bb1, bd1, bd4), writer.add_normal(-1, 0, 0))
    writer.add_face("Bridge_Steel", (bb1, bd4, bb4), writer.add_normal(-1, 0, 0))
    writer.add_face("Bridge_Steel", (bb2, bd3, bd2), writer.add_normal(1, 0, 0))
    writer.add_face("Bridge_Steel", (bb2, bb3, bd3), writer.add_normal(1, 0, 0))

    # Bridge Concrete Piers (Supports in river and near embankment)
    pier_z_locations = [-180.0, -90.0, -5.0, 45.0]
    for pz in pier_z_locations:
        p_base_y = terrain_elevation(0, pz)
        p_w = 4.0
        p_d = 5.0
        pw_x1, pw_x2 = -p_w, p_w
        pw_z1, pw_z2 = pz - p_d, pz + p_d

        cp1 = writer.add_vertex(pw_x1, p_base_y, pw_z1)
        cp2 = writer.add_vertex(pw_x2, p_base_y, pw_z1)
        cp3 = writer.add_vertex(pw_x2, p_base_y, pw_z2)
        cp4 = writer.add_vertex(pw_x1, p_base_y, pw_z2)

        tp1 = writer.add_vertex(pw_x1, br_deck_y - br_thickness, pw_z1)
        tp2 = writer.add_vertex(pw_x2, br_deck_y - br_thickness, pw_z1)
        tp3 = writer.add_vertex(pw_x2, br_deck_y - br_thickness, pw_z2)
        tp4 = writer.add_vertex(pw_x1, br_deck_y - br_thickness, pw_z2)

        # 4 faces of pier
        writer.add_face("Bridge_Concrete", (cp1, cp2, tp2), n_south)
        writer.add_face("Bridge_Concrete", (cp1, tp2, tp1), n_south)
        writer.add_face("Bridge_Concrete", (cp2, cp3, tp3), writer.add_normal(1, 0, 0))
        writer.add_face("Bridge_Concrete", (cp2, tp3, tp2), writer.add_normal(1, 0, 0))
        writer.add_face("Bridge_Concrete", (cp3, cp4, tp4), n_north)
        writer.add_face("Bridge_Concrete", (cp3, tp4, tp3), n_north)
        writer.add_face("Bridge_Concrete", (cp4, cp1, tp1), writer.add_normal(-1, 0, 0))
        writer.add_face("Bridge_Concrete", (cp4, tp1, tp4), writer.add_normal(-1, 0, 0))

    # Write files
    write_mtl(out_mtl)
    writer.write_obj(out_obj, "RostovEmbankment.mtl")
    print(f"Success! Model written to:\n  OBJ: {out_obj}\n  MTL: {out_mtl}")
    print(f"Statistics: {len(writer.vertices)} vertices, {sum(len(f) for f in writer.groups.values())} polygons across {len(writer.groups)} materials.")

if __name__ == '__main__':
    main()
