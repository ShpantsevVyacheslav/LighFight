# Offline finite framing candidate

`FiniteClipBounds.cs` compiles alongside unchanged `AdaptiveDanceMath.cs` using existing Mono csc. `--test` passed domain/key retention, incomplete-output rejection, full-vertex/margin accumulation and corner-transformed bounds under yaw -90/+90/170°, translated roots and uniform scales .7/1/1.8. This is offline tooling, not a Unity runtime component.

`prepare_bounds_mesh.py` copies ALL994452 original float32 positions and four named-index influences from immutable `original-indexed-attributes.npz` into FBM1. It records source/output SHA256 without touching source. Before running the full pass, cross-check its index-to-name order against NDA1/source manifest; joint-index compatibility is a required input gate. No vertex stride or weight normalization.

Run tool arguments: NDA1 path, FBM1 path, new output JSON path, explicit absolute margin in native metres. Domain is sorted union of every exact Float64 source key and inclusive120Hz grid/endpoints. Every pose evaluates original exact source FK/interpolation then complete original LBS. Fixed C=ReflectX*B converts bounds into Unity source frame once. Output gives six extrema, their times and margin. Timeout after1200seconds throws; partial results cannot publish. Existing output is never overwritten.

**Not yet run on full mesh/domain.** Do not treat small synthetic PASS as actual framing acceptance. Full pass requires a quiet CPU window, input name-order gate, source/hash bundle and measured wall/CPU/RSS. No Unity/Blender/player launched for this implementation.

Coverage is FINITE, not continuous: even C1 paths can have unseen excursions between samples. At sampled poses, full original vertex AABB encloses triangle interiors (convex combinations; no shader displacement). Explicit margin is a camera choice, not a derivative bound. This first tool is ungrounded steady Dance only; it does not cover support translations or arbitrary entry/return blends. Those require separately identified actual owner state trajectories and hashes. Cloth/weight changes invalidate geometry input hash and bounds.

Runtime proposal: serialized six bounds + input hashes + domain/margin qualification, transformed by root/camera affine matrices. No runtime vertices, BakeMesh, source-key decode for framing or vertex sampling. Direct camera-space corner transformation avoids double world-AABB inflation. Scale1.8 multiplies native positional errors and margins by1.8; angular errors are unchanged. Bounds asset remains review-only until actual full-domain/GPU framing gate and artist approval.

Coordinated rerun command (DO NOT invoke while another heavy owner is active):

```sh
/Users/macbook1/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 art/source/unique-dances-20261018/packet/dense-reimport/adaptive-dance/run_finite_bounds.py --run --margin-native-m 0.025 --output /tmp/lighfight-finite-bounds-run-v1
```

Wrapper writes exact inputSHA/size before work, command, generated typed-meshSHA, stdout and final outcome; verifies original inputs unchanged afterwards. Existing output directory is rejected. Every30seconds reports walltime/PID; kernel logs completed/total poses and current source time. Degree1, no new packages, no engine. Kernel timeout1200s and outer1260s stop only the owned process; timeout/partial output is FAILED, never usable bounds. Margin.025m is an explicit proposed framing margin in native source units, not measured continuous error or artist acceptance. Full pass pending authorized window.
