# Initial mobile rendering budget — 18 October 2026

Fresh mobile installs use High / 30 FPS. Desktop remains Maximum / 60 FPS. Existing explicit preferences are retained on either platform. Geometry, skin weights and original texture data are unchanged.

`Prototype.LoadOptions()` chooses the platform; `LoadOptionsForDevice(bool)` loads the same persisted options and applies actual graphics settings. Four new cases exercise fresh mobile/desktop and persisted Maximum/60 choices through that load path.

Actual graphics-backed Unity PlayMode: **12/12 passed**, including 10 graphics cases and 2 dense review-owner lifecycle cases. Root parsed the attached XML. Runtime 31.0549611 seconds; no skipped cases. Existing render-budget, frame-rate persistence, resolution, invalid-preference and actual diffuse-lighting tests passed.

This is a safer initial budget, not an iPhone thermal acceptance or a new installed phone build. Existing user Maximum/60 preferences are intentionally not silently overridden. The dense review actor is not promoted by these tests.
