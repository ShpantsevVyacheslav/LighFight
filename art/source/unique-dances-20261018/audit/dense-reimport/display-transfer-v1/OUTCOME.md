# Explicit GPU output-transfer gate — PASS

Unity6000.3.26f1 Metal Editor job exited0. No actor, lighting grid, model import or material-factor change was executed. The heavy window was immediately released.

Known float linear inputs0/.18/.5/1 are uploaded to an explicitly linear float texture, passed through the existing game FittedFilmic(exposure1), then exactlyone explicit IEC-sRGB shader, into a LinearARGB32 target. Readback Texture2D is explicitly linear and PNG serializes the resulting encoded bytes. GL.sRGBWrite is explicitlyfalse across both passes and restored.

Actual RGB bytes are0/141/206/232. Worst GPU versus independent CPU formula error.001573503, equivalent.40124byte, under1.01/255 tolerance. Source0 remains exactzero. See CSV and4pixel PNG. This qualifies this adapter only; it does not retroactively qualify the previous implicit-transfer captures, the production path, or BlenderAgX equivalence.

Raw originalRGB normal control now generates13mips and usesBilinear/aniso1, matching actual imported normal count/filter. RawRGB averaging and Unity normal-map mip renormalization/reconstructedZ remain different semantics, explicitly recorded. Original PNG bytes remain unchanged. No color/material/character acceptance or promotion.
