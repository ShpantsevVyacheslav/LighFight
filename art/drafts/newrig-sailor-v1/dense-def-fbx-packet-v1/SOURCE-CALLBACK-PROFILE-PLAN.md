# Source callback profile plan — prepared, not executed

Current source motion quality and geometry acceptance remain separate. The 2M local medial-trapezius improvement is limited; underarm sleeve stretching remains open. This optimization changes only the offline evaluator cost, never geometry, weights, source curves, target accuracy, interpolation or runtime settings.

The original helper updates the dependency graph after each of 52 mapped FK controls. Candidate `tools/44_profile_fk_finger_batches.py` retains every nonfinger control update in its original order, then batches independent finger controls by proximal/intermediate/distal depth: 25 explicit loop updates. Shared reset, legs, root, source evaluation and C1 support remain unchanged. Cross-finger dependencies could invalidate this assumption; the result must fail closed.

After explicit heavy-window authorization, run from repository root:

```sh
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/44_profile_fk_finger_batches.py
```

The job warms both paths and compares 14 authored poses twice across dance, boxing, kick and walk. Verification is outside callback timing. It checks all 136 weighted DEF world matrices and bone angles, the conservative support-AABB bound derived from every original positive influence, and direct all-994,452-vertex LBS at the worst witness. Gates: matrix element ≤1e-5, bone ≤0.001°, full-original surface bound ≤1 µm. Original geometry/UV hashes must remain exact. It writes `source-finger-batch-profile.json` with separate baseline/candidate timings and explicit finite-pose status.

This finite screen alone cannot authorize a full-dance bake. If it passes and is materially faster, compare the candidate against the existing independent 498-pose frozen original oracle across all three clips, with identical source/callback fingerprints, plus authored ends and acceleration extrema. Only then adopt the evaluator for a separate full-dance adaptive candidate. Keep the previous source cache and evaluator immutable. Do not represent Mac offline callback speed as game, GPU, phone or thermal performance.

No new Blender job, full-dance fit or export has been run for this plan.

## Actual short profile

Script44 exited0: 14 authored poses ×2, every one of 136 weighted DEF world matrices component-identical; bone error and all-original surface bound were zero, including direct 994,452-vertex worst witness. Median original callback 0.327607 s versus finger-batched 0.210038 s (about36% less). Dance specifically 0.436888→0.323788 s because unchanged C1 support still restores fingers sequentially. These are offline Mac callback timings, not game or phone performance. Full3311-cache equivalence check46 is running; no candidate has entered production.

Prepared next narrow candidate48 batches only independent finger depths during C1 orientation replay, preserving all nonfinger order, then compares all498 original independent poses. Prepared49 full-dance fit requires both durable gate reports and matching script SHA before running. It retains unchanged original source keys, uses tighter internal0.08mm/0.008° fit, fresh shifted120Hz/non-dyadic holds and all-original direct witnesses. Sample/wall budgets fail closed with checkpoints; no partial clip is accepted. Export50/reimport51 are separate versioned jobs and retain source geometry/custom normals/tangents/map bytes.

## Full frozen-source gate completed

Script46 exited0 after948.64s: **PASS_ALL_FROZEN_SOURCE_POSES**, all3,809 poses (3,311 frozen boxing source-cache poses plus498 independent dance/box/kick source poses). Every component of all136 weighted DEF world matrices was component-identical (maximum element difference0). Bone-angle extraction differed by at most0.00000520° numerically; skin-composition bound0.230525µm, directall994452-vertex worst0.177453µm. The latter numerical differences compare cached NumPy skin composition with mathutils composition, not changed source bone poses. Original source attributes remain exact. No source approximation, export or production change was made.

Durable result: `finger-batch-frozen-cache-gate.json`. Heavy window released after process exit. Scripts48/49/50/51 remain prepared and unexecuted pending the next explicit serialized window.

## C1 replay batching gate completed

Script48 also exited0: **PASS_ALL498_FROZEN_ORIGINAL_POSES**, all136 world matrices component-identical. Maximum numerical angle0.00000520°, all-original skin-composition bound0.105818µm, direct994452-vertex worst0.083430µm. C1 replay updates36→9; observed dance callback median0.234225s versus original short-profile0.436888s (different bounded runs, not a capacity benchmark). Original geometry/UV/source curves unchanged. Script49 full-dance tight fit has started only after matching46/48 SHA gates; no full-dance fit/export acceptance yet.
