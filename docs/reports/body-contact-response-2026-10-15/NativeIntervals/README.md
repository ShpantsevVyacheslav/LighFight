# Exact Generic/LNC1 interval candidate — offline mathematical model

Main untouched. No Unity, Blender or player launch. This is a portable conservative model and negative interval certificate, not a qualified Unity-runtime collision oracle or gameplay acceptance.

## Actual authority inspected

IslandNativeStrikeReviewPacket/Editor/IslandStrikeLosslessPack.cs writes LNC1: exact float key times, values, in/out tangents, in/out weights and WeightedMode plus wrap modes and joint-channel mappings. NativeSupportPoseData.Decode reconstructs Unity AnimationCurve; Apply evaluates each scalar with AnimationCurve.Evaluate, normalizes the four evaluated quaternion channels, and writes original local TRS. The new Generic codec probe calls Apply directly. The existing shipping overlay additionally restores rig root TRS, anchors hip XZ, blends with Quaternion.Slerp/Vector3.Lerp, and applies grounding. Those extra paths are NOT equivalent to the restricted direct-Generic model.

LNC1 is a lossless STORAGE codec, not an independently specified scalar evaluator. Weighted time inversion details and Unity's floating arithmetic are not exposed by this code. No claim that a few finite samples certify the hidden evaluator is made.

## Implemented portable mechanism

NativeIntervals.cs supplies outward IEEE-double interval arithmetic; complete unweighted cubic Hermite interval polynomials intersected with their valid Bezier control hull; weighted Bezier full-segment control hulls for weights in[0,1]; normalized quaternion denominator bounds; interval rotation/TRS and parent-before-child FK. Inverse weighted-time sampling is unnecessary for a full control hull. Weighted bounds currently do not contract within a key segment, which can be broad but cannot become a false negative in the exact-real curve model. All overlapping key segments are included, including interior overshoot invisible at endpoints. Pre/post wrap/extrapolation, nonfinite/step tangents, normalization intervals that can contain zero, invalid hierarchy and invalid weights remain unsupported.

Rest local TRS must be the original authored values for absent channels. Expand all nonanimated ancestors into the hierarchy and remap skin bone indices; the LNC1 path list alone is not guaranteed to include every ancestor. Multiply original bind matrices after FK. No bind/weight/topology replacement is performed.

IntervalSurface.cs bounds every original positive-weight influence point through interval bone matrices and weight-sum intervals. Complete leaf bounds use every corner of every original face and all four source weights. Construction validates the face partition, tree reachability, every original leaf influence, source weight sums, and child-to-parent influence enclosure. Sparse metadata cannot silently become a certificate. Bounds overlapping at a leaf return Indeterminate; overlap is never invented contact or damage.

NativeNegativeOracle.cs implements the existing portable source-phase interface for one exact-model native actor and a fixed target. It returns ProvenClear only after the entire interval is geometrically separated in this declared model. It allows genuine separated motion rather than freezing all attacks. It returns Indeterminate for interior-crossing/near-ambiguous intervals; a positive contact/impact-time solver remains outstanding. Reverse source return uses the same full interval, not endpoints.

## Hard qualification limits

The current model assumes exact-real Hermite/Bezier + normalized quaternion/TRS semantics enclosed by outward double arithmetic. It DOES NOT yet bound Unity AnimationCurve.Evaluate rounding, float Quaternion.Normalize, Transform matrix construction, weighted inverse-time numerical details, or accumulated Unity FK rounding. Thus do not plug NativeNegativeOracle into the live Animator bridge as a Unity certificate. Model proofs are not runtime proofs. No arbitrary epsilon is used to conceal this gap. A specified portable runtime evaluator, or a verified bound for actual Unity evaluation, is needed before that handoff.

Context requires direct Generic source, unblended, no hip anchor, no grounding, no extra IK, fixed hierarchy and fixed root. Any missing condition returns Indeterminate. Shipping production attacks currently fail some conditions. Root-only translation belongs to TranslationSweep; pose intervals do not certify root/facing changes. Source root/hip anchoring, all-sole floor correction and entry/return blend need explicit interval operators before enabling those paths. No camera/render/source assets changed.

## Verification and next import gate

Portable Mono tests cover endpoint-hidden Hermite overshoot, interval subranges against independently evaluated polynomials, weighted control hull, quaternion normalization, singular denominator, FK/rest channels, grounding/blend rejection, original-point metadata omission, far full-interval clearance, early allowed motion and interior-crossing rejection. An endpoint-only mutation must fail the first overshoot assertion. See interval-tests.log and mutation-red.log.

LncInspect accepts compressed .bytes through ReadDeflate or an unchanged decompressed .lnc1 through ReadRaw. The installed standalone Mono lacks libMonoPosixHelper for Deflate; Python stdlib zlib raw-deflate decompression produced island-existing-greeting.lnc1 for the inventory. No package was installed. This inventory is existing Island Greeting storage, NOT the not-yet-exported new box_01/box_02/front_kick source curves. New strike files must be supplied after the authorized Generic exporter runs; retain their original hashes.

Later isolated Unity qualification: export all exact local channels, complete hierarchy/rest/original binds, source mesh/influences and original maps hashes. Verify actual evaluator paths and floating-error bounds, then test all-vertex enclosures at adversarial key boundaries/overshoots/quaternion sign changes and frozen holdouts. Such tests detect violations but are not by themselves a proof for an unspecified black-box evaluator. Compare raw Generic and LNC1 EOF. Only then connect a qualified interval implementation to AnimatorPhaseCollisionBridge. Legal near impacts, continuous stop/retract, passive-limb anatomy, reactions, floor, map bounds and CPU remain separate required gates. No all-jabs-disabled pass.
