# World

Rooms and encounters (`TentacleVania.World`).

## CombatArena (Combat Prototype)

- `Prefabs/CombatArena_Room.prefab` is the greybox room (developer decisions): a 20×20 m floor centred on the origin, full-height back wall, 3 m side walls, low (1 m) front wall facing the camera, four 2×2×3 m pillars at (±5, ±4), a 4×8 m entry corridor to the south (the creature spawns at z = −15) and a 4×4 m exit stub to the north. Greybox materials (URP Lit) are in `Materials/`; characters never use them.
- Two `ArenaDoor`s: south (starts open, 1 m slab) and north (sealed, 4 m slab). Doors slide into the floor with PrimeTween; the slab's collider is the blocker. The NavMesh was baked with both doors closed, so enemies never path into the corridor; rebake after moving geometry (see the Enemies CLAUDE.md).
- `ArenaEncounter` on the room root: when the creature enters the trigger box (18×17 m inside the walls) both doors seal and the waves in `Data/CombatArenaWaves.asset` run. Each wave's enemies are shuffled and spawned 0.3 s apart round-robin at six wall hatches (`SpawnPoints`), then walk in; the next wave starts 2 s after the last enemy dies (consumed or killed). After the last wave both doors open; nothing else happens (developer decision).
- Waves (developer-approved): 1: 3 scientists + 1 guard; 2: 2 guards + 1 handler + 2 scientists; 3: 2 handlers + 2 guards + 1 scientist; 4: 2 handlers + 3 guards + 2 scientists.
- Scene: `CombatArena` holds the room instance, the `GameplayCamera`, `CameraBounds` (21×29 m around room and corridor), `CreatureSpawn`, `Navigation` and the light.
- Debug: the World tab shows the arena state and has Start arena now and Skip wave (kill all). `Tools/UnityCheck/evals/gc-profile-start.cs` starts the arena with god mode on, so `unity-gc-profile.sh` profiles real combat.
- Enemies are instantiated per wave; pool them when a room's spawn counts make it show up in the profiler.
