# Meshy source versus runtime quality audit

Measured with Blender 4.2.22, mesh loop triangles (not Unity split vertex count).

| Character | Original generation | Reduced body stage | Current mesh |
|---|---:|---:|---:|
| Island | 305044 | 61801 | 87226 |
| Rose | 350168 | ~80000 | 108390 |

Island original: .tools/meshy/island-premium/island-high.glb. This is Meshy 7.1; the earlier Meshy 6 Lite import is a different model and must not be treated as its source.
Island runtime source: Art/Drafts/IslandMeshyStaticFists/island-finger-candidate.fbx.
Rose original: art/source/rose-premium-v2/model.glb.
Rose runtime prefab inherits Art/Drafts/Rose/rose-motions.fbx. Hand preparation FBX has 108378 triangles, final animation export 108390.
Added hand geometry means final totals conceal the much stronger reduction of the body.

Rose fit_rose_fists.py applies DECIMATE at 80000/350168 (~0.2285). bake_rose_skin.py creates a new atlas and exports only base color plus constant roughness; original normal and roughness detail were not rebaked. Unity skin-albedo importer limits the 4096 source to 2048 by default. Island normal importer has a 1024 platform entry; effective platform override must be checked before attributing device appearance to this setting.

Clay renders are in art/previews/source-comparison: equal camera recipe, normalized framing, studio lighting, material and output size; no textures. Both final comparisons use the original Meshy 7.1 T-pose and the game rig rest T-pose. These isolate the unanimated surface; they do not compare a provider animation against a game animation. Full PBR/animation comparison still needs restored source-to-rig asset preparation. Do not claim that all visual degradation is explained by polygon counts alone.

Findings: aggressive body reduction and lost material maps are confirmed pipeline quality regressions. Clay review preserves broad Rose silhouette but loses fine surfaces. Additional factors include altered normals, skinning, and lighting. Do not further decimate or promote smoothing experiments as restoration. Next correction should preserve high-resolution source surface and original PBR maps while transferring the existing skeleton and fists, then verify deformation and mobile performance. No playable asset was replaced by this audit.
