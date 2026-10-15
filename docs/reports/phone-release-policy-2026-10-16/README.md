# Phone export policy — thermal investigation remains open

Build104 was a Unity Development export linked as Xcode Debug. Smoothness was accepted by the owner, but strong heat after a few minutes persists. Debug build provenance is a candidate overhead source, not a measured thermal cause.

Ordinary phone exports now default to Release. `LIGHFIGHT_BUILD_CONFIGURATION=Development` explicitly requests diagnostic Development/Debug; unset or `Release` uses Unity BuildOptions.None + OptimizeSpeed, Xcode Release, Android assembleRelease. Empty/unknown values fail before build/host actions. Source geometry, texture resolution, shaders, rendering quality and 60fps target are unchanged.

Apple's installed SDK disables the Unity IL2CPP compiler-configuration setter: on iOS, generated native C++ compiler optimization is selected in Xcode. The policy leaves that unsupported field untouched, chooses supported code-generation/options, and the shell selects the matching Xcode product. Android compiler configuration is scoped/restored along with code generation. Failed builds restore temporary settings.

Four sandbox shell tests passed against the actual script (default/explicit Release, explicit Development and fail-before-tool invalid values). They use fake Unity/Xcode/host directories and prove policy/product/Data-copy behavior only. Actual Unity6000.3.26f1 EditMode: 272/272 passed, including 11 iOS/Android policy/exception-restoration cases (XML attached). A linked phone Release export and actual thermal comparison remain pending. External embedded host build commands must also select Release; this helper does not modify another project's host.

The opt-in telemetry packet remains `.tools/thermal-sustain-review/BUILD-AND-REPRODUCE.md`: 120s screening / 600s sustained with raw frame timings, iOS thermal category, process CPU/footprint, and a shadow-pass-only condition preserving authored materials. No actual thermal success claimed. iPhone was offline in Instruments during preparation; autonomous local work continued.
