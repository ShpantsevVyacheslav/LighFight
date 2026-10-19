# Playable zero-twist preview — 19 October 2026

Local URL: http://127.0.0.1:8765/prototypes/roster-portraits/twist-review/

Full original Sailor:994452control vertices,1305574seam vertices,1988914triangles. All positive full-bary weights preserved exactly under seam expansion, max7 influences exported across8Float32 slots. No topology reduction or weight pruning. Raw4K base/normal maps preserved; metallic and roughness channels repacked from original decoded maps. Babylon PBR differs from Unity snapshot lighting/shading.

Three clips: Guard constant native box_02 initial combat-start pose, original Bubble dance, zero-twist Bubble with250ms correction envelope. Guard is NOT a new authored fighting idle.96Hz local-TRS bake; source motion and game assets untouched. UI provides0/100selection, pause/scrub and350ms weighted return transition. A comparison switch retains timeline/pause in controller code; concurrent manual interaction during testing prevents an isolated end-to-end assertion of that control.

Roundtrip gate:12exact-key holds across3clips, sampled original full-positive source hierarchy vs decoded glTF. Max world position discrepancy1.44e-7m (<1um gate). All exported weight slots verified against source for every seam vertex. Initial exporter failures retained in draft output; preview-node channel offset and coordinate-scale baking were fixed before acceptance.

Browser visually loaded real model and dance midpoint. Shader uses8influence channels. Screenshots captured from actual browser. No exact framebuffer-parity, normal handedness numerical proof, phone benchmark or shipped-app acceptance claim. Current app/prototype roster models were not replaced.153MB binary is local review data, not committed/released production optimization.
