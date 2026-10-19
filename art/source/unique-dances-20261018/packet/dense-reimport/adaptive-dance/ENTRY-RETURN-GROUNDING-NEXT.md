# Next narrow scope: grounding and transition framing

## Coordinate contract

The accepted native AABB already contains C=ReflectX*B source-world positions. Bridge desiredWorld=anchorWorld*C*sourceWorldPose*C^-1*K_i, with fixed bind-only K_i. Bounds must use anchorWorld exactly once; do not repeat C, renderer.localToWorld, or the renderer's approximately100scale. Accepted root domain: translated root, yaw only, positive uniform scale .7/1/1.8. Arbitrary parent shear/nonuniform scale/roll/pitch not qualified for the simple vertical-floor envelope.

For yaw-only uniform scale s, native vertical translation d maps to world displacement s*d*worldUp. Root translation adds once; native positional error and explicit .025m margin scale s, angular errors do not. Height normalization must be an outer uniform wrapper whose transform is included in anchorWorld; actual scale must be explicit, not inferred from mesh/head height. Current source is about1m, not accepted gameplay1.8m. A world-space grounding correction cWorld maps back to native cWorld/s. If caps are world units, divide by s before expanding native bounds. Never multiply a world cap twice.

## Actual grounding: compact, sufficient inputs

Current source-foot finite per-pose minima lie in[-.017099462015501134,+.016213403484881778]m. For a fixed scalar cap interval[L,U], monotonic clamp gives cMin=Clamp(-highestMinimum,L,U), cMax=Clamp(-lowestMinimum,L,U). Adding interval[0,cMin,0]..[0,cMax,0] to the accepted original fullbody AABB conservatively encloses each translated sampled body; no fullmesh rescan is needed. This loses correlation but never assumes it. Source up-only is[0,.0170994620] beforecap; signed includes -.0162134035 beforedowncap.

Actual runtime support uses quantized bindpoints/weights and its own scalar operation order. Required compact actual inputs: typed143168point owner/provenanceSHA; original-ID/name mapping; full current bone matrices per tested pose; actual GroundOffset parent/world matrix; runtime cap values and signed-mode state; scale s; before/after UncorrectedMinimum/LastCorrection. Need bound epsilonPoint on actual-min minus source-min (not an unexplained tolerance). Then widen minima range by epsilonPoint and apply monotonic clamp. If correction is applied to a child affecting only selected bones, or another component moves actor afterwards, simple wholebody translation proof fails closed. Require explicit common ancestor of ALL136 deform bones and exact restoration/reset contract.

Next bounded actual job: use already exported143168 fixture and exact fulltyped/runtime MeasureMinimum at8extrema+0/EOF and source floor extrema6s/12.033333333s, yaw±90/170 and scale1/1.8. Record corrections and all136 bone before/after worldtranslation deltas; prove same translation/no rotation. CPU transformed expandedbound containment follows algebra; GPU edge mask at these actual grounded cases supplies render-order check. No repeat4759×1M. Do not claim physical no-penetration of all clothes/body from shoe grounding alone.

## Entry/return are not covered by endpoint AABB union

Slerp between rotations can sweep outside both endpoint surfaces; local-axis scaling and parent FK compound this. Existing arbitrary SourceBox→Dance entry cannot be declared safe from steadyDance bounds. A dedicated neutral→Dance0 and DanceEOF→same neutral can be qualified; arbitrary combat→Dance fallback remains excluded until its captured start pose is provided.

Compact inputs for a conservative transition envelope:

1. Actual target parent-first136 hierarchy, bind matrices, anchor and original named four-weight correspondence; all target transforms affected by owner blend (including unmapped intermediary parents).
2. Captured initial local TRS arrays and exact destination local TRS; source/neutral pose SHA, owner easing/blend code SHA and authored duration. Actual owner uses smoothstep easing and Quaternion.Slerp/Vector3.Lerp; do not substitute source normalized-LERP for this owner blend.
3. Immutable complete original support summary per four-bone tuple:16D features(w0*x,w0*y,w0*z,w0,...w3*z,w3), double outward lower/upper per leaf. Derived ONCE from ALL original vertices/influences; no sparse training/stride. Store original weight/position hashes and parent order. Existing cached foot summary cannot stand in for wholebody.

For a queried actual transition pose, each complete16D feature box dotted with four bone skin rows bounds all assigned vertices exactly up to explicitly outward-rounded arithmetic. Union all boxes gives conservative wholebody bounds without visiting1M vertices. Loose tuple boxes may be refined offline; no geometry changes. This is mathematical support acceleration, not LOD.

To certify an entire .28s transition continuously, enclose actual local Slerp/Lerp over easing parameter interval[0,1], then interval-FK through all parents and bind matrices, then feature-box dot ranges. Dependency makes this possibly too broad; subdivide parameter intervals under explicit error/budget. Ambiguous/budget exhausted must return Unqualified, not a sampled-only continuous PASS. Derive quaternion angular intervals from shortest Slerp actual endpoint sign conventions; source generic NLERP implementation is not its oracle. Initial implementation can instead sample actual owner every120Hz with explicit margin and label FINITE TRANSITION ONLY—this remains honest but does not prove continuous coverage.

A smaller practical first transition gate: actual neutral→Dance0 and EOF→neutral, union per-pose complete feature-box bounds over120Hz plus all easing endpoints. Compare summary output against direct shaderLBS at0/.25/.5/.75/1 blend fractions (10fullmesh passes total, not4759). GPU full landscape masks with entry/EOF/return fences, rootyaw andscale1.8. Publish separate transition dataset and union it with accepted steady bounds only if both scopes match. If conservative summary makes actor too small, improve tightness or use a separately approved camera transition; do not reduce margin by looking at screenshots.

No production provider enablement, SMR.localBounds/catalog changes or new engine job in this preparation. Originalweights only; future B weight variant invalidates body/foot support summaries. B deltamask update can replace only affected feature leaves if all unchanged vertices are accounted for exactly, with original/B hashes and disjoint coverage proof.

## Implemented offline tool

`weighted_transition_bounds.py prepare ORIGINAL.npz manifest.json NEW-summary.npz` groups ALL994452 original vertices by exact four-index tuple, preserving original influence slot order/zero slots/weights. Computes complete16D feature lower/upper, per-group counts and vertexGroup partition; verifies full coverage and writes original/manifest/summarySHA. This is one immutable geometry preprocessing, not sampling a motion. Actual full-source preparation not yet run while dense owns heavy window.

`query SUMMARY.npz ACTUAL-CAPTURE.npz NEW-output.json --margin M` requires named-order skinMatrices[poses,bones,4,4], strict times, source geometrySHA, captured start-poseSHA and owner-blendSHA. The matrices must map ORIGINAL mesh-local points to the intended bound frame (including actual binds, fixed C and required root convention), not bare bone-world matrices. At every captured pose, dot complete feature intervals with all four matrix rows; widen by gamma32 absolute product sums +nextafter. Union ALL captured poses, not only endpoints. Exact upstream matrix error is a separate capture gate; this arithmetic budget is not permission to hide it. Summary/source SHA mismatch fails closed.

Synthetic `test` PASS:100random signed coefficient/feature cases against exhaustive point results; a rotating test has a middle-pose excursion beyond endpoint union, explicitly proving endpoint-only method rejected. No actual transition capture/query PASS claimed. Output marks actualGPUAccepted=false/directFullMeshHoldoutsAccepted=false. Continuous Slerp/FK bounds remain unimplemented/excluded.
