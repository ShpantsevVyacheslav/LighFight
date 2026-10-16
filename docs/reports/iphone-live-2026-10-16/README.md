# iPhone build 108: live observation

Device: iPhone 17 Pro Max, iOS 27.0.1. LighChat staging build 108; LighFight content `tripo11-diagnostics-ios-20261016-r3`, Unity 6000.3.26f1, Release (not Development).

## Observed, not a controlled comparison

410 one-second samples, elapsed 412.35 s. USB power: Charging throughout. Includes options, fight, and victory; therefore not 7 minutes of continuous battle.

- Active fight: 210 samples, tier 0, target 60 FPS, AA 2, shadows All, screen 2868x1320.
- Fight median FPS 59.994; median per-window p95 frame time 16.827 ms; median whole-process CPU 0.787 core equivalents.
- First observed thermal transition: nominal (0) → fair (1) at elapsed 210.285 s, during fight, still tier 0 / AA 2. This precedes the later options change to tier 2 / AA 4.
- Thermal fair does not imply critical throttling, and stable FPS does not demonstrate low power consumption.
- Native CPU, footprint, thermal and low-power readings are now verified in the installed iOS build. Footprint is for the whole host process.
- GPU timing unavailable in this Release configuration. No watts, degrees Celsius or GPU bottleneck established.

## Instruments connection

CoreDevice app-container copy succeeds. xcdevice reports available over USB; DDI reports compatible and usable. xctrace lists the device Offline and recording fails before starting. Apple device-session log reports `kAMDNotConnectedError`, recommending reconnect. No valid Instruments trace has been collected. User asked to reconnect USB and unlock; pending at report creation.

## Next measurement

Reconnect Instruments and capture CPU/GPU while reproducing heat. For an energy comparison use a cooled phone, fixed quality, brightness and battle sequence, and distinguish charging from an unplugged run. THERMAL-01 remains unresolved.

CSV SHA-256: `82b79d1264b43332e393edb3716a0e78402073c8a90d420629356024ba894ba1`.

## Follow-up snapshot

940 samples, elapsed 948.50 seconds. Latest active fight: fighter 4 versus 5, tier 2, AA 4, target 60, USB charging. Thermal state serious (2); latest FPS 29.99 and p95 33.62 ms. This supports a real heat/performance problem but does not isolate CPU versus GPU or establish which quality setting caused it. Instruments recording is still in progress at this snapshot. Raw data: `build108-observation-02.csv`.

## Instruments validation correction

Exported trace TOC reports duration 10.000 seconds, end reason Device disconnected, recording mode Windowed (5 seconds), despite configured 10-minute limit. CLI process lifetime was not recording duration. This is not a sustained CPU/GPU profile and must not support a root-cause claim. Repeat with verified continuous recording; preserve the long CSV as separate evidence.

## Cooled selection observation

Fresh journal fight-20261016-141411-567-a47ad70b96b6412aa6c20cdc85d2e4ae.csv. Copied 50 flushed samples, 47 stage=selection samples, fighter 4, opponent 3, arena 0, tier 2, AA 4, shadows All, target 60, Charging. Selection median FPS 59.994, median whole-process CPU 0.873 core equivalents. All selection thermal samples nominal (0). Only 50 seconds flushed at copy time; do not claim 60 measured seconds. GPU unavailable; CPU does not isolate Unity from host or identify a bottleneck. Raw data cooled-selection.csv.

## Shadow toggle observation

Same fighter 4 versus 1, arena 0, tier 2 / AA 4 / target 60 / Charging. Fight shadows All: 71 samples, median CPU 1.477 cores, median FPS 59.994. Shadows Disable: 65 samples, median CPU 1.255 cores, median FPS 59.995. Observed CPU median lower by ~15%; not a causal energy estimate: first run nominal and second fair, sequential uncontrolled thermal conditions and bot motions. GPU still unavailable. Long victory interval with shadows All: 214 samples and CPU median 1.420 cores; fair already reached during that interval. Avoid keeping user on victory while waiting between experiments. Raw CSV and grouped summary saved alongside this report.

## 30 FPS sequential observation

63 active-fight samples at target 30, measured median 29.997 FPS, CPU median 1.002 core equivalents, median per-window p95 33.556 ms. Same fighter 4 / opponent 1 / arena 0 / tier 2 / AA 4 / shadows Disable / USB Charging. Prior 60-FPS no-shadow run had CPU median 1.255; observed ~20% reduction, not an isolated causal estimate because all 30-FPS fight samples were thermal serious (2), while prior no-shadow fight was fair (1). Serious first occurred during the intervening victory period before target changed to 30. It does not prove a cooled 30-FPS run heats at the same rate. Raw cooled-30fps.csv, grouped cooled-30fps-summary.json. Stop further user heat testing until cooled controlled runs or functional profiling are ready.
