# Selection studio camera scope candidate

Implementation is new, Built-in runtime scope; no actor material edits, catalog promotion or RenderPipeline change. Offline Roslyn compilation of Runtime, Editor and Tests passed on 2026-10-18. Unity config generation EXIT0 and final actual PlayMode Renderer GPU/lifecycle test1/1 PASS; see OUTCOME.md for exact qualifications.

## Integration API

- Add `LighFight.Runtime.SelectionStudioLighting` to the world preview camera once.
- Assign `Resources.Load<SelectionStudioLightingConfig>("SelectionStudio/PreviewLighting")` once after generation; assign the game's owned directional key light.
- On roster rebuild call `SetSelectedRenderers(Renderer[])` once, using actors' actual renderers. Arrays are copied and restore-state storage allocated here only.
- Call `SetPreviewEnabled(true)` only for the selection/preview camera render. Call false before fight/loading/destruction; component disable also restores synchronously.
- No per-frame Resources lookup, DynamicGI, materials, SH coefficient conversion or renderer discovery.
- A held but rendered preview still requires a lighting scope each render. Disabled/non-preview cameras apply zero scopes. The scope has no Update/LateUpdate work.

`DenseRigifyReimportReview.BuildSelectionStudioConfig()` creates only Resources/SelectionStudio/NeutralStudio128.asset and PreviewLighting.asset. It verifies the qualified SIB SHA, creates128 RGBAHalf prefiltered8-mip cube and native RGB27 SH. Updates preserve asset GUIDs. Original source maps, current actor normal1 materials and source archive remain untouched; artist normal.5 is only the separately approved dense pilot material.

## Scope semantics

Camera.onPreCull captures ambient mode/probe/intensity, custom reflection/default mode/intensity, owned key enabled state and selected renderer light/reflection probe modes. Uses native SH and default global prefiltered cube, disables only owned key. OnPostRender restores exactly. Another camera interrupting a scope restores it before that camera renders; no nested preview scope is applied until outer completion. Reentrant same camera fails closed. OnDisable and explicit Restore also restore; steady-state callbacks use value structs and preallocated arrays.

## Actual gate

`LighFight.Tests.SelectionStudioLightingTests.ActualRendererPreviewRestoresFightEnvironmentAndDisabledDoesNoWork` uses actual ForwardBase renderer+camera, reads native SH.18 and global reflection.25 GPU values, checks root Flat/light/probe settings after preview, disabled zero applies, nested camera interruption and disable inside rendering. A separate future window should retain a mutation that bypasses restore or global probes to demonstrate meaningful rejection before claiming acceptance.

Full dense143168 original weighted-foot support remains separate, baseline-only. Exact feature-AABB acceleration, warm median/p95/max/alloc and combat inactive-zero gates remain open.
