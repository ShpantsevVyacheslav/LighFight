# Fresh Sailor rig pilot v1 — draft, not accepted

This tests a completely new local humanoid rig on the untouched original A-pose, rather than inheriting provider100k shoulder weights. Actual source audit: **994,452 vertices / 1,988,914 triangles**, one UVMap, no armature or vertex groups,6 boundary edges, no >2-face or loose edges. Full source and attribute hashes are in `stage01.json`.

Run stage1 from repository root:

```sh
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/01_audit_fit.py
```

The original dense mesh is never decimated, transformed, UV-edited or auto-weighted. An independent copied binding proxy is simplified to~50k triangles solely for affordable cross-section landmarks and subsequent skin-weight solving. This is not a proposed deliverable geometry reduction. Stage1 fits a fresh Rigify Human control metarig and renders front/side/top placement for review. Geometry hashes must remain byte-exact before/after.

## Planned executable stages and acceptance

1. Audit and fit (`01_audit_fit.py`). Inspect shoulders, elbows, wrists, hips/knees, fingers, feet and spine in placement views. Automated landmarks are an initial fit, not anatomical acceptance. Correct the metarig while leaving dense geometry intact.
2. Generate Rigify controls and game DEF chain. Bind only the independent proxy, verify zero unweighted/max4/normalization and no cross-side leakage. Abort if heat solver fails; remeshed proxy fallback is permitted, never dense auto-weighting.
3. Transfer proxy weights onto original dense vertices with closest-face barycentric interpolation, bound surface-distance diagnostics, top4 normalization, and zero-unweighted gate. Add only Armature modifier and groups to dense original. Rehash original positions/UV/polygons/indices and source image bytes. No Blender-only PreserveVolume/CorrectiveSmooth in game export.
4. Test raised arms, arm rotations, elbows and shoulder rhythm, then retarget comparable dance/boxing/kick from native source to this DIFFERENT rig. Native local rotation curves cannot simply be copied to different rest matrices. Inspect closeups and full range, shoulder strain tails, protected regions and contact/floor behavior. This determines quality; triangle preservation alone does not.
5. Export game DEF-only skeleton with baked animation, max4 weights and original geometry/maps, independent roundtrip checks. Keep controls/metarig in versioned .blend. Dense mobile performance and lower geometry options are separate decisions requiring measurements.

No shoulder-quality, complete binding, retarget parity, game compatibility, or phone performance claim is made by stage1.

## Actual local milestone (2026-10-18)

- Stage1 automated shoulder fit was visually rejected despite `fitProblems=[]`: it placed the pivot at pectoralis height. `01b_correct_fit.py` replaces that fit with explicit geometry-reviewed shoulder/clavicle landmarks and re-renders front/side/top. `stage01b-corrected-met-rig.blend` preserves this corrected control metarig.
- Stage2 was actually executed. Fresh control rig generated, with160 DEF bones. Proxy-only heat solve succeeded, no empty major deform groups; tiny unused brow/tongue groups remain. Barycentric transfer reached all994,452 dense vertices, **0 unweighted / max4 weights / maximum sum error1.8254e-7**. Positions, UV and polygon/index hashes exactly match stage1. Saved `stage02-bound-original-dense.blend`. Controls and helper bones are not a game export; unused face/tongue deform bones should be pruned at final export.
- Stage3 actually rendered rest, arm down, overhead with20°clavicle raise, arm forward and120°elbow. Shoulder is visibly smoother in these controlled poses, but overhead stretches collar/upperchest. This is a real visual limitation, not a passing final quality gate.
- Stage4 actually retargeted native dance25/50/75% via world-rest-aware rotation deltas to new torso/spine/neck/head/shoulder/arm/hand FK controls, rendered front+threequarter. It does **not** yet retarget root displacement, legs or fingers, nor bake full animation. The raised sleeve contour and collar/chest pull remain at75%; no complete shoulder fix is claimed. Framing differs slightly from earlier native audit, so this is comparable-phase evidence rather than pixel-parity evidence.

Current status: **dense-preserving fresh-rig feasibility demonstrated; motion quality remains incomplete; no GLB or production asset promoted**. No external service, upload or generation used. Existing native67-bone animation compatibility cannot be presumed for this new160-DEF hierarchy.

