# Dense original-source Dance review actor (not catalog content)

`DenseRigifyReimportReview.BuildDensePlayableReview` creates the existing agreed prefab path:
`Assets/DenseRigifyReview/PlayableReview/SailorDenseBOXReview.prefab`.
The name remains stable for the independent foot adapter. Its roles are explicit:

- Dance: all 2535 adaptive Float64 source keys, native NLERP/FULL FK, full23.1666667s tiny one-binding clock. No 1536Hz Dance AnimationClip dependency.
- SourceBox: copied qualified Generic BOX clip, diagnostic only, with separate key/memory budget.
- Idle API state: an explicitly labelled imported-default neutral review hold. BOX is never assigned to Idle or called a guard.

The standalone restored mesh, cloned Generic Avatar/controller, bound base/normal/derived metal-smoothness maps and key packet are checked for no FBX or unbound original rough/metal dependency. Original raw sources remain archived unchanged. Artist normal0.5 is explicit and original normal1 remains preserved.

## Owner API

Discover `IActorCurveWarmup` providers as the prototype does; warm all before showing/playing.
`DenseAdaptiveReviewOwner.PlayDance()` captures the actual prior local TRS before changing authority. Its LateUpdate runs after FighterView, before grounding, with0.28s smoothstep entry. It retains the actual authored EOF for one rendered frame, then returns0.28s to the explicit neutral review hold. `ReturnToNeutral()` supports early exit. Position, quaternion and scale blend together; actor world root/preview yaw are never overwritten.

The owner has no automatic combat hook. While neutral/disabled, it performs zero exact source evaluations. Disable aborts/restores the pose captured before entry but retains resident keys; explicit owner disposal releases its bridge idempotently. Prototype's existing typed loading/disposal lifecycle can own this component. This review owner is not yet a production selection hook.

`DenseAdaptivePoseBridge` remains an explicit manual evaluator with no Update/LateUpdate. `AuthoredDuration`, `IsWarm`, `Apply(seconds)` and typed warmup/disposal are its narrow contract. It rejects cold Apply.

## Foot API (agreed with diagnostics_review)

`DenseRigifySupportOwner.PointCount ==143168`, `OriginalFootIndices`, `ImportedFootIndices`, `bones`, `groundOffset`, typed warmup and disposal. Every selected original foot vertex and every positive original influence are retained. The foot adapter samples `SourceGenericBOX.anim` on the same nested Animator GameObject. Restored mesh order is unchanged.

Measured dense renderer lossyScale is100. BakeMesh(false)+full renderer TRS double-scales; compare candidate spaces explicitly. On the qualified source-key actor, false+rigid and true+full agree all renderer vertices with full shader-space LBS within0.7micrometre. Camera/independent floor oracles must not silently assume default BakeMesh+fullTRS.

## Verification state

Portable all7853 source poses and actual Unity bridge source/capture gates are archived under `art/source/unique-dances-20261018/audit/dense-reimport/`.
Owner, updated assembler and two new `DenseAdaptiveReviewPlaybackTests` compile with the actual Unity Roslyn references offline. Persistent prefab build, real owner PlayMode full entry/EOF/return/cancel/disable tests, floor adapter and standalone remain UNRUN until a coordinated engine window. No production catalog promotion or phone acceptance.

`FullAuthorSourceKeys.bytes` is a local reproducible review payload (NDA1,27621700bytes,SHA256296ea14ea4c70fc41d20f1e013d43cebd635daff06b61d3f881be2cf31ac67e4). Its source recipe/Float64 key hashes are under `art/source/unique-dances-20261018/packet/dense-reimport/adaptive-dance/`.
