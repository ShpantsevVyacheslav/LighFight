# Source quality audit — 2026-10-12

Exact source shown in the owner's Meshy screenshot was downloaded by task identity
`01a12cb8-8e0d-75b1-973b-6f75e4c7cbcf` using the Meshy connector.

| Stage | Triangles | Vertices | Skin |
|---|---:|---:|---|
| Visible web source GLB | 103074 | 63240 | No |
| Previous local original GLB | 66364 | 38294 | No |
| Previous native web rig GLB | 66364 | 38294 | Yes |

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
