# Soft lighting / original RGB-Z diagnostic — not accepted

Unity Metal job exited0 and the heavy window was released immediately. Twelve captures cover imported normal XY, original linear rawRGB normalized/preserveZ, and rawXY custom-Standard control; all factors/maps are unchanged. Six fullbody raster masks agree with all typed-LBS vertices within.94135px and retain8.33% framing margin.

Preserving original blue/Z does not eliminate foil-like cloth appearance. OriginalRGBZ versus importedXY neck mean absolute RGBbyte differences are.312 directional/.234 soft, p99≤5/4. RawXY control mean.128/.089,p99≤2. Maxedge outliers are187/113bytes. These are diagnostic images, not pixel-parity claims: rawRGB procedural texture had no mipchain whereas the actual imported normal has13mips. A future control must match mipcount/filter before attributing edge differences to RGB reconstruction.

Soft angular source-panel quadrature broadens and reduces sharp highlights, but the result remains shiny and is not visually accepted. It uses12directional rays corresponding to2x2samples of each source2m area panel, source positions and relative450/250/300power, panelcosine/distance weighting at actorcentre, normalizedtotal1.1. This is explicitly an angular approximation, not exact positional Cycles area-light integration/radiometry/occlusion. No source roughness/metallic/normal strength was adjusted.

The manual HDR→filmic→PNG result is dark (background RGB2,4,5). Its implicit output transfer is UNQUALIFIED; no color/brightness acceptance. A separate narrow known-input GPU transfer sentinel is prepared to enforce exactlyone explicit IEC-sRGB encoding into a Linear target/readback PNG. Existing FittedFilmic exposure1 remains different from source BlenderAgX.

Original source Mikk tangent transport is pending. Do not run another large lighting grid until it is imported and qualified. These captures are retained as rejected/intermediate diagnostic evidence, not production assets.
