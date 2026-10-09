#!/bin/sh
set -eu
project_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the Unity executable path}"
mkdir -p "$project_root/.tools/reports"
case "${1:-}" in
  setup) method=LighFight.Editor.Setup.Create ;;
  android) method=LighFight.Editor.Setup.ExportAndroid ;;
  ios) method=LighFight.Editor.Setup.ExportIos ;;
  test)
    exec "$UNITY_EDITOR" -batchmode -nographics -projectPath "$project_root/unity" -runTests -testPlatform EditMode -testResults "$project_root/.tools/reports/unity-tests.xml" -logFile "$project_root/.tools/reports/unity-test.log"
    ;;
  *) echo 'Usage: unity-stage.sh setup|test|android|ios' >&2; exit 1 ;;
esac
exec "$UNITY_EDITOR" -batchmode -quit -projectPath "$project_root/unity" -executeMethod "$method" -logFile "$project_root/.tools/reports/unity-${1}.log"
