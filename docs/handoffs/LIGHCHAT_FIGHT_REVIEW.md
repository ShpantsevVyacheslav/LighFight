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
