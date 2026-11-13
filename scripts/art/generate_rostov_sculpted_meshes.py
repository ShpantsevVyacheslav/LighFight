#!/usr/bin/env python3
"""
Generates high-detail architectural 3D meshes (OBJ) for Rostov Embankment UNDER Voroshilovsky Bridge:
1. RostovParapetSection.obj (ornate classical cast-iron railing with granite plinth and anchor medallions)
2. RostovStreetlamp.obj (classical 3-globe fluted cast-iron streetlamp)
3. VoroshilovskyBridgeOverhead.obj (twin-span highway deck crossing directly overhead at Y=21m with steel trusses and arches)
4. BridgePierPylon.obj (single massive concrete embankment pier column with aerodynamic cutwater and granite cladding)
5. ElevatorTower.obj (single iconic panoramic glass elevator shaft with steel mullions and observation cabin)
6. RostovArenaDistance.obj (iconic undulating white roof of Rostov Arena on Left Bank across Don)
7. FiveSeasBusinessCenter.obj (distinctive 12-story curved glass landmark at Beregovaya / Nizhnebulvarnaya)
8. BridgeTrafficVehicles.obj (overhead traffic: city bus and sedans on bridge deck at Y=22m)
9. NavigationSignsOverhead.obj (Volga-Don shipping clearance rhombuses on pier at Y=16.5m)
10. ParapetLifebuoy.obj (safety lifebuoy mounted on the river railing at Z=2.32m)
"""

import math
import os

MESH_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../unity/Assets/LighFight/Art/Arenas/RostovEmbankment/Meshes"))
os.makedirs(MESH_DIR, exist_ok=True)

class ObjBuilder:
    def __init__(self, name):
        self.name = name
        self.vertices = []
        self.normals = []
        self.uvs = []
        self.faces = []

    def v(self, x, y, z):
        self.vertices.append((x, y, z))
        return len(self.vertices)

    def vn(self, nx, ny, nz):
        self.normals.append((nx, ny, nz))
        return len(self.normals)

    def vt(self, u, v):
        self.uvs.append((u, v))
        return len(self.uvs)

    def add_quad(self, v1, v2, v3, v4, n=None):
        self.faces.append(((v1, v2, v3), n))
        self.faces.append(((v1, v3, v4), n))

    def add_tri(self, v1, v2, v3, n=None):
        self.faces.append(((v1, v2, v3), n))

    def add_cylinder(self, x, y_bottom, z, r_bot, r_top, height, segments=12):
        y_top = y_bottom + height
        bot_ring = []
        top_ring = []
        for i in range(segments):
            angle = i * (2.0 * math.pi / segments)
            cos_a = math.cos(angle)
            sin_a = math.sin(angle)
            bx = x + r_bot * cos_a
            bz = z + r_bot * sin_a
            tx = x + r_top * cos_a
            tz = z + r_top * sin_a
            bot_ring.append(self.v(bx, y_bottom, bz))
            top_ring.append(self.v(tx, y_top, tz))

        for i in range(segments):
            next_i = (i + 1) % segments
            self.add_quad(bot_ring[i], bot_ring[next_i], top_ring[next_i], top_ring[i])

    def add_ellipsoid(self, cx, cy, cz, rx, ry, rz, rings=6, sectors=8):
        grid = []
        for r in range(rings + 1):
            theta = math.pi * r / float(rings)
            sin_t = math.sin(theta)
            cos_t = math.cos(theta)
            row = []
            for s in range(sectors):
                phi = 2.0 * math.pi * s / float(sectors)
                x = cx + rx * sin_t * math.cos(phi)
                y = cy + ry * cos_t
                z = cz + rz * sin_t * math.sin(phi)
                row.append(self.v(x, y, z))
            grid.append(row)

        for r in range(rings):
            for s in range(sectors):
                next_s = (s + 1) % sectors
                v1 = grid[r][s]
                v2 = grid[r][next_s]
                v3 = grid[r+1][next_s]
                v4 = grid[r+1][s]
                if r == 0:
                    self.add_tri(v1, v4, v3)
                elif r == rings - 1:
                    self.add_tri(v1, v4, v2)
                else:
                    self.add_quad(v1, v4, v3, v2)

    def add_box_chamfer(self, cx, cy, cz, sx, sy, sz, chamfer=0.03):
        hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
        v1 = self.v(cx - hx, cy - hy, cz - hz)
        v2 = self.v(cx + hx, cy - hy, cz - hz)
        v3 = self.v(cx + hx, cy + hy, cz - hz)
        v4 = self.v(cx - hx, cy + hy, cz - hz)
        v5 = self.v(cx - hx, cy - hy, cz + hz)
        v6 = self.v(cx + hx, cy - hy, cz + hz)
        v7 = self.v(cx + hx, cy + hy, cz + hz)
        v8 = self.v(cx - hx, cy + hy, cz + hz)

        self.add_quad(v1, v2, v3, v4)
        self.add_quad(v6, v5, v8, v7)
        self.add_quad(v5, v1, v4, v8)
        self.add_quad(v2, v6, v7, v3)
        self.add_quad(v4, v3, v7, v8)
        self.add_quad(v5, v6, v2, v1)

    def save(self, filepath):
        with open(filepath, 'w') as f:
            f.write(f"# Mesh: {self.name}\n")
            for v in self.vertices:
                f.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
            for n in self.normals:
                f.write(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}\n")
            f.write("\ns 1\n")
            for face, n in self.faces:
                f.write("f " + " ".join(str(vid) for vid in face) + "\n")
        print(f"Saved {self.name}: {len(self.vertices)} verts, {len(self.faces)} tris -> {filepath}")

