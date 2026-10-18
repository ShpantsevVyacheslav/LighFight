# Actual first-state diagnostic: arithmetic boundary, not a point/bounds mismatch

One explicitly authorized diagnostic Unity batch completed **EXIT0** and released its heavy window. No second benchmark, epsilon change, point removal, weight normalization, geometry edit or production patch. Raw witness/FSF/ID files and Unity log are retained here. The earlier benchmark remains REJECTED.

## Exact observed state

143168actual typed points,136bones,20ordered groups,6012nodes. BOX source time0. Both minima select point45761, original source vertex299687, imported vertex299687. Its two original positive influences are DEF-toe.R (bone94, weight bits0x3F664CE6) and DEF-foot.R (bone93, bits0x3DCD98D6); the portable4slot record pads the two absent influences with zero weight. No original positive influence is discarded. Witness JSON includes every bind coordinate/weight/vertical-row float bit and the full row snapshot.

| Evaluator in actual Unity | Minimum | Float bits |
|---|---|---|
| Accelerated BVH |0.0117189614|0x3C4000E3|
| Full143168-point scan of the SAME core's private Exact leaf, called directly |0.0117189614|0x3C4000E3|
| Independent typed-array full loop |0.0117189605|0x3C4000E2|
| Repeated runtime typed kernel on each individual point, then full reduction |0.0117189605|0x3C4000E2|
| Existing CandidateFootGrounding |0.0117189605|0x3C4000E2|

The difference is one ULP. Both argmins are the same point. Thus **at this actual state BVH pruning does not cause the failure**: an exhaustive call to every actual core leaf returns exactly its accelerated result. The input point choice/IDs also do not cause it.

All136bone rows remain unchanged after the later CandidateFootGrounding ResetForSample. nativeRig and owner groundOffset local positions are zero before/after the initial reset. Both primary kernels consumed the same exported float[] row snapshot, with no Transform read or reset between them. This excludes an interleaved ground-offset reset for this witness.

## Offline reproduction and cause qualification

`../offline-diagnostic/ActualPoseReproduce.cs` reads the exported ACTUAL FSF without any source-fixture reconstruction. Standalone Unity-bundled Mono returns0x3C4000E3for BVH, exhaustive core leaves, independent single-expression flat loop and staged-dot float loop. All select45761. A separate **explicit widened intermediate expression profile** over ALL143168points returns0x3C4000E2at the SAME point, exactly reproducing the actual Unity typed/Candidate value. See `../offline-diagnostic/actual-pose-reproduce.log`.

The core source stores the four-term dot in a `float value` before weighting. The typed/Candidate expression directly accumulates `(four-term dot)*weight`. The witness identifies an arithmetic evaluation/rounding boundary difference across the Unity execution and portable/leaf kernels, rather than a geometric/BVH point omission. The widened profile explains the observed one-ULP value. We did not capture JIT machine instructions, so the exact engine instruction/optimizer mechanism remains unverified; do not label this as a proved FMA bug or a universal platform behavior.

No tolerance is loosened. The next kernel design must preserve the ACTUAL intended evaluator contract on each backend (or use one genuinely shared evaluator) and prove it over actual poses before benchmarking. Simply declaring the portable all-float value canonical, changing Candidate arithmetic, or forcing a double profile to obtain a pass would change the current contract and is not done here.

## Allocation calibration

A freshly allocated byte[1048576] is observed (length1048576, first byte123) and kept alive explicitly. `GC.GetAllocatedBytesForCurrentThread` remains0before/after. Both `GC.GetTotalMemory(false)` and `Profiler.GetMonoUsedSizeLong` rise from8839168to9891840 (+1052672bytes).

Therefore this actual Unity thread-allocation counter is **not calibrated/usable**. Zero deltas from the adapter/bridge on this backend must not be called zero managed allocation. Portable standalone Mono allocation results are separate. Future Unity allocation acceptance needs a supported calibrated mechanism; no zero-native-allocation claim is made.

## Files / reproduction

- `actual-pose0.fsf`: FSF1 header,143168original typed points×4slots (indices/weights/bindXYZ),136vertical bone rows, one pose, original order.
- `actual-original-imported.ids`: point count, original IDs[], imported IDs[].
- `witness.json`: round-trip values, raw float bits, both argmins/positive influences, all row bits, native/ground offset and GC calibration.
- `unity.log`: named export job/engine exit; no benchmark repeated.
- `proof-files.json`: SHA256manifest.

Offline command (no engine): compile the unchanged packet ExactFootFeatureBounds.cs plus `../offline-diagnostic/ActualPoseReproduce.cs` with bundled Mono csc, then run the resulting executable on actual-pose0.fsf. The harness performs a one-state diagnostic, not a timing benchmark.
