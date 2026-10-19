# Bounded current-source100k-twoactor correctness fixture

UNRELEASED review only. Main/settings/original source assets unchanged. Offline reference C# compilation EXIT0 (installed Unity assemblies); Python syntax and full snapshot verification PASS. Staged257files/116215205source bytes in OWN `.tools/skinning-current100k-review`, unresolvedGUID0. No dense assets/GPUBatched. Sources: full current Core/Runtime/NativeSupport plus recursive selected prefab GUID/import closure and explicit filmic shader; main ProjectSettings and package-lock copied unchanged, only isolated screencapture module added. Fixture scripts have one named class per file. Original assets/importer metas preserved.

Heavy owner must be explicitly released first. `run_bounded.py` refuses active Unity/Blender/fixture player processes and logs argv/PID/budget/exit. Disk preflight currently16GiBfree: require>=8GiBfree before each job; owned project cap5GiB for two apps+Library+source, reserve1GiB temporary headroom (6GiB maximum planning budget). No dense imports. If exceeded/timeout, stop and report; never delete others' outputs. Main source change invalidates snapshot and blocks builds.

From repository root, sequential explicit commands:

```sh
python3 docs/reports/skinning-mode-audit-2026-10-19/fixture/run_bounded.py build-CPU
python3 docs/reports/skinning-mode-audit-2026-10-19/fixture/run_bounded.py player-CPU
python3 docs/reports/skinning-mode-audit-2026-10-19/fixture/run_bounded.py build-GPU
python3 docs/reports/skinning-mode-audit-2026-10-19/fixture/run_bounded.py player-GPU
/Users/macbook1/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3 docs/reports/skinning-mode-audit-2026-10-19/fixture/compare.py
```

Each build<=240s external PID watchdog; each correctness player<=180s, with internal coroutine180s watchdog. Planning upper bound14minutes for both builds/players; expected lower, not a timing claim. No simultaneous engines. Log+result JSON under OWN/RunLogs. All main inputs/source/fixture hash-verified immediately before each build. Current source snapshot, scene file, persistent GUID checks, selected asset dependencies and actual compiled DLL hashes retained per mode. After import serializes owned-only settings, verify may reject intentional serialization differences; investigate rather than weaken current-main/source checks.

Correctness DEFAULT: two mirrored-facing original100k Master actors, guard/jab,120warmframes; fixed Greeting0 and4jab phases, each repeated after .5real seconds+6stableEOFs with actual Animator/overlay/blend/TRS fences. Exactly1automatic camera render each EOF. No timing samples run without explicit permit. Full local matrix fixtures, input renderer counts, effective AA/quality/skin/fps settings, observed poses, calibrated known1MiBallocation probe, marker discovery, source snapshot SHA and runtime errors saved. CPU repeat frozen frames require bit-identical pixels; CPU/GPU retain original maxchannel16/over2fraction.001/alpha0 and matrix1e-5 thresholds. Failures stop promotion; no raster epsilon change. No Camera.Render/BakeMesh used.

**Remaining gate:** this fixture does NOT yet implement validated GetVertexBuffer position/normal readback or actual Metal dispatch capture. Configuration/compute capability/BakeMesh cannot substitute. `compare.py` alone never issues profile permit. It requires explicit `--review-approved` plus `CurrentSkinningEvidence/dispatch-and-visual-review.json` matching snapshot, PASS actualGPUBufferParity, true metalComputeDispatchObserved, PASS visualReview and SHA-verified real artifact paths. If tooling cannot supply these, label correctness/dispatch incomplete and stop before timing. Current authorized run is CPUrepeat+CPU/GPU correctness only; do not run profile from this document automatically.

After all required review gates and root timing authorization, same existing players can be run with `--profile`;200samples per guard/jab after120warmframes, no source/IO/memory/image operations in measured loop. Report raw calibration support, async GPU valid counts, median/p95/worst. No phone/thermal/currentRigify or GPU promotion acceptance is implied.
