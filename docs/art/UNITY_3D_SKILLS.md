# Unity / 3D assistance — 2026-10-12

Installed local text skills (no toolkit scripts executed yet):

- Unity-Technologies/skills at 9d282100540601cfddc4bbd69f36a239d7a6f55c: unity-cli.
- scenario-labs/skills at 25b154dde7e84c31b8d7aea88858e81bf34a14f5: scenario-unity-expert, scenario-unity-animation, scenario-unity-rendering-lighting, scenario-blender-expert, scenario-blender-rigging.

Sources: https://github.com/Unity-Technologies/skills and https://github.com/scenario-labs/skills. Files reside under /Users/macbook1/.agents/skills. Unity CLI installed1.0.0-beta.12. Unity6000.3.26f1; Blender4.2.22. Blender toolkit targets5.2; do not assume its scripts or API names work in4.2. Our renderer is Built-in; URP recipes are not applied as a surprise migration.

## Use for the source fidelity investigation

Preserve the vendor mesh, UVs, texture bytes, bind transforms and animation channels. Compare fixed poses before applying retargeting or skin cleanup. Check deformation numerically AND inspect front/side/three-quarter close-ups: face, collar, torso, elbows, wrists, knees. A passing import test does not prove an attractive model or phone performance.

Do not blindly adopt four-influence export defaults: this source has up to8 influences, and the four-influence mutation already fails our measured surface parity gate. Do not remesh the source or rebuild its rig before native-source comparison.

API supports rigging and library animation, including1–10 preset clips per task in current official docs: https://docs.meshy.ai/en/api/animation . Installed Meshy MCP currently exposes one action_id per animation call, not action_ids/motion_task_id. Never bypass connector restrictions with manual credential-bearing HTTP. Applying animation to corrected web rig previously returned Invalid task mode; an API-created alternative rig has24 rather than66 joints. They are not interchangeable.

Victory web card explicitly selected and source URL observed as Animation_Victory_Fist_Pump_withSkin.glb; browser download produced no local file. Official convert accepted corrected source URL as task01a12f2a-2ed1-7371-a5d4-a553656a5596 then FAILED invalid_input/No valid objects found, zero consumed credits. This does not establish why the provider rejects it, nor that the visible web model is empty. Source native pack and production actor promotion remain pending.

## Courtyard draft

Run scripts/art/build_courtyard_reference.py through Blender, then CourtyardReferenceSetup.ImportDraft and CaptureDraft through Unity. Requires retained local photo05 and sedan GLB assets; large draft assets are intentionally not part of this code commit. No GameContent slot changed. NativeMaterialImport performs the existing glTF metallic/roughness channel conversion; car images are extracted byte-for-byte. Layout audit rejects undersized floor or decorative overlap. Margin of1cm prevents float-rounding from shrinking the authored11x4m coverage. Camera Z differs acrossBlender/FBX, verified by the door facing the capture.

Self-review: the initial Unity draft lost photo and car textures; photo recovered by explicit ExtractTextures, car by explicit PBR material creation. Car remains excessively dense for a mobile background prop; retained original is the fidelity baseline, not a performance approval. Gravel repetition and trees are still blockout quality. Do not call this arena final.
