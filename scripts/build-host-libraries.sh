#!/bin/sh
set -eu
root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the Unity executable}"
: "${LIGHCHAT_ROOT:?Set LIGHCHAT_ROOT to the LighChat checkout}"
plugin="$LIGHCHAT_ROOT/mobile/packages/lighfight_host"
[ -f "$plugin/pubspec.yaml" ] || { echo 'LighFight plugin is missing' >&2; exit 1; }
mkdir -p "$root/.tools/reports"
case "${1:-}" in
 ios)
  "$root/scripts/unity-stage.sh" ios
  xcodebuild -project "$root/unity/Builds/iOS/Unity-iPhone.xcodeproj" -scheme UnityFramework -configuration Debug -sdk iphoneos -derivedDataPath "$root/.tools/ios-framework" CODE_SIGNING_ALLOWED=NO build > "$root/.tools/reports/ios-framework.log" 2>&1
  python3 - "$root" "$plugin" <<'PY'
import sys, shutil
from pathlib import Path
root, plugin = map(Path, sys.argv[1:])
source = root / '.tools/ios-framework/Build/Products/Debug-iphoneos/UnityFramework.framework'
dest = plugin / 'ios/Frameworks/UnityFramework.framework'
shutil.copytree(source, dest, dirs_exist_ok=True)
shutil.copytree(root / 'unity/Builds/iOS/Data', dest / 'Data', dirs_exist_ok=True)
PY
  ;;
 android)
  "$root/scripts/unity-stage.sh" android
  editor_dir=$(dirname "$(dirname "$(dirname "$(dirname "$UNITY_EDITOR")")")")
  android="$editor_dir/PlaybackEngines/AndroidPlayer"
  java="$android/OpenJDK/bin/java"
  launcher=$(ls "$android"/Tools/gradle/lib/gradle-launcher-*.jar)
  "$java" -classpath "$launcher" org.gradle.launcher.GradleMain -p "$root/unity/Builds/Android" :unityLibrary:assembleDebug > "$root/.tools/reports/android-aar.log" 2>&1
  mkdir -p "$plugin/android/libs"
  cp "$root/unity/Builds/Android/unityLibrary/build/outputs/aar/unityLibrary-debug.aar" "$plugin/android/libs/unityLibrary.aar"
  ;;
 *) echo 'Usage: build-host-libraries.sh ios|android' >&2; exit 1 ;;
esac
