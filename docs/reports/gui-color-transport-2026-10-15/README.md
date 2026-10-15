# Confirmed menu texture color transport defect — 2026-10-15

The actual macOS Metal player exposed an extra display encoding in GUI.DrawTexture in the project's linear color space. Imported portrait background (57,60,66) became (130,133,139); direct camera gray124 became185 when displayed through the old preview path. Source mesh/detail/texture pixels were unchanged.

The dedicated texture-only Graphics.DrawTexture material performs normal linear sampling/output. It replaces twelve existing texture draw sites, respects the production positive axis-aligned GUI.matrix, crop/stretch/fit, explicit tint vs GUI.color, alpha, round corners and repaint timing. One owned material is disposed by Prototype.OnDestroy. Shader lives in Resources; no copied framebuffer/source texture caches. Text labels and their authored linear colors are unchanged. Border strokes and rotated/sheared matrices are explicitly unsupported because current production has neither.

## Verified before main integration

Actual windowed Metal player with nonuniform scale1.2×1.3 and offset20,70, Half4xMSAA, LDR and fitted-filmic cameras: zero color failures and zero owned material after disposal. Independent arithmetic verification:58 checks,0 failed. Same binary bypass mutation:18 color failures/exit1; omitted-dispose mutation:1 surviving owned material/exit1. Pixel captures and actual logs are retained under Evidence; reproduction source/settings are under ReproductionProject. Root inspected the LDR capture.

## Main integration

Applied exact twelve draw sites plus OnDestroy disposal. Main Metal EditMode aggregate passed229/229 (main-229-green.xml); actual all-eleven/five-arena selected-content player passed (main-player-r2):33loads/33releases,0HTTP/0errors,0remaining bundles. Original portrait background57/60/66 is reproduced exactly; root viewed roster and Cobalt title/material frame. No iPhone color/performance claim until the new framework is built and exercised. This fixes one confirmed display stage; source neck/collar artifacts, skinning and source detail limitations remain separate.
