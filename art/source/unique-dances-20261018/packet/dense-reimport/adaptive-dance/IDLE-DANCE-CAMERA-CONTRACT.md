# Large idle → full Dance camera, review-only finite contract

## State contract

Idle does NOT reserve future fullDance extents. Approved imported-neutral review hold has its own exact fullmesh bound (already available from10holdout artifact) and its own camera center. Desired idle fill is80–85% usable landscape canvas height, limited by width/aspect and an explicit UI-safe viewport. This target is not yet artist acceptance; current neutral is not calibrated combat guard. No new height normalization/grounding assumptions.

On Dance request retain current rendered pose+camera; loading precedesvisible entry. Camera and actor transition begin together only after key/support warmups complete. During actual.28s owner entry, use per-pose finite complete rebased envelopes, NOT endpoint union. FullDance camera takes over after entry, remains fixed clip-wide through EOF. Return has its own actual captured76-domain half; camera may approach largeIdle only as return envelopes permit. At returnedneutral endpoint use exact neutral largeIdle camera. Cancel/rosterchange destroys/release old owner and restores appropriate nextcamera; no stale interpolation from last actor. Selection UI can overlap actual fullscreen preview only if intended; viewport for fit must be actual usable canvas, not assumed narrowportrait cell.

## Moving-camera containment math

For fixed orientation and aspect a, transform each sampled body envelope into that camera basis. Let its center/extents be b(t),e(t). Choose a continuous planned image-plane camera center c(t)=Lerp(cIdle,cDance,SmoothStep(u)). Required orthographic halfheight at time t is

r(t)=max(abs(bY-cY)+eY, (abs(bX-cX)+eX)/a).

An interpolated camera size that contains both endpoint envelopes can still clip intermediate moving poses. Therefore evaluate r(t) at ALL38actual entry /38return captured times; never infer safety from endpoint fit. Camera depth must separately keep full near/far extent inside clipplanes. No camera yaw interpolation in this first contract; actorrootyaw is fixed during eachcamera transition. Orientation/yawchanging camera needs separately transformed envelopes perframe.

Proposed smooth size curve is base(u)=Lerp(sIdle,sDance,SmoothStep(u)) plus A*16u²(1-u)² (C1 zero at both endpoints). Choose A=max(0,max_sample[(r(t)*(1+margin)-base(u))/bump(u)]). At endpoints bump=0: exactendpoint bound MUST fit endpoint camera or report infeasible. Never conceal a mismatching endpoint by divisionepsilon, jump or instantcameraresize. A large A is a tightness/usabilityRED even if finitecontainmentPASS. Can subdivide featureleaves or use exactneutral bound at endpoint; cannot silently substitute exactneutral bound for nonzero transitiontime. This formula yields smooth curve but only FINITE sampled containment, no continuouscertificate.

If A creates outward zoom farther than fullDance camera, do not automatically accept it: renderreview and explicit zoomrange limit are required. Alternative legal behavior is delay visible source motion while camera smoothly pre-rolls to an envelope proven for heldneutral, then start sourceentry; this changes timing intentionally and must preservefullsource duration/EOF/return. It does not permanently shrink Idle. No arbitrary source bone edits, materialchanges, scalechanges or posefreeze-as-pass.

## Coordinate/height/floor limits

Input frames are existing native C bounds; actorAnchor maps once. Use same native identity/yaw and uniformscale used by currentproof. Size/centers/native margins scale with wrapper; worldunitgroundcap must map through s. Existing accepted transition proof is rootidentity UNGROUNDED originalweights. Existing footlift envelope/actualplan is reused, no repeat143168/1Mscan. Until actualpoint/cap/commonancestortranslation gate passes, this camera contract remains ungrounded. Height gameplaycalibration and B weights invalidate relevant bounds; no automaticproductionreuse.

## Minimal bounded actual next job (await authorization)

1. Reuse warmed persistent originalweight actor and38+38matrices; derive exactneutral endpoint frame from prior fullholdout, rebasedtransition envelopes elsewhere. Compute A and log endpoint feasibility, maxzoom, camera center/size at every sample for1280×720. Fullscreenusableaspect explicit16:9; do not fit a portraitcell.
2. Test two directions with actual owner and deterministiccamera time; all38+38sample envelope inequalities mustPASS or jobFAIL.10existingfullmeshholdouts are sufficient to crosscheck actualskin against theseenvelopes; no full4759×1Mrepeat. Capture GPUmasks at5fractions eachdirection with actualengineupdate/render fences; include fullmaterial screenshots for largeIdle/entrymid/Dance/end/returnmid. GPUmask must be isolatedactor, not floor/UI.
3. Judge initial/final Idle target80–85% and maximum camera zoom excursion/smoothvelocity from measured frames. No cuts, endpointjumps, one-frameSnap or residual fullDance framing afterreturn. Sourcefullauthoredclock/EOF semantics remain unchanged.
4. Rootyaw±90/170+scale1.8 can first be pureaffinecamera math; actualtransitionroottransformacceptance still requires statedlimited additionalGPUcases. Roll/pitch/nonuniformscale excluded. Budget≤600s, residualdisk≥8GiB, oneengine, explicitrelease.

No Unity/Blender/player launched for this contract. No productionFighterView/SMR.localBounds/catalog hook. Cachedcamera data should be compact scalar curves+centers+inputhashes; no perframe Python/featurequery/BakeMesh/vertex scans.
