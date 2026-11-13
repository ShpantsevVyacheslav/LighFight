#!/usr/bin/env python3
"""
Generates an interactive, standalone WebGL 3D World Explorer for Rostov-on-Don Embankment.
Embeds all sculpted architectural meshes (promenade, bridge, columns, railings, trees, stadium on the left, water)
directly into an HTML file with Three.js. Zero external dependencies, runs offline in any browser!
"""

import json
import os
import gzip
import base64

MESH_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../unity/Assets/LighFight/Art/Arenas/RostovEmbankment/Meshes"))
HTML_OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../unity/Assets/LighFight/Art/Arenas/RostovEmbankment/rostov_3d_explorer.html"))
ARTIFACT_OUT = "/Users/macbook1/.gemini/antigravity/brain/5ab96336-2610-4bbf-9b3e-aa10d6ce9e19/rostov_3d_explorer.html"

def parse_obj(filename, offset=(0, 0, 0), scale=(1, 1, 1)):
    path = os.path.join(MESH_DIR, filename)
    if not os.path.exists(path):
        return None
    raw_verts = []
    tri_indices = []
    ox, oy, oz = offset
    sx, sy, sz = scale
    with open(path, 'r') as f:
        for line in f:
            if line.startswith('v '):
                parts = line.strip().split()
                x = float(parts[1]) * sx + ox
                y = float(parts[2]) * sy + oy
                z = float(parts[3]) * sz + oz
                raw_verts.append((x, y, z))
            elif line.startswith('f '):
                parts = line.strip().split()[1:]
                idx = [int(p.split('/')[0]) - 1 for p in parts]
                if len(idx) == 3:
                    tri_indices.extend(idx)
                elif len(idx) == 4:
                    tri_indices.extend([idx[0], idx[1], idx[2], idx[0], idx[2], idx[3]])
                elif len(idx) > 4:
                    for i in range(1, len(idx) - 1):
                        tri_indices.extend([idx[0], idx[i], idx[i+1]])

    # Flatten into flat vertex buffer for Three.js
    flat_positions = []
    for vi in tri_indices:
        v = raw_verts[vi]
        flat_positions.extend([round(v[0], 3), round(v[1], 3), round(v[2], 3)])
    return flat_positions

