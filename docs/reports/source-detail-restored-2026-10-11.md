# Source detail restoration

Replaced the playable Island and Rose meshes with original Meshy body geometry, original UVs and original PBR texture maps. No body decimation or surface smoothing in this restoration. Original hands are excluded; existing tested closed fists/cuffs and Island watch remain. A narrow wrist overlap joins the source body to those retained parts; this is not a newly retopologized seamless surface.

| Fighter | Previous playable triangles | Restored body | Retained wrists/fists/watch | New total |
|---|---:|---:|---:|---:|
| Island (3) | 87226 | 272194 | 32958 | 305104 |
| Rose (4) | 108390 | 333118 | 32253 | 365371 |

Existing 24-bone skeletons, animation controllers and contact timings are preserved. Body weights are transferred by barycentric projection from the existing skinned surface and normalized to the strongest four influences. Island elbow, knee, calf and closed-fist corrective offsets are transferred too. This preserves their behavior but does not repair all defects of the original Meshy rig or source geometry.

Restored albedo and normals: 4096px. Original metallic/roughness: 2048px. Standard PBR materials use original normal maps and the glTF metallic blue/roughness green channels converted to Unity metallic red/smoothness alpha. Old hand materials remain on their original UVs. Mesh assets use binary serialization (about 53MB and 20MB on disk); compact serialization does not reduce triangles.

Reproduction: run scripts/art/restore_source_detail.py with Blender and argument island or rose, then LighFight.Editor.SourceDetailRestore.Run in Unity. Blender working files remain in art/source/restored-detail, outside Assets. Original Meshy source locations are recorded in source-quality-audit-2026-10-11.md.

## Verification

Initial full graphics EditMode suite: 109/109 passed. New tests require source-detail triangle counts and 4K albedo/normal/PBR bindings, normalized weights and finite bounded skin in every attack's startup/contact pose. Reverting both playable prefabs to reduced meshes makes both new cases fail on triangle thresholds (source-detail-mutation.xml). Final full graphics EditMode suite after binary compaction and watch boundary correction: 109/109 passed (source-detail-final.xml). Unity close-up screenshots: art/previews/character-quality/fighter-3-restored-close.png and fighter-4-restored-close.png. They show source surface restored but also remaining wrist shade transitions and original hair artifacts.

## Self-check

No Meshy credits spent. No regeneration, push, deployment or independent iOS build. iPhone performance of the larger meshes and 4K materials is not verified here. Original hair artifacts, rigid fists, wrist material transitions and rig limitations remain; restoring source detail is not a claim of finished professional character art.
