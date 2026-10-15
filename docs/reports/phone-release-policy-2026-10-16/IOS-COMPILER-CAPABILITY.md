# Correction: Apple native compilation belongs to Xcode

The initial policy incorrectly assumed successful API reference compilation implied iOS compiler-selection support. Root's actual 269/270 test exposed this; the expected Debug readback was wrong. It was not a runtime regression or an absent compiler API.

Evidence:

- Installed 6000.3.26f1 `UnityEditor.Apple.Extensions.Common.dll`, `UnityEditor.Apple.ScriptingImplementations.AllowIL2CPPCompilerConfigurationSelection()`: read-only Mono.Cecil IL dump returns literal false (`ldc.i4.0; ... ret`). See sdk-capability.log and reproducible sdk-capability.cs/rsp. No Unity engine was launched.
- Unity's official 6000.3 reference implementation checks that capability and returns without applying the setter, telling the caller to configure the generated IDE project: https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Editor/Mono/PlayerSettings.bindings.cs#L1041-L1049
- Official iOS 6000.3 build reference separates Xcode Release/Debug (optimized/testing) from Unity Development Build (symbols, profiler and DEVELOPMENT_BUILD): https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/iphone/ios-building-and-delivering/build-settings-ios
- Installed UnityEditor.xml confirms API signatures but makes no promise every module supports selection. Reference compile alone was insufficient capability proof.

Apply `ios-compiler-correction.patch` to current main paths (BuildPolicy and BuildPolicyEditMode); `git apply --check` passed at preparation. No asmdef/GUID changes. iOS scope now never reads/sets/restores the unsupported compiler selection: it preserves it untouched, pins supported OptimizeSpeed code generation, sets BuildOptions explicitly and restores code generation. Android continues explicit Release/Debug compiler selection and restoration. The existing shell separately selects matching Xcode Release/Debug. External embedded host framework build must honor that configuration explicitly too; setting Unity Development alone does not prove Xcode's selected compiler configuration.

Tests now 11 cases: seven parser, four scope cases covering iOS/Android × Release/Development. iOS asserts compiler value remains exactly the initial value while options/codegen are correct; Android asserts requested compiler value. Every scope deliberately throws and verifies original compiler/codegen restored. This is a capability-specific contract, not removal of the failing assertion. Root must run actual Editor tests; packet has not launched Unity. Patched helper referencecompile succeeded again (`ios-correction-reference-compile.log`). Historical `release-policy.patch` is superseded; use the incremental correction only.

No claim of temperature improvement: real matching Release/Development phone builds and sustained profiling remain required.
