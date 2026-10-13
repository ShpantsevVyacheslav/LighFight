# Continuous spatial weight field — full-density Rose

**Verdict: acceptable shoulders/waist in the reviewed native box/kick poses; ready for isolated Unity integration QA.** This supersedes the rejection of the earlier transfer candidates. It does not establish performance or all-animation acceptance. No main project files or original sources were edited, no paid action/upload occurred, and nothing was promoted.

## Deliverables

- `rose-original-dense-native-spatial.blend`: full original dense mesh, unchanged original GLTF material/packed textures, native 67-bone skeleton and all original imported action datablocks.
- `rose-original-dense-native-spatial-rig.fbx`: rig-only interchange export, 110,217,612 bytes. Native animation clips remain in the original provider FBX and the candidate blend.
- `rose-h31-original_basecolor.jpg`, `rose-h31-original_rm.jpg`, `rose-h31-original_normal.png`: all original **4096×4096** image bytes, copied directly from GLB packed buffers. SHA256s match the extracted `original-image-*` files.
- FBX `.fbm` folder contains byte-identical basecolor and normal image copies. **The packed RM map requires explicit material setup**: GLTF G=roughness, B=metallic. FBX cannot represent the original GLTF channel-separation shader faithfully. The blend's original source material remains intact.

## Method

The validated original and native donor rest surfaces remain in their native aligned coordinates. A tapered Gaussian field reconstructs weights from the 48 nearest donor rest vertices. Bandwidth is fixed at **0.007 original height**. The furthest neighbor contributes zero through a quadratic radial taper, avoiding hard neighbor cutoff jumps. Weights are normalized; values below 1e-6 are discarded. Regions with stable head/neck/hand/finger/foot/toe influence preserve the earlier donor-triangle interpolation, with a smooth transition into the spatial field. This preserves native facial/hand behavior while stabilizing shoulder, torso, waist, and leg weight gradients.

Only skin weights changed. No vertex coordinate edits, remesh, actual welding, UV edits, bone relocation, corrective shape keys, or Blender-only deformation modifier was introduced. The armature modifier uses linear skinning without Preserve Volume.

## Preservation and interchange checks

Reopened blend (`spatial-reloaded-verification.json`) preserves exact original float32 position and corner-UV hashes, triangle-index hash, and native bone rest-matrix hash:

- **952,961 vertices / 1,865,580 triangles / 5,596,740 corners**.
- Native **67 bone bind matrices** unchanged from untouched import baseline.
- **0 unweighted vertices; maximum 8 influences**.
- Maximum reloaded float32 weight sum error **1.5871e-7**.
- Original image SHA256s unchanged; original shader/material retained in the blend.

Blender FBX roundtrip (`spatial-fbx-roundtrip.json`) preserves exact vertex count, triangle-index and UV hashes, 67 bone names/parents, eight influences and zero unweighted vertices. Interchange precision introduces maximum world position error **1.22085e-7**, bone-head error **6.4462e-7**, and rest-matrix component error **2.1182e-5**. Applying the original native box/kick actions directly to the imported rig matches source candidate posed positions within **5.6116e-6 maximum**, or **0.000573% of original height**. Thus native action compatibility is measured, rather than inferred from matching bone names alone.

## Deformation results

Opened and reviewed clay geometry and original-texture renders for box_02 frames24/44 and front_kick_01 frames24/34. Comparison pairs use the same camera and geometric scale. The previous shredded side-torso/armpit streaks and weight-smoothing seam cracks are gone. Shoulders maintain a coherent round volume; elbow and waist folds are plausible; calves, footwear, fingers, face, glasses and hair remain detailed. Small faint shading patches can still be seen in the clay close-ups, but the prior major torso defect is resolved in these samples. Original-texture views also pass the sampled visual gate.

`spatial-deformation-metrics.json` compares unchanged donor, original direct transfer, and spatial candidate. Edge strain is posed length/rest length:

| Native pose | Direct-transfer maximum | Spatial maximum | Edges above5×, direct→spatial |
|---|---:|---:|---:|
| box24 | 17.595 | 4.278 | 1090→0 |
| box44 | 12.771 | 3.314 | 386→0 |
| kick24 | 20.443 | 5.124 | 774→5 |
| kick34 | 21.562 | 4.539 | 828→0 |

Dense edge count is2,818,409. p99 strains also improve: box24 1.623→1.605, box44 1.509→1.493, kick24 1.636→1.603, kick34 1.608→1.588. The unchanged donor has maximum strains17.466,12.595,14.130,14.512 respectively; smoothing reduces extreme local gradients while retaining gross native motion.

`spatial-pose-surface.json` samples50,156 dense vertices per pose against the unchanged posed donor surface. Across four poses, p99 nearest-surface deviation stays at **0.105–0.123% of original height**; maximum deviation is **0.682–1.222%**. These are nearest-surface distances, not one-to-one anatomical correspondence errors. Rest source geometry remains exactly unchanged.

## Evidence to open

- `spatial-box_02-upper-dense.png` vs `spatial-box_02-upper-donor.png`.
- `spatial-front_kick_01-upper-dense.png` vs `spatial-front_kick_01-upper-donor.png`.
- `spatial-box_02-original-textures-upper.png`.
- `spatial-front_kick_01-original-textures-upper.png`.
- `spatial-box_02-44-dense.png` and `spatial-front_kick_01-34-dense.png` for second sample poses.
- Earlier rejected controls and original untouched source render remain in `REPORT.md`.

## Remaining checks

No Unity or mobile performance test was run. Unity must retain **all eight influences** during import and runtime skinning; silently limiting this export to four would invalidate the reviewed weight field. Configure original basecolor/normal plus packed RM channels using original UVs, and validate normal-map convention/material appearance in the engine. FBX is rig-only, so original provider clips need the existing matching native hierarchy/animation integration. Other six native actions, full motion timelines, adversarial joint poses, and game-camera distances have not been accepted. Do not equate this sampled acceptance with a blanket claim that every aspect of quality is restored.