Next stage commands (after inspecting fit):

```sh
BLENDER=/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender
"$BLENDER" --background --factory-startup --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/01b_correct_fit.py
"$BLENDER" --background --factory-startup --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/02_proxy_bind_transfer.py -- --reviewed-fit
"$BLENDER" --background --factory-startup --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/03_pose_review.py
"$BLENDER" --background --factory-startup --python-exit-code 1 --python art/drafts/newrig-sailor-v1/tools/04_native_rotation_pilot.py
```

Final75% retarget scene is saved as `stage04-dance75-retarget-review.blend`; `stage04-preservation.json` independently confirms dense attribute hashes exact after retarget. Blender exited normally. Large `.blend` files are local reproducible review artifacts (one exceeds100MB); commit scripts/JSON/PNGs separately unless large-file storage is deliberately configured.

## Full-body motion gallery and portability finding

`gallery-v1/all-motion-fullbody-sheet.png` and `all-motion-shoulders-sheet.png` show26actual rendered images at13authored phases (boxing3/kick3/headhit2/walk2/dance3).54FK controls include root translation scaled by measured height, legs/feet/toes and all30fingersegments. Source phases/times and limitations are in `gallery-v1/manifest.json`. Whole-body minimum reaches−32.953mm; noIKfoot-contact solver is claimed. Box frame0 is authored boxing start, not standalone guard idle. Facial expressions are not retargeted.

**Additional gate caught:** generated Rigify DEF bones use segmented B-bones8–10. Thus gallery-v1 is explicitly a Blender deformation diagnostic, not portableGLB/GPU proof. Armature PreserveVolume isfalse. `09_game_lbs_gate.py` creates a separate single-segment trueLBS variant and independently compares final evaluated mesh samples against weighted4x4matrices before corrective inversion. No game export accepted until that variant is visually checked.

NEW autoheat partition audit (`newrig-partition-audit.json`) found collarCentral mean61.68% shoulder/arm influence and axillaryTorso mean36.18%; not equivalent to earlier native67-bone Neck audit. Geometric region masks are approximate and need visual review before treating every flagged vertex as a leak. This supports a targeted sleeve/body partition experiment before blind smoothing or arbitrary cap flattening.

## Bounded gameLBS + partition result

True matrix-LBS variant (`game-lbs-v1/report.json`) disables52segmentedDEF B-bones, preserves skinweights/basegeometry, and matches actual evaluated mesh against independent weighted4x4skinning on12,000vertices each in dance75% and kick45%: maximum0.271micrometres. This proves the chosen matrix deformation math, not final visual quality or Unity import.

`partition-v1/rest-mask-red-treatment-green-feather.png` shows the actual geometry mask. `partition-v1/samephase-before-after-NOT-ACCEPTED.png` compares unchanged-LBS versus the targeted collar/torso partition at dance50/75%.172,867vertices were treated with smooth regionfalloff; centralcollar arm/clavicle contribution was progressively capped20%, axillarytorso5%, with excess reassignedto spine002/003. Lateral sleeves, distal arms and head weights were retained; max4weights/0unweighted remain. **Viewed result: no compelling shoulderfix; sleeveball andcollarpull remain.** It is not promoted, and the measured collararmfraction must not be equated with proven erroneous leakage. Rastermean absolute difference~1.81/1.87byte is not a qualitymetric.

The gallery is the requested actuallocal new-rig screenshotcollection, with originaldense detail preserved. The experiment proves localfresh-rig/fullgeometry feasibility but doesnot prove the remaining garmentdeformation solved. No new GLB replaces the originalprototype models; noUnity/main/phone changed. `FINAL-LOCAL-REVIEW.json` records allthree originalGLB hashes unchanged.

Next justified art step: deliberately sculpted few local pose-space shoulder/garmentcorrectives on the trueLBS variant, exactprotectedcollar/head/distal anchors, firstsamephase visualgate then shoulder elevation/twist +othermotions/intermediates. `06_designed_cap_corrective.py` is an **unexecuted diagnostic idea**, not a tested fix or accepted target: a numerical radial-envelope limit must not masquerade as anatomical sculpture. No all8export or per-framefullmesh morphbake should proceed before visualacceptance.
