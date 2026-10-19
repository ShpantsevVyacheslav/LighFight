# V6 startup-fenced control PASS; V7 proposed

CPU build EXIT0/36.243s; player EXIT0/12.591s. Startup entry at UTC2026-10-18T23:02:34.6343320Z / uptime2.077479 / frame1 observes splashFinished=false. Finish observed uptime2.504676 / frame966; real settle completes uptime4.506555 / frame5269. Captures at uptimes4.622812,4.670690,4.710474,4.748538,4.787243 all observe finished=true. This directly demonstrates120uncapped frames are not a real-time startup fence.

ON1/ON2, OFF1/OFF2 and ON1/ON3 are each byte-exact across all800000RGBA pixels (maxRGB0, alpha mismatch0). All five corners are [12,18,26,0]. ON2/OFF1 changes29366pixels, maxRGB228/maxAlpha255, as expected for visible probe removal; no acceptance of four-pose projected ROI is inferred from this control alone. V5 same-state drift disappears after the explicit startup fence. Startup capture is a supported confound; old V1–V5 and original phase1 lack contemporaneous splash/uptime evidence, so their individual cause remains unproven. Do not rewrite historical RED.

Evidence `evidence/cpu-startup-control-v6/` preserves exact source, startup/capture timestamps, PNGs, state, raw rest buffer, logs, exit results, source hashes and archive hashes. Only control passed: no original actor buffer, GPU dispatch, timing, mobile or production acceptance.

## Proposed V7, requires root handoff

Minimal patch: keep the V6 splash+real-settle fence and all telemetry; switch ControlOnly=false, separate output CurrentBufferBuildsV7/BufferRunLogsV7. Keep all five predeclared models and numeric thresholds; no geometry, settings, shader, alpha or capture-state edits. Execute CPU first under240sbuild/180splayer/8GiB limits. Four known poses including heldout must uniquely identify P/N/T layout and pass actual projected foreground ROI/outside-zero gate and held-state checks. Only then allow original two actors full unlimited-weight position/finite-PNT and repeated raw-buffer witnesses. Stop on any failure; no GPU or timing automatically. Root reviews CPU evidence before authorizing further work. V7 has not been patched or launched.
