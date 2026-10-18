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
