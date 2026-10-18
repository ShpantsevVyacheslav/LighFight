# Read-only transport diagnosis

No Unity or Blender run for this audit. No exporter/oracle edits.

1. Strong candidate: imported animation action is transferred between different rest skeletons. Static FBX has Pose1 and Deformer137; animation-only FBX has neither. All137 common Model defaults differ, including bone local translations/rotations. Blender can infer animation-only rest from posed defaults and convert animation relative to it. Copying rotation_mode alone cannot make that action valid on the static-bind armature. This remains a hypothesis until the one-frame witness below.

One-frame witness in existing import fixture (do not bake/re-export): before transferring action, compare staticRig.data.bones["DEF-breast.L"].matrix_local with animatedRig.data.bones["DEF-breast.L"].matrix_local. Evaluate animatedRig with its own imported action at action.frame_range[0], update depsgraph, compare animatedRig.matrix_world @ animatedRig.pose.bones[n].matrix against inverse(B) @ ref.boneWorldUnityYUp[n] @ B. Then separately compare same static rig after action transplant. Direct-animated PASS + transplant RED proves fixture rest-space mismatch. Direct-animated RED means continue pre-export frozen-pose gate.

2. Independent ordering issue: exporter captures frozen mats before scene.frame_set(frame), then references reread live rig after it. Freeze both references and exported target pose from the same source evaluation; compare source-before/frame-reset/target-after-evaluation at one frame before trusting reference parity. This can exist independently of rest-space mismatch.

Gross local shear is not supported by current saved desired-pose references: across15 poses and136 weighted bones, desired local column nonorthogonality max1.6552e-5, column lengths approximately1 within1.4e-5. This does not certify exporter frozen matrices, but rejects a large reference-pose TRS-representability explanation.

Evidence: static-animated-fbx-model-properties.json; reference-local-trs-orthogonality.json. All animation quality gates remain OPEN.
