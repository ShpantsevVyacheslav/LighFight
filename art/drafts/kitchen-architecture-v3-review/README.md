# Architecturev3 — REJECTED as final reference-faithful room

Source: `art/source/kitchen-tripo-v1/architecture-original-v3.glb`, SHA256 `47ef72503ba5fd5303a1031a6b239c633cca79ca1304fc1197f72fd43a704ef7`.

Actual untouched GLB:1739754triangles,918426POSITION vertices (provider UI/provenance reports918429). Imported bounds in Blender Z-up: X[−.309128,.309128], Y[−.490326,.490326], Z[0,.630920]. These are provider-normalized units; no real-world scale is inferred.

The generation genuinely contains two walls and a substantial floor, unlike the earlier furniture-only outputs. It fails the visual reference gate: a duplicate blind-like sheet with ragged rods floats inside the room in front of the back wall, while another blind/window feature sits on the right wall. The reference requires a plain back wall and a single right-wall window. The front-left floor corner is diagonally cut rather than rectangular. Do not call this a matching reconstructed room or assemble/promote it as final architecture.

`front.png`, `interior-threequarter.png`, `threequarter.png` (exterior angle), and `top.png` are actual Blender4.2 Cycles CPU renders of the original source. Top view makes the detached interior sheet clear. Original embedded PBR materials/pixels were used; no clay replacement or geometry changes.

Full-triangle BVH floor proof:101×101rays across a5%-margin bounding rectangle yield9981low-floor hits,108misses near the cut corner,112wall/high hits mostly at the right edge. This full-grid test remains RED. Central10.4%-margin89×89subset yields7921/7921floor hits, with no sampled internal floor gaps. This is a finite-grid test, not proof of watertightness/continuous mathematical coverage. Full coordinates and height distribution are retained in floor-grid.json/floor-summary.json; the map marks missing/high samples explicitly.

Source-file hash, original POSITION/NORMAL/UV/index buffer hashes and embedded-image hashes are in audit.json. Imported position/index/UV hashes remained identical after rendering; original file bytes were verified unchanged. No remeshing, decimation, source modification, external API, Unity import, or promotion occurred. Jobs were sequential with fixed6CPUthreads.

Reproduce with installed Blender:

```
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-architecture-v3-review/tools/audit_architecture.py
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/kitchen-architecture-v3-review/tools/render_interior.py
python3 art/drafts/kitchen-architecture-v3-review/tools/summarize_floor.py
```

Next separate pilot: **Tripo furniture + manually reconstructed photo-based architecture**. Preserve downloaded masters; use a clean rectangular floor, two straight beige walls, one right-wall window, and unchanged original-v2 furniture geometry. This is explicitly a hybrid derivative, not another Tripo room generation.
