# Framed GPU review — 2026-10-18

Unity 6000.3.26f1, Metal Editor, actual engine update frames and asynchronous GPU readback. Review job exited 0; this is not production, player, phone, or material acceptance.

## Proven

- Four exact texel-centre GPU samples of the actual packed metallic/smoothness texture match CPU R/A exactly (max error 0). All succeeded on their first attempt. A white-first-frame scalar upload explanation is rejected for this framed capture.
- Six full-body raster masks at dance phases .25/.50/.75, front/back, agree with projection of all 1,021,226 imported seam vertices and typed engine-readback weights: worst bounding-edge difference .9414063 pixels. All expected bounds retain 8.33% viewport margin. The prior immediate-frame cropped-foot result is not present after actual frames; no automatic camera shrink was applied.
- Actual Standard variant keywords are _METALLICGLOSSMAP and _NORMALMAP. Changing only _Metallic/_Glossiness uniforms to zero changes zero RGB bytes, as expected when the mapped variant is active.
- Original map bytes/factors/4K uncompressed inputs and exact derived scalar packing remain unchanged.
- BakeMesh coordinate diagnostic records actual renderer lossyScale (100,100,100). In this importer fixture, BakeMesh(true) followed by renderer localToWorld agrees with independent typed LBS within .551 micrometres. BakeMesh(false)+fullTRS produces ~100m error; the measured fixture result must be retained rather than assumed from another transform setup.

## Still rejected / open

- Original four-map Standard render still shows pronounced foil-like striping on cloth and bright hair. No material acceptance.
- At identical .50 front pose/camera, disabling ONLY _NORMALMAP removes the foil-like striped highlight and yields a much calmer cloth appearance. This is a diagnostic, not a proposed final no-normal material. It isolates the normal-map/tangent-space path as the next lead; it does not yet distinguish imported normal texture decode, tangent basis/handedness, original normal-map convention, or lighting response.
- Normal-map source bytes and strength have not been altered. No roughness/metallic factors were adjusted to disguise the issue.
- Source ground minima and rejected 24Hz off-key animation fidelity remain separate existing REDs. The new adaptive BOX source fit is not part of this FBX or this Unity run.
- No catalog promotion, full-motion player acceptance, or phone claim.

Compare unity-dance-0.50-positiveZ-neck.png with unity-dance-0.50-positiveZ-neck-DIAGNOSTIC-no-normal.png. All quantitative values are in adjacent CSV/TXT artifacts.
