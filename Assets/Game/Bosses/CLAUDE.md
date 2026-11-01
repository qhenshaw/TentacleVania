# Bosses

Boss framework (`TentacleVania.Bosses`), built on the enemy state machine (`Enemies/CLAUDE.md`). Real bosses (Warden Drone, Surgical Automaton) are separate tasks: subclass `BossEnemy` or use it as is with their own data.

## Structure

- `BossEnemy : Enemy` takes its vitals, attacks, stagger and timings from its `EnemyDefinition` like any enemy. Bosses set a large stagger bar and `StaggerStun` 1.0 s there, have no `Consumable` (adds are the sustain), and use a `DespawnDelay` longer than the death pause plus the absorb flight. The enemy definition's `SpawnDelay` is the intro: the boss is idle and the gameplay camera follows it (`CameraFocus.Target` set to the boss, restored after) for that long. No cutscene cameras (developer decision): cutscenes stay on the gameplay camera's angle. After the creature has died to a boss once (`WorldFlag.DiedToBoss`, recorded when the creature dies while that boss is `Active`), later attempts cut the intro to `_shortIntroTime` 1 s (developer decision); F5 Bosses "Reset boss deaths (full intros)".
- `BossDefinition` (ScriptableObject) holds the boss-only data:
  - the save `Id` and the `UI` table `NameKey` for the boss bar;
  - `Phases`, each with `StartsBelow` (health fraction), an `AttackMask` (bit per enemy-definition attack index, 0 for all), `MoveSpeedScale`, an `AddPrefab` with `MaxAdds`, `AddInterval` and `AddsOnStart`, and a phase-change `Sting`;
  - the `RewardAbility`, its `Music` (empty uses the shared boss theme, see `Audio/CLAUDE.md`) and the intro, death and absorb FMOD events.
  `Core.Progression.BossProgress.PhaseFor` (unit tested) picks the phase.
- **Adds:** they spawn `AddSpawnDistance` from the boss on the NavMesh once the intro ends, and die with the boss.
- **Finishing consume (design: every boss ends in a unique finishing consume, Grab at 0 HP):** bosses keep a `Consumable`, `Locked` until the end; `BossEnemy` sets its heal to 1, so the finishing consume restores full health (developer decision). `Health.Floor` = 1 HP stops lethal damage; at the floor `BeginFinishing` kills the adds, plays the death sound, stuns the boss indefinitely and unlocks the consumable, so it glows feedable and Grab consumes it (heal and biomass from the enemy definition). `FeedProtection` is off for bosses (no ×0.5 below 25%), and they can't be grabbed before the floor. A boss without a `Consumable` dies at the floor. `BossEnemy.Finishable` fires at the floor.
- **Death:** `BossProgress.Defeat(id, ability)` saves `WorldFlag.DefeatedBoss`, adds the ability to `Build.Abilities` (raising `AbilityUnlocked`; `AbilityController` equips it only into an empty slot, developer decision) and requests an autosave, all at the moment of death so quitting during the absorb can't lose the reward.
- **Absorb:** after a consume, the burst, sound and `Absorbed` play right away. After a debug kill, after `DeathPause` the `Essence` child (an orb with the `BossEssence` VFX trail) arcs into the creature over `AbsorbDuration`, then the `AbsorbEffect` burst and sound play and `BossEnemy.Absorbed` fires (HUD banner).
- **No refights:** a boss whose id is defeated destroys itself on enable. Boss arenas (`ArenaEncounter._bossArena`) also stay cleared and open.
- `BossEnemy.Active` is the current boss: `UI/Hud/BossBarView` shows its name, health (with phase ticks) and stagger at the top of the screen. `Enemy.IsBoss` keeps it out of the overhead enemy bars. Static events: `IntroStarted`, `PhaseChanged`, `Absorbed` (hooks for PA lines and music).

## Warden Drone (`Warden/`; design: Warden Drone page and Changelog "Warden Drone fight", which supersede the task's older notes)

- **Assets:** `WardenDrone.prefab` (`WardenDrone : BossEnemy`; the Med-Drone body ×2.2 with a red eye as placeholder), enemy data `WardenDrone.asset` and boss data `WardenBoss.asset` (id `SH/WardenDrone`, matching the PA script; music `event:/Music/BossWarden`; reward Bio-Shield).
- **Numbers:** 1,350 HP, stagger 800 (TV-224 balance pass for a ~3 min first clear with 4 breaks; math in the task), an 8 s stun (the damage window; developer decision), 5 m/s, a 3 s intro, machine, consume 50% / 200 biomass.
- **Immunity:** a `StaggerShield` incoming modifier zeroes health damage unless `IsStaggerBroken`. Stagger still builds, and the window applies the usual ×1.5.
- **Movement:** it holds 6–8 m from the creature (tries ±40°/±80° on the NavMesh) and always faces it.
- **Attacks (yellow, Electric):**
  - Shock Prod: melee 20° at 6 m, 15 damage, 0.6 s wind-up, used at 3–8 m. The drone lunges forward at 22 m/s while it's active.
  - Electrified Sweep: 270° at 4 m, 12 damage, 0.8 s wind-up, used within 4 m.
  - Phase 2 (≤40% HP) uses copies at ×0.75 timings and ×1.25 move speed (attack masks 0b0011 and 0b1100).
