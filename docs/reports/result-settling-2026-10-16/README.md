# Result-pose settling and exact native-curve evaluation

## Problem and phone evidence

Build108 CSV confirms heat and eventual 30 FPS at target60. Cooled sequential run: selection median CPU0.840 cores; fight1.477; victory1.420. No-shadow fight1.255; 30FPS/no-shadow1.002 on already serious phone. These are whole-host CPU readings, not GPU/power or controlled energy comparisons. USB charging and thermal changes confound comparisons. Instruments TOC duration10s/device disconnected: no sustained GPU profile.

## Result presentation

Every result frame previously sampled both combat poses, then winner/healthy-loser previews again; finger/native-overlay/grounding late passes continued after nonloop end. New SettledActorPose runs after final grounding at execution order400, retains local TRS, stops Animator and known pose writers together, and restores original enabled flags upon direct Pose/Preview or Reset. No global FPS, quality, Time.timeScale, mesh, texture, or arena change. Authored source duration/loop overrides carrier policy; loops stay running. Winner and loser complete independently. Exact-equal transforms are not needlessly rewritten (quaternion setters can normalize). Native active-pose lease releases on disable; warm leases survive.

Diagnostics add resultHeld=00/10/01/11 to identify actual settling on next device run. This still renders the 3D scene at selected FPS; no claim that GPU cost is removed.

## Active native curve candidate

Original packed curves already share AnimationCurve objects. Old Apply repeatedly evaluated the same object at identical seconds across channels/joints. Candidate indexes by object identity, evaluates each unique curve once per call, retains all original transform writes/Normalize, detects mutable reference replacements and evaluates changed keys/wraps on every call. No value cache across calls/times. Plan lifetime follows decoded data/pin leases; serialized assets unchanged.

Initial evidence:72 main clip assets /11roles /18,936 phase cases bit-exact localTRS and worldmatrices; all72 evaluator medians faster, median per-clip reduction16.29%, zero warmed managed allocations. Final expanded audit covers all73 source assets /19,199 phase cases, all bit-exact; all73 faster, median per-asset evaluator reduction16.67%, worst candidate/reference ratio0.8766. Source inputs and original meta hashes unchanged. These are Editor timings of the evaluator only, not whole-frame/phone savings.

## Verification status

RED:4 result regression cases failed before change (no settling). First full EditMode277/277 passed. Native-roster PlayMode22/22 passed: all11 models, Victory/fallback/Fall/Defeat/Dance, real final late pass, exact held transforms over frames, grounding counter stops, resume preserves flags. Final combined EditMode285/285 passed; all73-source audit passed. Final combined PlayMode22/22 passed with native-curve candidate enabled.

Independent read-only review found no critical/important defect in settling or unique-curve candidate. Malformed null Joint entries can now fail during prewarm rather than first Apply; current valid source assets unaffected.

## Acceptance remains open

Do not mark THERMAL-01 fixed. Need release phone build and matched cooled active battle + victory CPU/thermal runs; GPU profile still unavailable. Keep source geometry/texture quality. Device testing is deferred until verified code/build is ready.
