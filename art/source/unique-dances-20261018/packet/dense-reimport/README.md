# Dense Rigify Unity review gate — prepared, not yet run

Canonical input is only SailorOriginalDenseDEF-CombinedReview24.fbx from the owner's final manifest. Rejected animation-only files must not be staged.

Owned importer/gate: unity/Assets/DenseRigifyReview/Editor/DenseRigifyReimportReview.cs. Offline Roslyn compilation passed; this is not an engine or player result.

Convert current owner sidecar with prepare_dense_reference.py PACKET /tmp/lighfight-dense-rigify-reference.drq. The converter retains original array payload bytes and double reference coordinates. Large QA sidecar stays outside Assets/Git.

After explicit routed heavy window, copy canonical FBX and four original maps into Assets/DenseRigifyReview, configure review-config.json with model/oracle/map paths and actual source factors, then execute DenseRigifyReimportReview.ConfigureAndRun in a graphics-enabled Mac batch. No catalog assignment.

Geometry: full source vertices, triangle count and corner winding/UV/skin tuple multiset; importer seam duplicates are allowed only for identical original tuples. Imported position/UV/weight tolerance1e-6, joint0.1mm, rotation0.01deg. Spatial cells are candidate lookup, not acceptance. Ambiguous UV cells fail rather than merge.

Animation: actual Generic Animator, AlwaysAnimate/Normal, exact original15 key phases, original4096 mapped vertex LBS plus136worldbones. This does not certify off-key24Hz interpolation.

Materials: original four4K maps, uncompressed, sRGB base only, data maps linear. Original node/factor provenance required. Currently unit scalar factors supported exactly; otherwise only material gate blocks (geometry/keyed still runs).

Captures: full source-skin bounds camera margin and fullbody/neck positiveZ/negativeZ at dance .25/.5/.75, actual material; original vertex LBS avoids BakeMesh scale ambiguity. These Editor captures are not a full-motion player/phone claim.

## Framed GPU diagnostic (prepared, not run)

Run DenseRigifyReimportReview.RunFramedReview in a graphics-enabled Mac batch WITHOUT -quit; its bounded EditorApplication.update coroutine exits explicitly on success/error. Output framed-gpu-review-v1 preserves the rejected immediate renders. All10 other Generic imports remain held.

Existing packed PNG is reused when byte-identical, not reimported. Four exact texel-centre scalar probes compare CPU sourcepack R/A against floating GPU readback after actual Editor updates; async readback fences and bounded value convergence record any initial white samples. Actual renderer matrices update per render, poses span distinct Editor updates, camera aspect is explicitly1. GPU white silhouette bounds must agree with ALL CPU projected typed-readback skinned vertices within3pixels, no automatic zoom compensation. BakeMesh false/true with full/scalefree transforms are recorded independently; no assumed double-scale convention. Phase .50 also renders no-normal diagnostic only.

The source data/material factors remain unchanged. These checks may reject render/material appearance despite geometry/keyed CPU passes. Full24Hz off-key motion remains rejected.
