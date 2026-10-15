# Guard and crouched strikes — 2026-10-16

Status: all11 reviewed candidates promoted to local shipping prefabs; phone build/acceptance unverified. This report must not be read as phone acceptance or a thermal fix.

## User feedback and scope

The latest phone retest accepted smoothness, but rejected persistent heat, invisible block and the exercise-squat posture. Joystick DOWN replaces the crouch disc (commit4cbed77; hysteresis and pause/reset tests). Release export policy is commitbb4e8da, preserving authored geometry, maps, AA and frame-rate; no measured temperature claim.

## Diagnosed animation defects

- Old Idle and Guard shared the same BoxingGuard clip. New standing cover uses the original Meshy Block1 Human motion, held at sourcephase0.30; source obtained from the actual web viewer, skeleton-only GLB independently roundtrip-checked through Blender/Unity. Own target Avatars stay unchanged. Low/moving guard continues the existing protected composition and is not claimed to use the new standing Block1.
- Old lower-body crouch was deep air_squat. Idle crouch now combines the original ready stance/guarded arms with a shallow source Body/Legs duck at phase0.30/weight0.55. No authored mesh, weights, materials, root scale or manual joint offsets changed.
- An arms-only punch experiment lost54.784degrees of inherited source torso heading, causing misses. It was rejected. Accepted candidate low jab/cross/body/uppercut retain the COMPLETE original attack including Hips/Spine/Chest, with a legs-only shallow duck. Logical move IDs, hit windows, radii and damage are unchanged. Other kicks still use their standing source clips; CROUCH-36 remains broader unfinished work.
- Upward-only shoe grounding left some low attacks floating. Opt-in signed correction is limited to Crouch and four low strike states; every pose recomputes it from original immutable support points. A first .16m cap and attack-only whitelist failed exact surface QA:42cap-saturated frames and27return frames. Scoped .20m +Crouch whitelist resolves the three-actor cases. All11 exact audit then found12cap failures (Bone3/Witch9), plus3Ranger jab frames with -1.158mm penetration from missing original support vertex14471. Two measured actor caps now use actual max+5mm (.213008/.223233m), bounded<=.30; other9remain.20. Ranger appends exactly that original vertex and all3original weights to grounding AND overlay support sets, preserving old prefixes/defeat supports. Full all11 rerun is required; firstall11 is explicitly RED. No reduced support/model quality: exact all-original shoe vertices and ALL stored influences are independently checked at EOF, including unreadable original meshes through Editor read-only mesh data.

## Verified before all11 promotion

-12 synthetic state/cap/reset/exit/local-pose/heading grounding regressions PASS in actual Unity.
-3actor exact fixed player:8,100actual frames,90cases, full original shoe surface within1mm, no failed frame parity. Own original mesh GUID/localID/fingerprint and influence cap verified. Island/Rose/Retro near/far source comparison; screenshots retained, raw JSON compressed with verified CRC.
-All11 standing block player:5,940actual frames/66cases, max contact-to-EOF position1.131e-6m, rotation0, damage0. Root viewed ready→cover→return contact sheet for every actor. Low/moving block included numeric transition coverage but not a new dedicated clip.
-All11 source upperbody gate:352phases. Nonleg local pose unchanged. Leg blend returns Unity Humanoid legStretch offsets toward original prefab rest (largest2.191mm); accepted only collinear nonlengthening delta<=3mm, scales1, tangential<=1e-5. Explicit3mm mutation RED; no tolerance waiver disguised as floating-point noise.
-New authoring regression ran RED on shipping Bone: Idle==Guard. Must pass after promotion. Existing controller/prefab identity gates preserve own Avatar, original mesh/material/bone references.

## Reproduce / evidence

