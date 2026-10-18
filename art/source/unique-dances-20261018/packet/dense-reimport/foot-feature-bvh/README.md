# Exact full-foot feature-BVH — offline review candidate

No main/runtime grounding changes; no Unity/Blender/player launched. The original actor/mesh/UV/binds/weights/maps stay untouched. This packet accelerates a minimum over the SAME original support points; it is not geometric LOD or a sparse support mask.

## Actual portable result

Final portable Mono: **2502 assertions PASS**, all143168 original weighted-foot points across513 frozen source poses (15 keyed +498 independent all-three-clip poses),64 arbitrary signed-affine row cases and1920 random small-mesh cases. Query result equals the independent full float-LBS minimum exactly.13 ordered bone-tuple groups,6953 nodes; average4254.4 exact point evaluations vs143168. Lower-bound endpoint→upper-bound mutation exits1 at small suite1/pose0 (incorrect2.684251 vs correct−7.13623). Production source was never mutated.

Final host-contended `nice10` portable timings: accelerated median1.1955ms/p95 2.3034/max3.8194; fullscan median4.9051ms/p95 5.9868/max7.1119. Source parity+random pass wall4975.927ms/processCPU4898.799ms. Earlier same-code family measured .9319ms/3.8664ms medians, demonstrating host/timing variability; no thermal/60Hz/Unity/phone acceptance.128 warm queries allocate **0 managed bytes** by Mono thread-allocation counter. Native Unity allocations, Transform-row capture, held-pose cache and combat-inactive-zero remain to be measured in the later adapter.

The original selected-point dataset is143168 vertices with ANY positive original DEF-foot.L/toe.L/foot.R/toe.R influence. ALL original influences of each selected vertex survive. This selection criterion is the existing full weighted-foot master; it does not independently prove whole-shoe/full-mesh floor coverage.

## Conservative math

Each group shares its ordered four bone IDs. A point's16D features are `(w*x,w*y,w*z,w)` repeated per original influence. Node min/max features include EVERY original point assigned to that node. For supplied float32 bone vertical rows, sign-directed interval dot products bound the exact real LBS scalar. A node can be skipped only if its conservative lower bound cannot improve an already exactly evaluated float minimum.

Float leaf arithmetic matches CandidateFootGrounding's original order: four-term float dot, multiply by original weight, accumulate influences. Lower bounds subtract `gamma64 * sumAbsProducts` plus double dot-roundoff and a subnormal absolute guard. This covers the <=32 elementary float operations and double-vs-float reassociation. An additional ALL-influence unweighted coordinate bound rejects overflowing intermediate dot products before queries; tiny weights cannot hide overflow. Nonfinite/malformed/unsupported input throws and never returns a floor-clear result. No query budget can silently truncate coverage.

Original flattened inputs are copied into immutable owned arrays. Construction/grouping/sorting/stack arrays allocate at loading time. Query uses a preallocated traversal stack and value statistics. **One instance is single-thread/non-reentrant**. A future threaded adapter must own one traversal state per query. Seeds initialize best only; every unseeded point remains represented by conservative bounds.

## Reproduce

From repository root, run `prepare_source_fixture.py` with the bundled NumPy Python. It reads original indexed NPZ/frozen matrices and writes ignored reproducible original-full-feet.fsf; fixture/source hashes are in fixture-provenance.json. Compile ExactFootFeatureBounds.cs + FootFeatureTests.cs with existing Unity Mono csc.exe and run against that FSF. Runtime used:

`/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/Resources/Scripting/MonoBleedingEdge/bin/mono`

Compiler uses the same prefix then `lib/mono/4.5/csc.exe`. FootAllocationProbe.cs is a separate executable with the same core source. No new packages.

## Next measurable production-adapter gate

Flatten the ACTUAL typed-restored Unity support arrays (not a reconstructed source fixture) into this cache under selected-actor loading. Compare every actual sampled pose against existing full CandidateFootGrounding float loop AND independent full source/BakeMesh shoe floor. Preserve existing exact-row held cache; inactive/combat query count0. Measure warm median/p95/max/managed+native allocations, cache-build/load CPU and bytes in actual player. Keep independent source master/support provenance. Do not change upward/downward caps, point mask, phase, geometry or weights to obtain green.

## Actual Unity qualification (2026-10-18)

Portable parity remains separate. The first actual143168-point Unity adapter reported a one-ULP leaf/full arithmetic mismatch at the same argmin; shared E3 scalar and independent float-step agree, unchanged Candidate uses E2. Actual integration remains RED/open; see diagnostics owner reports. No epsilon/source change or production adapter acceptance. Previous counter0 alone is not calibrated allocation evidence; Unity positive1MiB allocation returned0. Standalone counter0 likewise requires its own positive calibration before claiming no allocation.

Subsequent standalone-only calibration: knownbyte[65536] returned65568 allocatedbytes, observedbyte17;128warmqueries then0bytes. mono-positive-allocation-calibration.log qualifies standaloneMono GC0, independently of Unity invalidthreadcounter and stillREDCandidate integration.
