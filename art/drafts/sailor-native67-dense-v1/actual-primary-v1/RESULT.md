# Raw own-dance native67 versus fresh Rigify control

Completed headless Blender4.2 CPU6, exit0,90.18s. No transfer/candidate weights, C1, corrective, DQS or source edits. Three authored dance phases.45/.50/.55; identical shared pose-follow cameras front/back-three-quarter/world−X.18actual clay images plus18 separately labeled unoccluded Xray joint passes. The joint overlay is a diagnostic projection, not visibility through cloth.

**Visual outcome:** native67 already gathers at the sleeve/underarm, but it is substantially milder than fresh Rigify at.45/.50. Rigify pulls torso cloth toward the sleeve more strongly. These are not equivalent failures. Neither is accepted as a finished shoulder solution; a versioned original-dense native67 transfer remains a reasonable controlled A/B, not a promised fix.

**Pose confound:** actual semantic head vectors differ consistently across all three phases: R upperarm11.392–11.393°, R forearm10.447°; L upperarm9.882°, L forearm11.058°. Thus raw-retarget equality does not establish native-versus-Rigify pose correspondence. Fixed rest-axis/anatomical mapping is a plausible explanation; do not attribute the visual difference exclusively to weights. `joint-correspondence.json` records actual world vectors, lengths, rest-relative angles, world−X camera-depth components and neck-to-shoulder distances. No anatomical weight-error diagnosis is made from bone names.

All136DEF and mapped-control matrices exactly match the unchanged raw retarget at each phase; C1 invocation count0. Source bone proof NPZs retain both actual rigs. Native cluster-authoritative67heads agree within.696µm; all49992native rest vertices agree within.123µm. Actual LBS versus independent source-weight evaluation: dense full994452vertices ≤.3172µm, native full49992 ≤.2372µm. Original attributes, sparse weights and bind hashes unchanged after all renders. Dense1988914triangles and native99999triangles remain separate original meshes, so there is no same-topology cross-rig error claim.

Clay uses the existing8sample diagnostic lighting, not art/material acceptance. Shared upper-body framing can clip distal limbs; no full-body/framing acceptance is inferred. Xray images expose actual world joints only. No Unity/phone/runtime quality claim.

Reproduce from repository root with an unused owned output:

```
/Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python art/drafts/sailor-native67-dense-v1/tools/compare_raw_native_rigify.py -- --output art/drafts/sailor-native67-dense-v1/actual-primary-v2 --max-seconds 600
```

`compose_primary.py` and `audit_primary_joints.py` currently target the retained v1 output. Raw source provenance and inherited frozen evaluator hashes are in report.json. Blender exited and the heavy window was explicitly released before offline composition/audit. No further engine work is authorized by this result.
