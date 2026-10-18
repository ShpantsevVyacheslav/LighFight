# Dense original fresh-rig review transport — NOT accepted shipping motion

This packet is separate from the native67/99,999-triangle prototype. It retains the unmodified original dense Sailor 994,452 indexed vertices / 1,988,914 triangles, original UVs and original source texture file bytes. The skin weights are inferred local fresh-rig weights, not authored provider weights. Weighted DEF bones only are exported; no Rigify controls or segmented B-bones are a runtime dependency.

`SailorOriginalDenseDEF-CombinedReview24.fbx` is the canonical Generic skin + three-take review source. The earlier static and animation-only files are retained diagnostic inputs; the animation-only local-curve transplantation is REJECTED. Dance adds the measured local trapezius support; box/kick serve as uncorrected fresh-rig retarget controls. Source FBX and authored action keys are unchanged at their original paths, and no derived take may be labeled original-curve preserving.

**24 Hz bake is a transport candidate, not a no-quality-loss result.** A 96/120 Hz independent original-evaluation oracle, including off-key times and exact authored end, is still required. Root thresholds are 0.1 mm surface / 0.01 degree bone error; failure requires higher/adaptive sampling or analytic correction, not relaxing those thresholds. Dense local three-pose visual improvement does not close underarm cloth stretching or all-motion quality.

Coordinate convention for reference JSON: Blender world metres `(x,y,z)` maps once to Unity world metres `(x,z,-y)`. Bone matrices use `B * M * inverse(B)` with that same orthonormal basis. NPZ retains raw Blender local coordinates and actual mesh/armature world matrices; apply each stored world matrix once. Do not apply the original object scale twice. Authored clips use source Blender frame ranges and 24-frame/s timebase; derived phase is normalized over the complete source range, inclusive exact end. Export metadata must state actual durations.

**Do not reuse original vertex indices as Unity/FBX indices.** FBX may split/reorder by normals/UV/material. Map original bind-world position + UV corner + canonical DEF bone-name/weight tuple, allowing equivalent seam duplicates. Verify full corner triangle/UV/weights coverage separately; then evaluate pose references through that verified mapping. Index-only comparison is invalid. Sidecar `original-indexed-attributes.npz` is authoritative original topology, UV and weights, not an assertion of importer index identity.

Required next gates: bind-pose skin witness on exported DEF skeleton; imported full positions/triangle/corner UV/weights/bind correspondence; original raw image hashes; original-evaluator off-key and end parity; actual Generic Animator EOF render/floor/entry-return proof. No catalog replacement, Unity promotion, phone performance or thermal claim.

## Canonical repaired transport

Stage **only `SailorOriginalDenseDEF-CombinedReview24.fbx`** as the mesh+animation review source. Its three takes are `Dense_dance_01_review24`, `Dense_box_02_review24`, `Dense_front_kick_01_review24` (importers may prefix the armature name). It carries geometry once and authoritative skin clusters/bind pose for all takes.

The separate `*-derived-DEF.fbx` files are **REJECTED** for action transplantation: no skin BindPose authority; their inferred posed rest differs from the static skin by up to 1.23245 matrix element. Actual original animation-only WORLD poses were correct within 0.00001145 matrix element, proving the issue was cross-rest local-curve transport. Euler-mode copying did not resolve it. Old RED evidence is retained.

Repaired combined FBX actual Blender reimport: 15 keyed pose samples × 4,096 original-index references, maximum surface error **0.003766 mm**, bind-nearest error **0.0001802 mm**; same 994,452 imported vertices. `combined-keyed-sample-parity.json` is PASS **keyed only**. This is not full topology/UV/weight correspondence, Unity acceptance or off-key fidelity proof. Source attributes, original maps and original source actions remain unchanged.

Reproduce repair with `tools/33_combined_skin_transport.py`, then keyed check with `tools/34_combined_fbx_parity.py`; headless Blender factory startup, CPU6, python-exit-code1. The repair recovers each animation's own absolute world poses, converts them to the static authoritative bind with `convert_local_to_pose(invert=True)`, writes bulk local FCurves and exports a common skinned FBX. Never reassign the rejected relative actions to a different rest rig.

## Unity FBX handedness witness (independent owner)

The sidecar's `B=(x,z,-y)` remains its declared **canonical reference basis**, not an assertion that Unity's FBX importer performs only B. Actual Unity import was independently measured by the review owner: importer world coordinates require `C = ReflectX * B`, with triangle winding reversed; all 136 named bind joints matched within 0.4574 µm. Renderer world scale already includes centimetre conversion, so transform points once and do not multiply by 100 again. See `art/source/unique-dances-20261018/audit/dense-reimport/unity-handedness-witness.json` and `unity-coordinate-witness.csv`. Apply this explicitly in the importer QA oracle; no actor mirroring or source edits are authorized. The Blender-to-Blender off-key gate compares Blender world metres directly and does not use this Unity conversion.

## Current authoritative boxing derivative (supersedes review24 for boxing)

For the latest boxing review use **SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx**, SHA256 `631c5b585e1206a03a7638e14c2603741eb31bc7d6375bb434f7bfc33b7d9afe` (150,963,372 bytes; local only). It includes original custom normals and exported tangent/binormal data, every original named weight, original dense geometry/UV and byte-exact original maps. Actual imported take: `SailorDense_DEF|Scene`, exact duration 2.7916667 s, 384 FPS transport lattice.

Independent original-source off-key Blender reimport: 1,215 holdouts, maximum whole-original surface bound 0.0488631456 mm / bone 0.007812317°, within unchanged 0.1 mm / 0.01° gates. All-original direct surface witnesses ≤0.04548966 mm. See `adaptive-box-tspace-fbx-offkey.json`. This is high-fidelity derived motion, not original curve-byte preservation. CombinedReview24 remains a historical keyed diagnostic and is rejected off-key; dance/kick are not upgraded by this boxing result. Unity review exited successfully but full tangent/visual interpretation is pending. No catalog, phone or complete skin-quality acceptance follows.

## Full dance derivative

Latest complete dance review is `SailorOriginalDenseDEF-DanceAdaptiveTspace-v1.fbx`; see **FULL-DANCE-HANDOFF.md** for SHA, exact scope, reproduction and remaining Unity gates. Actual Blender reimport passed7,848 independent frozen-source holds (0.0783596mm /0.00801855°), with all3,088,451 imported named weight bits exact. Full23.1667s;1536FPS; large445.9MB local-only review artifact. No complete skin-quality, Unity full-dance or phone acceptance.