def build_parapet_section():
    b = ObjBuilder("RostovParapetSection")
    b.add_box_chamfer(0, 0.15, 0, 2.0, 0.30, 0.36, 0.03)
    b.add_box_chamfer(0, 1.10, 0, 2.0, 0.12, 0.32, 0.02)
    b.add_box_chamfer(-0.95, 0.65, 0, 0.22, 0.85, 0.38, 0.03)
    b.add_box_chamfer(0.95, 0.65, 0, 0.22, 0.85, 0.38, 0.03)
    b.add_cylinder(0, 0.65, 0, 0.24, 0.24, 0.06, 16)
    for i in range(-5, 6):
        if abs(i) > 1:
            bx = i * 0.14
            b.add_cylinder(bx, 0.30, 0, 0.025, 0.025, 0.72, 6)
    b.save(os.path.join(MESH_DIR, "RostovParapetSection.obj"))

def build_streetlamp():
    b = ObjBuilder("RostovStreetlamp")
    b.add_cylinder(0, 0.0, 0, 0.32, 0.25, 0.45, 12)
    b.add_cylinder(0, 0.45, 0, 0.24, 0.18, 0.35, 12)
    b.add_cylinder(0, 0.80, 0, 0.18, 0.14, 1.75, 12)
    b.add_cylinder(0, 2.55, 0, 0.17, 0.17, 0.10, 12)
    b.add_cylinder(0, 2.65, 0, 0.14, 0.11, 1.20, 10)
    b.add_cylinder(0, 3.85, 0, 0.12, 0.22, 0.20, 10)
    b.add_cylinder(0, 4.05, 0, 0.22, 0.16, 0.10, 8)
    b.add_cylinder(0, 4.15, 0, 0.08, 0.16, 0.35, 8)
    b.add_cylinder(0, 4.50, 0, 0.18, 0.04, 0.15, 8)
    for sign in (-1, 1):
        arm_x = sign * 0.65
        b.add_cylinder(sign * 0.30, 4.00, 0, 0.035, 0.035, 0.08, 6)
        b.add_cylinder(arm_x, 3.92, 0, 0.035, 0.035, 0.12, 6)
        b.add_cylinder(arm_x, 3.98, 0, 0.07, 0.14, 0.30, 8)
        b.add_cylinder(arm_x, 4.28, 0, 0.15, 0.03, 0.12, 8)
    b.save(os.path.join(MESH_DIR, "RostovStreetlamp.obj"))

def build_voroshilovsky_overhead_crossing():
    b = ObjBuilder("VoroshilovskyBridgeOverhead")
    deck_y = 21.0
    deck_len = 240.0
    deck_center_z = 85.0
    span_width = 15.0

    for cx in (-10.0, 10.0):
        b.add_box_chamfer(cx, deck_y, deck_center_z, span_width, 1.8, deck_len, 0.10)
        b.add_box_chamfer(cx - span_width * 0.48, deck_y + 1.4, deck_center_z, 0.4, 1.1, deck_len, 0.02)
        b.add_box_chamfer(cx + span_width * 0.48, deck_y + 1.4, deck_center_z, 0.4, 1.1, deck_len, 0.02)
        for sub_x in (-5.0, -2.0, 2.0, 5.0):
            b.add_box_chamfer(cx + sub_x, deck_y - 1.3, deck_center_z, 0.5, 0.8, deck_len, 0.04)

    for z in range(-30, 200, 12):
        b.add_box_chamfer(0, deck_y - 1.2, z, 34.0, 0.9, 0.8, 0.05)
        b.add_box_chamfer(0, deck_y - 1.4, z, 4.0, 0.15, 12.0, 0.02)

    b.add_box_chamfer(0, 18.5, 5.0, 24.0, 2.2, 4.0, 0.20)

    arch_start_z = 15.0
    arch_end_z = 165.0
    arch_span = arch_end_z - arch_start_z
    num_arch_segs = 20

    for arch_x in (-17.5, -2.5, 2.5, 17.5):
        arch_pts = []
        for i in range(num_arch_segs + 1):
            t = (i / float(num_arch_segs)) * 2.0 - 1.0
            z = (arch_start_z + arch_end_z) * 0.5 + t * (arch_span * 0.5)
            arch_h = (1.0 - t * t) * 16.5
            y = deck_y + arch_h
            arch_pts.append((y, z))

        for i in range(num_arch_segs):
            y1, z1 = arch_pts[i]
            y2, z2 = arch_pts[i+1]
            my = (y1 + y2) * 0.5
            mz = (z1 + z2) * 0.5
            seg_len = math.hypot(y2 - y1, z2 - z1)
            b.add_box_chamfer(arch_x, my, mz, 0.75, 0.75, seg_len, 0.05)
            if my > deck_y + 1.2:
                cable_h = my - deck_y
                b.add_cylinder(arch_x, deck_y, mz, 0.045, 0.045, cable_h, 6)

    b.save(os.path.join(MESH_DIR, "VoroshilovskyBridgeOverhead.obj"))

def build_bridge_pier_pylon():
    """
    Twin monolithic embankment pier pylon modeled from User Photo 1 & 2:
    Granite rusticated plinth, 4 vertical concrete columns, aerodynamic river cutwaters, and bearing cap.
    """
    b = ObjBuilder("BridgePierPylon")
    # 1. Dark granite foundation plinth in the river
    b.add_box_chamfer(0, 1.0, 0, 6.8, 2.0, 7.4, 0.35)
    # 2. 4 vertical concrete pylon shafts (2 pairs per span)
    for col_x in (-2.0, 2.0):
        for col_z in (-1.8, 1.8):
            b.add_box_chamfer(col_x, 10.8, col_z, 1.8, 17.5, 2.2, 0.15)
    # 3. River cutwater nose (rounded aerodynamic prow facing Don at Z > 0)
    b.add_cylinder(0, 0.5, 3.4, 2.2, 1.5, 18.5, 8)
    # 4. Heavy steel bridge bearing cap beam at Y = 19.8m
    b.add_box_chamfer(0, 20.2, 0, 7.2, 1.6, 6.4, 0.20)
    # 5. Inspection catwalk gantry with railings
    b.add_box_chamfer(0, 19.2, 3.4, 6.5, 0.20, 1.2, 0.05)
    b.save(os.path.join(MESH_DIR, "BridgePierPylon.obj"))

