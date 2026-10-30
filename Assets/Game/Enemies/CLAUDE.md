# Enemies

Enemy framework (`TentacleVania.Enemies`). Real enemies (Scientist, Security Guard, Handler) are separate tasks built on it; `Debug/TestBrute` (prefab + definition) exercises every attack kind and is only for testing.

## Structure

- `Data/EnemyDefinition` (ScriptableObject): vitals (Balance Sheet HP and armor), move and turn speed, spawn delay, circling range, despawn delay and a list of `EnemyAttack`s.
- `Data/EnemyAttack`: kind (`Melee` swept arc, `Area` cylinder, `Projectile`), telegraph (`Dodgeable` yellow / `Undodgeable` red), wind-up / active / recovery / cooldown, the target range it's used at, damage, stagger, type, and the shape fields for its kind. `TrackDuringWindUp` is how much of the wind-up the enemy keeps turning to follow the target before committing.
- `Enemy` is the state machine (`Spawning`, `Chase`, `Circle`, `Attacking`, `Stunned`, `Dead`). Per-enemy behaviour comes from subclasses overriding `TrySelectAttack`, `ChaseDestination`, `CircleDestination`, `OnActiveStart` and `OnDeath` (e.g. the Scientist flees). `Stun(seconds)` cancels any attack (the stagger task's entry point). Enemies die through `Health.Died` and are destroyed after `DespawnDelay` (pooling and gore come later).
- `EnemyManager` is created on first use in the active scene and ticks every enemy from one `Update` (never per-enemy `Update`). It owns the attack tokens and pools projectiles per prefab.
- Movement is a `NavMeshAgent` (rotation driven by `Enemy`). Each room scene bakes its own NavMesh with a `NavMeshSurface` on a `Navigation` object (Default layer only, physics colliders); `CombatArena`'s data is `Scenes/CombatArena/NavMesh-CombatArena.asset` (LFS). Rebake after changing a room's floor or walls.

## Enemy types (`Types/`, prefabs in `Prefabs/`, definitions in `Data/Enemies/`)

Numbers are the developer-approved baselines (HP and damage from the Balance Sheet; stagger bar = HP).

| Enemy | HP | Behaviour | Attacks |
| --- | --- | --- | --- |
| Handler (base `Enemy`) | 50, speed 4.8 | Walks the creature down (default chase and circling) | Heavy stun: RED melee 140° at 2.6 m, 20 dmg + `StunDuration` 1.0 s, within 2.4 m (1.1 / 0.2 / 0.9, cooldown 5), listed first so it's preferred; interrupt it by breaking its stagger bar (an interrupted attack starts no cooldown, so it tries again). Shock poke: yellow electric thrust 2.2 m, 10 dmg (0.4 / 0.12 / 0.45, cooldown 1) |
| Security Guard (`GuardEnemy`) | 40, speed 4.5 | Holds 5–8 m from the creature: closes in when farther, backs off to 6.5 m (trying ±30°/±60° on the NavMesh) when nearer; faces the creature within 12 m | Baton: yellow melee 1.8 m, 90°, 8 dmg, within 1.8 m (0.45 / 0.15 / 0.5, cooldown 1.2). Pistol: yellow projectile 18 m/s, 8 dmg, 4–11 m (0.45 / 0.05 / 0.4, cooldown 1.4) |
| Scientist (`ScientistEnemy`) | 20, speed 4 | Flees to 6 m away whenever the creature is within 7 m (tries 0°, ±35°, ±70° off straight away on the NavMesh), stands still when farther, cowers when every escape is blocked; faces the creature only when cornered or within 2 m | Stun prod: yellow melee, 1.4 m, 60°, 4 dmg, only within 1.6 m (0.5 / 0.15 / 0.6, cooldown 1.5) |

New enemy prefabs are built from the TestBrute layout by an eval (replace the `Enemy` component with the subclass, give it its own CharacterMaster material and definition) and added to `Debug/Resources/DebugEnemySpawnList`.

## Rules (developer decisions)

- Stagger: each enemy has a `StaggerBar` sized by `EnemyDefinition.MaxStagger` (defaults to its max health); it fills with each hit's `StaggerDamage` and never drains. Breaking it calls `Stun(StaggerStun)` (2.0 s, Balance Sheet) and adds a `StaggerDamageTaken` ×1.5 incoming modifier until the stun ends; the bar can't fill while stunned. The enemy glows pale blue while broken (placeholder for stagger-break poses) and raises `Staggered`. Bosses will set a larger bar and a 1.0 s stun in their definitions.

- Attack tokens: at most 2 melee/area and 2 ranged attacks winding up or active at once (`EnemyManager.MeleeTokens` / `RangedTokens`). Enemies that want to attack without a token circle at `CircleRange`.
- Telegraphs: the whole enemy glows yellow or red, ramping from 25% to full across the wind-up (`TelegraphGlow`, through the enemy's `CharacterMaterials` on the CharacterMaster shader). Area attacks also show a URP decal (`GroundIndicator`: ring at the exact hit radius plus a fill growing with the wind-up); the hit is an overlap cylinder at the same center and radius, locked when tracking ends. Red attacks set `HitFlags.Undodgeable`, which the creature's dash i-frames don't block.
- Collision: enemies are solid to the walking creature (capsule on the `Enemy` layer); the creature's dash excludes the `Enemy` layer, so it passes through.
- Melee hits are swept: sphere casts from the enemy outward along the arc, subdivided to at most 10° per cast so fast swings never skip a target. Projectiles are pooled kinematic-free rigidbodies with trigger colliders aimed at `CombatTargets.CreatureAimPoint`; a dash-blocked projectile flies on.

## Pitfalls

- Enemy materials must use the CharacterMaster shader and the prefab needs a `CharacterMaterials` listing its renderers, or telegraphs, hit flashes and the stun glow won't show. (URP Lit can't be used: it drops `_EMISSION` at runtime when the base emission is black.)
- Ground decals project through everything in their box, so the indicator box is a 0.6 m slice at floor level; use decal layers if it needs to skip characters entirely.
