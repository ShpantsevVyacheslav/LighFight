# LighFight Unity prototype

Open this directory with Unity 6.3 LTS. Pinned Editor: 6000.3.26f1 (Apple Silicon).
Imported and compiled with Editor 6000.3.26f1; prototype scene generated.
All 46 EditMode tests passed in Unity (2026-10-10); standalone core: 40/40. Device execution still needs verification.
Android Gradle export (`unityLibrary`, ARM64 IL2CPP) and iOS Xcode export
(`UnityFramework`) completed successfully. These are source exports, not verified
APK/IPA builds or an integrated LighChat release.

1. Import packages. Set Active Input Handling to **Input Manager (Old)** or **Both**
   (prototype uses legacy Input).
2. Run **LighFight → Create prototype scene**. This creates and registers the scene.
3. Enter Play: arrows move/crouch, J/K/U/I attack, Up/Down change attack row,
   Left Shift guards, R resets. On-screen buttons are temporary IMGUI controls.
4. Open Test Runner and run EditMode tests.

This is source scaffolding plus a playable primitive prototype, not production art.
Unity compilation and package resolution are verified locally. Device builds and
visual behavior on phones still need verification.
ContentBootstrap uses a versioned manifest and Addressables catalog to download the
remote starter before creating the game. The CDN is not published yet. HostBridge now emits ready/launched/exit events to the native LighChat plugin.
The Flutter host integration lives in LighChat/mobile/packages/lighfight_host;
physical-device lifecycle verification is still required. No CDN or production credentials included.

See [implementation plan](../docs/LIGHFIGHT_IMPLEMENTATION_PLAN.md).

## Standalone core verification

`../scripts/test-unity-core.sh` compiles the actual Core C# files and runs the same
NUnit combat tests using .NET 8 + NUnitLite. It does not test Unity APIs, rendering,
package imports or native builds. No Unity API stubs are used.

## Batch workflow

Set `UNITY_EDITOR` to the installed Unity executable, then run:

```sh
./scripts/unity-stage.sh setup
./scripts/unity-stage.sh test
LIGHFIGHT_EMBED_PROTOTYPE=1 ./scripts/unity-stage.sh android
LIGHFIGHT_EMBED_PROTOTYPE=1 ./scripts/unity-stage.sh ios
```

Run from the LighFight root. Exports are development projects under `unity/Builds`;
Android export contains the `unityLibrary` module. Signing and host-app integration
are separate steps. Scripts require an activated Editor and the matching platform modules.

The prototype uses 960×540 logical touch coordinates, simultaneous touch polling,
background pause and fixed-tick attack consumption. These controls remain temporary
and do not replace the final localized, safe-area-aware mobile HUD.

## Native host artifacts

`scripts/build-host-libraries.sh` (in the repository root) exports Unity, builds
Debug Android AAR or device iOS UnityFramework, and copies it into the local
LighChat plugin. Set UNITY_EDITOR and LIGHCHAT_ROOT before running. Generated
binaries are ignored. See LighChat/docs/arcitecture/lighfight-unity.md for the
MethodChannel protocol, iOS CocoaPods opt-in, and device acceptance checks.

Production exports require `LIGHFIGHT_MANIFEST_URL` for the target platform.
The `LIGHFIGHT_EMBED_PROTOTYPE=1` flag explicitly exports the local preview.
See `../docs/CONTENT_DELIVERY.md` and `../docs/ART_PIPELINE.md`.
