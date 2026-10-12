# Courtyard reference mismatch — 2026-10-12

Both original v1 and retry v2 produced a soldier with a staff, rather than the yard.
The exact v2 local input `assets/references/photo-05.jpg` was independently viewed:
parked white and black cars, gray wall, pink door, red railing, trees and gravel.
The task was submitted without pose_mode or rigging. No remesh requested.

v2 task: `01a12d96-139b-73a5-8e37-dd4d8de3fcde`, SUCCEEDED, consumed35.
Source hash and parameters are recorded in `art/source/courtyard-meshy-v2/generation.json`.
Downloaded GLB was locally rendered from three sides; front shows a soldier.
Both outputs are rejected and excluded from runtime.

Read-only inspection of installed @meshy-ai/meshy-mcp-server0.6.1 shows
file_path is read with fs.readFile and assigned to request.image_url.
No default character pose is added by that handler. This is code-path evidence,
not a capture of the actual remote request. Provider interpretation or connector
routing remains unresolved. Stop paid retries; next verify the reference thumbnail
through the web upload flow before another submission. Scene Compose remains a
composition/image reference tool, not a verified playable arena export.

Local iOS framework build succeeded with current production assets; no claim of
new arena or full high-source character promotion.
