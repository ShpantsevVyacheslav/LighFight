# Unique dances: acquisition and isolated V4 calibration review

Status: 11 original Meshy FBXs acquired/hash verified; all55 source choreography pairs differ (minimum37.53° cyclic-phase/sign-invariant RMS). Actual acquisition53credits is recorded by parent. No additional provider work, production roster promotion, or phone acceptance is claimed here.

## Authoritative candidate

Sailor/action74 Gangnam is the first direct-Generic pilot:

- `unity/Assets/UniqueDancesReview/Fighter-9/Review.exact-generic-anatomic-v4.prefab`: exact imported Generic source curves, original target mesh/materials/Avatar/weights/binds, tiny full10.133334s clock.
- `.support.prefab`: separate optional bounded C1 clavicle correction; derived calibration accepted by root, final collar visual acceptance OPEN.
- `Runtime/ExactGenericDanceRetarget.cs`: typed visible-loading warmup, resident lease/disposal, no cold decode, no non-Dance combat bone-cache work, explicit original native full-shoe ownership/cleanup. Runtime source skeleton26Transforms/0Renderers, no donor mesh or Human solver.
- `Editor/UniqueDanceExactGenericSetup.RunAnatomicV4`: reconstructs BOTH temporary donor/target actual rest from imported renderer-world × inverse(original bind matrix), preserving original localScale. Builds corresponding virtual arm-T orientations while retaining anatomical girdles. Animated FBX Model defaults/approved serialized guard are not bind rest.
- `Editor/UniqueDanceSailorClavicleBindPilot.Run`: derived calibration, independent FK/root cases, kinematic/full-surface floor and matched phase/extreme front/back render gates.

All paths above are relative to the repository; authoritative scripts/assets are in the Unity folder, not the old Human-bake packet copies.

## Evidence and precise density scope

`audit/generic-sailor/true-bind-reconstruction-v4.csv` records original mesh GUID/localID, counts, units, max/RMS and retained localScale:

| Mesh | Indexed vertices | Triangles | Bones | Bind surface max / RMS |
|---|---:|---:|---:|---:|
| Current approved native67 Sailor |57025|99999|67|1.8343µm /0.47585µm|
| Acquired motion donor |110983|198590|24|0.7233µm /0.33520µm|

Target mesh GUID `cb91d1731e78d48e4b8b62500b86f8ed`; donor Generic-copy GUID `d975053f60fa64945ae4f817e3a86939`. This is every indexed vertex of the current native67 model, **not the separate fresh2M Rigify research model**. Original source buffers were not modified or renormalized. Raw action74 and its Generic import copy have equal SHA256 `f152392bb9266f7735feebbe27a4d6dd1823d4c9bec2a598244bdec4f17ee15b`.

`audit/generic-sailor/anatomic-v4-{summary.txt,independent-parity.csv}`:12856 independent Generic/FK samples under yaw0/+90/-90/170°, translated roots and scales1/.7/1.3/1. Max angle0.0000834°, position1.518µm. Original Generic channel/key fields are losslessly LNC1-packed with exact decoder key-bit checks.

`audit/sailor-analytic-c1-anatomic-v4/`:64 matched before/after phase0/.25/.5/.75/1 and extreme front/back images;384Hz correction-kinematic grid and separate128Hz full-original-indexed-surface LBS floor grid. C1 world arm orientation0.0000326°, max hand42.212mm (2.15% of1.960m height), head/feet/endpoints0, outer shoulder never lowered, minimum grounded original surface−0.0132µm. These are numerical checks, not final cloth/neck acceptance.

`audit/sailor-collar-material-diagnostic/`: atlas/no-normal/flat-clay same-pose images and5486 original-index medial-region weight/pose witnesses. V3 shelf exists geometrically. Images now explicitly labeled `original-prefab-guard`, not mathematical bind. Independent read-only raw FBX Cluster.TransformLink audit JSONs and `packet/tools/audit_fbx_bind_girdle.py` crosscheck bind anatomy; Unity imported matrices remain the calibration authority.

`audit/player-exact-generic-sailor-anatomic-v4/`: final actual Mac Mono player, full authored10.1333s nonloop clip;630 EOF observations including entry/endhold, post-entry independent real Generic Animator/FK comparison,24 return frames. Source/target updateMode Normal; reference AlwaysAnimate, explicit normalized Play+Update(0). Max post-entry FK error5.416µm/0.000977°, full-surface floor−0.164µm, minimum viewport margin12.72%. Final QA initial yaw is fixed; remaining largest observed step0.174m is first entry frame at65.75ms (readback timing), not a Human arm-roll flip. No further screenshot/microtiming-only iterations are implied.

Measured on this Mac, not a phone: evaluator mean0.0418ms/max0.5871ms;10425-point floor mean0.3829ms/max0.5456ms,629scans/630 observations. Total selected actor loading (existing native resources plus new source)185.88ms process CPU/198.65ms wall and13.48MB Unity allocation delta, under typed loading gate.

Pilot bytes: exact source264287B +pose header1767B +full-author clock2765B; derived prefab70903B/controller44268B/optional C1 variant3909B. Original actor assets are shared. QA app additionally includes development runtime/current project assets and the independent Generic reference; its~1.26GB build is not the incremental shipping dance payload. Original raw FBXs/export masters remain retained.

## Regression evidence

`audit/lifecycle/` contains exact XMLs and manifest:

- Earlier5/5 then later6/6 focused lifecycle GREEN; new inverse-bind calibration1/1 GREEN separately.
- Cold-lazy-decode and missing explicit shoe-set ownership mutation:0/2passed,2 intended failures. Driver byte-restored SHA256 `803076e60983f5052ae2db2e8af2b39955e7076de4fc9ae133e503c5b378dae1` before player builds.
- Selection immediate/untracked playback mutation:0/2passed,2 intended failures; sibling lifecycle implementation restored. Full source endpoint and.28s selection return are independently tested.
- New bind regression reads original inverse-bind rotations and rejects retained V3 posed-default calibration by>1° while V4 is<.01°; exact target source mesh reference remains shared and runtime donor renderers0.

## Rejected baselines and open work

Human solver/dense retarget review baselines are REJECTED: nine of11 have severe roll discontinuities; Sailor has near180° key-to-key arm flips. Tiny-clock equality against those baselines proves carrier substitution only, not deformation acceptance. Sparse-resampled corrective interpolation REDs remain preserved. Use direct Generic V4, not those Human-derived poses/carriers.

Prior apparent−94mm grounded-floor failure was a QA coordinate error: default/false BakeMesh then TransformPoint doubled nativeRig scale2. Independent original LBS and true-mode witness agree; corrected source-surface floor passes. Frozen evidence is in `audit/generic-sailor/skin-scale-coordinate-witness.{csv,json}`.

V4 bind mechanism accepted as derived calibration only. Native67 residual medial clothfold needs source comparison before any local PSD/weight partition; no weights have been edited. User's latest2M fresh-Rigify screenshot belongs to a separate dense-owner pipeline and is not accepted by this pilot. Remaining10 acquired styles still need direct-Generic target integration/visual/player gates. No all11 mass promotion or phone completion.

Future edge recorded by root: a winner that falls back to Dance while FullPreviewMotion=false may enter with no previous actual combat pose cached, because the tiny Human clock is evaluated before the direct driver captures its entry. Current Sailor has Victory and does not exercise this path. Before Island/fallback mass integration, add an explicit entry-capture hook or use FighterView's existing blend snapshot; do not add per-frame combat bone caching.
