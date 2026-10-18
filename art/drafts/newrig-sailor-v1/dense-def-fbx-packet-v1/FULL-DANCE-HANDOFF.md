# Dense Sailor full-dance review handoff — 2026-10-18

**Blender original-source off-key transport PASS; Unity full-dance acceptance pending.** No catalog or shipping asset changed. Local medial-trapezius improvement remains narrow; underarm sleeve stretching is still open. This transport result is not a complete skin-quality acceptance.

Authoritative candidate: `SailorOriginalDenseDEF-DanceAdaptiveTspace-v1.fbx`, **445,878,204 bytes**, SHA256 `ea4a6c4f99658cb583a7ee9846fd9a5e401784afdf5c39c9a2a5debab37efa85`. This large reproducible local binary must not be staged in ordinary Git. Actual imported take **SailorDense_DEF|Scene**, full **23.1666667 s**, **1536 FPS / 35,585 inclusive transport frames**. Declared authoring action name is Dense_dance_01_adaptive_tspace_v1. It is a derived FK/C1 motion, not original curve-byte preservation. The source native FBX and its authored curves remain untouched.

## Actual proof

- Callback44 short profile: 14 poses×2, all136 DEF world matrix component differences0; median0.327607→0.210038s. Gate46: all3311 frozen boxing source poses+498 independent dance/box/kick poses, all136 world matrix components identical. These are offline Mac callback timings, not game/phone benchmarks.
- Callback48: C1 finger orientation replay36→9 graph updates, all498 independent source poses component-identical; direct all994452-vertex worst difference0.083430µm. Source callback changed only evaluation scheduling, without approximating truth.
- Full original dance fit49: 17,985 source evaluations, 2,535 accepted keys, 7,848 independent interval-third and shifted120Hz holds. Source max surface bound0.0786603mm / bone0.00798345°. Source geometry/UV hashes exact. Actual4360.49s.
- Actual skinned FBX reimport51: **all3,088,451 imported named nonzero weights bit-exact**, max4 retained; original indexed bind-world positions matched within0.129805µm. All7,848 frozen-original-source holds: **max whole-original surface bound0.0783596mm / bone0.00801855°**, below unchanged0.1mm/0.01° gates. Direct ALL994452-vertex worst witnesses0.0716066/0.0774253/0.0740309mm, each below its bound.
- Export50 loads the authoritative original mesh (994452vertices/1988914triangles), original custom normals and UVs; all original named weight bits and raw4K image bytes checked before export. Original normal witness0difference, tangent witness8.45e−8. `use_tspace=True`. Source normal strength1 is retained; the separately reviewed studio normal-strength0.5 material choice is not baked into or confused with the source asset.

These are finite holdouts, not a continuous mathematical guarantee. The latest FBX still requires independent Unity complete corner topology/UV/bind/material/weight restoration and actual Generic Animator EOF/off-key/floor/camera checks. Source-authoring geometry hashes and imported indexed positions/weights are not mislabeled as a new independent full-corner topology/UV audit. Previous Box/CombinedReview24 Unity evidence does not automatically accept this file. Unity may split/reorder vertices and prune tiny weights despite importer metadata; use the existing verified original-index correspondence/weight-restoration path, never assume matching integer indices. Apply the documented single coordinate conversion C=ReflectX*B and fixed per-bone bind gauge; no moving fit.

## Reproduce sequentially, one CPU6 process at a time

From repository root, use `/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python <script>`:

1. `tools/44_profile_fk_finger_batches.py`
2. `tools/46_finger_batch_frozen_cache_gate.py`
3. `tools/48_profile_support_finger_batches.py`
4. `tools/49_full_dance_optimized_adaptive.py` (requires matching durable46/48 PASS hashes). Optional `-- --resume-cache <verified optimized checkpoint>`; strict config equality required.
5. `tools/50_export_full_dance_tspace.py` (requires complete fit PASS).
6. `tools/51_full_dance_fbx_oracle.py` (actual imported skin weights plus independent frozen-source holds).

Scripts live in `art/drafts/newrig-sailor-v1/tools`; reports/cache/FBX live beside this README. NumPy is required; no SciPy. Use the unchanged authoritative stage02-bound-original-dense.blend and original local source/native files referenced in sourceConfig. Cache/checkpoint NPZ serialization is lossless; binaries are local artifacts with SHA/bytes inventory. Large uniform FBX sampling is review transport, not an optimized phone payload or a measured runtime memory budget. Geometry, original4K maps and precision were not reduced. All Blender processes exited; heavy window explicitly released.

`full-dance-keyed-original-source-reference.json` supplies all136 original-source world matrices at phase0/.25/.5/.75/1, with exact source24/import1536 frame times. It is a source oracle, not an additional reimport result. Add explicit actual Unity endpoint poses:51 tested off-key holds and exact duration, but did not independently pose exact0/end.
