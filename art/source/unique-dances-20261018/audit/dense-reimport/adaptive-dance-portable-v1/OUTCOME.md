# Exact adaptive Dance: portable gate

PASS standalone Mono only. NDA1 stores all 2535 accepted source keys, Float64 TRS and full 136-bone hierarchy/rest, 27,621,700 bytes. No new key reduction, sampled 1536 Hz transport, Humanoid conversion, or source buffer modification.

Linear position/scale plus hemisphere-normalized quaternion NLERP and FULL rest/FK match the actual source evaluator. Across 7848 independent frozen original source holds plus five keyed/end poses, the all-original-influence AABB-corner deformation bound is **0.078660273 mm** and maximum polar-frame bone angle **0.0079834471 degrees**. The corner bound includes every original vertex/influence; it is not a sparse surface sample. These finite holdouts do not certify every time in the continuum.

7999 checks pass. Warm128 evaluations observed counter0, but no positive sentinel was retained for that standalone counter: allocation qualification is therefore OPEN, not proof of zero allocations. The full oracle loop includes allocating independent polar-matrix test math and file reads; its wall time is not the evaluator frame cost. Host-contended timing is not a quiet benchmark.

Removing shortest-hemisphere quaternion correction in a temporary source copy fails the independent q/-q test (exit 1). A second source-only mutation holds the previous key instead of interpolating: first independent original pose at 0.0034722222s fails at 0.606321mm / 0.460163 degrees (exit 1), demonstrating the source holdout gate rejects wrong temporal evaluation. The original source files remain unchanged by both mutations.

All lengths/tolerances above are in the original native source-world metre frame, approximately one metre tall. A future gameplay height wrapper requires separately reported physical-world error and tolerances; native 0.1 mm must not silently transfer to a 1.8x actor.

## Open gates

Unity float TRS/skin transport, root yaw/translation/scale, actual player, floor/support, cold/warm lifecycle, entry/exit, performance and visual acceptance remain OPEN. The 3.329 GB Unity review clip is rejected as a shipping payload. No production catalog promotion.

`green.log`, `hemisphere-mutation-red.log`, and `source-hashes.json` retain executable result scope. The temporary NDA and DNC binaries are reproducible from archived raw masters; their byte hashes are in source-key-packet.json and dance-oracle-transport.json.

## Subsequent standalone positive calibration

StandaloneMono recheck retained a known byte[65536]: threadcounter65568, observedbyte17, then128warm exact-key evaluations0bytes. All7853sourceposes/7999checks stillPASS with unchanged0.078660273mm/.0079834471degree bounds. See positive-calibration-recheck.log. This qualifies standaloneMono only, not Unity Editor where the same API is unsupported0.
