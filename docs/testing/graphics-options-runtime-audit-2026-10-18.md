# Runtime graphics/FPS options audit — 2026-10-18

## Baseline actually implemented

The existing menu was functional: `lf.graphics`, `lf.fps`, and the independent `lf.shadows` toggle persisted; `Application.targetFrameRate` requested30/60; lights, materials and arena shadow flags followed the toggle. Selection preview resolution already used65%/85%/100%. Maximum selected4x MSAA/high shadow maps; Low and High both selected2x MSAA, medium shadow maps and25m shadow distance in combat. No combat framebuffer scaling existed. Desktop VSync could supersede the requested frame cap. Runtime has no automatic thermal frame cap; this work does not add one.

## Runtime changes

| Tier | Mobile framebuffer, each axis | MSAA | Shadow map | Shadow distance | Light shadows when enabled |
|---|---|---|---|---|---|
| Battery/Low |65%|off|Low|10m|Hard|
| High |85%|2x|Medium|18m|Soft|
| Maximum |100%|4x|High|25m|Soft|

The source meshes, textures, compression/import settings, authored clips, skinning weights and materials are not reduced. All tiers keep unlimited skinning, texture mip limit0, and the existing Maximum LOD bias4. Lower tiers change the pixel/lighting budget. Independent shadow OFF remains effective in every tier; quality does not silently override it. Requested FPS remains independently30 or60, with desktop VSync disabled so it does not override that cap. Invalid persisted tier/FPS values are normalized and saved.

The actual project pipeline is Built-in (`GraphicsSettings.m_CustomRenderPipeline=0`), so mobile combat uses `Screen.SetResolution`, not an unused URP render-scale property. The first-use player framebuffer size is retained as a baseline; switching back to Maximum restores it rather than repeatedly scaling the already reduced framebuffer. Orientation transposes the stored baseline. The desktop window is not resized. Existing selection preview scaling remains active and uses the same tier scale; because mobile screen resolution is also lower, its preview render target scales further relative to the original display size. Selection AA now follows the actual tier (Low1sample rather than2x).

Visual quality is reapplied when actors are configured and when entering combat, including lights on newly loaded/inactive hierarchies. These reapplications do not rewrite preferences. Options changes still persist preferences through the existing path. Sound now includes inactive sources so a newly activated actor follows the setting.

## Automated verification and limits

Added PlayMode cases exercise actual `QualitySettings`, light shadows,30/60 target requests, VSync removal, persisted reload, invalid preference normalization, unchanged full-detail settings, and mobile framebuffer sizing/aspect/Maximum restoration. Existing EditMode shadow regression expectations now include Low hard shadows and restore VSync after each case. No Unity process was launched by this subtask; the root agent runs the coordinated test batch and reports its results separately. `git diff --check` passed on the edited source/tests.

The mobile-only `Screen.SetResolution` branch requires a player to establish actual accepted framebuffer dimensions; Editor size calculations are not device proof. Unity applies a resolution request after the frame, and hardware/OS refresh or load can make achieved FPS lower than requested. AA is subject to platform support. No iPhone thermal improvement or60FPS performance guarantee is claimed. Current heating investigation remains open.

API references: [Unity Screen.SetResolution](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/screen/setresolution), [Unity Application.targetFrameRate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html).
