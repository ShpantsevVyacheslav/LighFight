# Actual fight startup ambient fill — 2026-10-18

## Change

`Prototype.Start` already assigned ambientLight=(0.45,0.43,0.40), but HostFightReview and EmbeddedBootstrap serialize AmbientMode.Skybox (0). Startup now explicitly selects AmbientMode.Flat before assigning that existing color. Actor assets, source materials, meshes and texture bytes are unchanged.

## Regression

A graphics-enabled PlayMode test invokes actual Prototype.Start in an ordinary active scene with a black incoming skybox. It disables direct lights and renders an actual diffuse sphere through a Forward camera into a linear floating target. The material represents 18% linear reflectance; its Color property receives the corresponding sRGB value. A second rendering restores the previous Skybox-mode behavior with the same ambient color and black skybox. This is a renderer/scene regression, not a Graphics.Blit SH query or a setter-only assertion.

Metal GPU readback:

- Startup fill: RGBA=(0.031,0.028,0.024,1).
- Skybox-mode control: RGBA=(0,0,0,1).
- Focused test: 1/1 passed.
- Entire FightGraphicsQualityTests fixture: 6/6 passed, zero failed/skipped. This also covers real tier, framebuffer and FPS settings.

Durable test outputs: `.tools/autonomous-20261018/startup-ambient-play.xml` and `graphics-and-ambient-play.xml`, with matching logs. The black-skybox control exercises the previous behavior in the same fixture.

## Fixture corrections retained

The first attempt used WaitForEndOfFrame, which the Unity batch test runner rejects; it was corrected to normal player updates followed by synchronous Camera.Render/ReadPixels. The second attempt incorrectly supplied sRGB 0.18 for the linear 18% card; its actual linear reflectance was about0.027. The corrected fixture supplies Color(0.18).gamma. Both earlier RED logs/XML remain under startup-ambient-batch-fixture-red and startup-ambient-srgb-card-red; neither is presented as a failed production-lighting measurement.

## Scope

This verifies actual startup diffuse illumination on the Mac Editor GPU. It does not accept the new dense Rigify actor, its normal-map appearance, full dance motion, mobile frame budget, or iPhone thermal behavior. Those retain their separate gates.
