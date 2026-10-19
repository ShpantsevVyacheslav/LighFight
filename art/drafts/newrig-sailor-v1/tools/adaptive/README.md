# Offline source-adaptive core and adapter

`curve_core.py` is a tested pure mathematical core. The real-source adapter and boxing export have now executed; historical preparation notes below are superseded by the dated current status at the end. No production change is performed.

Root-runnable bounded first probes (after serialized heavy-window release):

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/36_adaptive_source_cache.py -- --clip box_02 --interval-seconds 1.8541666666666667 --rates 96 120 --max-samples 300
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/36_adaptive_source_cache.py -- --clip dance_01 --interval-seconds 15.9375 --rates 96 120 --max-samples 300
```

The initial probes fit only endpoints of higher-rate cells, then independently evaluate original source at quarter/mid/three-quarter times. They cover the known original24 failure intervals; they do not establish whole-clip quality. Source poses are cached once, complete local/world bone matrices retained, original all994452 vertices used to build positive-influence support AABBs. Nonuniform scales enter full skin matrices. Export hierarchy FULL inheritance is explicit, original constrained DEF world poses are reexpressed into it; other inheritance modes are rejected by the pure core.

`--full` recursively refines all original authored24 intervals and checks new non-dyadic1/3,2/3 holdouts after fit; failures are durable, not auto-waived. No final geometry acceptance uses a vertex subset. Before trusting real-asset surface bounds, validate actual all-vertex LBS at selected extrema as well. Six pure regression fixtures cover bound-vs-full weighted vertex errors, nonuniform scales, full bind hierarchy, common reflection invariance, unsupported data rejection, nonlinear interpolation with1001independent holdouts, cache reuse and fail-closed limits.

At the initial preparation milestone no engine had been launched. Original24 transport remains rejected off-key (8.88152mm /1.653619degree worst across498stratified holdouts). Higher rates are candidates, not an assumed fix. Actual final FBX/Unity interpolation still requires independent reimport proof.

Current core regression suite: **8/8 PASS** after adding antipodal-quaternion continuity and zero/nonfinite quaternion rejection. Source local-TRS reconstruction itself must meet the accuracy bound at every cached sample; a source shear/scale mismatch fails before refinement. Adapter records actual constrained Rigify inheritance flags and explicitly reexpresses poses into the FULL-scale exported hierarchy.

The full-mode independent post-fit fence additionally spans the complete clip with uniform120Hz holdouts shifted by1/7cell (non-dyadic, separate from fitted quarter samples), plus non-dyadic1/3,2/3times per accepted interval. This is finite sampling, not a continuous mathematical animation guarantee. All994452original vertices are evaluated directly at the three worst bound witnesses to verify the conservative AABB bound on the actual asset. Screen mode still checks only explicitly selected source intervals and cannot accept a clip.

Known-worst interval adaptive probe (subsequently executed; results below):

```
Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/36_adaptive_source_cache.py -- --adaptive-interval --clip box_02 --interval-seconds 1.8541666666666667 --max-samples 300
```

This fits only source interval `[44/24,45/24]` adaptively from independent original source callback data, then checks non-dyadic1/3,2/3points and shifted120Hz holdouts inside that interval. It does not escalate one fixed rate or accept the whole clip. It preserves both authored interval endpoints, uses bounded source cache, and evaluates all original vertices at three worst holdout bounds. Full-clip fidelity and final FBX/Unity interpolation are still separate gates.

## Actual bounded real-asset milestones

- Fixed96/120Hz known-worst boxing interval: **both RED**.96Hz bound0.596474mm / bone0.092758degree;120Hz0.381493mm /0.059550degree. Original all-vertex witness confirms actual96Hz0.558143mm≤0.596474mm bound. No higher-rate success was assumed.
- Actual adaptive interval `[44/24,45/24]`: **PASS_DIAGNOSTIC_NOT_EXPORT**,102unique source evaluations in35.946s.17accepted keys, minimum spacing1/384s,37independent post-fit holdouts including non-dyadic thirds and shifted120Hz. Maximum surface bound0.0348792mm; maximum bone0.00522424degree. All994452original vertices evaluated at three worst witnesses:0.0325077/0.0311002/0.0311632mm, each below its bound. Source attributes exact;136actual constrained DEF inheritance flags FULL.

Artifacts: `dense-def-fbx-packet-v1/adaptive-box_02-ADAPTIVE_INTERVAL_NOT_CLIP_ACCEPTANCE.json` and matching `-cache.npz`; log `adaptive-box-interval.log`. This validates one source interval only, not a full clip, continuous-time guarantee or FBX/Unity interpolation. No new exported model was produced. Heavy window released immediately afterward.

## Actual full boxing source-fit milestone

Full `box_02` completed in1072.90s with2766unique source poses and406accepted adaptive keys. **PASS_DIAGNOSTIC_NOT_EXPORT:**1145independent post-fit holdouts, max conservative whole-original-surface bound0.0916473mm (t0.817857s), max bone0.009970833degree (t1.526190s). Actual all994452vertices at three worst bound witnesses:0.0865506/0.0707122/0.0769836mm, each below its bound. Original source attributes exact. No continuous-time or exported-FBX claim; angular margin is tight and final transport can still fail.

Lossless source cache and configuration fingerprint are preserved in `adaptive-box_02-FULL_ADAPTIVE-cache.npz` and verified checkpoint. No source-reduction acceptance was used. Exact original authored curve times seed the fit. Offline export preflight derives lattice1/16ofsource24frame → **384FPS /1073inclusive samples**, preserving all406keys and exact2.7916667s duration. Source geometry is994452vertices/1988914triangles; no reduction/compression. Estimated new FBX80–100MB, unmeasured until export. See `adaptive-box-export-preflight.json`.

Next gates: actual single skinned export37, independent frozen-source reimport38, then Unity Generic EOF/fullattrs/material/weights restored gate. Do not widen0.1mm/0.01degree tolerances. If the angular margin is consumed, use a stricter internal fit and fresh independent holdouts, retaining this candidate as evidence.

## Current transport status — 2026-10-18

The first adaptive boxing FBX narrowly failed the unchanged angular gate: 0.010066984° versus 0.01°. It remains preserved as RED. A tighter internal fit (0.08 mm / 0.008°) reused all 2,766 original cached source poses bit-for-bit and added 545 poses. It accepted 441 keys and passed 1,215 fresh independent holdouts, with source bound 0.0492266 mm / 0.007824889°.

`42_export_tight_source_tspace.py` exported the authoritative original mesh, custom normals and tangent/binormal data, original named weights and raw map bytes. `43_tight_tspace_fbx_oracle.py` actually reimported that FBX and passed the same 1,215 frozen-source holdouts: full-original surface bound **0.0488631456 mm**, bone **0.007812317°**. Direct all-994,452-vertex witnesses were at most **0.04548966 mm**. These are finite off-key Blender transport gates, not continuous-time or final Unity visual acceptance. Other review24 motions remain off-key RED.

Current local-only FBX: `dense-def-fbx-packet-v1/SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx`, 150,963,372 bytes, SHA256 `631c5b585e1206a03a7638e14c2603741eb31bc7d6375bb434f7bfc33b7d9afe`. Actual imported take is `SailorDense_DEF|Scene`, duration 2.7916667 s, transport lattice 384 FPS. This is derived motion, not byte-preserved original curves. Independent Unity Tspace review exited successfully; its basis/image interpretation is still pending.

See `SOURCE-CALLBACK-PROFILE-PLAN.md` for the unexecuted bounded callback optimization. No batching change has entered the source evaluator or any exported asset.
