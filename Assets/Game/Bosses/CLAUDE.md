# Bosses

Boss framework (`TentacleVania.Bosses`), built on the enemy state machine (`Enemies/CLAUDE.md`). Real bosses (Warden Drone, Surgical Automaton) are separate tasks: subclass `BossEnemy` or use it as is with their own data.

## Structure

- `BossEnemy : Enemy` takes its vitals, attacks, stagger and timings from its `EnemyDefinition` like any enemy. Bosses set a large stagger bar and `StaggerStun` 1.0 s there, have no `Consumable` (adds are the sustain), and use a `DespawnDelay` longer than the death pause plus the absorb flight. The enemy definition's `SpawnDelay` is the intro: the boss is idle and the `IntroCamera` child (a `CinemachineCamera` at priority 50, otherwise -10) frames it for that long.
- `BossDefinition` (ScriptableObject) holds the boss-only data:
  - the save `Id` and the `UI` table `NameKey` for the boss bar;
  - `Phases`, each with `StartsBelow` (health fraction), an `AttackMask` (bit per enemy-definition attack index, 0 for all), `MoveSpeedScale`, an `AddPrefab` with `MaxAdds` and `AddInterval`, and a phase-change `Sting`;
  - the `RewardAbility`, its `Music` (empty uses the shared boss theme, see `Audio/CLAUDE.md`) and the intro, death and absorb FMOD events.
  `Core.Progression.BossProgress.PhaseFor` (unit tested) picks the phase.
- **Adds:** they spawn `AddSpawnDistance` from the boss on the NavMesh once the intro ends, and die with the boss.
- **Finishing consume (design: every boss ends in a unique finishing consume, Grab at 0 HP):** bosses keep a `Consumable`, `Locked` until the end. `Health.Floor` = 1 HP stops lethal damage; at the floor `BeginFinishing` kills the adds, plays the death sound, stuns the boss indefinitely and unlocks the consumable, so it glows feedable and Grab consumes it (heal and biomass from the enemy definition). `FeedProtection` is off for bosses (no ×0.5 below 25%), and they can't be grabbed before the floor. A boss without a `Consumable` dies at the floor. `BossEnemy.Finishable` fires at the floor.
- **Death:** `BossProgress.Defeat(id, ability)` saves `WorldFlag.DefeatedBoss`, adds the ability to `Build.Abilities` (raising `AbilityUnlocked`; `AbilityController` equips it only into an empty slot, developer decision) and requests an autosave, all at the moment of death so quitting during the absorb can't lose the reward.
- **Absorb:** after a consume, the burst, sound and `Absorbed` play right away. After a debug kill, after `DeathPause` the `Essence` child (an orb with the `BossEssence` VFX trail) arcs into the creature over `AbsorbDuration`, then the `AbsorbEffect` burst and sound play and `BossEnemy.Absorbed` fires (HUD banner).
- **No refights:** a boss whose id is defeated destroys itself on enable. Boss arenas (`ArenaEncounter._bossArena`) also stay cleared and open.
- `BossEnemy.Active` is the current boss: `UI/Hud/BossBarView` shows its name, health (with phase ticks) and stagger at the top of the screen. `Enemy.IsBoss` keeps it out of the overhead enemy bars. Static events: `IntroStarted`, `PhaseChanged`, `Absorbed` (hooks for PA lines and music).

## Debug

- **Test boss:** `Debug/Resources/DebugTestBoss.prefab` (the Handler scaled 1.4×, `Debug/TestBoss.asset` 400 HP / 150 stagger, `Debug/TestBossPhases.asset`). Phase 1 uses the poke only. Phase 2 starts at 50%: all attacks, ×1.3 speed, and up to 2 Scientist adds every 6 s. It rewards Bio-Shield.
- **F5 Bosses tab:** boss readout, spawn test boss (revives it first), HP to next phase, spawn add, bring boss to 0 HP (finish), kill boss (skip consume), reset defeated bosses.