Review packet `.tools/guard-crouch-v3`: named Editor jobs and actual player probes. Final frozen inputs `combined-roster-final-r2-frozen.json` precede the all11 build; source/asset SHA must match before promotion. No automatic visual acceptance. Build Development StandaloneOSX Metal, capture Update→3coreticks→one render pose→LateUpdate→actual EOF backbuffer; no Camera.Render substitute. Full shoe audit uses captured EOF skin matrices; no4-influence fallback, BakeMesh, importer changes or inferred pre/post frame.

Raw earlier RED and fixed3actor/guard proofs archived with verified ZIP CRC under `.tools/guard-crouch-v3/archived`. Root-owned reproducible QA apps were removed only after evidence capture to provide disk; sourcejobs/manifests/results preserved. Final shipping must contain accepted controller/mask/clip dependencies, not QA MonoBehaviours/scenes.

## Heat remains open

Phone is Offline for Instruments. Latest audit confirms foreground paused/options path can retain camera and actor LateUpdate eligibility; frozen-final-backdrop suspension implementation packet is prepared but not accepted. Existing ShadowsOFF swaps PBR and is not a pure shadow-cost experiment. No native temperature/CPU/GPU/watts evidence has been acquired for the new Release build, so THERMAL-01 stays InProgress. Details: `.tools/thermal-sustain-review/ProductionRenderAudit` and `PauseSuspensionPacket`.

## Final all11 local acceptance

Actual second all11 Development Metal player completed23,760frames/264cases (11×original/candidate×4moves×3gaps×90), max contact→EOF worldposition1.114e-6m/rotation0, farDamage0,62positive candidate cases. Full original surface auditor produced23,760renderer rows, candidateCrouchUnaligned0 at1mm. Every candidate row is checked; original baseline rows are retained as controls, not claimed floor-aligned. Two root-viewed sheets show all11 crouch/4attacks/return; standingBlock sheet also viewed. PNGs preserve all selected near.90phases plus17/20/21/40 at .65/far3; ALLframe JSON/skin matrices remain complete, so reduced redundant screenshots do not reduce numerical coverage.

Promotion verified frozen hashes, exact ownAvatar/mesh/material/bone identity, source support prefixes and onlyRanger14471 append (3original stored influences). All11 original prefabs retain their GUIDs. An immediate second promotion invocation returned `COMBINED_PROMOTION_IDEMPOTENT_NOOP`. QA C#directories and reviewscenes moved outsideAssets to `.tools/guard-crouch-v3/AcceptedReviewQA`; only accepted controller/mask/clip dependencies are committed. Other unreferenced artist review assets stay local, outside catalog bundles.

Independent scoped code review found no confirmed Critical/Important defect. It notes a presentation-path duck-layer reset concern masked by native provider overrides; no visible regression established, no unverified change made. Low/moving guard and low kicks remain acknowledged follow-up scope.

Final actualUnity checks after promotion and QA-code removal:273/273EditMode,8/8PlayMode, exit0. New distinct-cover/fullsource4low authoring test changed RED→GREEN; posture PlayMode confirms source phase/layers and protected retreat. Core101/101passed earlier this turn; Core code is unchanged by posture changes. Thermal status remains open; user accepted earlier smoothness only.

## Delivery boundary

Gameplay source commit `bc28b33` plus prior `4cbed77`/`bb4e8da` sent to the LighChat build owner (task “Нативная лента iOS”) for ordinaryRelease framework+host and fresh embeddedcontent `tripo11-guard-crouch-ios-20261016-r1`. Message delivery succeeded; build start/completion/install is not established by that acknowledgement. Phone heat remains unverified. No later gameplay code change is included by the documentation-only follow-up. Accepted raw23760frame R2 evidence was compressed with verified ZIPCRC to `.tools/guard-crouch-v3/archived/LOW-ALL11-R2-ACCEPTED.zip`; actualsourcejobs, fullsurface rows, summaries and viewed sheets retained. ReproducibleQA app removed after evidence to free phone-build disk. GeneratedUnity YAML contains its normal empty-field trailing spaces; source/docs diffcheck passed; frozen serialized bytes were intentionally not normalized after acceptance.
