# Deferred clip-global runtime camera bounds gate

Run only after owner and floor correctness qualification; never block persistent actor/foot work. This is a separate baked review Bounds asset, not a runtime vertex/Bake pass.

Inputs: exact2535Float64 key bytes SHA; source/review mesh positions, topology, UV, named readback weights and bind SHA; fixed bind-only K_i SHA; source C basis; explicit native actor frame; exact owner/root/floor policy. Geometry and source motion remain untouched.

Sample time union: all2535 adaptive key times, inclusive120Hz grid, inclusive60Hz grid,0/full23.1666667s. Deduplicate by exact source-double time/rational provenance, not rounded normalized float phase. The60Hz subset is retained in the manifest to identify actual sample roles. This remains FINITE sampling with a declared margin, not a continuous extrema proof.

At each sample, evaluate the qualified exact bridge and COMPLETE weighted typed shader-space LBS world positions, then transform once into the stated actor root frame. Never use unweighted per-influence corner union for camera bounds. BakeMesh diagnostic must honor the measured renderer scale100 convention, but full typed source-qualified LBS is authoritative. Baked camera Bounds includes an explicit fixed percentage/absolute margin, separate from exact sampled extrema.

Record ungrounded native source bounds separately from the allowed floor/root support correction interval. Do not silently transfer native0.1mm source tolerances to a later1.8x gameplay height wrapper. Root motion/floor envelopes and physical-world margins must be separate metadata.

To accelerate the complete extrema union, a conservative weighted-feature BVH could query each axis and its negative with exact leaf LBS, retaining every vertex/influence. However the first actual Unity foot accelerator currently has an unresolved exact-min mismatch: do NOT reuse that algorithm for this gate until its independent typed witness is resolved. Bounded full-LBS baseline is valid; no stride, sparse training extrema or mesh decimation.

Acceptance: all source/root hashes frozen; original full weighted extrema from independently selected endpoint/extreme holdouts must be contained; explicit GPU full-body masks from the final fixed camera remain inside declared margins front/back across all five user phases and additional extremes. Runtime reads only immutable Bounds/config, no BakeMesh/vertex access. Record build time/peak memory and sample count; no mobile framing/thermal acceptance from Editor numbers.
