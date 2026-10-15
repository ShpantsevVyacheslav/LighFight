# Final production diff review — no blocking finding

Reviewed current four-file uncommitted diff against746f921 plus GroundingPerformanceTests, PhysicalContactSamplingTests and NativeContactSamplingTests. No main edits or Unity/player launch. Read durable current actual-player result2400frames/errors0 and held shipping11 gate330cases/maxXYZdifference1.1920928955078125e-7m. Root reports full258/258; this agent did not rerun it. This review does not claim phone thermal/power verification.

## Contact window correctness

PhysicalContacts.cs:35 samples `Startup-1 <= Frame < Startup+Active`, so the first damaging active tick has the immediately preceding pre-active pose available. All25 shipping moves have positive startup>=10; no zero-startup move is silently covered by this reasoning. Combat.cs:116 calls sampling before Resolve and Advance; therefore the window aligns to actual hit tests rather than the advanced rendered Frame. Whenever either fighter is in that window, BOTH bodies are sampled, preserving the defender body and simultaneous outgoing contacts. Already-connected attacks still conservatively sample their remaining active window (extra work but no hit omission).

PhysicalContacts.cs:36 clears BOTH Body.ready flags when no sweep is needed, preventing a previous unrelated attack's segment from joining the next attack. First pre-active sample seeds before=now; the first active tick then sweeps from the real immediately preceding pose. `FighterView.ClearContactPoseSample` clears native hook validity and contactPoseFighter. NativeSupportOverlay.InvalidateContactPose sets contactFrame=-1, so same-render-frame active→recovery catchup cannot replay a stale source phase/pose. Old stored heading/context values become unreachable because contactPoseFighter=null and hook validity is false. Finger driver keeps its own once-per-frame delta accounting; not clearing that counter avoids advancing the blend twice in one frame. It still selects the current Animator state in LateUpdate.

Current tests cover pre-active+allactive counts, leaving-active same-frame hook invalidation, all25 procedural outcomes at near/far range, plus actual Retro/Island frontKick/jumpKick near/far damage/hitserial/down parity against forced always-sampling. The native test exercises ordinary one-core-step/EOF progression. Reverse attacker, simultaneous/linked attacks and multi-core-step-per-frame are not exhaustively included in that new native test; source symmetry and existing contact contracts support it, but don't label these four cases exhaustive all11 runtime contact parity. No concrete blocker identified from that scope.

## Grounding cache input completeness

CandidateFootGrounding.cs:14–22 keys raw minimum by the actual shoePoints array reference and exact `.Equals` on every current palette localToWorldMatrix AFTER ResetForSample. A moved actor, bone position/rotation/scale, changed native rest transform that affects those matrices, or palette matrix change causes recomputation. Merely replacing palette Transform objects with numerically identical matrices does not require recomputation: the raw min depends on indexed matrices, not Transform identity. Selection of a different support array invalidates through its reference. maxCorrection deliberately is NOT part of raw-min key; Apply clamps the same raw min each time, so changing cap does not force another weighted scan.

Nested Point.influences, bindPoint, bone index and weight values must remain immutable while array reference is unchanged. Current shipping Runtime/NativeSupport search found no in-place mutation of those fields; native overlay only selects preauthored array references. `InvalidateMinimumCache` is explicit for in-place authoring edits. Future runtime weight/point editing must call it or replace the array; this is a documented invariant, not a self-validating content hash. Nonblocking useful regression: mutate one existing influence bindPoint/weight, call InvalidateMinimumCache, assert exactly one new scan and correct new minimum. It should NOT be expected to detect unannounced in-place edits.

Overlay selects the correct regular/body array and unchanged cap policy, then calls the same MeasureMinimum used by Apply. ResetForSample restores authored nativeRest before measuring, so the applied lift is not accidentally counted as raw floor. The actual330shipping held-pose gate confirms correction/raw minimum/repeat0/bonechange1/restored1 for all tested contexts, beyond the80randomweighted-transform unit test. No source support, mesh, UV, bind or weight reduction was introduced. Caching has no arbitrary positional epsilon.

## Original-minimum and allocation invariant (precise wording)

Original raw minimum is:

`min_over_points( sum_over_influences( matrix[bone].MultiplyPoint3x4(bindPoint) * weight ).y )`

The new scalar expression computes exactly the Y row with the same influence order. It avoids calculating X/Z for floor-only work; platform/compiler floating operation differences mean **tolerance parity**, not universal bit identity. Observed all11 gate maxerror~0.119micrometers is below requested10micrometer gate. The original floor set, weights, transforms, cap and whole-rig correction semantics remain the authority.

The old per-point `Vector3.zero`, MultiplyPoint3x4 return value and Vector3 arithmetic are VALUE STRUCT operations, not per-point managed heap allocations. The matrix array was already lazily reused. Therefore do not describe this as removing thousands of Vector3 allocations. The actual gains are fewer duplicate whole-set scans, Y-only arithmetic and removing entire unnecessary contact Animator/grounding paths. In steady shape/size, MeasureMinimum itself adds no intentional per-point heap allocations; this is not a claim the full Animator/contact path is GC-free (capturing predicates/input arrays remain). First-use/re-sized matrix arrays, diagnostics and Unity internals are separate.

## Outcome wording

Accept source review with no blocking findings, backed by root's developer-player and test evidence. Retro forced recovery improved on the measured Mac; retain exact baseline/intermediate/final evidence and label this CPU floor/contact optimization, not geometry restoration, complete zero-allocation conversion or proven five-minute iPhone thermal fix. Actual-device long-run confirmation is still needed for the user's P0 report.
