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
- **Death:** `BossProgress.Defeat(id, ability, secondAbility)` saves `WorldFlag.DefeatedBoss`, adds the ability (and the definition's optional `SecondReward`, shown as "A + B" on the reward banner; and spawns the definition's optional `DropPrefab` pickup where it died, which needs a fixed pickup save id) to `Build.Abilities` (raising `AbilityUnlocked`; `AbilityController` equips it only into an empty slot, developer decision) and requests an autosave, all at the moment of death so quitting during the absorb can't lose the reward.
- **Absorb:** after a consume, the burst, sound and `Absorbed` play right away. After a debug kill, after `DeathPause` the `Essence` child (an orb with the `BossEssence` VFX trail) arcs into the creature over `AbsorbDuration`, then the `AbsorbEffect` burst and sound play and `BossEnemy.Absorbed` fires (HUD banner).
- **No refights:** a boss whose id is defeated destroys itself on enable. Boss arenas (`ArenaEncounter._bossArena`) also stay cleared and open.
- **Mini-bosses and squads (TV-148):** optional bosses are ordinary `BossEnemy`s in side arenas gated by the usual world gates (`World/Overload/OverloadDoor`, hazards with resistances, Breach walls). `BossDefinition.RewardMutation` (a mutation id) is granted alongside the ability rewards. Several `BossEnemy`s sharing one `BossDefinition` fight as one squad (the Hunter-Killer Squad): `Roster` tracks them, only the last one alive gets the 1 HP floor and the finishing consume (`LastStanding`), earlier members just die and hand `Active` to a survivor, and `BossProgress.Defeat`, the rewards, drop and absorb happen once, on the last. The boss bar shows `GroupNormalized`, the squad's combined health.
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

## Dr. Okafor's Exo-Suit (`OkaforExoSuit/`; design: Enemies & Bosses row, TV-190; placeholder numbers)

- **Assets:** `OkaforExoSuit.prefab` (`OkaforBoss : BossEnemy`, the test boss body tinted cyan with a steel `SuitCockpit` that hides while staggered), data `OkaforExoSuit.asset`, boss data `OkaforExoSuitBoss.asset` (id `RD/OkaforExoSuit`, key `boss.okaforExoSuit`, reward `ElectricResistance`), voice set `OkaforExoSuitVoice`.
- **Numbers:** 2,800 HP, stagger 1,400 with a 3.5 s window, 4 m/s.
- **Suit plating (design: stagger breaks expose the pilot):** health damage ×0.5 while the suit is closed and ×2 while staggered (on top of the ×1.5 window).
- **Attacks (Electric):** Arc Blast (projectile, 14, 3–10 m), Arc Sweep (180° melee 2.8 m, 18), Discharge (red area 4.5 m, 26, from ≤60%).
- **Phases:** 100% blast + sweep, Researcher adds (max 2, every 16 s); ≤60% + Discharge, Test Drone adds (1 on start, max 2); ≤25% ×1.25 speed, Test Drone adds (2 on start, max 3).
- **Arena:** the test chamber's electrified floor is a `World/Hazards/FloorPattern` (placed by the Research Division spec's test chamber hook).

## The Seeper (`Seeper/`; design: The Seeper page, TV-191; placeholder numbers)

