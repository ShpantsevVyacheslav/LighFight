# Native67 surface and broader-pose review — 2026-10-19

## User steering / current decision

User suggested the dance itself could force awkward shoulder poses and requested other frames. Added12 uncorrected source-pose captures (dance0/10/20/30/65/80/95%, box15/65%, walk25%, greet40%, cheer60%) and assembled16-frame gallery with previous same-model uncorrected dance45/50/55% and kick35%. Same original994,452-vertex geometry/1,988,914triangles, raw4K maps, native67 rig and camera/lighting setup. These images use the explicitly derived full-closed-triangle bary weights, no DQS and no clavicle pose correction. Material/pose source identity is in each JSON; no crop of the underlying render, contact sheets merely resize/layout snapshots. Detail cameras intentionally show upper body; fullbody views are separate.

Visual observation: neutral dance start/walk/greeting look materially calmer; the strongest shoulder-to-armpit fold appears around dance45–50% with elbows out/hands at waist. This supports a pose-sensitive motion/skin interaction. It does NOT prove the entire rig is defective or that the clip alone is anatomically impossible. Do NOT globally apply the experimental clavicle correction based on those3similar frames. Next targeted comparison should use another existing dance on the same rig/model, and distinguish clip motion from weights/pose correctives.

## Actual surface-render mechanism

Actual Unity6000.3.26f1/AppleM1/Metal. Full variable Float32 CSR compute produces positions, normalized linear-blended normals and Gram-Schmidt tangents preserving source tangent.w. Independent CPU reference checks ALL1,305,574 seam vertices for each sample. GPU output is synchronously read then uploaded to an owned Mesh clone for these snapshots; this is a review harness, NOT a realtime/mobile renderer. Original topology/UV survive cloning, original base/normal/roughness/metallic image bytes4096² are SHA-checked, imported uncompressed with declared color spaces and sampled as independent raw PBR maps. Shader/lighting are derived Unity lookdev, not a claim of bit-identical Tripo/Blender shading.

Initial standardBakeMesh control produced a100x scale mismatch and is REJECTED (surfaceV2's pruned metric is invalid). Replaced by explicit CPU emulation of ALL imported typed weights using the same matrix convention. Corrected field control maximum position effect of importer pruning/quantization is0.3252mm in5sample poses at original~.98m height; not sufficient to explain centimetre-scale shoulder changes. This is not a wholeSMR renderer parity claim.

SurfaceV4/derivedBary/DQS/corrective/survey all meet their declared full-surface numeric gates. Some repeated static framebuffer bytes differ (preserved pairedA/B PNGs and counts), so overall status remains FRAMEBUFFER_REPEAT_RED_NO_PROMOTION even when the process exits0. No exact pixel-repeat acceptance, no production promotion, no claim that GPU skinning caused the raster difference. The issue also occurs with ordinary static MeshRenderer snapshot clones after GPU readback.

## Weight / skin-method experiments

1. Spatial field original dense/native67: full Float32 preserves every inferred source coefficient, but shoulder fold remains in actual textured Unity images.
2. Full closed-triangle bary transfer:2,608,534positive coefficients, max7 WITHOUT any cap/pruning; derived from all positive native donor triangle weights. Original dense geometry/binds/clips are unchanged. RestBvh point approximation first failed numerical criteria (V1/V2 preserved); V3 recomputes Float64 closest CLOSED points from ORIGINAL positions on selected donor triangles, with tested interior/edge/vertex handling. Max selected-triangle rest distance0.94504mm within.001H gate; not a new global-nearest proof. This changes transfer semantics, not geometric detail. Fold still appears; rejected as sole quality fix.
3. Native67 full-bary DQS: distinct from prior Rigify DQS trials. V1 rejects near-rigidity gate atLeftUpperLeg1.001358e-5. V2 explicitly projects near-rigid transforms to quaternion+translation, with a NEW separate full-positive-support physical bound<=10um (actual<=4.22um), not a silent threshold waiver. All positive weights consumed; CPU/GPU position/normal/tangent numeric checks PASS. DQS changes deformation geometry and normals but fold remains; rejected as sole fix. Source bones/curves unchanged.

## Experimental clavicle pose correction — not deployed

Source pose key45–50 has downward-sloped clavicle vectors during raised upper arms. Review helper lifts clavicle by smooth C1 activation35–95degree arm elevation, cap16degrees, then restores authored upper-arm WORLD orientation. This is an empirical derived pose adjustment, NOT an authored anatomy solution or unchanged native motion. It intentionally translates the arm/wrist; clips/bind/basegeometry are untouched. Original v1 piecewise activation snapshots retained separately from final C1v2.

Actual final C1 sequence:6816 original shifted120Hz+endpoint holds across all8clips; neck remains fixed, maxclavicle length error<.0002mm, upper-arm orientation error<.00005degrees, maxwrist shift2.251cm at ORIGINAL~.98m scale, maxadded correction velocity.407m/s, fresh-source-resample repeaterror0. Requires an authored pose resample BEFORE every application; applying twice without fresh baseline accumulates and is not allowed. No entry/return state-owner, continuous-time, contact, surface-selfintersection, clipping or phone acceptance from this pose-only pass. At game height wrappers physical wrist displacement scales accordingly.

It raises the shoulder line but does not eliminate the fold universally. After user steering, retain as experimental only, no global skeleton rewrite.

## Separate native-motion fidelity gate remains RED

Captured actual uncorrected Unity world poses for ALL67named bones at6816 frozen original times and compared against preserved Blender oracle using fixed bind gauge, no moving fit. Original spatial positive-support envelopes cover every original vertex/influence; add previously verified full-seam rest residual bound. Maxsurface bound0.1704mm vs0.1mm limit; angularmax0.07949degrees vs0.01limit; maxjointposition0.1462mm. Actual FBX SHA matches accepted192Hz exported carrier. This confirms an additional small temporal/import discrepancy, but not a centimetre-scale cause of the shoulder appearance. Do not mark native8 Unity source fidelity accepted; failure report/CSV/binary receipt preserved. This gate is independent of the intentional clavicle correction/DQS geometry changes.

## Still open

Alternative dance same-character comparison, targeted accepted quality fix, raster repeat diagnosis, temporal source-oracle parity, actual realtime renderer/bounds/grounding/entry-return/contact, all-character rollout, phone thermal/frame budget and release. P0 heating remains OPEN. No phone installation, paid generation or user-tested status this slice. Large FBX/blend/CSR/rawpose files remain local, not ordinaryGit; LOCAL-ARTIFACTS.json has receipts.
