# Local integration verification — 2026-10-18

Unity6000.3.26f1, Built-in pipeline, StandaloneOSX test target. Tests run sequentially in the main project with graphics enabled, not an iPhone build.

- Full EditMode:298/298 passed, no compile errors or exceptions. Results `.tools/autonomous-20261018/full-edit.xml`; job `20261018-141742-testsEditMode-8bd5`.
- Full PlayMode:31/31 passed, no compile errors. The logged `InvalidOperationException: fixture warmup failure` is explicitly injected and expected by `SelectedContentLifecycleTests` to verify failed content warmup recovery, not an unexpected game failure. Results `.tools/autonomous-20261018/full-play.xml`; job `20261018-142539-testsPlayMode-59f3`.
- Focused native contact/close-combat tests:18/18 passed, including moving defender and native Animator pose sampling. These verify bone-capsule collision proxies and bounded source playback; they are not proof of exact whole skinned-mesh nonintersection.
- Focused graphics PlayMode:5/5 passed. Real mobile framebuffer resolution path requires device confirmation; no claim that iPhone heating is resolved.
- Prototype gesture state tests:5/5 passed. Single-pointer attacks, two-pointer yaw/pinch, no vertical tilt, no multi-touch ghost strikes, right-mouse orbit. Browser model loads verified; no user/device acceptance claimed.

Trapezius art work remains under visual review. Original model geometry, UVs, image payloads and original animation clips are preserved in the append-only native67 candidate. No rejected shoulder experiment has been promoted.
