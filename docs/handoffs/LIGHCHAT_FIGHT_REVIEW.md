# LighFight: two artist characters and expanded combat in LighChat

Use the existing Game Hub LighFight tile. No separate app and no Flutter tile changes are required. iPhone build 63 validated launch of the older HostArtReview, not this new battle.

New opt-in profile: `LIGHFIGHT_EMBED_FIGHT_REVIEW=1`. Set both older preview flags to zero; conflicting profiles are rejected. The ordinary downloadable-content profile remains available when all preview flags are zero.

```sh
env UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity LIGHCHAT_ROOT=/Users/macbook1/Desktop/LighChat LIGHFIGHT_EMBED_FIGHT_REVIEW=1 LIGHFIGHT_EMBED_ART_REVIEW=0 LIGHFIGHT_EMBED_PROTOTYPE=0 /Users/macbook1/Documents/ChatGPT/LighFight/scripts/build-host-libraries.sh ios
```

The build owner then runs the existing `LIGHFIGHT_IOS=1` pod/Flutter staging recipe with all native-feed flags. Only the build owner should export/copy UnityFramework and install on the device; no competing export was performed in this chat.

`HostFightReview.unity` embeds local review resources: Rose/Pink Panther defaults selected; her opponent is the Island Fighter. Both have 25 attacks and matching skeleton clips. Six other roster slots and the five arenas remain starter prototypes. This is a combat review build, not a production art-complete release. Production content still uses Addressables/CDN.

On device: LighChat → gamepad → Играть → LighFight → НАЧАТЬ БОЙ. Virtual arrows move/crouch/select alternate attacks; four primary attack buttons use direction modifiers; Все 25 приёмов opens the complete move panel. Блок defends. Тренировка disables health loss and timer; turn it off for a KO/getup test. Пауза/Заново/Выход are at the top. The selection screen also has ВЫХОД. Exit uses the existing HostBridge/native unload flow; duplicate exit events are suppressed until the next Launch.

The battle needs landscape layout. Unity already uses LandscapeLeft; the LighChat host route must allow landscape while Unity is visible and restore the app orientation on exit. Do not leave this two-fighter/25-button screen squeezed into portrait. This host change belongs to the LighChat build owner.

Device verification owned by the build agent: launch, both character skins/fists/cuffs, hold movement while tapping attacks, block high/mid/low, KO fall and getup, pause/resume, background/foreground, exit to Game Hub, relaunch twice. Observe actual frame rate, memory and load time before selecting mobile LOD/texture budgets. Editor tests cannot prove device performance or the native unload path.

Editor checks: all 32 motions sampled over 61 phases; all 25 attacks on each rig sampled over their complete combat timeline (1290 frames each), no nonfinite or oversized deformation, 9 support states; core timing tests and contact-timing mutation; embedded battle launch/exit/relaunch test.

## Combat polish follow-up
Clean-hit chains: jab → cross → hook/body; body → knee. Cancel window is first six recovery ticks, maximum three attacks. Whiffs/blocks cannot cancel. Input buffer remains eight ticks. Renderer blends pose changes for 40–100 ms, excluding the actor root, while contact simulation is unchanged. Block uses Guard pose rather than damage Hit. Runtime contact freeze lasts three ticks (five for counter), preserving queued input. Bot approaches at distance, retreats inside 1.05 m between attacks and can punish recovery after its reaction delay.
Verification: 44 core tests, mutation removing clean-hit requirement fails. Device acceptance still pending; check chain input, stance transitions and contact feedback at 30/60 FPS. No Meshy credits used by this update.

## Native Unity selection UI
FightSelection.cs replaces old full-screen selection grid. Landscape central single-fighter viewport; opponent/arena hidden during preview and restored on BeginFight. Drag rotates, pinch or mouse wheel zooms, attack button cycles jab/cross/frontKick; guard has subtle breathing. Dance remains explicitly unavailable (no new credits spent). Rounded choice buttons replace dropdowns: training/bot, difficulty, wins1–3,60/90seconds. Settings feed FightSession and BotController. Stats show real shared health/damage/reach rather than invented fighter strength. Existing battle controls/hit detection are unchanged by this selection update. Verify touch gestures and viewport restoration on phone.58Unity/47core tests pass; timer mutation is detected. Physical device validation pending.

## Spatial combat and physical contacts
Runtime battle now installs PhysicalContacts on every new Match. Damage requires swept hand/elbow/toe/knee sphere vs animated head/torso/leg capsules during active ticks; one connection per attack. Imported rigs without the required bones fail closed. Torso uses actual Meshy `Spine` bone (Spine02 is lower spine). Closed-fist centre is wrist plus5cm along forearm, radius9.5cm; body capsules approximate the surface, not per-triangle collision. Fixed attack facing permits stepping outside the strike trajectory. Core distance-only mode remains for headless simulation tests; Unity battle always enables required geometry.
Movement X/Z normalized, rectangle X±5m/Z±1.5m, separation75cm; bot follows in both axes. Joystick retains finger id during simultaneous attack touches; buttons H1/H2/F1/F2 select labelled actions, neutral H1→H2→H1 selects confirmed jab→cross→hook chain. Directions alter attacks; crouch and held guard remain separate. W/S sidestep on keyboard; arrows/J/K/U/I also retained. Contact particles use actual impact positions. UI input/rendering respect Screen.safeArea. Sidesteps currently reuse existing walking clips; dedicated lateral clips are not yet generated.
Device checks: move diagonally without speed boost, sidestep during opponent startup, boundary stops, held joystick plus attacks/block, cancelled touch/background clears input, normal hits at close range, visible misses cause no damage, single hit per strike, round reset/relaunch. Phone performance/gesture validation pending; no Meshy credits used.

