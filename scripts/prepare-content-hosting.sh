#!/bin/sh
set -eu
project_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
: "${UNITY_EDITOR:?Set Unity Editor executable}"
: "${LIGHFIGHT_CONTENT_VERSION:?Choose a new immutable content version}"
content_origin=${LIGHFIGHT_CONTENT_ORIGIN:-https://lighfight-assets-72b24.web.app}
mkdir -p "$project_root/.tools/reports"
for content_platform in Android iOS; do
  LIGHFIGHT_CONTENT_URL="$content_origin/lighfight/$content_platform/$LIGHFIGHT_CONTENT_VERSION" \
    "$UNITY_EDITOR" -batchmode -quit -projectPath "$project_root/unity" \
    -buildTarget "$content_platform" -executeMethod LighFight.Editor.ContentBuild.Build \
    -logFile "$project_root/.tools/reports/content-$content_platform.log"
  python3 - "$project_root" "$content_platform" "$LIGHFIGHT_CONTENT_VERSION" <<'PY'
from pathlib import Path
import shutil,sys
root=Path(sys.argv[1]); platform,version=sys.argv[2:]
source=root/'.tools/content'/platform/version
output=root/'.tools/publish/lighfight'/platform/version
if output.exists(): raise SystemExit('Published staging version already exists; choose a new version')
shutil.copytree(source,output)
state=root/'unity/Assets/AddressableAssetsData'/platform/'addressables_content_state.bin'
if state.exists():
    archive=root/'.tools/release-state'/platform/version
    archive.mkdir(parents=True,exist_ok=True)
    shutil.copy2(state,archive/state.name)
# Mutable pointer is a manifest; catalogs and bundles keep immutable versioned URLs.
shutil.copy2(output/'manifest.json',output.parent/'latest.json')
PY
done
printf 'Prepared content in %s/.tools/publish. No cloud publication performed.\n' "$project_root"