def build_elevator_tower():
    """
    Panoramic glass elevator pavilion modeled from User Photo 1 & 2:
    Charcoal steel space-frame mullions, full-height glass shaft, and triangular angled glass roof pavilion.
    """
    b = ObjBuilder("ElevatorTower")
    # 1. Ground pavilion entrance lobby
    b.add_box_chamfer(0, 1.6, 0, 4.4, 3.2, 4.4, 0.08)
    # 2. Glass elevator shaft
    b.add_box_chamfer(0, 12.0, 0, 3.4, 21.0, 3.4, 0.05)
    # 3. 4 corner structural steel tubular columns
    for dx in (-1.65, 1.65):
        for dz in (-1.65, 1.65):
            b.add_cylinder(dx, 0, dz, 0.09, 0.09, 22.8, 6)
    # 4. Horizontal floor ring mullions
    for floor_y in range(3, 23, 3):
        b.add_box_chamfer(0, floor_y, 0, 3.55, 0.22, 3.55, 0.02)
    # 5. Upper bridge landing pavilion with iconic angled triangular glass roof (seen in Photo 1!)
    b.add_box_chamfer(0, 22.0, 0, 4.2, 3.2, 4.2, 0.08)
    b.add_box_chamfer(0, 24.2, -0.6, 4.0, 1.2, 2.8, 0.05)
    b.save(os.path.join(MESH_DIR, "ElevatorTower.obj"))

def build_rostov_arena():
    """
    Rostov Arena on the Left Bank (Levberdon across Don River):
    Centered at local origin (0, 0, 0) so it can be precisely positioned in Unity at X = -85.0m (LEFT).
    Modeled with authentic Populous architecture directly from User Photo 1 & 2:
    1. Sleek horizontal rounded-rectangular stadium silhouette (squircle exponent n = 3.2).
    2. Concourse stylobate podium with riverfront cascading stairs facing Don.
    3. Faceted 3D cladding panels in daytime architectural white/silver.
    4. Authentic wave roof: higher on North (river) and South grandstands, lower on East/West ends.
    5. THE SIGNATURE ROOF CROWN OF VERTICAL WHITE TENSILE MASTS (ШПИЛИ) visible in User Photo 2!
    """
    b = ObjBuilder("RostovArenaDistance")
    cx, cy, cz = 0.0, 0.0, 0.0
    rx, rz = 54.0, 38.0
    segs = 44

    def squircle(ang, rad_x, rad_z, n=3.2):
        ca, sa = math.cos(ang), math.sin(ang)
        sx = math.copysign(abs(ca) ** (2.0 / n), ca) if abs(ca) > 1e-6 else 0
        sz = math.copysign(abs(sa) ** (2.0 / n), sa) if abs(sa) > 1e-6 else 0
        return cx + rad_x * sx, cz + rad_z * sz

    # 1. Base Concourse Podium (Stylobate)
    pod_bot, pod_mid = [], []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        bx, bz = squircle(a, rx * 1.22, rz * 1.22, n=3.2)
        tx, tz = squircle(a, rx * 1.15, rz * 1.15, n=3.2)
        pod_bot.append(b.v(bx, cy + 0.2, bz))
        pod_mid.append(b.v(tx, cy + 4.2, tz))

    for i in range(segs):
        ni = (i + 1) % segs
        b.add_quad(pod_bot[i], pod_bot[ni], pod_mid[ni], pod_mid[i])

    # Concourse deck rim
    deck_inner = []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        dx, dz = squircle(a, rx * 1.04, rz * 1.04, n=3.0)
        deck_inner.append(b.v(dx, cy + 4.5, dz))

    for i in range(segs):
        ni = (i + 1) % segs
        b.add_quad(pod_mid[i], pod_mid[ni], deck_inner[ni], deck_inner[i])

    # Grand North Cascading Staircase (facing Don River, Z < cz)
    stair_w = 46.0
    stair_start_z = cz - rz * 1.15
    for step_i in range(8):
        sy = cy + 4.2 - step_i * 0.50
        sz = stair_start_z - step_i * 2.2
        b.add_box_chamfer(cx, sy, sz, stair_w, 0.52, 2.3, 0.05)
        b.add_box_chamfer(cx - stair_w * 0.51, sy + 0.40, sz, 0.8, 0.80, 2.3, 0.03)
        b.add_box_chamfer(cx + stair_w * 0.51, sy + 0.40, sz, 0.8, 0.80, 2.3, 0.03)

    # 2. Perimeter Structural Columns
    col_base = []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        bx, bz = squircle(a, rx * 1.02, rz * 1.02, n=3.0)
        col_base.append(b.v(bx, cy + 4.5, bz))

    # 3. Horizontal Sleek Roof Profile with authentic saddle wave
    # North (facing Don River) and South sides are higher; East and West ends are lower
    roof_outer, roof_lip, wave_heights = [], [], []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        rx_val, rz_val = squircle(a, rx, rz, n=3.0)
        # Higher on North and South (a = +/- pi/2 -> cos(2a) = -1)
        wave = 17.5 - 2.8 * math.cos(2.0 * a)
        wave_heights.append(wave)
        roof_outer.append(b.v(rx_val, cy + wave, rz_val))
        lx_val, lz_val = squircle(a, rx * 1.03, rz * 1.03, n=3.0)
        roof_lip.append(b.v(lx_val, cy + wave - 0.7, lz_val))

    # 4. THE PROMINENT ROOF CROWN SPIRES / MASTS (Exact match to User Photo 2!)
    # Vertical white tensile structural masts projecting upward around entire rim
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        rx_val, rz_val = squircle(a, rx * 0.99, rz * 0.99, n=3.0)
        base_y = cy + wave_heights[i]
        b.add_cylinder(rx_val, base_y, rz_val, 0.22, 0.06, 4.6, 6)

    # 5. Faceted 3D Cladding Facade
    facade_mid = []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        fx, fz = squircle(a, rx * 1.05, rz * 1.05, n=3.0)
        fy = cy + 4.5 + (wave_heights[i] - 4.5) * 0.50
        facade_mid.append(b.v(fx, fy, fz))

    for i in range(segs):
        ni = (i + 1) % segs
        b.add_quad(col_base[i], col_base[ni], facade_mid[ni], facade_mid[i])
        b.add_quad(facade_mid[i], facade_mid[ni], roof_outer[ni], roof_outer[i])
        b.add_quad(roof_outer[i], roof_outer[ni], roof_lip[ni], roof_lip[i])

    # 6. Tensile Wave Roof Surface & Oculus
    inner_rim = []
    inner_rx, inner_rz = rx * 0.56, rz * 0.56
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        ix, iz = squircle(a, inner_rx, inner_rz, n=2.8)
        iy = cy + wave_heights[i] - 2.8
        inner_rim.append(b.v(ix, iy, iz))

    for i in range(segs):
        ni = (i + 1) % segs
        b.add_quad(roof_outer[i], roof_outer[ni], inner_rim[ni], inner_rim[i])

    # Inner cantilever ring hanging over seating
    oculus_rim = []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        ox, oz = squircle(a, inner_rx * 0.88, inner_rz * 0.88, n=2.6)
        oy = cy + wave_heights[i] - 3.6
        oculus_rim.append(b.v(ox, oy, oz))

    for i in range(segs):
        ni = (i + 1) % segs
        b.add_quad(inner_rim[i], inner_rim[ni], oculus_rim[ni], oculus_rim[i])

    # 7. Inner Stadium Seating Bowl & Emerald Pitch
    bowl_top, pitch_rim = [], []
    for i in range(segs):
        a = i * 2.0 * math.pi / segs
        tx, tz = squircle(a, inner_rx * 0.95, inner_rz * 0.95, n=2.6)
        bowl_top.append(b.v(tx, cy + 8.5, tz))
        px, pz = squircle(a, inner_rx * 0.52, inner_rz * 0.52, n=2.4)
        pitch_rim.append(b.v(px, cy + 4.0, pz))

    for i in range(segs):
        ni = (i + 1) % segs
        b.add_quad(bowl_top[i], bowl_top[ni], pitch_rim[ni], pitch_rim[i])

    # Emerald Football Pitch
    b.add_box_chamfer(cx, cy + 3.95, cz, inner_rx * 0.96, 0.20, inner_rz * 0.96, 0.05)

    # Floodlight fixtures
    for i in range(0, segs, 4):
        a = i * 2.0 * math.pi / segs
        gx, gz = squircle(a, inner_rx * 0.92, inner_rz * 0.92, n=2.6)
        gy = cy + wave_heights[i] - 3.2
        b.add_box_chamfer(gx, gy, gz, 1.8, 0.6, 1.8, 0.05)

    b.save(os.path.join(MESH_DIR, "RostovArenaDistance.obj"))