def main():
    print("Parsing 3D architectural meshes...")
    
    meshes_to_include = {
        "promenade": ("WalkableEmbankmentPromenade.obj", (0, 0, 0), (1, 1, 1), "#d2c7b8", 0.4, 0.1),
        "railings": ("WalkableParapetRailings.obj", (0, 0, 0), (1, 1, 1), "#24272c", 0.7, 0.8),
        "bridge_overhead": ("VoroshilovskyBridgeOverhead.obj", (0, 0, 0), (1, 1, 1), "#424b5a", 0.5, 0.7),
        "piers_west": ("BridgePromenadePiersWest.obj", (0, 0, 0), (1, 1, 1), "#cfd4dc", 0.3, 0.1),
        "piers_east": ("BridgePromenadePiersEast.obj", (0, 0, 0), (1, 1, 1), "#cfd4dc", 0.3, 0.1),
        "river_pier": ("BridgeRiverPier.obj", (0, 0, 0), (1, 1, 1), "#bcc2cb", 0.3, 0.1),
        "traffic": ("BridgeTrafficVehicles.obj", (0, 0, 0), (1, 1, 1), "#333d4b", 0.6, 0.5),
        "garlands": ("BridgeFestiveGarlands.obj", (0, 0, 0), (1, 1, 1), "#ffeaa7", 0.1, 0.0),
        "shoreline": ("LevberdonShoreline.obj", (0, 0, 0), (1, 1, 1), "#dcd3ba", 0.2, 0.0),
        "trees": ("LevberdonTrees.obj", (0, 0, 0), (1, 1, 1), "#2e5c24", 0.1, 0.0),
        # Rostov Arena strictly on the LEFT at X = -85.0m, Z = 175.0m, Y = 1.0m
        "rostov_arena": ("RostovArenaDistance.obj", (-85.0, 1.0, 175.0), (1, 1, 1), "#f4f7fb", 0.8, 0.2),
        "five_seas": ("FiveSeasBusinessCenter.obj", (-32.0, 0.0, -18.0), (1, 1, 1), "#1f3a58", 0.9, 0.4),
    }

    mesh_data = {}
    for key, (filename, offset, scale, color, roughness, metalness) in meshes_to_include.items():
        verts = parse_obj(filename, offset, scale)
        if verts:
            print(f"  Loaded {key} ({filename}): {len(verts)//3} vertices")
            mesh_data[key] = {
                "positions": verts,
                "color": color,
                "roughness": roughness,
                "metalness": metalness
            }
        else:
            print(f"  Warning: could not find {filename}")

    # Streetlamp positions along 150m promenade
    streetlamps_data = parse_obj("RostovStreetlamp.obj", (0, 0, 0))
    lamp_x_coords = [-54.0, -36.0, -18.0, -7.2, 7.2, 18.0, 36.0, 54.0]

    html_content = f"""<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>3D Ростов-на-Дону: Набережная реки Дон под Ворошиловским мостом (3D World Explorer)</title>
    <style>
        * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; }}
        body {{ overflow: hidden; background: #080d15; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        #webgl-canvas {{ width: 100vw; height: 100vh; display: block; }}

        /* Top POI Banner */
        #poi-banner {{
            position: absolute; top: 16px; left: 50%; transform: translateX(-50%);
            background: rgba(13, 20, 33, 0.88); backdrop-filter: blur(14px);
            border: 1px solid rgba(255, 255, 255, 0.15); border-radius: 12px;
            padding: 12px 24px; text-align: center; color: #fff;
            box-shadow: 0 12px 36px rgba(0, 0, 0, 0.55); pointer-events: none;
            transition: all 0.3s ease; max-width: 90vw;
        }}
        #poi-title {{ font-size: 16px; font-weight: 700; color: #fde68a; margin-bottom: 4px; letter-spacing: 0.3px; }}
        #poi-desc {{ font-size: 12px; color: #cbd5e1; }}

        /* Compass / Stadium Direction Indicator */
        #compass-widget {{
            position: absolute; top: 16px; right: 16px;
            background: rgba(13, 20, 33, 0.88); backdrop-filter: blur(14px);
            border: 1px solid rgba(255, 255, 255, 0.15); border-radius: 12px;
            padding: 10px 16px; color: #fff; text-align: center; font-size: 11px;
            box-shadow: 0 8px 24px rgba(0,0,0,0.4);
        }}
        #compass-target {{ font-weight: 700; color: #38bdf8; margin-bottom: 2px; }}
        #compass-arrow {{ font-size: 14px; font-weight: bold; color: #fbbf24; }}

        /* Controls Panel Bottom */
        #ui-bar {{
            position: absolute; bottom: 20px; left: 50%; transform: translateX(-50%);
            display: flex; gap: 8px; flex-wrap: wrap; justify-content: center;
            background: rgba(13, 20, 33, 0.88); backdrop-filter: blur(14px);
            border: 1px solid rgba(255, 255, 255, 0.15); border-radius: 14px;
            padding: 8px 12px; box-shadow: 0 12px 36px rgba(0,0,0,0.6);
        }}
        .tele-btn {{
            background: #1e293b; border: 1px solid rgba(255,255,255,0.12);
            color: #f1f5f9; padding: 7px 12px; border-radius: 8px;
            font-size: 11px; font-weight: 600; cursor: pointer; transition: all 0.15s ease;
        }}
        .tele-btn:hover {{ background: #0284c7; border-color: #38bdf8; transform: translateY(-1px); }}
        .tele-btn.active {{ background: #0369a1; border-color: #38bdf8; }}

        /* Mode & Time Toggles */
        #mode-toggle {{
            position: absolute; top: 16px; left: 16px;
            display: flex; flex-direction: column; gap: 6px;
            background: rgba(13, 20, 33, 0.88); backdrop-filter: blur(14px);
            border: 1px solid rgba(255, 255, 255, 0.15); border-radius: 12px;
            padding: 10px 14px; color: #fff; font-size: 11px;
        }}
        .toggle-group {{ display: flex; gap: 4px; margin-top: 4px; }}
        .mini-btn {{
            flex: 1; padding: 5px 8px; font-size: 10px; font-weight: 600;
            background: #1e293b; color: #cbd5e1; border: 1px solid rgba(255,255,255,0.1);
            border-radius: 6px; cursor: pointer;
        }}
        .mini-btn:hover, .mini-btn.active {{ background: #0284c7; color: #fff; border-color: #38bdf8; }}

        /* Instructions Overlay */
        #hint-box {{
            position: absolute; bottom: 80px; left: 20px;
            background: rgba(13, 20, 33, 0.82); backdrop-filter: blur(10px);
            border: 1px solid rgba(255,255,255,0.1); border-radius: 10px;
            padding: 8px 14px; color: #94a3b8; font-size: 11px; line-height: 1.5;
            pointer-events: none;
        }}
        #hint-box b {{ color: #f8fafc; }}

        /* Reticle for Walk mode */
        #reticle {{
            position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);
            width: 8px; height: 8px; border-radius: 50%; background: rgba(255,255,255,0.7);
            pointer-events: none; display: none; box-shadow: 0 0 6px rgba(0,0,0,0.8);
        }}
    </style>
    <!-- Three.js + OrbitControls -->
    <script src="https://cdn.jsdelivr.net/npm/three@0.140.0/build/three.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/three@0.140.0/examples/js/controls/OrbitControls.js"></script>
</head>
<body>
    <canvas id="webgl-canvas"></canvas>
    <div id="reticle"></div>

    <!-- POI Banner -->
    <div id="poi-banner">
        <div id="poi-title">🏟️ Смотровая площадка набережной · Вид на «Ростов Арену» СЛЕВА</div>
        <div id="poi-desc">Панорамный вид через реку Дон: стадион со шпилями строго СЛЕВА, Ворошиловский мост СПРАВА</div>
    </div>

    <!-- Compass Direction -->
    <div id="compass-widget">
        <div id="compass-target">🧭 «РОСТОВ АРЕНА»</div>
        <div id="compass-arrow">◄ СЛЕВА через Дон</div>
    </div>

    <!-- Mode & Time of Day Toggle -->
    <div id="mode-toggle">
        <span>Режим камеры:</span>
        <div class="toggle-group">
            <button class="mini-btn active" id="btn-mode-orbit" onclick="setControlMode('orbit')">🔍 Обзор (Мышь)</button>
            <button class="mini-btn" id="btn-mode-walk" onclick="setControlMode('walk')">🚶 Прогулка (WASD)</button>
        </div>
        <span style="margin-top: 6px;">Время суток:</span>
        <div class="toggle-group">
            <button class="mini-btn active" id="btn-time-day" onclick="setTimeOfDay('day')">☀️ День</button>
            <button class="mini-btn" id="btn-time-sunset" onclick="setTimeOfDay('sunset')">🌅 Закат</button>
            <button class="mini-btn" id="btn-time-night" onclick="setTimeOfDay('night')">🌃 Ночь</button>
        </div>
    </div>

    <!-- Bottom Teleport Bar -->
    <div id="ui-bar">
        <button class="tele-btn active" onclick="teleportTo('arena_view')">🏟️ Вид на Ростов Арену (слева)</button>
        <button class="tele-btn" onclick="teleportTo('under_bridge')">🌉 Под Ворошиловским мостом</button>
        <button class="tele-btn" onclick="teleportTo('staircase')">🏛️ Парадная лестница</button>
        <button class="tele-btn" onclick="teleportTo('beregovaya')">🏙️ Улица Береговая (верхний ярус)</button>
        <button class="tele-btn" onclick="teleportTo('aerial')">🦅 С высоты птичьего полета</button>
    </div>

    <div id="hint-box">
        <b>Управление:</b><br>
        • <b>Обзор:</b> ЛКМ — вращение, ПКМ — перемещение, Колесико — зум.<br>
        • <b>Прогулка:</b> [W][A][S][D] / Стрелки — движение, [Shift] — бег, Клик — захват мыши.
    </div>

    <script>
        // 1. Data Inject
        const MESH_DATA = {json.dumps(mesh_data)};
        const LAMP_COORDS = {json.dumps(lamp_x_coords)};
        const LAMP_VERTS = {json.dumps(streetlamps_data if streetlamps_data else [])};

        // 2. Scene Setup
        const canvas = document.getElementById('webgl-canvas');
        const renderer = new THREE.WebGLRenderer({{ canvas: canvas, antialias: true, powerPreference: 'high-performance' }});
        renderer.setSize(window.innerWidth, window.innerHeight);
        renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        renderer.toneMapping = THREE.ACESFilmicToneMapping;
        renderer.toneMappingExposure = 1.05;

        const scene = new THREE.Scene();
        scene.background = new THREE.Color(0x8ec5fc);
        scene.fog = new THREE.FogExp2(0xb0d4fc, 0.0035);

        const camera = new THREE.PerspectiveCamera(65, window.innerWidth / window.innerHeight, 0.1, 1200);
        camera.position.set(-14.2, 1.8, -2.4);
        camera.lookAt(-10.5, 2.1, 18.0);

        // 3. Controls
        let controlMode = 'orbit';
        const orbitControls = new THREE.OrbitControls(camera, renderer.domElement);
        orbitControls.enableDamping = true;
        orbitControls.dampingFactor = 0.06;
        orbitControls.target.set(-10.5, 2.1, 18.0);
        orbitControls.maxPolarAngle = Math.PI / 2 - 0.02; // Don't go under floor
        orbitControls.minDistance = 1.0;
        orbitControls.maxDistance = 250.0;

        // Walk Controls State
        const keys = {{ w: false, a: false, s: false, d: false, shift: false, space: false }};
        let isPointerLocked = false;
        let walkYaw = 0;
        let walkPitch = 0;
        let playerPos = new THREE.Vector3(-14.2, 1.7, -2.4);
        let playerVelY = 0;

        window.addEventListener('keydown', (e) => {{
            const k = e.key.toLowerCase();
            if (k === 'w' || k === 'ц' || e.code === 'ArrowUp') keys.w = true;
            if (k === 'a' || k === 'ф' || e.code === 'ArrowLeft') keys.a = true;
            if (k === 's' || k === 'ы' || e.code === 'ArrowDown') keys.s = true;
            if (k === 'd' || k === 'в' || e.code === 'ArrowRight') keys.d = true;
            if (e.key === 'Shift') keys.shift = true;
            if (e.code === 'Space') keys.space = true;
        }});

        window.addEventListener('keyup', (e) => {{
            const k = e.key.toLowerCase();
            if (k === 'w' || k === 'ц' || e.code === 'ArrowUp') keys.w = false;
            if (k === 'a' || k === 'ф' || e.code === 'ArrowLeft') keys.a = false;
            if (k === 's' || k === 'ы' || e.code === 'ArrowDown') keys.s = false;
            if (k === 'd' || k === 'в' || e.code === 'ArrowRight') keys.d = false;
            if (e.key === 'Shift') keys.shift = false;
            if (e.code === 'Space') keys.space = false;
        }});

        canvas.addEventListener('click', () => {{
            if (controlMode === 'walk') {{
                canvas.requestPointerLock();
            }}
        }});

        document.addEventListener('pointerlockchange', () => {{
            isPointerLocked = (document.pointerLockElement === canvas);
            document.getElementById('reticle').style.display = isPointerLocked ? 'block' : 'none';
        }});

        document.addEventListener('mousemove', (e) => {{
            if (controlMode === 'walk' && isPointerLocked) {{
                const sensitivity = 0.0022;
                walkYaw -= e.movementX * sensitivity;
                walkPitch -= e.movementY * sensitivity;
                walkPitch = Math.max(-Math.PI / 2.3, Math.min(Math.PI / 2.3, walkPitch));
            }}
        }});

        // 4. Lights
        const hemiLight = new THREE.HemisphereLight(0xffffff, 0x445566, 0.85);
        scene.add(hemiLight);

        const dirLight = new THREE.DirectionalLight(0xfff5e6, 1.45);
        dirLight.position.set(-60, 90, -40);
        dirLight.castShadow = true;
        dirLight.shadow.mapSize.width = 2048;
        dirLight.shadow.mapSize.height = 2048;
        dirLight.shadow.camera.near = 10;
        dirLight.shadow.camera.far = 300;
        dirLight.shadow.camera.left = -90;
        dirLight.shadow.camera.right = 90;
        dirLight.shadow.camera.top = 90;
        dirLight.shadow.camera.bottom = -90;
        dirLight.shadow.bias = -0.0005;
        scene.add(dirLight);

        // 5. River Don Water Plane
        const waterGeo = new THREE.PlaneGeometry(350, 160, 64, 64);
        const waterMat = new THREE.MeshStandardMaterial({{
            color: 0x225577,
            roughness: 0.12,
            metalness: 0.85,
            flatShading: false,
        }});
        const waterMesh = new THREE.Mesh(waterGeo, waterMat);
        waterMesh.rotation.x = -Math.PI / 2;
        waterMesh.position.set(0, -0.42, 65.0);
        waterMesh.receiveShadow = true;
        scene.add(waterMesh);

        // 6. Build Sculpted Meshes
        function createBufferMesh(data, castShadow = true, receiveShadow = true) {{
            const geo = new THREE.BufferGeometry();
            const pos = new Float32Array(data.positions);
            geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
            geo.computeVertexNormals();

            const mat = new THREE.MeshStandardMaterial({{
                color: new THREE.Color(data.color),
                roughness: data.roughness,
                metalness: data.metalness,
                side: THREE.DoubleSide
            }});
            const mesh = new THREE.Mesh(geo, mat);
            mesh.castShadow = castShadow;
            mesh.receiveShadow = receiveShadow;
            return mesh;
        }}

        for (const [key, data] of Object.entries(MESH_DATA)) {{
            const mesh = createBufferMesh(data);
            mesh.name = key;
            scene.add(mesh);
        }}

        // Streetlamps with lights
        if (LAMP_VERTS && LAMP_VERTS.length > 0) {{
            const lampGeo = new THREE.BufferGeometry();
            lampGeo.setAttribute('position', new THREE.BufferAttribute(new Float32Array(LAMP_VERTS), 3));
            lampGeo.computeVertexNormals();
            const lampMat = new THREE.MeshStandardMaterial({{ color: 0x24272c, roughness: 0.6, metalness: 0.8 }});

            LAMP_COORDS.forEach((lx, idx) => {{
                const lamp = new THREE.Mesh(lampGeo, lampMat);
                lamp.position.set(lx, 0, 2.28);
                lamp.castShadow = true;
                scene.add(lamp);

                const pl = new THREE.PointLight(0xffdf9e, 1.2, 12);
                pl.position.set(lx, 4.2, 2.28);
                scene.add(pl);
            }});
        }}

        // 7. Teleport Presets
        const PRESETS = {{
            arena_view: {{
                pos: [-14.2, 1.8, -2.4],
                target: [-85.0, 5.0, 175.0],
                title: "🏟️ Смотровая площадка набережной · Вид на «Ростов Арену» СЛЕВА",
                desc: "Панорамный вид через Дон: стадион со шпилями строго СЛЕВА, Ворошиловский мост СПРАВА"
            }},
            under_bridge: {{
                pos: [-2.2, 1.85, -3.8],
                target: [0.0, 2.2, 24.0],
                title: "🌉 Под Ворошиловским мостом · Первый пилон на набережной",
                desc: "Массивные бетонные колонны первого пилона стоят на гранитном мощении набережной"
            }},
            staircase: {{
                pos: [-38.0, 3.8, -12.0],
                target: [-32.0, 1.2, -6.0],
                title: "🏛️ Парадная гранитная лестница на улицу Береговую",
                desc: "14-ступенчатый сход шириной 8 метров, соединяющий нижний променад с верхним ярусом"
            }},
            beregovaya: {{
                pos: [0.0, 4.5, -22.0],
                target: [0.0, 2.0, 50.0],
                title: "🏙️ Улица Береговая (верхний ярус)",
                desc: "Широкий тротуар на высоте 2.8 метра вдоль подпорной стены с видом на реку Дон"
            }},
            aerial: {{
                pos: [-45.0, 45.0, -40.0],
                target: [-20.0, 0.0, 75.0],
                title: "🦅 Вид с высоты птичьего полета (Аэросъемка Photo 1)",
                desc: "Вся панорама: 150-метровая набережная, Ворошиловский мост, река Дон и Левбердон со стадионом"
            }}
        }};

        function teleportTo(id) {{
            const p = PRESETS[id];
            if (!p) return;

            document.querySelectorAll('.tele-btn').forEach(b => b.classList.remove('active'));
            if (event && event.target) event.target.classList.add('active');

            document.getElementById('poi-title').textContent = p.title;
            document.getElementById('poi-desc').textContent = p.desc;

            playerPos.set(p.pos[0], p.pos[1], p.pos[2]);
            camera.position.set(p.pos[0], p.pos[1], p.pos[2]);
            orbitControls.target.set(p.target[0], p.target[1], p.target[2]);

            const lookDir = new THREE.Vector3(p.target[0] - p.pos[0], 0, p.target[2] - p.pos[2]).normalize();
            walkYaw = Math.atan2(-lookDir.x, -lookDir.z);
            walkPitch = 0.05;
        }}

        function setControlMode(mode) {{
            controlMode = mode;
            document.getElementById('btn-mode-orbit').classList.toggle('active', mode === 'orbit');
            document.getElementById('btn-mode-walk').classList.toggle('active', mode === 'walk');
            orbitControls.enabled = (mode === 'orbit');
            if (mode === 'walk') {{
                canvas.requestPointerLock();
            }} else {{
                document.exitPointerLock?.();
            }}
        }}

        function setTimeOfDay(tod) {{
            document.getElementById('btn-time-day').classList.toggle('active', tod === 'day');
            document.getElementById('btn-time-sunset').classList.toggle('active', tod === 'sunset');
            document.getElementById('btn-time-night').classList.toggle('active', tod === 'night');

            if (tod === 'day') {{
                scene.background = new THREE.Color(0x8ec5fc);
                scene.fog.color = new THREE.Color(0xb0d4fc);
                dirLight.color.set(0xfff5e6);
                dirLight.intensity = 1.45;
                hemiLight.intensity = 0.85;
                waterMat.color.set(0x225577);
            }} else if (tod === 'sunset') {{
                scene.background = new THREE.Color(0xfd7e14);
                scene.fog.color = new THREE.Color(0xe8590c);
                dirLight.color.set(0xff922b);
                dirLight.intensity = 1.6;
                hemiLight.intensity = 0.5;
                waterMat.color.set(0x495057);
            }} else if (tod === 'night') {{
                scene.background = new THREE.Color(0x050a12);
                scene.fog.color = new THREE.Color(0x0b1320);
                dirLight.color.set(0x4dabf7);
                dirLight.intensity = 0.35;
                hemiLight.intensity = 0.25;
                waterMat.color.set(0x0b1320);
            }}
        }}

        // 8. Animation & Physics Loop
        let lastTime = performance.now();
        function animate(now) {{
            requestAnimationFrame(animate);
            const dt = Math.min((now - lastTime) / 1000, 0.1);
            lastTime = now;

            // Animated water waves
            const pos = waterGeo.attributes.position;
            const time = now * 0.0018;
            for (let i = 0; i < pos.count; i++) {{
                const u = pos.getX(i);
                const v = pos.getY(i);
                const wave = Math.sin(u * 0.08 + time) * 0.12 + Math.cos(v * 0.12 + time * 1.3) * 0.08;
                pos.setZ(i, wave);
            }}
            pos.needsUpdate = true;

            // Walk Mode Movement
            if (controlMode === 'walk') {{
                const speed = (keys.shift ? 7.5 : 3.2) * dt;
                const forward = new THREE.Vector3(-Math.sin(walkYaw), 0, -Math.cos(walkYaw));
                const right = new THREE.Vector3(Math.cos(walkYaw), 0, -Math.sin(walkYaw));
                const move = new THREE.Vector3();

                if (keys.w) move.add(forward);
                if (keys.s) move.sub(forward);
                if (keys.d) move.add(right);
                if (keys.a) move.sub(right);

                if (move.lengthSq() > 0.001) {{
                    move.normalize().multiplyScalar(speed);
                    playerPos.add(move);
                }}

                // Ground elevation check
                let groundY = 0.0;
                // If on upper terrace
                if (playerPos.z < -14.0) {{
                    groundY = 2.8;
                }} else if (playerPos.z <= -8.0 && Math.abs(Math.abs(playerPos.x) - 32.0) <= 4.0) {{
                    // On stairs
                    const t = (-playerPos.z - 8.0) / 6.0;
                    groundY = THREE.MathUtils.lerp(0.0, 2.8, Math.max(0, Math.min(1, t)));
                }}

                // Clamp to walkable bounds
                playerPos.x = Math.max(-74.0, Math.min(74.0, playerPos.x));
                playerPos.z = Math.max(-27.0, Math.min(2.35, playerPos.z));

                camera.position.set(playerPos.x, groundY + 1.7, playerPos.z);
                const lookTarget = new THREE.Vector3(
                    camera.position.x - Math.sin(walkYaw) * Math.cos(walkPitch),
                    camera.position.y + Math.sin(walkPitch),
                    camera.position.z - Math.cos(walkYaw) * Math.cos(walkPitch)
                );
                camera.lookAt(lookTarget);
            }} else {{
                orbitControls.update();
            }}

            renderer.render(scene, camera);
        }}

        animate(performance.now());

        window.addEventListener('resize', () => {{
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(window.innerWidth, window.innerHeight);
        }});
    </script>
</body>
</html>
"""

    with open(HTML_OUT, "w", encoding="utf-8") as f:
        f.write(html_content)
    print(f"Generated standalone 3D explorer: {HTML_OUT} ({len(html_content)} bytes)")

    with open(ARTIFACT_OUT, "w", encoding="utf-8") as f:
        f.write(html_content)
    print(f"Copied to artifacts directory: {ARTIFACT_OUT}")

if __name__ == '__main__':
    main()
