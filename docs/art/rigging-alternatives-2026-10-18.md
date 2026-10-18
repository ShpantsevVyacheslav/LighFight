# Rigging alternatives and dense-mesh pilot

## Verified local inputs

Sailor original A-pose FBX retains 994,452 control vertices / 1,988,914 triangles, no armature or vertex groups. Current native animated prototype GLB contains 99,999 triangles. Dense source topology audit found six boundary edges, zero edges with more than two incident faces and zero loose edges. These counts do not prove deformation-friendly edge flow.

A skeleton can bind a dense mesh without reducing its topology. A separate binding proxy may be used for stable weighting, with those weights transferred to the untouched original surface. More triangles retain surface detail but do not automatically fix joint placement, weights or edge flow. Full-detail authoring and sustained mobile rendering are separate acceptance gates.

## Candidate paths

1. Local fresh Blender rig: fit a reproducible humanoid metarig, build game deform bones, bind separate proxy, transfer normalized weights (max four influences) to dense source. Verify dense position/topology/UV hashes before/after. Test shoulders under raises/twists, then retarget comparable dance/punch/kick/reaction motions. Current pilot owns art/drafts/newrig-sailor-v1; it is not a promoted rig or a proven quality improvement yet.
2. AccuRIG: guided joint placement and skin weighting, FBX/Unity export; official standalone requirements list Windows. This is an alternative benchmark, not a demonstrated fix for these models. No purchase or external upload performed.
3. Adobe Mixamo: free with Adobe ID, humanoid auto-rigging and motion library. Requires clean centered neutral humanoid input; automated results are not guaranteed better. No external upload performed.

Existing automatic native-weight/corrective-smoothing experiments failed visual acceptance; no rejected candidate was substituted into the prototype.

## Sources checked

- https://docs.blender.org/manual/en/5.2/sculpt_paint/weight_paint/editing.html
- https://docs.staging.blender.org/manual/en/latest/modeling/modifiers/modify/data_transfer.html
- https://www.reallusion.com/auto-rig/accurig/
- https://manual.reallusion.com/AccuRig-2/1.0/03-introducing-the-user-interface/system_requirements.htm
- https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html
