# Sailor underarm/trapezius diagnostic — prepared, not source-tested

Owner scope is this directory only. The accepted full-dance transport packet is read-only. **No Blender/Unity job has run for this diagnostic. No weights, geometry or corrective targets have changed.** The metric core has actual RED→GREEN evidence; the source-specific masks and measurements still require their staged runs and visual review.

The purpose is to locate the remaining underarm stretch before designing a small game-exportable corrective or a targeted weight edit. Current C1 medial-trapezius support is measured as OFF/ON, not silently treated as a complete skin-quality fix.

## Files and proof

- `tools/skin_metrics.py`: NumPy-only canonical original-edge strains (median/p95/p99/worst), triangle area/collapse, transported-normal opposition witness, simple closed cross-sections, fixed-frame projected envelopes, anchor deltas and prospective candidate fences.
- `tools/test_skin_metrics.py`: **12/12 actual PASS**, after preserved RED runs. Covers rigid rotation, known scale/collapse, worst-tail false improvement, original boundary-edge IDs, normal orientation, nonuniform-scale cofactor, open/multiple/self-crossing sections, unreviewed masks/anchor failures, input immutability and known contour shifts. `evidence/metrics-*.log` retains RED/GREEN output. These are numeric tests, not character-quality proof.
- `tools/prepare_regions.py`: proposes 14 overlapping bone/geometry envelopes from the original indexed sidecar and true rest matrices. It does not use bone-weight proportions to claim leakage. Collar/armhole names are provisional spatial proposals, not identified garment seams or fixed sculpt anchors.
- `tools/probe_source_off_on.py`: staged headless source evaluator, metrics and matched renders. It verifies the unchanged frozen source blend/native FBX/shared evaluator SHAs, original geometry hashes and original indexed-sidecar identity. All outputs are fenced here. The legacy definitions receive their correct `__file__`; their incidental old-directory creation is suppressed. The old dance packet is never rewritten.

## Run only after explicit serialized engine handoff

From repository root use the installed Blender, one process at a time:

```sh
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/probe_source_off_on.py -- --stage masks
```

This proposes regions and renders untouched rest geometry in front/back-three-quarter/side, with an owned diagnostic color attribute on a temporary mesh copy. A composite legend has overlap precedence; inspect individual regions when needed:

```sh
# Substitute a region key from region-proposals.json; output stays in a separate directory.
... --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/probe_source_off_on.py -- --stage masks --mask-region collar_proposal --output art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/mask-collar-v1
```

Do not call the envelopes anatomical boundaries before viewing the images. If wrong, make a versioned region proposal; do not paint/smooth weights automatically. A review receipt must bind the mask SHA and at least three owned image SHAs; see `review-receipt.example.json`. The probe runs unreviewed diagnostics but labels them provisional; prospective acceptance fails closed when masks are unreviewed.

Next run all source metrics (no renders by default, wall guard900s):

```sh
... --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/probe_source_off_on.py -- --stage metrics
```

Cases: dance .25/.5/.75; all four selected actual high-clavicle/elbow-below-shoulder chest-relative witnesses (L .773831/.360162, R .6875/.827788); neutral; boxing at1.8541667s; front kick .45; actual65° synthetic arm elevation. The latter is explicitly synthetic and asserts the measured angle; kicks retain source legs/root/feet/fingers. Selection was made from existing C1 source poses and is not an unbiased proof. OFF and ON independently reset/evaluate the same source time. C1 applies only to dance, with a separately labeled synthetic stress control; box/kick/neutral remain unchanged controls.

Matched geometry renders (default three dance phases, 24 images including full-body front and front/back-three-quarter/side close-ups):

```sh
... --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/probe_source_off_on.py -- --stage renders
# Example actual critical pose; use --source-material for a separately stored normal1 source-material A/B.
... --python art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/probe_source_off_on.py -- --stage renders --case-names selected-R-2
```

The paired camera frame is shared, derived from both poses; close cameras use the OFF neck reference for both. Each render verifies ALL994452 evaluated game-LBS vertices against the frozen manual LBS pose (≤1e-5m), uses segmented DEF bones/PreserveVolume OFF, and preserves the original source arrays. Default clay/normal-OFF is explicitly diagnostic; `--source-material` uses unchanged source maps/normal1. Lighting is the existing controlled Blender rig, not claimed to equal the separate Unity studio IBL. Materials/lighting cannot count as skinning improvement. Default stage outputs are separate `masks-clay-v1`, `metrics-clay-v1`, `renders-clay-v1`, or `*-source-v1` folders. No source `.blend` is saved.

## Interpreting the measurements

Every selected original edge/triangle is measured, including region boundary edges; no geometry sampling accepts a fix. Worst source IDs are retained. Compare rest→OFF and rest→ON strain tails; do not improve a mean by worsening the localized maximum. Projected envelopes retain actual extremal vertex IDs and shared height bins, but include occluded points and do not prove a visible silhouette or holes. Inspect the matched images.

The normal witness transports a rest face normal through an averaged local LBS linear map using its cofactor (with rest object space removed once). Opposition is a fold witness for inspection, **not** a proof of topological inversion/self-intersection. A section area is available only for one simple closed ring. Open, branched, multi-component or crossing contours are unavailable; all real intersection segments remain in the report. No convex hull, cap or whole-garment volume is invented.

OFF→ON elbow/hand movement is reported as natural C1 girdle movement, not automatically rejected: the approved support intentionally moved hands slightly. A future corrective must instead compare against the fixed C1 baseline at the same pose, with head/upper-neck and elbow/distal anchors unchanged. The pure candidate fence rejects unreviewed masks, worse p99/worst strain, new collapse and anchor drift. It is a numerical regression fence, not visual acceptance. Collar proposal is a measurement region, not a blanket upper-chest pin.

Next correction is not implemented here. After valid masks/metrics/images, design a few localized pose-dependent targets or a restricted sleeve/body weight reconstruction, preserving original triangles/UV/map bytes and unaffected anchors. No repeat of broad blur, no pose clamping, no Blender-only PreserveVolume, no full-mesh-per-frame morph sequence. Gate the affected actual poses plus neutral/boxing/kick/65° controls and smooth activation/return before export.

Exact convenience entry points (do not run until the heavy window is released):

```sh
art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/run_probe.sh masks
art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/run_probe.sh metrics
art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/run_probe.sh renders --case-names selected-R-2
art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools/run_probe.sh renders --case-names selected-R-2 --source-material
```

Pure numeric regression command (no engine):

```sh
/Users/macbook1/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 -m unittest discover -s art/drafts/newrig-sailor-v1/skin-quality-underarm-v1/tools -p test_skin_metrics.py
```

Source-albedo mask overlay: `tools/run_probe.sh masks --source-material` writes
`masks-source-v1/`. It mixes the diagnostic vertex colors at 50% with each
original Base Color in owned material copies. This is an explicitly labeled
mask visualization, not a proposed material recolor. Source images, node trees,
weights and mesh data remain unchanged. Overlap colors show the last listed
region; the NPZ retains every overlapping membership separately.
