# Active-battle thermal investigation — 2026-10-16

The user clarified that strong heating occurs during **active fights**, after several minutes. Smoothness of the earlier optimization was accepted; heating remains open (THERMAL-01). Paused-menu work is deferred and is not an explanation or fix for this complaint.

## Evidence contract

Use current original artist models, textures, materials, skin weights, animation curves and maximum settings. Do not infer a phone thermal improvement from Mac timing. Distinguish a short screening run, a matched sustained device test, and user acceptance.

The latest locally available phone manifest at this investigation start is `lighfight-ios-build104-manifest.json` in the LighChat host testing artifacts: source `40ab374`, **Debug Development IL2CPP**. A normal Release framework plus Release host rebuild has been handed off; completion is not established by sending that request. This is a configuration hypothesis, not a confirmed cause of heat.

The physical iPhone was Offline in the latest Instruments inventory. No Celsius, watts, phone GPU duration or sustained thermal improvement is claimed here.

## Diagnostic corrections

- Resolve the workload by move IDs and require observed starts and active frames for jab, front kick, jump kick and side kick. A submitted command is insufficient evidence that the intended movement ran.
- Preserve warm-up outside the measured interval. Record both actors' actual attack indexes/phases, hit serials and down states.
- Report valid inclusive CPU recorder distributions independently (main, render, wait, pose, contacts, grounding and cap). These overlap and must not be summed as CPU attribution. Missing/invalid recorders are unavailable, not zero cost.
- Report minute buckets of native process CPU, thermal states and memory. Whole measured intervals only; no invented interpolation over minute boundaries. Mac native iOS metrics remain unavailable.
- Existing Shadows OFF also replaces the character material, so it cannot isolate shadow cost. Pure-shadow diagnostic changes light shadows only, with the original material/property fingerprint checked before and after.
- `attack_windows.py` groups observed player attack phases and explicitly retains concurrent opponent activity. Correlation by phase is not causal attribution.

## Parser checks

`python3 test_summary.py`: 6/6 synthetic checks passed. Covers delayed positive GPU samples, unavailable OS metrics, interrupted runs, recorder validity/zero work, and CPU intervals crossing minute boundaries.

`python3 test_attack_windows.py`: 2/2 synthetic checks passed. Covers phase grouping/recorder availability and refusal of unknown attack indexes. These tests do not prove any runtime or thermal result.

Actual screening evidence is added below only after a successful build, full runtime interval, workload coverage checks, and visual inspection.

## Actual screening and scoped implementation

The initial actual player completed 120.002s / 7,183 frames with zero errors. Jab, front kick, jump kick and side kick coverage passed; 89 hit-serial changes occurred. Every sampled frame had exactly one scene-camera and one total-camera render. GPU timings were positive and uniquely timestamped on this Mac; no phone thermal data was available. Raw render counters had EOF alignment aliasing (zero then doubled counts), so they do not establish duplicate rendering.

The matched second pair used the same collector and original Retro/Island/office assets. Both 120s runs completed with zero errors and coverage passed. Only the reviewed grounding CS differed in their built production source manifests. Read [RESULT.md](RESULT.md) and [comparison.json](comparison.json) for the retained frame tails and scope.

| Inclusive CPU marker, ms | Baseline | Vertical-row candidate |
|---|---:|---:|
| Grounding Apply mean | 1.213 | 0.783 |
| Grounding Apply p95 | 2.398 | 1.494 |
| Retro body-support Cap mean | 5.005 | 3.109 |
| Retro body-support Cap p95 | 8.851 | 5.654 |

These markers overlap other work; do not add their savings. Scan frequency barely changed, so fewer cache misses do not explain the entire observed gain. The compact row representation also changes generated per-influence copying/access work. Workloads are real-time histories with different hit counts, not deterministic frame-for-frame simulation replays.

Whole-frame p95 **did not improve** (18.158→18.417ms); worst wall time increased (188.150→388.195ms). The 388ms frame had only 1.006ms Grounding / 0.123ms Cap. GPU also retained a 61.663ms outlier. No cause is established for those tails; no heat or overall frame-rate fix is claimed. Render Thread recorder registered valid but had zero sampled blocks, so its zero duration is unavailable evidence.

Candidate implementation stores only the exact world-Y matrix row required by the existing weighted minimum, uses exact `Vector4.Equals`, and preserves operand order, reset, immutable-support/in-place-invalidation contract, floor sign and correction clamp. Geometry, weights, source clips, materials and render settings are untouched. Independent review approved this narrow optimization. Numerical QA: baseline RED reproduced 50 unnecessary scans vs one under horizontal motion; expanded GREEN 9/9 includes exact float-bit comparison through 256 weighted nonuniform-scale/tilted/sheared poses and sub-epsilon vertical invalidation. No new independent full-mesh/contact parity claim is derived from time-series screenshots.

Main EditMode: 277/277 passed with Metal after promotion. The first nongraphics invocation had four graphics-dependent failures and is retained as invalid-environment evidence (`main-ground-row-edit.xml.gz`); it is not counted as passing. PlayMode: 8/8 passed with Metal.

Platform/boot limits: Apple M1 Metal, 1280×720, Development, CPU skinning, Max/60/4xMSAA/HDR, Mac quality index5 / four shadow cascades. iPhone inherits index2 / one cascade; this is not a phone GPU budget. Inline diagnostic bootstrap loads exact source Retro8/Island3/arena0 as diagnostic0/1/0 and does not test shipping Addressables loading/residency. Build helpers temporarily disabled automatic AA packaging and parked owned duplicate catalog outputs, restoring settings/paths afterward. Original full11/5 artist/import dependencies were gated before and after each build.

Completed diagnostic apps were removed after preserving helpers, source/art SHA lists, manifests, raw CSVs, screenshots and executable SHA provenance under `.tools`; no artist data was removed. Production pause-suspension remains uninstalled/deferred. THERMAL-01 remains in progress pending active-fight Release/device measurement.

Reproduction source freeze: commit `62c50c3` plus the exact artist/import SHA lists in `build-manifests`. Diagnostic helpers are preserved as executed against that pre-promotion freeze; their strict main-source gate intentionally refuses unrelated later trees. Baseline and candidate grounding source snapshots are retained in `diagnostic/Runtime`. These C# files are evidence, outside Unity Assets, and must not be imported together as duplicate classes.

Raw frame CSVs are losslessly stored as `frames.csv.gz`; uncompressed SHA/length are in `raw-evidence-archives.json`. Run `gunzip -k frames.csv.gz` in a copied evidence directory before using the preserved summary scripts. Original uncompressed CSVs also remain in `.tools`.
