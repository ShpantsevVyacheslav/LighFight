# Native67 full weights — actual geometry, restoration and GPU experiment

## Scope and outcome

Original full Sailor, 994,452 vertices / 1,988,914 triangles / 67 native bones. No geometry reduction, source weight threshold, source material or source map edits. Isolated Unity6000.3.26f1 / Apple M1 / Metal only. No production catalog, game renderer, phone or user acceptance changes.

### Complete correspondence PASS

All 1,305,574 Unity seam vertices decode exact original IDs, covering all994,452 originals. Fixed signed-axis conversion derived from ALL67 true inverse-bind joint locations (not current animated transform defaults), maxjoint residual0.00020435mm. All original/seam positions checked, max0.00014471mm. Every1,988,914 oriented triangle and every corner UV checked, exactly matching original topology/UV, one global winding reversal. No nearest correspondence or incomplete seam representative used for restore.

### Standard SetBoneWeights path rejected

Original variable CSR expands to3,909,499 positive coefficients over all seam duplicates. Importer initially returns3,568,263/max7. Explicit SetBoneWeights on an owned in-memory clone restores3,789,768/max9 but loses119,731 positives across108,860 seam vertices. Actual native mesh storage staysUNorm16; minimum returned positive1/65535. Maximum Float32 weight difference0.00003712034. No threshold waiver. Failed clone destroyed; source mesh unchanged.

This is confirmed numerical loss, not proof that those small coefficients explain the visible shoulder issue.

### Full Float32 GPU numeric mechanism PASS

Structured Float32 CSR buffers preserve all2,922,048 original positive source coefficients bit-exact on actual GPU readback, including4.958639e-18 and max10 per vertex. Variable loops consume all stored influences, no top4/top8 or threshold. Tested all994,452 original vertices at BOX35/70%, Dance45/50%, Kick35%:4,972,260 GPU position evaluations.

Independent CPU double accumulation uses actual sampled Unity Float32 skin matrices and original source weights/positions; maximum GPU-vs-CPU error in WORLD metres 1.70665995786e-07 (predeclared limit1e-6m). Actual mesh local-to-world scale100 is included in error conversion. All5poses physically nonstatic (maxrestdisplacement>.01m) with plausible worldheight. Inputs/output finite and buffers disposed after exit. This validates a review calculation, not a working game renderer or Blender-to-Unity pose fidelity.

## Preserved failures / limits

RestoreV1 compile failed on mutableNativeArray using-variable indexing; fixed with writable aliases, no evidence removed. RestoreV2 incorrectly used current FBX transform poses asbind and failed0.387m; V3 uses trueinversebind positions and passes. GPUV1 incorrectly labeled local-coordinate errors metres; preserved as local-unit diagnostic only. GPUV2 explicitly transforms residuals into worldmetres and is authoritative.

Still open: original Blender offkey oracle vs actualUnity playback, normals/tangents and PBR visual review, GPU output wired into actual render geometry, bounds/floor/trueIdleGuard, resource lifetime, iPhone memory/thermal/frame budgets. Do NOT ship this1M-vertex candidate or mass-promote rigs based on this numeric pass. Existing phone heatP0 remains open.

## Reproduction / files

`tools/prepare_unity_full_weight_packet.py` reads original indexed attributes plus authoritative native67 CSR/oracle (never oldRigifyweights) and creates localbinarypacket. SHAmanifest preserved here. `tools/run_unity_weight_review.py` runs only isolatedproject with600s/6GiB/pressure guards and records actualexit. Revieweditor scripts and compute shader are under `unity/Assets/Native67SpatialReview/`; sourceFBX and packet remain large localartifacts, not ordinaryGitcontent. Each actualrun has its own output directory/log; no overwritten results.
