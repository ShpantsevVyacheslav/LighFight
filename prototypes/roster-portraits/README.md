# Borderless roster portrait study — 2026-10-18

Mobile landscape study, primary 844×390 and 932×430. Three independent AI portraits derived from retained original photos; no 3D-render portrait or frame baked into image. PNG alpha allows free silhouettes on the shared background. Existing Unity UI and fight-design prototype remain unchanged. Names/styles are presentation drafts, not gameplay-stat assignments.

Interactions: roster → real animated 3D detail, previous/next fighter, rotation/zoom, dance icon, fast directional swipe attack preview, separate rules, arena and opponent dialogs, settings tabs with locally persisted choices. No actual battle launches. Graphic/audio/control settings are design preferences only and do not configure Unity. Native model exports and verification are documented in models/README.md. PreviewGuard is a held source combat-start pose with open hands, not an authored breathing/boxing idle. Native attack clips finish before returning to that pose with a 300 ms crossfade. Stats are presentation drafts, not balanced gameplay data. Three portraits are a style trial, not the completed11-fighter roster. Real likeness requires user visual review; do not imply identity-perfect reconstruction.

Files: ../../art/portraits/generated-v1/*.png. Source references: ../../art/references/additional-roster/{retro-denim,striped-sailor,summer-beard}.jpg. Generated with built-in imagegen with transparent_background=true, original photo identity reference; Retro asset additionally used only as treatment reference for other portraits.

Browser verification: all three models load; roster/detail navigation, next fighter, dance, rules 60→90 seconds, graphics tab, arena dialog and opponent selector checked in local landscape wrapper. JS syntax checked. Touch pinch and full swipe mapping still require device validation; this does not resolve the paused iPhone thermal investigation.

## Review corrections — 2026-10-18

The detail viewer now uses a full-phone WebGL canvas, rather than a canvas clipped to the central grid column. Geometry may extend into header/footer/side regions; interactive UI controls remain above it. The physical screen remains the viewport boundary. Removed decorative arrows from Continue / Fight. Arena cards use clean captures of current 3D game environments under arenas/, not photographic source references; see their provenance manifest.

Shoulder deformation during native dance is an open art defect. Matched native FBX/GLB sampling reproduces the same deformation; see models/shoulder-audit*. This UI correction does not claim a rig fix. A validated shoulder-weight or motion replacement pass is still required.

Switching detail actors no longer inserts roster portraits while loading or on error. Only the 3D viewer or a loading/error status is shown. Roster portrait assets remain exclusive to roster/opponent selection.

## Gesture arbitration — 2026-10-18

One-finger or left-mouse directional swipe previews one strike. Two fingers provide horizontal yaw and pinch zoom; vertical movement never tilts the actor. Once a gesture contains two pointers it cannot become a strike when one lifts. Right-mouse drag or keyboard arrows rotate horizontally; wheel/+/- zoom. Native model-viewer camera controls are disabled to prevent competing gesture handlers. Pure input-state tests:5/5 passed. Touch-device validation remains separate from desktop/unit proof.