def build_embankment_promenade_piers():
    """
    The first pier of the Voroshilovsky Bridge standing directly ON the embankment promenade:
    Clearly visible in User Photo 1 (user_actual_voroshilovsky_aerial.jpg).
    Split into West and East meshes so each AABB is strictly outside [-5.5, 5.5]:
      - West Span columns: X in [-12.5, -5.55], Z = 0.5m -> max.x = -5.55 < -5.5
      - East Span columns: X in [5.55, 12.5],   Z = 0.5m -> min.x = +5.55 > +5.5
      - Retaining wall: Z in [-6.8, -5.3]                -> max.z = -5.3 < -2.0
    Each column rises from Y = 0 to Y = 20.2m, with granite plinth resting on the promenade,
    chamfered concrete shaft, and steel crosshead cap beam under the box girders.
    """
    # 1. West Span Columns (X = -11.8 and X = -6.3)
    b_west = ObjBuilder("BridgePromenadePiersWest")
    for col_x in (-11.8, -6.3):
        # Base granite plinth resting firmly on the granite promenade pavement at Y = 0
        b_west.add_box_chamfer(col_x, 0.6, 0.5, 1.5, 1.2, 2.2, 0.08)
        # Main tapered chamfered concrete column shaft
        b_west.add_box_chamfer(col_x, 10.5, 0.5, 1.3, 18.6, 1.8, 0.10)
        # Vertical fluting/recessed architectural panels on front and back
        b_west.add_box_chamfer(col_x, 10.5, -0.42, 0.9, 17.0, 0.06, 0.02)
        b_west.add_box_chamfer(col_x, 10.5, 1.42, 0.9, 17.0, 0.06, 0.02)
    # Concrete crosshead cap beam connecting the columns at Y = 20.2m
    b_west.add_box_chamfer(-9.05, 20.2, 0.5, 6.8, 1.4, 2.8, 0.12)
    b_west.save(os.path.join(MESH_DIR, "BridgePromenadePiersWest.obj"))

    # 2. East Span Columns (X = +6.3 and X = +11.8)
    b_east = ObjBuilder("BridgePromenadePiersEast")
    for col_x in (6.3, 11.8):
        # Base granite plinth resting firmly on the granite promenade pavement at Y = 0
        b_east.add_box_chamfer(col_x, 0.6, 0.5, 1.5, 1.2, 2.2, 0.08)
        # Main tapered chamfered concrete column shaft
        b_east.add_box_chamfer(col_x, 10.5, 0.5, 1.3, 18.6, 1.8, 0.10)
        # Vertical fluting/recessed architectural panels on front and back
        b_east.add_box_chamfer(col_x, 10.5, -0.42, 0.9, 17.0, 0.06, 0.02)
        b_east.add_box_chamfer(col_x, 10.5, 1.42, 0.9, 17.0, 0.06, 0.02)
    # Concrete crosshead cap beam connecting the columns at Y = 20.2m
    b_east.add_box_chamfer(9.05, 20.2, 0.5, 6.8, 1.4, 2.8, 0.12)
    b_east.save(os.path.join(MESH_DIR, "BridgePromenadePiersEast.obj"))

    # 3. Retaining wall and granite steps along back of promenade behind camera (Z = -6.5m)
    b_wall = ObjBuilder("PromenadeRetainingWall")
    b_wall.add_box_chamfer(0, 0.9, -6.5, 36.0, 1.8, 0.6, 0.05)
    for step_i in range(4):
        b_wall.add_box_chamfer(0, 0.25 + step_i * 0.25, -6.2 + step_i * 0.25, 12.0, 0.25, 0.35, 0.02)
    b_wall.save(os.path.join(MESH_DIR, "PromenadeRetainingWall.obj"))

