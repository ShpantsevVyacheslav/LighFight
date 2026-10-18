# Autonomous fight-quality implementation

User authorization: work autonomously, no waiting for intermediate approvals or phone tests; preserve all original character geometry/textures, no decimation/compression. Fresh rigs may spread to remaining actors ONLY after successful visual/export tests. Main repo has pre-existing dirty art/Unity files: stage named changes only.

## Units and ownership

1. Root — prototype input: prototypes/roster-portraits/preview-input.js + tests, app.js, index.html. Match existing Unity preview semantics: one finger/leftmouse attack; two fingers horizontal orbit+pinch; rightmouse horizontal orbit, wheel zoom. Disable model-viewer built-in orbit/pan, fixed polar angle. Test arbitration, pinch cancellation, no ghoststrike, no verticalorbit; browser visual gate.
2. Unity combat owner — PhysicalContacts.cs, FighterView and core contact/separation logic. Capture failing geometric-contact/overlap cases before fix; moving-defender CCD, exactdamageevidence, blockedsourcefollowthrough/return and body separation. Report proxy limitations.
3. Unity quality owner — FightOptions.cs + focused graphics/applier helpers/tests and named integration lines. Audit what already works. Implement real framebuffer, AA/shadow budgets/FPS without source asset reductions. Verify persistence and actor/light recreation; mobile actualthermal acceptance remains open.
4. Blender art owner — versioned local shoulder correction, unique authored dance inventory. Preserve originaltriangles/UV/maps; current fresh rig NOT accepted. Gate dance + othermoves, exportableLBS + fewcorrectives only after visualgate; no blanket massrerig early.
5. Root — remaining arena generation/import: use Tripo and verified project reference photographs (photo01 kitchen, photo02 open space, photo03 meeting, photo04 washroom). Preserve original and any explicitly labelled derived cutaway reference. Review full room subject, depth, walkable floor and framing before promotion. Meshy text kitchen rejected by user,25 credits already spent; no texture/promotion. Tripo direct kitchen photo extracted only table/chairs; rejected; cutaway retry in progress. Record each actual charge in provider-specific ledger.
6. Root or availableUnityowner — assign unique actualdance assets/motions to roster only after inventory; distinct labels alone are notunique. Preserve sourceprovenance and verify hierarchy/retarget.

## Integration gates

- One Unity editor perproject, coordinated root unified test/build job, no competing editortests.
- Isolated core tests, targeted Unity graphics/contact tests then appropriate broader tests once.
- Browser gesture arbitration is not an iPhone gameplay verdict.
- Master-detail retention is separate from realtimemobile renderbudget; no claimcoolingfixed withoutdeviceevidence.
- New full-detail rigs are conditional, notdone unless shoulder+othermotion+portableexport accepted.
- Record statuses backlog/BACKLOG.md as code/local-tested/user-tested separately. Deliver concreteartifacts/reports and screenshots; don'taskuser to validateintermediate steps.
