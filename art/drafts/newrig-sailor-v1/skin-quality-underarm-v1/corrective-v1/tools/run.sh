#!/bin/bash
set -euo pipefail
here="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
exec /Users/macbook1/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --threads 6 --python-exit-code 1 --python "$here/run_candidate.py" -- "$@"