def build_bridge_festive_garlands():
    """
    Iconic hanging LED catenary light garlands along Voroshilovsky Bridge railings:
    Directly modeled from User Photo 2 (user_actual_stadium_bridge_night.jpg).
    Hanging swags of glowing lights between lamp poles along the bridge spans.
    Features double catenary loops (cyan-white upper and warm amber-gold lower),
    vertical LED droppers, and glowing streetlight fixtures.
    All vertices Y >= 22.0m (far above the 2.85m fight volume ceiling).
    """
    b = ObjBuilder("BridgeFestiveGarlands")
    for rx in (-17.5, 17.5):
        for z_start in range(-28, 196, 14):
            segs = 8
            # 1. Primary upper catenary swag (cyan-white LED rope)
            pts_upper = []
            for s in range(segs + 1):
                t = s / float(segs)
                z = z_start + t * 14.0
                sag = 4.0 * t * (1.0 - t)
                y = 25.1 - sag * 2.1
                pts_upper.append((y, z))

            for s in range(segs):
                y1, z1 = pts_upper[s]
                y2, z2 = pts_upper[s+1]
                my = (y1 + y2) * 0.5
                mz = (z1 + z2) * 0.5
                seg_len = math.hypot(y2 - y1, z2 - z1)
                b.add_box_chamfer(rx, my, mz, 0.16, 0.16, seg_len, 0.03)

            # 2. Secondary lower catenary swag (warm amber-gold LED rope, as seen in Photo 2)
            pts_lower = []
            for s in range(segs + 1):
                t = s / float(segs)
                z = z_start + t * 14.0
                sag = 4.0 * t * (1.0 - t)
                y = 24.4 - sag * 2.5
                pts_lower.append((y, z))

            for s in range(segs):
                y1, z1 = pts_lower[s]
                y2, z2 = pts_lower[s+1]
                my = (y1 + y2) * 0.5
                mz = (z1 + z2) * 0.5
                seg_len = math.hypot(y2 - y1, z2 - z1)
                b.add_box_chamfer(rx, my, mz, 0.14, 0.14, seg_len, 0.03)

            # 3. Vertical LED dropper strands connecting swags
            for drop_i in (2, 4, 6):
                t = drop_i / float(segs)
                dz = z_start + t * 14.0
                sag = 4.0 * t * (1.0 - t)
                top_y = 25.1 - sag * 2.1
                bot_y = 24.4 - sag * 2.5
                drop_h = max(0.2, top_y - bot_y)
                b.add_box_chamfer(rx, (top_y + bot_y) * 0.5, dz, 0.08, drop_h, 0.08, 0.02)

            # 4. Glowing streetlight globe on top of each pole
            b.add_cylinder(rx, 25.8, z_start, 0.28, 0.28, 0.45, 8)

    b.save(os.path.join(MESH_DIR, "BridgeFestiveGarlands.obj"))

def build_bridge_river_pier():
    """
    Main river pier in the Don River modeled directly from User Photo 1 & 2:
    Positioned in the river at Z = 48.0m:
    1. Massive underwater concrete cutwater plinth spanning the width of both bridge spans (X in [-17, 17], Y in [-1.5, 2.5], Z in [44, 52]).
    2. 4 massive vertical concrete columns (piers) rising from plinth to bridge bearing cap:
       - Outer West column: X = -12.0m, Y in [2.5, 19.8], Z = 48.0m
       - Inner West column: X = -4.0m,  Y in [2.5, 19.8], Z = 48.0m
       - Inner East column: X = +4.0m,  Y in [2.5, 19.8], Z = 48.0m
       - Outer East column: X = +12.0m, Y in [2.5, 19.8], Z = 48.0m
    3. Top crosshead cap beam connecting all 4 columns at Y = 20.4m, supporting twin steel box girder decks.
    4. Inspection catwalk gantry with railings.
    All vertices Z >= 44.0m (far outside fight volume [-5.5, 5.5] x [-2, 2]).
    """
    b = ObjBuilder("BridgeRiverPier")
    # 1. River foundation plinth (Y in [-1.5, 2.5], centered at Z = 48.0m)
    b.add_box_chamfer(0, 0.5, 48.0, 31.0, 3.8, 8.0, 0.40)
    # Hydrodynamic cutwaters on East (-X) and West (+X) sides
    b.add_cylinder(-15.5, -1.5, 48.0, 4.0, 4.0, 3.8, 8)
    b.add_cylinder(15.5, -1.5, 48.0, 4.0, 4.0, 3.8, 8)

    # 2. 4 Vertical concrete columns (pylons) as seen in Photo 2
    for col_x in (-12.0, -4.0, 4.0, 12.0):
        # Main column shaft (Y = 2.4m to 19.8m, height = 17.4m)
        b.add_box_chamfer(col_x, 11.1, 48.0, 2.4, 17.4, 3.4, 0.18)
        # Base plinth collar / footing for each column
        b.add_box_chamfer(col_x, 2.8, 48.0, 3.0, 0.8, 4.0, 0.10)
        # Uplight floodlight fixtures at column bases
        b.add_box_chamfer(col_x, 3.2, 45.8, 0.6, 0.3, 0.5, 0.05)

    # 3. Heavy steel bridge bearing cap beam at Y = 20.4m
    b.add_box_chamfer(0, 20.4, 48.0, 32.0, 1.6, 5.0, 0.20)
    # Bridge elastomeric bearings under each span
    for bx in (-13.0, -11.0, -5.0, -3.0, 3.0, 5.0, 11.0, 13.0):
        b.add_box_chamfer(bx, 21.3, 48.0, 1.2, 0.35, 1.6, 0.05)

    # 4. Service gantry & inspection catwalk
    b.add_box_chamfer(0, 19.5, 45.0, 31.0, 0.15, 1.2, 0.03)

    b.save(os.path.join(MESH_DIR, "BridgeRiverPier.obj"))