- **Stagger break (developer decision):** the `Model` drops from its hover and tips `_tipAngle` 85° onto its side at `_tippedHeight` 0.6 m in 0.3 s, lies there for the whole stagger stun (8 s), and rights itself back to hover in 0.5 s as the stun ends (`WardenDrone.Bob`).
- **Wall Beams (red, undodgeable):** every 15 s (×0.75 at low HP), 2 beams, or 3 at ≤40%. The beams cross the `WardenHall` (put one in the boss room, x along the long walls; otherwise 24×18 around the spawn, facing its yaw). They show as floor lanes for 1.2 s, then fire for 0.3 s, dealing 25 damage to a creature inside the 1.6 m width. `WardenRules` (unit tested) spaces the lanes evenly with a random shift, then moves the nearest lane onto the creature, so it must move to a safe lane. The visuals are three `LineRenderer`s on `TurretLaser.mat`. FMOD: `event:/Bosses/Warden/BeamWarn`, `BeamFire`, `ProdWindUp`, `SweepWindUp`, `ShockHit`.
- **Adds:** each stagger break summons 2 Security Guards near the boss (max 4 alive) once the stun ends (developer decision).
- **Finish:** the shared finishing consume. The PA plays `sh.warden` (lines 4–5, then Crane's first address).

## Security Chief Vance (`SecurityChiefVance/`; design: Enemies & Bosses row, TV-189; placeholder numbers)

- **Assets:** `SecurityChiefVance.prefab` (`VanceBoss : BossEnemy`, the test boss's Handler body tinted navy with a steel `RiotShield` cube; scaffolded with `content-new.sh boss`), enemy data `SecurityChiefVance.asset`, boss data `SecurityChiefVanceBoss.asset` (id `AD/SecurityChiefVance`, key `boss.securityChiefVance`, reward `FreightClearance`), voice set `SecurityChiefVanceVoice`. He waits in `AD_CommandPost`, the last room of Administration's 3-room siege (Siege1 and Siege2 are escalating guard waves).
- **Numbers:** 2,600 HP, stagger 1,300 with a 3 s window, 4.5 m/s (scaled from the TV-224 boss curve).
- **Riot shield:** while `ShieldUp` (phases 1–2) hits from within `_shieldArc` 120° of his facing deal no health damage unless his stagger is broken (flank him or break the bar); the last phase drops the shield.
- **Attacks:** Baton Combo (Electric, 2 hits of 12, 0.55 s), Shield Bash (10, 0.7 s), Shield Charge (red, 0.9 s wind-up, lunges at 16 m/s while active, used at 4–10 m), Shock Slam (Electric area 3.2 m, 22, 1.0 s; last phase).
- **Phases:** 100% baton + bash with up to 2 Security Guard adds every 15 s; ≤60% adds the charge and Riot Guard adds (1 on start, max 2); ≤30% shield off, baton/charge/slam, ×1.2 speed, Security Guard adds (2 on start, max 3). Adds are consumable heal sources (design).

## Surgical Automaton (`Automaton/`; design: Surgical Automaton page and Changelog "Surgical Automaton fight")

- **Assets:** `SurgicalAutomaton.prefab` (`SurgicalAutomaton : BossEnemy`; the test boss's Handler body ×1.6 in steel as placeholder), enemy data `SurgicalAutomaton.asset` and boss data `AutomatonBoss.asset` (id `BL/SurgicalAutomaton`, matching the PA script; the shared boss theme; reward `TentacleGrapple`, which unlocks Grapple without taking the slot).
- **Numbers:** 2,400 HP, stagger 1,300 (TV-224 balance pass for a ~4 min first clear with 4 breaks) with a 2.5 s window, takes damage normally, 4.5 m/s, machine, consume 50% / 300 biomass.
- **Attacks (yellow):**
  - Clamp Lunge: 1.0 s wind-up, 20 damage, used at 4–12 m. It picks the wall clamp anchor (8 around the theatre) best aligned with its facing (`AutomatonRules.AnchorAlong`, unit tested), shows a steel cable `LineRenderer`, and lunges up to 12 m toward it during the 0.45 s active window, sweeping a 140° arc.
  - Scalpel Flurry: within 3 m, 4 × 5 damage (`EnemyAttack.Hits` = 4).
  - Bone Saw Spin (phase 2 only, attack mask 0b111): area 3.5 m, 18 damage per hit, 4 hits over a 2 s spin.
- **Phase 2 (≤50%):** two opposite 45° blade sectors (two extra `GroundIndicator`s drawn as undodgeable sectors) warn for 1.5 s, then open and rotate at 10°/s around the table. A creature inside, between the table and wall radii (`AutomatonRules.InBlade`), takes 15/s as `HitFlags.Hazard` ticks every 0.25 s.
- **Adds:** Surgeons, 2 at each phase start (`BossPhase.AddsOnStart`, spawned once the intro ends) and one every 25 s, max 3.
- **Theatre:** put an `AutomatonTheatre` (wall radius, table radius) at the table centre. Without one, the fight centres on the spawn point (radius 11, table 2.5). The table needs a carving `NavMeshObstacle`.
- **Finish:** the shared finishing consume unlocks Grapple. The PA plays `bl.automaton` (lines 13–15, then Crane's second address).
- **Debug:** `Automaton/Debug/Resources/AutomatonTheatreRig.prefab` (table plus 8 anchor posts, radius 9.5). F5 Bosses: Spawn Automaton theatre rig, Spawn boss.

## Debug

- **Test boss:** `Debug/Resources/DebugTestBoss.prefab` (the Handler scaled 1.4×, `Debug/TestBoss.asset` 400 HP / 150 stagger, `Debug/TestBossPhases.asset`). Phase 1 uses the poke only. Phase 2 starts at 50%: all attacks, ×1.3 speed, and up to 2 Scientist adds every 6 s. It rewards Bio-Shield.
- **F5 Bosses tab:** boss readout, spawn test boss (revives it first), Spawn boss (any boss in `Debug/Resources/DebugBossSpawnList`, revived first), HP to next phase, spawn add, bring boss to 0 HP (finish), kill boss (skip consume), reset defeated bosses.
