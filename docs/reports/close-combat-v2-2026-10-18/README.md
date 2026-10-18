# Close combat v2 candidate, 2026-10-18

Status: implementation + isolated core GREEN; coordinated Unity/native/render verification PENDING. Existing unrelated dirty assets/settings were inspected and preserved. No Unity process launched by this work.

Proven baseline: core105 tests had3failures in the new cases: both-facing initial root overlap stayed at .10m; an approach contact at frame2 was discarded until nominal startup elapsed, so no damage occurred. Read-only runtime diagnosis: only Startup−1/active poses were sampled; defender motion was absent from sweep; the source animation continued through targets after a successful contact.

Changed core root overlap repair keeps the existing .60m root minimum, respects plane/map bounds and preserves side order. It transfers unavailable wall correction to the actor with space. This is root separation, not a full skinned-body envelope. Existing swept Walk/chain rejection remains.

Physical damage may now occur from frame1 through the nominal active end, only through the installed geometric physical validator. Distance-only legacy simulation retains its original startup rule. Core body distance alone never authorizes a physical hit. Hit-once, guard/crouch, interruption and recovery still belong to core.

SweptContact.Cast conservatively advances two linearly moving capsule axes using a segment-distance Lipschitz bound. Both actors' motion is included; fast endpoint-clear crossings are detected. Solver convergence tolerance is10micrometres; exhaustion/nonfinite inputs return Indeterminate. This is a bone-capsule proxy guarantee, not nonlinear skin/cloth CCD. Existing capsule radii are retained; upper torso terminates at the actual shoulder midpoint and shoulder axis instead of the lower Spine point.

At a proxy candidate, actual source poses are sampled in a bounded interval and bracketed against the target's actual sampled body proxies. A confirmed pose contact stops the source frame at the bracket boundary. If no real sampled pose confirms it, damage is suppressed, UnresolvedPoseContacts increments, and recovery retracts conservatively. Finite probing is explicitly not an exact nonlinear-curve or triangle-surface certificate. A positive candidate is not silently treated as a surface-mesh proof.

FighterView drives source phase and source heading from the stopped/retracting frame, including posture/arms phases. Recovery holds the contact for a core tick, returns through original source phases over at most8ticks, then the ordinary combat recovery ends. Individual source bones, mesh, UV, binds and weights are not edited. Unique AttackSerial prevents a previous stopped jab from freezing a new identical jab/chain. The old full remaining source strike is not played through the target.

Verified: isolated core111/111; this includes the previously red overlap/approach cases, fast punch/kick proxy crossings at two heights/both facings, a moving defender crossing a stationary strike, side/far misses, budget uncertainty, guard/crouch and bounded source return. Changed FighterView+PhysicalContacts compile against installed Unity modules and freshly compiled core via standalone Roslyn; this reference check has expected shadow-type warnings and is not Unity import/player proof.

Pending Unity tests: PhysicalContactSamplingTests positive close hit/stop/retrigger bothfacings; existing optimized-vs-always parity; NativeContactSamplingTests12near/far Island jab/frontKick cases at .65/.9/3m, bothfacings, guard/crouch, real near hitserial, farzero, recovery/retrigger and actual Animator source-phase agreement. Native per-frame yielding is required so native overlay blending advances on real frame boundaries. Root coordinates one combined Unity job.

Not guaranteed: exact full-source triangles/garment thickness, forearm/finger passive collisions, native nonlinear motion between bounded probes, hip/foot skin overlap or source self-intersections, whole-body ready-stance separation beyond roots, mobile frame budget, or raster/EOF penetration-free contact. Those require actual frame/geometry evidence; do not call this candidate a complete mesh-safe controller or phone acceptance.

Mutation proof: a standalone temporary copy removes defender-axis motion from SweptContact.Cast. The new moving-defender regression fails (9/10 pass,1fail) while the unchanged full core suite is111/111. No source mutation was applied to main. The first coordinated PlayMode focused job selected only5graphics tests; physical EditMode and native EnterPlayMode tests still need their proper run channel/filter.
