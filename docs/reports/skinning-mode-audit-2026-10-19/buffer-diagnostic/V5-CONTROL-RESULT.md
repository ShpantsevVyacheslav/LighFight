# V5 control: same-state drift remains RED

Review-only CPU build EXIT0 (36.637 s), player EXIT0 (8.185 s), Unity 6000.3.26f1 / Metal / Apple M1 / 800×1000. Control completed without runtime errors; completion is not calibration acceptance. Heavy window explicitly released after exit. No GPU, timing, original actor buffer witness, main settings or production source changes.

The fixed first known pose was captured at five successive automatic end-of-frame fences (frames 132–136): ON1, ON2, OFF1, OFF2, ON3. Each fence observed exactly one automatic camera render. A preceding asynchronous raw-buffer read matched V4 context. No sentinel, alpha correction, clear-alpha change, threshold reduction or alternative output model was introduced.

| Pair | max RGB | pixels RGB delta >2 | alpha mismatch pixels | max alpha delta |
|---|---:|---:|---:|---:|
| ON1 / ON2 | 28 | 29,991 | 769,941 | 29 |
| OFF1 / OFF2 | 17 | 577 | 799,389 | 26 |
| ON2 / OFF1 | 119 | 29,943 | 799,309 | 105 |
| ON1 / ON3 | 81 | 800,000 | 770,485 | 107 |

Corner RGBA progresses [28,29,31,205] → [26,27,30,176] → [25,26,30,150] → [23,25,29,124] → [21,24,29,98]. Both ON/ON and OFF/OFF drift. Thus changing the last visible object alone is insufficient to explain V4. This does not establish the source of the drift or explain original phase-1 GPU repeatability RED (which was actor-localized with alphaMismatch0).

All 35 read-only telemetry samples retain GL.sRGBWrite=true, camera clear RGBA=(.1,.12,.15,0), SolidColor, HDR and MSAA enabled, no explicit camera target. Every onPostRender sees the same 800×1000 temporary HDR target: R16G16B16A16_SFloat / ARGBHalf, linear (sRGB=false), MSAA4, one alpha component. Other capture observations see no active render target. Screenshot textures are R8G8B8A8_UNorm / RGBA32 with alpha. Format availability proves storage capability only: the frozen project has preserveFramebufferAlpha=0, so valid/stable physical framebuffer alpha is not established. The filmic shader passes source.a through and has no explicit blending; its source is logged ARGBHalf/Linear/supported. No observed target descriptor or GL.sRGBWrite change explains the drift.

Calibration/foreground-outside-ROI acceptance remains BLOCKED. All prior RED evidence remains intact, including original phase1 and V1–V4. Frozen original snapshot SHA remains 1ba15bd0ef44d7f56662c0cbc97162bc0cdca39e906be866745cacb2044085f9; later production material fix is deliberately outside this historical fixture. Next work must isolate capture/resolve behavior under a separately declared version, not fit new deformation models or claim unsupported GPU. No further engine run is authorized by this result.

Evidence: `evidence/cpu-control-drift-v5/` contains five PNGs, state snapshots, raw rest buffer/header, all telemetry, comparison JSON, exact source snapshots and source manifest, build/player logs/results, and archive SHA256 manifest. `control-runtime.json` compute=true is device capability, not dispatch evidence.
