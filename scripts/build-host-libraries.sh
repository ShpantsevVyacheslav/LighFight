#!/bin/sh
set -eu
# Validate before Unity, host mutation, or build output creation.
case "${LIGHFIGHT_BUILD_CONFIGURATION-Release}" in
 Release) xcode_configuration=Release; gradle_task=assembleRelease; aar_variant=release ;;
 Development) xcode_configuration=Debug; gradle_task=assembleDebug; aar_variant=debug ;;
 *) echo 'LIGHFIGHT_BUILD_CONFIGURATION must be Release or Development' >&2; exit 2 ;;
esac
root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the Unity executable}"
: "${LIGHCHAT_ROOT:?Set LIGHCHAT_ROOT to the LighChat checkout}"
plugin="$LIGHCHAT_ROOT/mobile/packages/lighfight_host"
[ -f "$plugin/pubspec.yaml" ] || { echo 'LighFight plugin is missing' >&2; exit 1; }
mkdir -p "$root/.tools/reports"
case "${1:-}" in
 ios)
  "$root/scripts/unity-stage.sh" ios
  xcodebuild -project "$root/unity/Builds/iOS/Unity-iPhone.xcodeproj" -scheme UnityFramework -configuration "$xcode_configuration" -sdk iphoneos -derivedDataPath "$root/.tools/ios-framework" CODE_SIGNING_ALLOWED=NO build > "$root/.tools/reports/ios-framework.log" 2>&1
  python3 - "$root" "$plugin" "$xcode_configuration" <<'PY'
import sys, shutil
from pathlib import Path
root, plugin = map(Path, sys.argv[1:3])
configuration = sys.argv[3]
source = root / ('.tools/ios-framework/Build/Products/' + configuration + '-iphoneos/UnityFramework.framework')
dest = plugin / 'ios/Frameworks/UnityFramework.framework'
import uuid
staging = dest.with_name(dest.name + '.next-' + uuid.uuid4().hex)
backup = dest.with_name(dest.name + '.previous-' + uuid.uuid4().hex)
dest.parent.mkdir(parents=True, exist_ok=True)
shutil.copytree(source, staging)
if (staging / 'Data').exists(): shutil.rmtree(staging / 'Data')
shutil.copytree(root / 'unity/Builds/iOS/Data', staging / 'Data')
had_previous = dest.exists()
if had_previous: dest.rename(backup)
try: staging.rename(dest)
except Exception:
    if had_previous: backup.rename(dest)
    raise
if had_previous: shutil.rmtree(backup)
PY
  ;;
 android)
  "$root/scripts/unity-stage.sh" android
  editor_dir=$(dirname "$(dirname "$(dirname "$(dirname "$UNITY_EDITOR")")")")
  android="$editor_dir/PlaybackEngines/AndroidPlayer"
  java="$android/OpenJDK/bin/java"
  launcher=$(ls "$android"/Tools/gradle/lib/gradle-launcher-*.jar)
  "$java" -classpath "$launcher" org.gradle.launcher.GradleMain -p "$root/unity/Builds/Android" :unityLibrary:"$gradle_task" > "$root/.tools/reports/android-aar.log" 2>&1
  mkdir -p "$plugin/android/libs"
  cp "$root/unity/Builds/Android/unityLibrary/build/outputs/aar/unityLibrary-$aar_variant.aar" "$plugin/android/libs/unityLibrary.aar"
  ;;
 *) echo 'Usage: build-host-libraries.sh ios|android' >&2; exit 1 ;;
esac
