# Unity integration path and safety gate

Current installed arena: `unity/Assets/LighFight/Content/arena-0.prefab`, catalog slot `GameContent.asset.arenas[0]`. It is still the prior procedural meeting room.

Existing API: `LighFight.Editor.MeshyArenaImport.Import(GameObject source,string path,int slot=-1)`. Its `Validate` calls `ArenaLayoutValidator.Validate`. Requirements: exactly one separate renderer `FightFloor`, flat top y≈0, covering x[-5.5,5.5], z[-2,2]; no other renderer bounds intersects that 11×2.2×4m combat volume. Do not weaken this gate to fit the review scene.

`meeting-hybrid-v1` and `meeting-details-v1` are 6.8×7m review rooms, combat lane only z±.55. They are not drop-in prefabs and have no FightFloor semantic. A derivative gameplay shell must widen the walls/floor, position furniture and small props behind z=2, and keep all furniture proportions. Import FBX (no runtime GLB loader dependency), reconstruct materials from source maps, validate, save prefab, update catalog slot0 and capture actual Unity view before accepting game integration. Unity build/device thermal validation remains separate.
