# Actual gameplay gate — pending a parent-authorized GPU window

This packet detects sampled source-surface intersection and candidate main-component containment. It does not yet implement continuous collision detection, root separation, strike response, or damage acceptance. The existing .65 m jab/cross zero-damage recordings remain RED. Mono first-contact CPU remains above the 4 ms two-actor target in two recorded cases.

## Qualification

Retain every original 99,999 face for surface hits. A separate containment orientation array excludes detached face 3337 and reverses face 2789; it changes no original geometry, UV, skin weights, binds, animation, or renderer. The main 99,998-face component is combinatorially closed with cyclic vertex links, but deformation self-intersections and geometric regularity have not been certified. Multi-ray parity is a candidate observation. Conflicting votes, near-coplanar/edge ambiguity, insufficient valid rays, and exhausted work return Indeterminate. No cap is added to the detached microfragment.

## Read-only adapter gate

Copy only the canonical C# files listed in README plus the frozen fixture bytes into an isolated review project. Warm both selected actors through IActorCurveWarmup before input, with a bounded real-time timeout and finally/disposal on cancellation. Validate original mesh, four weights, binds, bone order and source hashes. Production FullPreviewMotion must be false. Attach SourceSurfaceReviewTap after the existing BeforeContact pose sampler; retain the existing validator and damage system. LastStatus Clear is only absence of a sampled surface witness: inspect LastPairInspection, because Contained and Boundary are not safe clearance.

Capture exact matrices at contact time, live matrices, and QA baked-world vertices. Compare each with the independently frozen full-face oracle; do not regenerate expected witnesses to accommodate a failing implementation. Capture EOF rendered pose separately. CPU BakeMesh agreement is not raster proof.

## Required response implementation and gameplay proof

1. Before accepting ready/input movement, test swept root movement against the full source mesh. Use conservative temporal bounds and exact narrow queries; unresolved intervals or budget exhaustion must stop at the last qualified nonpenetrating state, never become Clear. An endpoint-only test is insufficient. Map bounds and both actors' root motion must be included.
2. Derive a bounded root translation/sliding response from actual witnesses. Re-sample pose/facing after lateral changes. Do not chase the active strike hand by continuously separating the opponent. Preserve source poses, feet/floor, tracking state and Walk transitions.
3. In the attack phase distinguish hand/forearm versus target head/torso and passive limbs by original bone-weighted witness anatomy. A passive limb obstruction is physical contact, not a new block mechanic. Stop/retract at the earliest real swept contact before penetration. Damage requires a legal surface impact; never inflate a capsule or award by distance.
4. Exercise real Update -> core -> ready separation -> sampled animation/contact -> Late/EOF ordering. Test jab and cross at .65 and .9 m, both facings, movement into contact, near corners and bounds. Require positive legal impacts/damage, no ready-body or active limb crossing, and source/render agreement. A zero-damage near profile fails.
5. Actual far 2.5 m and sidestep 1.3 m at frame 8 must miss with zero damage; a translated offline 4 m geometry fixture is not this gameplay proof. Include kicks only where current approved calibration permits them.
6. Record continuous interval bounds, original face IDs, witness anatomy, phase, damage event, root/facing changes, floor and camera margins. Measure portable Unity Mono and IL2CPP CPU/wall/allocations separately from .NET 6; no 60 Hz claim before actual player measurements.

## Mutations and termination

Mutate: skip warmup/capture; omit a frozen hit face; treat exhausted budget as Clear; bypass containment; bypass swept interval; disable stop/retract; use old phase-only impact gate. Each must fail a relevant positive/penetration assertion. Far/side cases must remain negative. Guard every probe with bounded deadlines, persistent failure propagation, and a top-level finally that writes the report and quits; a no-yield exception must not leave a player waiting forever.
