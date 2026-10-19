#!/bin/bash
set -euo pipefail
stage="${1:-masks}"
case "$stage" in masks|metrics|renders) ;; *) echo "Expected masks, metrics or renders" >&2; exit 2 ;; esac
if [ "$#" -gt 0 ]; then shift; fi
probe_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
exec /Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender \
  --background --factory-startup --threads 6 --python-exit-code 1 \
  --python "$probe_dir/probe_source_off_on.py" -- --stage "$stage" "$@"