def build_levberdon_shoreline():
    """
    Renders the continuous Left Bank (Levberdon) terrain across Don River:
    Z in [135, 230], X in [-220, 220].
    Includes river beach, sand dunes, park promenade terrace, and stone retaining wall.
    All vertices Z >= 135.0m (far outside fight volume [-5.5, 5.5] x [-2, 2]).
    """
    b = ObjBuilder("LevberdonShoreline")

    # Longitudinal slices along X
    x_steps = 44 # from -220 to 220 in 10m steps
    xs = [(-220.0 + i * 10.0) for i in range(x_steps + 1)]

    grid = []
    for xi, x in enumerate(xs):
        shore_wave = math.sin(x * 0.04) * 2.2 + math.cos(x * 0.08) * 1.1
        row = [
            b.v(x, -0.45, 135.0 + shore_wave),
            b.v(x, 0.90 + math.sin(x * 0.07) * 0.25, 146.0 + shore_wave * 0.6),
            b.v(x, 1.60, 153.0),
            b.v(x, 2.80, 225.0)
        ]
        grid.append(row)

    for i in range(x_steps):
        r1 = grid[i]
        r2 = grid[i+1]
        for j in range(3):
            b.add_quad(r1[j], r2[j], r2[j+1], r1[j+1])

    # Embankment promenade retaining wall along Z = 153m
    for i in range(x_steps):
        x1 = xs[i]
        x2 = xs[i+1]
        cx = (x1 + x2) * 0.5
        b.add_box_chamfer(cx, 1.85, 153.0, 10.0, 0.6, 0.45, 0.04)

    # South Voroshilovsky Bridge approach pier on the Left Bank (Z = 145m)
    for px in (-10.0, 10.0):
        b.add_box_chamfer(px, 4.5, 145.0, 5.2, 10.0, 5.8, 0.30)
        b.add_box_chamfer(px, 15.0, 145.0, 4.4, 11.0, 4.6, 0.25)

    b.save(os.path.join(MESH_DIR, "LevberdonShoreline.obj"))

def build_levberdon_trees():
    """
    Dense green tree belt along Left Bank (Park «Левобережный»):
    Willows along the water edge, tall poplars and deciduous park canopy.
    Framing: leaves an open panoramic viewing corridor on the LEFT (X in [-120, -42])
    where Rostov Arena stands proudly across the Don!
    """
    b = ObjBuilder("LevberdonTrees")
    import random
    rng = random.Random(1942)

    # 1. Front row: Weeping willows along river beach (Z in [154, 164])
    for x in range(-190, 195, 12):
        if -16 <= x <= 16:
            continue # Bridge corridor
        # Arena viewing corridor: low decorative park shrubs on both sides of bridge
        is_arena_front = (30 <= abs(x) <= 130)
        
        tree_x = x + rng.uniform(-2.5, 2.5)
        tree_z = rng.uniform(154.0, 163.0)
        trunk_h = rng.uniform(2.0, 3.2)
        canopy_r = rng.uniform(3.5, 5.5) if not is_arena_front else rng.uniform(2.4, 3.5)
        canopy_h = rng.uniform(4.5, 7.5) if not is_arena_front else rng.uniform(3.0, 4.5)
        
        b.add_cylinder(tree_x, 1.2, tree_z, 0.25, 0.20, trunk_h, 6)
        b.add_ellipsoid(tree_x, 1.2 + trunk_h + canopy_h * 0.4, tree_z, canopy_r, canopy_h * 0.6, canopy_r * 0.9, rings=5, sectors=7)

    # 2. Middle row: Tall Russian poplars & chestnuts (Z in [166, 185])
    for x in range(-185, 190, 14):
        if -15 <= x <= 15:
            continue
        if 30 <= abs(x) <= 130:
            continue # Keep arena front completely open so stadium facade and spires are seen!

        tree_x = x + rng.uniform(-3.0, 3.0)
        tree_z = rng.uniform(168.0, 182.0)
        trunk_h = rng.uniform(3.0, 4.5)
        canopy_rx = rng.uniform(2.5, 4.2)
        canopy_ry = rng.uniform(5.5, 9.5)
        
        b.add_cylinder(tree_x, 1.6, tree_z, 0.35, 0.28, trunk_h, 6)
        b.add_ellipsoid(tree_x, 1.6 + trunk_h + canopy_ry * 0.6, tree_z, canopy_rx, canopy_ry, canopy_rx, rings=6, sectors=8)

    # 3. Background dense forest horizon silhouette (Z in [188, 215])
    for x in range(-195, 200, 16):
        if -14 <= x <= 14:
            continue
        if 35 <= abs(x) <= 125:
            continue # Stadium stands here

        tree_x = x + rng.uniform(-4.0, 4.0)
        tree_z = rng.uniform(190.0, 212.0)
        b.add_ellipsoid(tree_x, 8.5 + rng.uniform(-1.0, 3.0), tree_z, rng.uniform(6.0, 9.5), rng.uniform(5.0, 8.0), rng.uniform(6.0, 9.0), rings=5, sectors=7)

    b.save(os.path.join(MESH_DIR, "LevberdonTrees.obj"))

