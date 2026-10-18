# Qualified broad studio IBL — real renderer proof and visual result

FinalStudioR4 Unity6000.3.26f1 Metal Editor job exited0. All engines released immediately.36 actual3D PNGs plus lighting anchors were captured; no production catalog/material/model was promoted.

## Fixed lighting/display path

- One neutral128cubemap with8 GGX-prefiltered mips plus independently Lambert-convolved L2 diffuseSH; broadpanel directions/relativepower follow the source3area setup, no sharp directional sun. This is derived neutral studio lookdev, not exact finiteBlenderareas/Cycles/AgX equivalence.
- An ordinary empty unsaved scene is madeACTIVE before explicitCustom ambientSH and customreflection are installed. Camera.scene is the same scene. In this fresh headless process, sole startupUntitled is replaced with a new unsaved scene; no scene asset is saved or user source file changed. Source/main RenderSettings are not used as an implicit lighting authority.
- All27 native SH coefficients are read back. Mathematical real-Ylm→Unity coefficient conversion is derived from native nineunitresponses aboveconstant2offset, with constant18%ambient and independent extra-direction tests. Worst independentbasis error2.087e-7. No scene/material/lightbrightness fit is used in that conversion.
- ACTUAL ForwardBase renderer with scene-ambient probe (LightProbeUsage.Off), Camera.Render and HDR centrepixel readback matches native CPUdiffuseSH within.00041318. This replaces the rejected Graphics.BlitSHprobe, which has no per-objectSH bindings and returned0. OldBlitzeros are explicitlyUNQUALIFIED, not proof that the actual renderer ambient was black.
- Actual renderer unity_SpecCube0 HDRdecode matches the analytic neutral studio environment across12directions within.00234712; cubemap orientation matches the declared basis, noYflip.
- Fixedexposure1, existinggameFittedFilmic, exactlyone explicitIEC-sRGB output transfer into a Linear target/rawPNG. Samejob known0/.18/.5/1 GPU sentinel passes (maximum.402byte error).
- Linear-vector18%graycard, dielectric(F0.04/smoothness.5) and chrome(metal1/smoothness.95) anchors are captured before subjects. Example central PNGRGB: gray130/130/130, dielectric135/135/135, chrome196/196/196. Those are reflected outputs under this studio, not a claim18%reflectance must produce a fixed18%PNGvalue.

## Source baseline and explicit art variants

Original geometry/UV/binds/namedweights/mapbytes remain unchanged. Originalsource4Kmaps, metallic/roughness factors and their channel packing remain. Source original-normal strength1 is retained as the baseline. Strength.5 and.25 are EXPLICIT DERIVED ARTIST CALIBRATION variants, not exactsource materialsettings and not modifications to the originalnormal texture.

All36subject PNGs use the same environment, exposure, output transfer and camera perpose. Three frozenoriginalDance poses .25/.50/.75 are set from originalboneworld*K_i (bindderivedonly), Animator disabled; these are STATIC POSE diagnostics, not fullDanceplayback/interpolation acceptance. SourceSkin max3.108µm, bonejoint max2.570µm, boneangle max.000418°. Fullbody raster margin8.33%, sixsourcebaseline masks max1.465698px difference against alltypedweighted vertices.

## Visual assessment

Unlike the rejected punctual/underqualifiedambient captures, the real studio IBL renders have filled skin, natural brown/blond hair and substantially calmer cloth; the prior chromedarkface/specular-dominant appearance is resolved under this qualified lighting model. Source-strength1 still has strong originalnormal ripple, also independently visible in BlenderSUN sourcecontrol. Strength.5 is my preferred review candidate: retains cloth detail while reducing the pronounced striped normal effect. Strength.25 is smoother, with more loss of the originalnormal detail. This is a bounded visual recommendation, not automatic production acceptance.

Checked front/back .25/.50/.75, especially raised-arm .75 neckcontour. Lighting/calibration changes do not alter anatomy or rig deformation. Any final judgement on the residualclavicle/collar shape belongs to the samefrozenpose comparison; no sourcebone editing or geometry smoothing was applied here.

The sourceTspace256facet comparison still has local residualmax2.242° (mostframes match~4e-6°); it remains separately recorded, not silently called bitexact. Originalfloorpenetration, fullDance transport/interpolation, actor/player lifecycle, actualphoneperformance, fullrosterquality and productionintegration remain separate open gates. Root independently fixed realgame Prototype.Start ambientMode and has6/6 graphics tests; this Editorstudio does not replace that runtime regression.

## Retained failures

Two scene-construction fixture failures occurred before GPU/source work (EditorAdditive with unsavedUntitled, then runtimeCreateScene outsidePlayMode). The third run rejected an unqualified BlitSHprobe and raw-Ylm/nativebasis mismatch. Logs/oldCSV are retained and labelled; only finalR4 actualRenderer evidence qualifies the studio images.