Validation (2026-10-11): Unity EditMode 66/66; standalone combat 54/54. Removing the physical-contact gate causes GeometricMissCannotDealDamage and RequiredPhysicalContactFailsClosed to fail (52/54), restoring it returns 54/54. Phone validation pending; no new Meshy credits spent.

## Gestures, branching chains and hit responses (2026-10-11)
Right swipe pad is isolated from joystick and attack buttons. Default gestures ON; toggle in battle. Tap=jab, right=cross, left=hook, up=uppercut, down=body; up-right=roundhouse, up-left=side kick, down-right=low kick, down-left=sweep. One tracked finger, End-only dispatch, 34 logical-unit minimum swipe, 14-unit tap allowance, .65s swipe ceiling; cancellation, pause, background, reset and round change clear it. Buttons remain usable simultaneously. Joystick direction no longer silently changes button attacks. Keyboard directional variants remain.

Confirmed chains: jab→cross→hook→low kick; cross→body→knee→elbow; body→uppercut; low kick→body→knee→elbow. Each link needs an actual clean contact and first six recovery ticks (8-tick input buffer). At most four attacks, damage scales 100/90/80/70 percent. Block or miss closes cancel path; full recovery allows a new independent attack. These are cancellable strings, not guaranteed uninterrupted combos: the opponent can defend/dodge and every strike still requires physical contact.

Hit response time is simulation-driven. Existing Hit clip gets small level-specific skeletal offsets (head recoil, body fold, low-impact hip tilt) and blends back; no new Meshy animation credits. Block has smaller recoil. Knockdown retains Fall/GetUp. Pushback follows X/Z vector. Bot can sidestep startup after reaction delay, use fast attacks against whiff recovery, continue confirmed strings, and reset strategy each round. Lateral movement still reuses walking clips.

Phone checks for build owner: joystick+swipe with separate fingers, canceled swipe, arrows/tap in pad, toggle, swipe outside pad, button chain while holding joystick, background/pause/round transition; hit reactions and guard/counter differentiation. Device feel and 60fps budget remain unverified in this phase.

Close-range calibration: MinimumSeparation is now .60m (previous .75m prevented short hooks connecting). A confirmed chain advances along locked facing by .02m for up to 12 startup ticks (.24m maximum); each step is clamped to arena and checked against opponent separation. It cannot home toward a sidestepping target. Existing hit capsules were not enlarged. Artist-model tests now verify both jab→cross and jab→cross→hook→low kick as physical contacts, including scaled damage; repeated sampling cannot accumulate reaction rotations.

Validation: Unity EditMode 88/88, standalone combat 72/72. Deliberately removing clean-hit gate, chain step and finger ownership creates three failing checks (69/72); source restored. Self-review caught cross/hook spacing failure on actual models and joystick direction unexpectedly changing attack buttons; corrected both. No Meshy credits used. Commit is local; build owner must validate touch feel/performance on iPhone.

## All-strike contact calibration and training feedback
A 550-scenario artist audit covers 25 moves × 2 rigs × 11 distances (.60, .70, .85, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 2.4, 4m). Each move now connects at a playable distance on each rig; every move misses at 4m. Distances are sampled observations against a standing idle target, not exact reach limits or proof of arbitrary opponent poses. Report: docs/reports/strike-contact-audit-2026-10-11.csv.
The old maximum-forward-extension detector missed actual striking segments: calibrated phases elbow=.25, jumpKick=.50, hammer=.30, uppercut=.70. Imported uppercut adds a 30° committed hip pivot over startup and removes it during recovery, with locked target direction; capsules unchanged. Bot now filters using conservative measured reach and closes to .8m rather than waving punches from 1.4m. No base damage/startup/recovery values changed this phase; character reach varies slightly with proportions. Selection shows observed .6–1.2m instead of unsupported 1.1–2.35m.
Training reports preparation, actual contact, block, miss, interrupted attack and available follow-up names. Miss advice asks to check distance/direction rather than guessing which caused it. Result persists after recovery; a new strike clears it. Existing training dummy does not move/attack; block/counter prompts need an opponent-capable mode to trigger naturally. New outcome state also works in normal matches.
Device status: new phase not verified on iPhone; no competing device export and no Meshy credits.

Validation: 93/93 Unity EditMode and 76/76 core. Removing miss outcome causes 2 core failures; restoring the old elbow phase makes artist audit fail on the unreachable elbow. Source restored. Self-review: found 4 unreachable moves on both rigs and overstated bot attack ranges, corrected with measured contacts. Phone touch/performance remains unverified.