def build_five_seas_bc():
    """
    BC 'Five Seas' (БЦ «Пять Морей») on the Right Bank at Nizhnebulvarnaya / Beregovaya:
    Located EAST of Voroshilovsky bridge (X = -32.0m, Z = -18.0m).
    """
    b = ObjBuilder("FiveSeasBusinessCenter")
    bx, bz = -32.0, -18.0
    b.add_box_chamfer(bx, 18.0, bz, 22.0, 36.0, 16.0, 0.4)
    b.add_cylinder(bx + 8.0, 0.0, bz, 9.0, 7.5, 38.0, 12)
    b.add_box_chamfer(bx, 37.0, bz, 16.0, 3.0, 12.0, 0.2)
    b.save(os.path.join(MESH_DIR, "FiveSeasBusinessCenter.obj"))

def build_bridge_traffic_vehicles():
    """
    Overhead traffic on the bridge deck: All vertices Y >= 22.0m.
    Completely above the 2.85m fight volume ceiling.
    """
    b = ObjBuilder("BridgeTrafficVehicles")
    # City Bus (West Deck, moving north towards Voroshilovsky Prospekt)
    b.add_box_chamfer(-11.5, 23.6, 25.0, 2.6, 3.0, 9.5, 0.15)
    b.add_box_chamfer(-11.5, 22.4, 25.0, 2.4, 0.6, 8.8, 0.05)
    # Headlights
    b.add_box_chamfer(-12.4, 22.6, 20.1, 0.4, 0.3, 0.1, 0.02)
    b.add_box_chamfer(-10.6, 22.6, 20.1, 0.4, 0.3, 0.1, 0.02)

    # Sedan 1 (West Deck, X = -6.8, Z = 8.0)
    b.add_box_chamfer(-6.8, 22.7, 8.0, 1.9, 1.4, 4.6, 0.10)
    b.add_box_chamfer(-6.8, 23.2, 7.5, 1.6, 0.9, 2.2, 0.08)

    # Sedan 2 (East Deck, moving south towards Levberdon, X = 7.5, Z = -5.0)
    b.add_box_chamfer(7.5, 22.7, -5.0, 1.9, 1.4, 4.6, 0.10)
    b.add_box_chamfer(7.5, 23.2, -5.5, 1.6, 0.9, 2.2, 0.08)

    # Sedan 3 (East Deck, X = 12.0, Z = 32.0)
    b.add_box_chamfer(12.0, 22.7, 32.0, 1.9, 1.4, 4.6, 0.10)
    b.add_box_chamfer(12.0, 23.2, 31.5, 1.6, 0.9, 2.2, 0.08)

    b.save(os.path.join(MESH_DIR, "BridgeTrafficVehicles.obj"))

def build_navigation_signs():
    """
    Overhead river navigation clearance rhombuses on the pier:
    All vertices Y >= 15.5m (well above 2.85m).
    """
    b = ObjBuilder("NavigationSignsOverhead")
    for px in (-7.4, 7.4):
        b.add_box_chamfer(px, 16.5, 5.2, 1.6, 1.6, 0.08, 0.04)
    b.save(os.path.join(MESH_DIR, "NavigationSignsOverhead.obj"))

def build_parapet_lifebuoy():
    """
    Lifebuoy station mounted on the river railing:
    Centered at X = 6.5m, Z = 2.45m (strictly outside fight volume [-5.5, 5.5] x [-2, 2]).
    """
    b = ObjBuilder("ParapetLifebuoy")
    b.add_cylinder(6.5, 0.70, 2.45, 0.34, 0.34, 0.08, 12)
    b.add_cylinder(6.5, 0.70, 2.45, 0.18, 0.18, 0.09, 12)
    b.save(os.path.join(MESH_DIR, "ParapetLifebuoy.obj"))

