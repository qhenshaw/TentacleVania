# Enemies

Enemy framework (`TentacleVania.Enemies`). Real enemies (Scientist, Security Guard, Handler) are separate tasks built on it; `Debug/TestBrute` (prefab + definition) exercises every attack kind and is only for testing.

## Structure

- `Data/EnemyDefinition` (ScriptableObject): vitals (Balance Sheet HP and armor), move and turn speed, spawn delay, circling range, despawn delay and a list of `EnemyAttack`s.
- `Data/EnemyAttack`: kind (`Melee` swept arc, `Area` cylinder, `Projectile`), telegraph (`Dodgeable` yellow / `Undodgeable` red), wind-up / active / recovery / cooldown, the target range it's used at, damage, stagger, type, and the shape fields for its kind. `TrackDuringWindUp` is how much of the wind-up the enemy keeps turning to follow the target before committing.
- `Enemy` is the state machine (`Spawning`, `Chase`, `Circle`, `Attacking`, `Stunned`, `Dead`). Per-enemy behaviour comes from subclasses overriding `TrySelectAttack`, `ChaseDestination`, `CircleDestination`, `OnActiveStart` and `OnDeath` (e.g. the Scientist flees). `Stun(seconds)` cancels any attack (the stagger task's entry point). Enemies die through `Health.Died` and are destroyed after `DespawnDelay` (pooling and gore come later).
- `EnemyManager` is created on first use in the active scene and ticks every enemy from one `Update` (never per-enemy `Update`). It owns the attack tokens and pools projectiles per prefab.
- Movement is a `NavMeshAgent` (rotation driven by `Enemy`). Each room scene bakes its own NavMesh with a `NavMeshSurface` on a `Navigation` object (Default layer only, physics colliders); `CombatArena`'s data is `Scenes/CombatArena/NavMesh-CombatArena.asset` (LFS). Rebake after changing a room's floor or walls.

## Rules (developer decisions)

- Stagger: each enemy has a `StaggerBar` sized by `EnemyDefinition.MaxStagger` (defaults to its max health); it fills with each hit's `StaggerDamage` and never drains. Breaking it calls `Stun(StaggerStun)` (2.0 s, Balance Sheet) and adds a `StaggerDamageTaken` ×1.5 incoming modifier until the stun ends; the bar can't fill while stunned. The enemy glows pale blue while broken (placeholder for stagger-break poses) and raises `Staggered`. Bosses will set a larger bar and a 1.0 s stun in their definitions.

- Attack tokens: at most 2 melee/area and 2 ranged attacks winding up or active at once (`EnemyManager.MeleeTokens` / `RangedTokens`). Enemies that want to attack without a token circle at `CircleRange`.
- Telegraphs: the whole enemy glows yellow or red, ramping from 25% to full across the wind-up (`TelegraphGlow`, through the enemy's `CharacterMaterials` on the CharacterMaster shader). Area attacks also show a URP decal (`GroundIndicator`: ring at the exact hit radius plus a fill growing with the wind-up); the hit is an overlap cylinder at the same center and radius, locked when tracking ends. Red attacks set `HitFlags.Undodgeable`, which the creature's dash i-frames don't block.
- Collision: enemies are solid to the walking creature (capsule on the `Enemy` layer); the creature's dash excludes the `Enemy` layer, so it passes through.
- Melee hits are swept: sphere casts from the enemy outward along the arc, subdivided to at most 10° per cast so fast swings never skip a target. Projectiles are pooled kinematic-free rigidbodies with trigger colliders aimed at `CombatTargets.CreatureAimPoint`; a dash-blocked projectile flies on.

## Pitfalls

- Enemy materials must use the CharacterMaster shader and the prefab needs a `CharacterMaterials` listing its renderers, or telegraphs, hit flashes and the stun glow won't show. (URP Lit can't be used: it drops `_EMISSION` at runtime when the base emission is black.)
- Ground decals project through everything in their box, so the indicator box is a 0.6 m slice at floor level; use decal layers if it needs to skip characters entirely.
