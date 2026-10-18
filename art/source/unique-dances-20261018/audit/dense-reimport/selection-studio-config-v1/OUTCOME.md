# Selection camera studio scope — actual PlayMode GPU pass

Final actual PlayMode test: **1/1 PASS, Unity EXIT0**, `lighfight-selection-studio-play-tests-r5.xml`. Config generation EXIT0. Normal runtime MonoBehaviour; **no ExecuteAlways**, MPBs, material edits, DynamicGI calls or Update/LateUpdate. Existing Built-in pipeline remains.

The actual ForwardBase renderer receives native SH.18±.004 and decoded global reflection.25±.004 during preview. The test verifies exact incoming Flat ambient probe/mode/intensity, reflection/default mode/intensity, owned key-light enabled state and selected renderer probe modes after rendering. It also checks disabled/non-preview zero scope applications, nested-camera failclosed baseline and disable inside rendering. No full suite or phone/performance acceptance is claimed.

Generated small assets: `Resources/SelectionStudio/PreviewLighting.asset` (GUID ab61f019344d049e2be9a286c7584638) and NeutralStudio128.asset (GUID6150714f8fd8043a3875cbbc47224593). Cube128 RGBAHalf,8mips, serialized~2MiB. Exact qualified source binary SHA verified, native27SH rederived by previously qualified native basis conversion. Config provenance and validated-file hashes retained.

## Withdrawn fixture diagnoses

First PlayMode invocation discovered0 tests because the new test was initially in the Editor-only asmdef; it was never a pass. Initial EditMode nativeSH failures did not exercise production component subscription. A temporary ExecuteAlways experiment established diffuse/reflection GPU correctness but exposed another fixture error: engine Flat mode regenerated the probe after the fixture manually set zero. Production ExecuteAlways and experimental SH MPBs were removed. Test moved into the EXISTING SelectedContentPlayMode test assembly (asmdef unchanged) and now snapshots actual Flat probe at the camera boundary. **No runtime SH propagation lag has been proven.** Earlier RED XML retained for traceability; these are fixture REDs, not meaningful production mutation proof.

A deliberate production restore/probe mutation is still pending a future coordinated window. SourceBox assembler was not executed in this window. Full dense143168-point support baseline/performance/acceleration and accepted full Dance remain separate open gates. Current actor materials retain source normal strength1; normal.5 is only separately approved dense artist pilot material.

## Production owner integration follow-up

Actual PlayMode `SelectionStudioPrototypeIntegrationTests`: **2/2 PASS, EXIT0**. Real Prototype Start/BeginFight/public roster rebuild/opaque-grid hooks restore an armed scope; actual deferred streamed startup has a null player safely until selected assets complete and then binds the loaded renderer. The test invokes the real pre-cull method deliberately at a transition boundary; it is an owner/lifecycle check, while the preceding1/1 test remains the actual Renderer GPU oracle. Final XML/log archived here. Root production hooks/config committedc721644; no actor material/catalog changes.