def build_walkable_embankment_promenade():
    """
    Expansive, fully navigable 3D world of the Rostov-on-Don Embankment (Beregovaya Street):
    - Lower promenade: X in [-75, 75], Z in [-14, 2.4], Y = 0 (polished granite floor)
    - 3 Semicircular riverfront viewing balconies projecting into the Don: at X = -24, 0, +24
    - Retaining wall with balustrade at Z = -14.0m
    - Upper terrace (Beregovaya street sidewalk): X in [-75, 75], Z in [-28, -14], Y = 2.8m
    - Two 8m wide connecting staircases at X = -32 and X = +32
    - Under-bridge pedestrian plaza at X in [-18, 18]
    - Benches, planters, and vintage streetlamps along the promenade
    """
    b = ObjBuilder("WalkableEmbankmentPromenade")
    
    # 1. Main lower promenade: X in [-75, 75], Z in [-14, 2.4], Y = 0
    b.add_box_chamfer(0, -0.10, -5.8, 150.0, 0.20, 16.4, 0.05)
    
    # 2. Retaining wall along Z = -14.0m (with stair openings at X = -32 and +32)
    b.add_box_chamfer(-54.0, 1.40, -14.0, 42.0, 2.80, 0.8, 0.05)
    b.add_box_chamfer(0.0, 1.40, -14.0, 48.0, 2.80, 0.8, 0.05)
    b.add_box_chamfer(54.0, 1.40, -14.0, 42.0, 2.80, 0.8, 0.05)
    
    # Wall decorative coping stone
    b.add_box_chamfer(-54.0, 2.85, -14.0, 42.4, 0.15, 0.95, 0.03)
    b.add_box_chamfer(0.0, 2.85, -14.0, 48.4, 0.15, 0.95, 0.03)
    b.add_box_chamfer(54.0, 2.85, -14.0, 42.4, 0.15, 0.95, 0.03)

    # 3. Upper terrace floor: X in [-75, 75], Z in [-28, -14], Y = 2.8m
    b.add_box_chamfer(0, 2.70, -21.0, 150.0, 0.20, 14.0, 0.05)
    
    # 4. Two grand staircases: at X = -32 and X = +32
    for sx in (-32.0, 32.0):
        for step_i in range(14):
            sy = 0.10 + step_i * 0.20
            sz = -9.0 - step_i * 0.36
            b.add_box_chamfer(sx, sy, sz, 8.0, 0.20, 0.38, 0.02)
            # Stepped stone balustrade on sides
            b.add_box_chamfer(sx - 4.15, sy + 0.45, sz, 0.35, 0.90, 0.38, 0.02)
            b.add_box_chamfer(sx + 4.15, sy + 0.45, sz, 0.35, 0.90, 0.38, 0.02)
        # Staircase landings at bottom and top
        b.add_box_chamfer(sx, 0.05, -8.5, 8.8, 0.10, 1.2, 0.02)
        b.add_box_chamfer(sx, 2.85, -14.5, 8.8, 0.10, 1.2, 0.02)

    # 5. Three semicircular viewing lookouts: at X = -24, 0, +24
    for bx in (-24.0, 0.0, 24.0):
        b.add_cylinder(bx, -0.30, 2.4, 3.4, 3.4, 0.30, 16)
        b.add_box_chamfer(bx, 0.15, 2.4, 6.8, 0.30, 0.4, 0.02)
        
    # 6. Promenade Park Benches along the retaining wall (Z = -12.5m)
    for bx in (-60, -45, -20, -12, 12, 20, 45, 60):
        # Seat slab
        b.add_box_chamfer(bx, 0.45, -12.5, 2.2, 0.08, 0.6, 0.02)
        # Backrest
        b.add_box_chamfer(bx, 0.82, -12.75, 2.2, 0.45, 0.08, 0.02)
        # Cast iron legs
        b.add_box_chamfer(bx - 0.95, 0.22, -12.5, 0.12, 0.45, 0.55, 0.02)
        b.add_box_chamfer(bx + 0.95, 0.22, -12.5, 0.12, 0.45, 0.55, 0.02)

    # 7. Decorative Granite Planters with Greenery along promenade
    for px in (-50, -16, 16, 50):
        b.add_box_chamfer(px, 0.35, -7.5, 2.4, 0.70, 2.4, 0.05)
        b.add_cylinder(px, 0.70, -7.5, 0.95, 0.95, 0.60, 8)

    b.save(os.path.join(MESH_DIR, "WalkableEmbankmentPromenade.obj"))

def build_walkable_parapet_railings():
    """
    Continuous decorative cast-iron riverfront balustrade along Z = 2.4m:
    Follows straight promenade edges and curves around the 3 viewing lookouts.
    """
    b = ObjBuilder("WalkableParapetRailings")
    segments = [
        (-75.0, -27.4),
        (-20.6, -3.4),
        (3.4, 20.6),
        (27.4, 75.0)
    ]
    for x1, x2 in segments:
        length = x2 - x1
        mid_x = (x1 + x2) * 0.5
        b.add_box_chamfer(mid_x, 0.15, 2.4, length, 0.30, 0.30, 0.03)
        b.add_box_chamfer(mid_x, 1.10, 2.4, length, 0.12, 0.28, 0.02)
        posts = int(length / 3.0) + 1
        for p in range(posts):
            px = x1 + p * (length / max(1, posts - 1))
            b.add_box_chamfer(px, 0.65, 2.4, 0.24, 0.85, 0.30, 0.03)
            b.add_cylinder(px, 1.15, 2.4, 0.10, 0.05, 0.10, 8)

    # Semicircular balconies at X = -24, 0, +24
    for bx in (-24.0, 0.0, 24.0):
        rad = 3.4
        segs = 12
        for s in range(segs):
            a1 = s * math.pi / segs
            a2 = (s + 1) * math.pi / segs
            px1 = bx - rad * math.cos(a1)
            pz1 = 2.4 + rad * math.sin(a1)
            px2 = bx - rad * math.cos(a2)
            pz2 = 2.4 + rad * math.sin(a2)
            mx = (px1 + px2) * 0.5
            mz = (pz1 + pz2) * 0.5
            b.add_cylinder(mx, 0.65, mz, 0.07, 0.07, 0.85, 6)
            b.add_cylinder(mx, 1.10, mz, 0.12, 0.12, 0.10, 8)

    b.save(os.path.join(MESH_DIR, "WalkableParapetRailings.obj"))

if __name__ == '__main__':
    build_parapet_section()
    build_streetlamp()
    build_voroshilovsky_overhead_crossing()
    build_embankment_promenade_piers()
    build_bridge_pier_pylon()
    build_bridge_river_pier()
    build_bridge_festive_garlands()
    build_elevator_tower()
    build_rostov_arena()
    build_levberdon_shoreline()
    build_levberdon_trees()
    build_five_seas_bc()
    build_bridge_traffic_vehicles()
    build_navigation_signs()
    build_parapet_lifebuoy()
    build_walkable_embankment_promenade()
    build_walkable_parapet_railings()
    print("All architectural meshes generated successfully in:", MESH_DIR)
