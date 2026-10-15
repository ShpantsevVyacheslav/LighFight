# Island original-native single-strike review — offline packet

Status: PREPARED ONLY. No Unity compilation, trajectory measurement, GPU frame, accepted segment or gameplay change has been performed for this packet. Host phone build owns the heavy window. No source image/geometry/UV/bind/weight data edited; no main writes.

## Exact authority

`inputs.json` freezes original native7 FBX SHA + original maps SHA. Native box_01 source frames1–54 (0–53/24seconds), box_02 frames1–68 (0–67/24), front_kick_01 frames1–61 (0–2.5), at24fps. These are whole-take boundaries, NOT attack segment boundaries. No in/out/contact times are invented. Unity verifies these durations/type and measures actual imported vertex count,67bones,100000triangles/maxweights. The source hash must equal `5cc8b7208f3267cb1e20909bcff4eb04a9b9bf58ce7f28ebb77bbdb516c41a24`.

Earlier `IslandCandidateEvidence/Native/native-sheet.png` is four-phase **Human SDK** evidence, not rawGeneric authority. Visually box_01 has open hands near face; box_02 starts crouched and ends extended; front_kick_01 appears a turning/backward strike at phase.35 despite provider name. None establishes a usable closed fist, exact contact trajectory, facing or completed recovery. `dense-mixamo/box-01.reference.json` belongs to different65bone earlier input, not this67bone source and is deliberately excluded. Never map full boxing loop to Guard or one swipe blindly.

## Next run, after host releases heavy window

Use the existing owned `ProductionStreamPlayerUnity` snapshot (not main), containing current `NativeSupportPoseData`. Copy ONLY packet Editor/Runtime files into `Assets/IslandNativeStrikeReviewTools/Editor` and `/Runtime`. No copies of whole main/roster or texture arrays needed. The packer requires the existing LNC1 NativeSupportPoseData API; no duplicate class or GUID substitution. Keep original map files outside the project until a real material/GPU phase is scheduled. Model importer settings apply ONLY to own SHA-identical review copy.

Run in that owned project, with absolute `-islandNativeSource` original FBX:

```
Unity -batchmode -nographics -projectPath OWNED_PROJECT -executeMethod IslandNativeStrikeReviewJob.Prepare -islandNativeSource ABSOLUTE_NATIVE_FBX -logFile OWNED_PREPARE_LOG -quit
python3 PACKET/analyze_trajectories.py OWNED_PROJECT/IslandStrikeEvidence
```

Job exports whole original Transform channels without re-keying/trim/time scaling. Existing validated LNC1 codec is scoped to `Assets/IslandNativeStrikeReview/Poses`; bitwise key/value/tangent/weights/wrap roundtrip gate runs immediately after each pose export. SourceCurveSHA, full stored meshSHA and original bytes SHA are checked. Original Generic direct-channel evaluator vs packed replay every1/240s + exact final frame: full original surface finite/error gate. CSV includes every transform, positions relative original root and full surface floor. No sparse human/thumb inference.

`candidate-windows.UNACCEPTED.json` lists only extension extrema of each hand/foot relative torso, bracketing minima and source finger endpoint distances. Euclidean reach alone cannot establish attack direction, contact or a closed fist; inspect full vector trajectories and actual mesh. A take with multiple peaks or no recovered endpoint remains rejected until one isolated event is explicitly selected. Raw times are always original seconds; source curves remain whole and exact.

To run actual original-Generic-controller→packed EOF fidelity after trajectory review:

```
Unity -batchmode -projectPath OWNED_PROJECT -executeMethod IslandNativeStrikeReviewJob.BuildCodecProbe -islandReviewPlayer OWNED_APP_PATH -logFile OWNED_BUILD_LOG -quit
OWNED_APP_EXECUTABLE -islandReviewOutput OWNED_OUTPUT
```

Run windowed; WaitForEndOfFrame is not a headless screenshot substitute. Runtime oracle uses original imported Generic AnimatorController (not cloned legacy SampleAnimation). Alternating1/3core samples per frame, current source time evaluated every tick; original world full-mesh comparison before yield and EOF, no tolerance waiver if root-motion/controller semantics disagree. This intentionally detects the earlier runtime Generic-vs-legacy issue. It is a codec diagnostic, **not gameplay contact acceptance**. Full BakeMesh each sample is QA only, never a performance claim.

## Required subsequent segment/target gates (all PENDING)

1. Choose explicit original source start/contactpeak/recoveredend after measuring both hands/feet. Inspect source finger bones+mesh, knuckle positions, thumb outside curled fingers; open/slapping poses cannot be called punch. Native finger curves must remain authoritative; no shared generic fist overlay. Record actor heading and actual source kick direction, not provider label.
2. Preserve whole original pose asset. Segment metadata may map elapsedtime into original seconds only; one outward attack per input, no loop, re-trigger only through recovery. Startup/recovery and3active ticks remain meaningful; tune no hit radii and apply no noncontact damage.
3. Target actual calibrated Island prefab: compare rawGeneric against copied target after native FK overlay, same actual2× actor scale/heading/rootXZ anchor and grounding. Mesh/binds/weights hash unchanged. Human phase carrier is not geometric source authority.
4. Full surface every phase at1/240 plus60/90/120Hz holds; entry actual shared Guard→segment and segment→Guard/restart, Walk→attack, target fingers/clothing/feet; original all-shoe floor reference, selfarm/chest and opponent-body/feet penetration. Source endpoint continuity must be measured, not hidden by a fast blend. Measure every blend frame; native source start/end may be incompatible with current Guard.
5. Real Update→SampleContactPose→late root/fingers/native hook→ground→LateUpdate→EOF: world bones/striking surface match same sampled phase/heading, rootmovement and3coreticks/frame. Original physical contact spheres, active windows and health checks. Near/far/sidestep/evade/return/interrupt holdouts, both sourcehands audited. Add red mutation skipping native hook, wrong segment/striking side, lost return; require green after restoring.
6. Actual source-PBR full-body and close hands frame at entry/startup/contact/recovery/return, plus paired fighters actual fightcamera. No quality or phone acceptance until those frames reviewed. Current packet creates no catalog entries, guard changes or combat promotion.

This packet intentionally stops at preparation/measurement. It does not claim the unresolved segment/contact/transition gates are already implemented or green.
