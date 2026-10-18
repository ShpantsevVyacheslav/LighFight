# Fixed Unity FBX coordinate contract

Source sidecar remains Blender-world metres under canonical B(x,y,z)=(x,z,-y). Actual Unity FBX importer adds one handedness reflection X. QA uses C=ReflectX*B: points C*p; world matrices C*M*inverse(C). Existing JSON points/matrices already transformed by B receive ReflectX only (conjugation for matrices), exactly once. Source geometry, rig, model root and poses are not modified or fitted.

Independent witness: all136 named bind joints match reflected canonical source within0.4574µm. First32 vertices include original named fourweights and show the same reflection, with harmless influence sorting. Root localToWorld determinant1; renderer determinant1,000,000 (centimetre mesh scale100 already included once). Importer bakeAxisConversion=false, useFileScale=true, globalScale1. FBX GlobalSettings YUp+, ZFront+, XCoord+, UnitScaleFactor1 centimetre.

Handedness also reverses face winding: first original triangle[360399,358498,358499] becomes Unity[360399,358499,358498]. Full gate therefore reverses original expected face winding once, retaining original cornerUV/weights. It does not accept either arbitrary winding.

Evidence: unity-coordinate-witness.csv, unity-handedness-witness.json, fbx-global-settings.json. First failed unconverted oracle log retained in unity-first-red/editor.log. These establish coordinate conversion, not animation or final art acceptance.