- **Assets:** `Seeper.prefab` (`SeeperBoss : BossEnemy`; the test boss's Handler body squashed into an acid-green `Mass` sphere listed in its `CharacterMaterials`), data `Seeper.asset`, boss data `SeeperBoss.asset` (id `QU/Seeper`, key `boss.seeper`, rewards `VentSqueeze` + `AcidResistance`), voice set `SeeperVoice`; `SeeperVent.prefab` (a `SeeperVent` marker with an acid grate) for the arena; `SeeperEruption.prefab` (an `Enemies/Traps/AcidBurst` without the acid pool, FMOD `event:/Bosses/Seeper/Eruption`).
- **Numbers:** 2,600 HP, stagger 1,200 with a 3 s window, 3 m/s.
- **Vent ambush (design core mechanic):** exposed for `_exposedTime` (7 s, 5 s in phase 2), it sinks (`Model` lowers 2.5 m over 0.6 s, FMOD `event:/Bosses/Seeper/Sink`) with every collider off, so it can't be hit until it erupts. It picks the vent nearest the creature (not the one it left; `SeeperRules`, unit tested), shows a 2 m eruption circle for 1.2 s, then bursts out there (22 Acid + Corrode) and rises. Vents are the `SeeperVent`s within 30 m, or a ring of 6 at 7 m around its spawn when fewer than 2 (CombatArena tests).
- **Attacks (yellow, Acid + Corrode, only while exposed):** Acid Spray (70° cone at 5 m, 3 × 8), Engulf (160° at 2.5 m, 18).
- **Phases:** 100% Outbreak Crawler adds (max 2, every 18 s); ≤50% multi-vent: a second eruption at the vent farthest from the first, which releases 2 Outbreak Crawlers (the split body; adds capped at 4), ×1.2 speed.

## Cryo Loader Mech (`CryoLoader/`; design: Cryo Loader Mech page, TV-192; placeholder numbers)

- **Assets:** `CryoLoader.prefab` (`CryoLoaderBoss : BossEnemy`; the test boss body ×1.4 in amber with a steel `HydraulicRam` cube), data `CryoLoader.asset`, boss data `CryoLoaderBoss.asset` (id `CS/CryoLoader`, key `boss.cryoLoader`, reward `Breach`), voice set `CryoLoaderVoice`; `Icefall.prefab` (an `Enemies/Traps/AcidBurst` without the pool, FMOD `event:/Bosses/CryoLoader/Icefall`).
- **Numbers:** 3,000 HP, stagger 1,500 with a 3 s window, 3.8 m/s, machine.
- **Ice maze (design core mechanic: maze shifting):** on spawn it raises `_mazeWalls` 5 pooled `Enemies/Traps/IceWall`s (5 m, no lifetime) in 8 slots around where it spawned (rotated with its yaw; never within 3 m of the creature or itself) and re-raises one in a free slot every 12 s; its walls shatter when it dies. Pooled walls carry a `Generation`, so a slot never claims a wall reused by a Frost Sentry.
- **Attacks (yellow, Physical):** Ram Charge (1.0 s wind-up, used at 3.5–12 m; lunges 15 m/s while active and shatters any of its walls within 2.2 m ahead, `IceWall.Touches`), Hydraulic Slam (area 3.2 m at 1.5 m ahead, 26 + `Slow` 0.5 for 2 s: the floor freezes), Cryo Vent Blast (60° cone at 5 m, 3 × 6 + `Slow` 0.35).
- **Phases:** 100% Cryo Technician adds (max 2, every 16 s); ≤50% Loader Bot adds (1 on start, max 2), ×1.15 speed, and Icefall: every 7 s three 1.6 m circles (one on the creature, two 3–6 m around it) drop ice after 1.1 s for 16 damage.

## Major Ross (`MajorRoss/`; design: Major Ross (Walker) page, TV-193; placeholder numbers)

- **Assets:** `MajorRoss.prefab` (`RossBoss : BossEnemy`; the test boss body ×1.5 in olive inside a steel `Walker` of primitive legs, hull, chaingun and missile pods), data `MajorRoss.asset`, boss data `MajorRossBoss.asset` (id `BA/MajorRoss`, key `boss.majorRoss`; `DropPrefab` `RossMutagen.prefab`, a Mutagen pickup with save id `BA/MajorRoss/Mutagen`; the unique mutation and Spine Volley rewards wait for their content tasks), voice set `MajorRossVoice`.
- **Numbers:** 3,400 HP, stagger 1,700 with a 3 s window, 3.2 m/s.
- **Phase 1 (100–30%), walker (attack mask 0b00111):** Chaingun Sweep (yellow, 120° at 7 m, 4 hits of 6 over 1 s, used at 2.5–7 m), Missile Barrage (Trap kind with the `Enemies/Traps/Grenade` burst on the creature's marked spot plus `_extraRockets` 3 more 3–5 m around it, staggered 0.2 s apart, 18 each), Stomp (area 3.2 m around it, 20, within 3 m). Soldier adds (1 on start, max 2, every 15 s) are the heal source.
- **Phase 2 (≤30%), on foot (mask 0b11000):** the walker is wrecked (`Walker` hidden, the body shrinks to ×0.65, FMOD `event:/Bosses/MajorRoss/WalkerWreck`, a 2 s stun), then Ross holds 4–6 m like a guard with a Rifle (9, any range to 7 m) and Grenades (every 5 s), ×1.5 speed, rallying 2 Soldiers (max 3, every 12 s).

## Captain Dana Reyes (`Reyes/`; design: Captain Dana Reyes (Flame Corps) page, TV-194; placeholder numbers)

- **Assets:** `Reyes.prefab` (`ReyesBoss : BossEnemy`; the test boss body in orange), data `Reyes.asset`, boss data `ReyesBoss.asset` (id `AR/Reyes`, key `boss.reyes`, reward `FireResistance`), voice set `ReyesVoice`.
- **Numbers:** 3,600 HP + 150 heat armour, stagger 1,700 with a 3 s window, 3.8 m/s.
- **Fire zones (design core mechanic):** pooled `Enemies/Traps/FireZone`s (`FireZone.prefab`: a ground decal drawn yellow and filling over the warning, then red while lit; a creature inside, `FireZone.Inside` (unit tested), takes 12/s Fire as `Hazard` ticks every 0.25 s plus Burn; FMOD `event:/Bosses/Reyes/Ignite`).
- **Attacks (yellow, Fire + Burn):** Flame Stream (70° sweep at 5.5 m, 6 hits of 5 over 1.2 s), Napalm Grenade (Trap kind with the Grenade burst, 12, then a 2.2 m fire pool lit for 7 s where it lands).
- **Phases:** 100% Flame Trooper adds (max 2, every 16 s); ≤50% Fire Slice: every 7 s she snaps to a random heading and lights a 45° wedge 14 m long in front of her (1.2 s warning, 6 s; FMOD `event:/Bosses/Reyes/FireSlice`), carving the arena into burning wedges; 1 Flame Trooper on start, ×1.15 speed.

## Overwatch Core (`OverwatchCore/`; design: Overwatch Core page, TV-195; placeholder numbers)

- **Assets:** `OverwatchCore.prefab` (`OverwatchBoss : BossEnemy`; a cyan `Core` cylinder in its `CharacterMaterials` inside a steel `Shell` sphere, the Kenney body shrunk away), data `OverwatchCore.asset` (no attacks, speed 0, machine), boss data `OverwatchCoreBoss.asset` (id `CC/OverwatchCore`, key `boss.overwatchCore`, reward `Overload`), voice set `OverwatchCoreVoice`; `SecurityWall.prefab` (a steel `Enemies/Traps/IceWall`) and `LaserZone.prefab` (an Electric `Enemies/Traps/FireZone` without a status; `FireZone` takes its damage type and status from the prefab).
- **Numbers:** 2,400 HP; no stagger.
- **Room hacking (design core mechanic):** the core never attacks. Every cycle it reconfigures for 7 s with the `Shell` closed (a `CoreShell` incoming modifier zeroes all damage; FMOD `event:/Bosses/Overwatch/Reconfigure`): 3 security walls rise on a 5 m ring around it (skipped where the creature stands) for the cycle, the previous Sentry Guns are removed and 2 (3 from phase 2) deploy at new spots on an 8 m ring. Then the shell opens for 4 s (`event:/Bosses/Overwatch/CoreOpen`), the only time it takes damage.
- **Phases:** 100% Security Drone adds (1 on start, max 2, every 14 s); ≤66% Security Guard adds and 3 laser wedges (8° × 13 m from the core, 1.2 s warning, 3 s, 20/s Electric) each reconfiguration; ≤33% Security Drone adds (max 3, every 12 s) and one Assault Mech joins.

## Excavator Rig (`ExcavatorRig/`; design: Excavator Rig page, TV-196; placeholder numbers)

- **Assets:** `ExcavatorRig.prefab` (`ExcavatorBoss : BossEnemy`; the test boss body ×1.4 in yellow with a steel `Drill` and an orange `ExposedEngine` block on its back, hidden until the drop), data `ExcavatorRig.asset`, boss data `ExcavatorRigBoss.asset` (id `DC/ExcavatorRig`, key `boss.excavatorRig`, reward `Burrow`), voice set `ExcavatorRigVoice`; `SurfaceBurst.prefab` (a pool-less `AcidBurst`) and `CollapsePit.prefab` (a Physical `FireZone`).
- **Numbers:** 3,800 HP, stagger 1,800 with a 3 s window, 3.6 m/s, machine.
- **Burrow & surface (phase 1, design core mechanic):** surfaced for 7 s, then it sinks (`Model` drops 3 m over 0.7 s, FMOD `event:/Bosses/Excavator/Rumble`; all damage ignored until it rises), shows a 2.5 m circle under the creature for 1.3 s, bursts up there (26) and rises. Each dive leaves a 1.8 m collapse pit where it went down (20/s Physical hazard, up to 6), shrinking the arena.
- **Attacks (yellow):** Drill Jab (60° at 3 m, 18); Drill Sweep (240° at 3.5 m, 2 × 14; phase 2).
- **Phase 2 (≤50%), drop down:** the floor gives way (greybox stand-in: the pits clear, FMOD `event:/Bosses/Excavator/Collapse`, a 2.5 s stun); it no longer burrows, and hits from within 120° behind it deal ×1.75 to the exposed engine. Adds: Dig Crew (max 2, every 15 s), then Security Drones as maintenance drones (1 on start, max 2).

## Containment Reactor AI (`ReactorAI/`; design: Containment Reactor AI page, TV-197; placeholder numbers)

- **Assets:** `ReactorAI.prefab` (`ReactorBoss : BossEnemy`; a green `Core` cylinder in its `CharacterMaterials`, the Kenney body shrunk away), data `ReactorAI.asset` (speed 0, machine), boss data `ReactorAIBoss.asset` (id `BS/ReactorAI`, key `boss.reactorAI`, reward `RadiationResistance`), voice set `ReactorAIVoice`; `ReactorRod.prefab` (a `BossPart`, the shared destructible boss part in `Bosses/BossPart.cs`: `Health` on the Enemy layer with a trigger collider, so spawning it never shoves the creature; raises `Pulled` when destroyed, `Restore()` puts it back; 150 HP here); `Enemies/Prefabs/RepairDrone.prefab` (`Enemies/Types/TaskDroneEnemy`: flies to its `Goal` instead of the creature and doesn't attack while it has one).
- **Numbers:** 2,000 HP; no stagger.
- **Control rods (design core mechanic):** 4 rods spawn on a 4.5 m ring at 45° offsets. The core takes no damage until a rod is destroyed; each one pulled exposes it for 6 s (FMOD `event:/Bosses/Reactor/RodPulled`), and with all 4 out it stays exposed. Each pull sends a Repair Drone (an add) to that rod: 3 s within 1.6 m reinserts it (`event:/Bosses/Reactor/RodRestored`), so kill it or lose the progress.
- **Escalation by rods pulled:** 1+ radiation bursts (3 Radiation `FireZone`s, 2.5 m, 1 s warning, 1.5 s, 18/s, every 5 s); 2+ Steam Jets (yellow 180° sweep at 9 m, 3 × 8 + Slow 0.3); 3+ drones fly ×1.6 faster; 4 the whole floor pulses (16 m, 1.5 s warning, 10/s, every 6 s).

## Dr. Elias Crane (`Crane/`; design: Dr. Elias Crane page, TV-198; final boss; placeholder numbers)

- **Assets:** `Crane.prefab` (`CraneBoss : BossEnemy`; the test boss body in white with a steel `LabRig` and a red `Heart` sphere shown only while exposed), data `Crane.asset`, boss data `CraneBoss.asset` (id `MV/Crane`, key `boss.crane`, no ability reward: the finale follows), voice set `CraneVoice`; `SedativeMist`, `LimbSweep` and `LimbGrab` (`FireZone`s: Slow 0.4 for 1.5 s; Physical; Root 1.5 s with a red warning), `FleshBurst` (a pool-less `AcidBurst`) and `CraneLimb.prefab` (a 300 HP `BossPart`).
- **Numbers:** 5,000 HP, stagger 2,000 with a 3 s window, 3.8 m/s.
- **Phase 1 (100–66%), lab rig (mask 0b0011):** Specimen Restraints (yellow projectile, 10 + Root 1.2 s), Sedative Mist (a 3 m Slow cloud on the creature's spot for 6 s); Hive Tissue Spawn adds (max 2).
- **Phase 2 (66–33%), hive fusion (mask 0b1100):** the rig comes off and the body grows ×1.25; Tissue Eruptions (four `FleshBurst`s around the creature's spot, 18), Mirror Tendrils (140° at 3.5 m, 2 × 12); ×1.2 speed, 2 Spawn on start (max 3).
- **Phase 3 (33–0%), arena-filling horror:** the body grows ×2 and roots in place; it takes no damage while any limb stands (`HeartGuard`). Three `CraneLimb`s spawn 5 m around it, each driving one attack every 2.5 s in turn: Slam (a 2.5 m `FleshBurst` on the creature, 28), Sweep (a 110° × 14 m `LimbSweep` wedge toward it, 1 s warning) and Grab (a red 1.5 m `LimbGrab` on it: Root and 40/s for 0.3 s). Severed limbs stop their attack (FMOD `event:/Bosses/Crane/Sever`). With all three severed the `Heart` shows and it takes damage for 12 s; when the window runs out a random limb regrows (`event:/Bosses/Crane/Regrow`). At 1 HP the shared finishing consume devours him.

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

## Sentry Titan (`SentryTitan/`; design: Sentry Titan page, TV-199; optional; placeholder numbers)

- **Assets:** `SentryTitan.prefab` (`SentryTitanBoss : BossEnemy`, scaffolded from the Cryo Loader and its subclass swapped), data `SentryTitan.asset` (3,200 HP, 2,400 armor; attacks Autocannon, the Defense Turret's projectile, and Stomp, the Cryo Loader's slam), boss data `SentryTitanBoss.asset` (id `CC/SentryTitan`, key `boss.sentryTitan`, reward `Shock` plus the `ConcussiveSlam` mutation), `TitanShell.prefab` (an `AcidBurst` without the pool) and `TitanMissile.prefab`. It's fought in `CC_TitanBay`, a Boss-role side arena off A4 with no security door.
- **Friendly fire (design core mechanic):** every shell is an `EnemyManager.PlaceBurst` with `Team.Environment` and the Titan as `Attacker`, so it also hits the Titan; a `SelfFire` incoming modifier scales those hits ×5 armor and ×0.5 health (`SelfHits` counts them). Artillery Barrage (7 shells, 2 of them around the Titan itself, every 9 s), arcing rockets (4 around the creature, 6 s) and the grenade launcher (3 in a line toward the creature, 5 s) all run on timers, ×0.75 from phase 2, when 2 seeking missiles (pooled, steering 120°/s, exploding on contact or after 5 s) join every 8 s.
- Swapping a scaffolded boss's subclass breaks its `DebugBossSpawnList` entry (it points at the removed component): repoint it after the swap, or F5 throws a NullReferenceException in `BossDebugCommands.Register`.

## Debug

- **Test boss:** `Debug/Resources/DebugTestBoss.prefab` (the Handler scaled 1.4×, `Debug/TestBoss.asset` 400 HP / 150 stagger, `Debug/TestBossPhases.asset`). Phase 1 uses the poke only. Phase 2 starts at 50%: all attacks, ×1.3 speed, and up to 2 Scientist adds every 6 s. It rewards Bio-Shield.
- **F5 Bosses tab:** boss readout, spawn test boss (revives it first), Spawn boss (any boss in `Debug/Resources/DebugBossSpawnList`, revived first), HP to next phase, spawn add, bring boss to 0 HP (finish), kill boss (skip consume), reset defeated bosses.
