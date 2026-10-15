# Character quality: measured pipeline stages

This is an evidence checkpoint, not completed game/iPhone acceptance.

| Stage | Measured finding | Decision |
|---|---|---|
| Tripo dense original → selected rig export | Gala1,933,246→100,000tri; Cobalt1,966,715→99,999tri. Untouched originals retained separately. | Native100k preservation is not original dense restoration. Full-density Master2M donor-rig experiment remains separate; measured desktop cost prevents assuming it is the mobile default. |
| Provider texture export | Gala/Cobalt native base4K, scalar2K, normal1K vs original all4K. | Do not describe the export as all4K. No silent4K direct normal substitution. |
| Scalar encoding | Occupied paired samples favor one sRGBdecode for Gala/Retro/Sailor/Cobalt, two for Summer. | Derived metallic/smoothness must apply source-specific conversion before smoothness inversion. Gala and Cobalt selected calibrated materials are promoted; Retro/Sailor/Summer changes remain pending GPU comparison. |
| Albedo/normal handling | Gala albedo identity outperforms encode/decode. Gala/Cobalt raw normal beats gamma/Yflip/direct original4K on native tangent frame. | Sourcebytes retained. Albedo remains sRGB, normals raw/linear; scalar fix is not global image correction. |
| Unity importer | Cobalt minimal metadata defaulted to wrong texture shape and mismatched tangent/index/file-scale settings; enhanced all-attribute gate rejected them. | Explicit source importer settings; equality covers normals/tangents/colors/UVs/submeshes/blendshapes as well as geometry/skin. Final Cobalt corrected gate passed independently and was integrated in local commit 089879b. |
| Unity pose/runtime | Gala main245frame probe and combined Gala/Cobalt main490sample probe pass; independent sourceGeneric isolated1464sample proofs pass; 227main tests pass. | Distinguish same-data main integration proof from independent source comparison. Shared22Human strikes are not all Tripo-native attacks. |
| Authored defects | Frontlit Gala/Cobalt still show source eye/neck/collar artifacts. | Material correction does not remove geometry/texture defects. Further source detail/repair remains separate. |
| Physical body exclusion | Sphere/capsule source coverage pushed legal jab/cross out of range; anisotropic alternative still fails frozen coverage. | Both rejected, not promoted. Complete-topology BVH prototype preserves geometry but has not met runtime performance gates. Source animation fidelity does not certify pair contact/body exclusion. |

Evidence: gala-main-2026-10-15/main-222-green.xml and main-player/summary.json; isolated-INTEGRATE.md/STATUS.md; mobile-source-textures-2026-10-14.json; original-density Master reports; .tools/tripo-dense-review/PBRScalarAudit and Cobalt packet. Current catalog11Tripo/11total (local commit089879b). Phone build remains pending.

Recovery gate: a dedicated unchanged-code player reproduced64/200 whole-body floor failures, worst−183.999mm during Fall/GetUp and transitions. Shape after removing rigid floorY remains within0.119µm and hipXZ is unchanged. This is an open runtime grounding defect; five native support states passing does not certify shared recovery. Minimal fix and actual transition proof are in progress.

Recovery candidate checkpoint: actual blend-aware player600EOF samples passed at1/3ticks, including early Guard/Greeting interruption; whole-floor minimum−0.000238mm, floor-only source-shape delta≤0.267µm, hipXZ/local bone delta0. Final support-provenance clearance remains under test before main integration.

Contact negative result: complete-topology fixed-array C# prototype passed346standalone assertions, but two-actor skin/refit283–549ms is rejected for realtime. Kept outside production; timing diagnosis continues.

Recovery accepted in local maine9ecef8: final760/0 plus main228/228Metal; 76settled and40freshGreeting samples clear effective recovery provenance and restore normal shoe support. No bone/curve/source geometry changes. All11/all5 real embedded delivery build now running, not yet accepted.

Confirmed UI-stage quality loss: actual Mac Metal player imported portrait0RGB56/60/66→screen129/133/139 viaIMGUI; basic/rounded andHDRfilmic/LDR paths agree, rawsource/importflags unchanged. A custom Graphics.DrawTexture passthroughshader restores56/60/66 in actualplayer. PlainlinearRTcache doesnotfix it andextra-decode8bitcache losesprecision; both rejected. Matrix/crop/tint/rounded/previewRT gates remain under test before mainpromotion. This finding certifies UItransport, not all3Dsource quality.
