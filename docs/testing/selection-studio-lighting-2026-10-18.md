# Selection studio lighting — 18 October 2026

Production commit `c721644` adds a small precomputed128cubemap/native RGB27 SH fixture scoped to the selected character camera. Actor materials and source geometry are unchanged. The component snapshots/restores fight ambient/probe/reflection/light and selected-renderer probe settings, including disable and nested-camera paths. No DynamicGI update, per-frame image bake, MPB, ExecuteAlways or new URP migration.

Actual graphics PlayMode: SelectionStudioLightingTests1/1PASS. Actual owner integration: SelectionStudioPrototypeIntegrationTests2/2PASS, covering real Start/BeginFight/roster rebuild and deferred streamed startup with no initial actor. Root parsed both NUnit XML files. Early invalid EditMode lifecycle/Flat-probe fixtures were rejected; they do not prove a runtime SH propagation delay.

Generated Resources fixture: `unity/Assets/LighFight/Resources/SelectionStudio/PreviewLighting.asset`, cubemap `NeutralStudio128.asset`. Qualified source SIB SHA03ff5432a09eb70af0adb6a6064f8109732a051bb4af310928e44db463660992. Original actor materials are unaffected; the separate full-density Sailor review's normal-strength0.5 is an explicit artist candidate, not an original-source factor.

Detailed evidence: `art/source/unique-dances-20261018/audit/dense-reimport/selection-studio-config-v1/`. Test XML originals also saved under `/tmp/lighfight-selection-studio-play-tests-r5.xml` and `/tmp/lighfight-selection-studio-prototype-tests.xml` during this run. GPU/owner tests do not certify iPhone thermals or the new full-density actor's skin quality.
