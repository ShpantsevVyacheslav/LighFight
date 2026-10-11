# Source quality audit — 2026-10-12

Exact source shown in the owner's Meshy screenshot was downloaded by task identity
`01a12cb8-8e0d-75b1-973b-6f75e4c7cbcf` using the Meshy connector.

| Stage | Triangles | Vertices | Skin |
|---|---:|---:|---|
| Visible web source GLB | 103074 | 63240 | No |
| Previous local original GLB | 66364 | 38294 | No |
| Previous native web rig GLB | 66364 | 38294 | Yes |
| New API rig of exact visible source | 103074 | 63244 | Yes, 24 joints |

The two source models differ. The old source-to-rig comparison did not establish
parity with the currently displayed web source. These numbers alone do not prove
that Unity decimated the model or that this accounts for the full visual loss.
Hashes and counts are retained in `art/source/island-web-visible/comparison.json`.

`SourceQualityComparison.Run` imports the visible source without skin or motion,
uses its original PBR textures, and captures identical framing with lit and unlit
materials. A source portrait replaces the guard-pose Island roster image.
The model's imported root axis conversion must be preserved when applying yaw;
overwriting the full root rotation produced a sideways diagnostic capture, fixed
before accepting the portrait.

Pending: controlled old-source/native-rig/game pose comparison, texture import
and mobile platform overrides, material channels/tangent normals, actual device
render resolution. The high-detail source is a diagnostic draft and has not yet
replaced the animated game fighter. No claim of completed quality restoration.

New source rig preserves all 103074 triangles. Task `01a12cce-fe74-76da-a18c-87a071282081`. API result has 24 joints versus the older web rig’s 66, so animation and hand deformation parity remain unproven; do not promote based on triangle count alone.

## Controlled provider checks (2026-10-12)

The exact source and API rig agree in evaluated neutral surface: 103074 triangles,
63240 source vertices / 63244 API vertices; maximum normalized nearest distance
5.96e-7. Original embedded albedo/normal/MR image hashes are identical.
The jab and greeting diagnostic renders reproduce sleeve/torso distortion before
Unity, using original albedo only. This isolates deformation from Unity lighting.

A separate direct **web humanoid rig** was created with its crotch marker corrected
from abdomen to anatomical hip region: task `01a12ce8-2266-7554-86ad-e0d5f2017332`.
Connector status reports 0 consumed credits. Running GLB export retains 103074
triangles, 63240 vertices, 66 skin joints and byte-identical UV accessor and PBR
images. Evaluated neutral-surface maximum difference is 4.03e-7 normalized height.
Running still reveals sleeve and leg/cloth deformation before Unity. Correcting
markers alone does not establish quality acceptance.

Direct web FBX export succeeded for **Wave_One_Hand**, with original skin and
textures in `art/source/island-web-visible/web-greeting-fbx`. It contains the
character's 103074 triangles plus an 80-triangle diagnostic helper. The FBX has
80 bones including exporter end bones. Diagnostic Blender rendering excludes
the helper. Do not use the temporary FBX-to-GLB conversion: default glTF export
warned that it discards influences above four. Production must use native FBX or
an explicitly verified eight-influence path.

Portrait capture now reuses SourceQualityComparison for Island, so rerunning the
roster capture cannot replace the neutral source portrait with the old combat rig.
Unity source capture compiled/executed successfully; 119 EditMode tests passed,
2 source merge tests passed, wrong-joint-target mutation detected. New web rig and
wave are diagnostic candidates; game actor has not yet been replaced by them.

Material ablation now captures full PBR, no normal map, diffuse-only and unlit with identical camera/mesh/light. Disabling normals exposes stronger face facets; disabling metallic/specular reduces highlights but does not repair deformation. Thus blanket removal of normal maps is rejected. Images: `island-visible-no-normal.png`, `island-visible-diffuse.png`. Source capture batch exited 0; no production material altered.
