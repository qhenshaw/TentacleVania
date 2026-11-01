# Abilities

Active abilities (`TentacleVania.Abilities`), on the ability input (RT / Q), paid from the charge meter (`Creature/CreatureCharge`).

## Structure

- `AbilityDefinition` (ScriptableObject): display name, charge `Cost`, and `CreateRuntime(AbilityContext)`, which builds the ability's plain C# `AbilityRuntime` (`Activate`, `Tick`, `Cancel`, `Dispose`) once at equip, so activations allocate nothing.
- `AbilityController` (on the creature prefab) holds the one equipped ability (`Equip(definition)` swaps it), ticks the runtime, and on `Ability.Pressed` spends the cost with `CreatureCharge.TrySpend` and activates. Nothing fires while control is locked (stun, consume) or the creature is dead; death or disabling cancels without effects. `AbilityContext.RefreshStatus` hands the status glow back to the charge meter (`CreatureCharge.RefreshGlow`) when an ability ends.
- `Core.Progression.BossProgress.AbilityUnlocked` (a boss reward) equips the unlocked ability through the `ContentCatalog` only when the slot is empty (developer decision). A save restores exactly its `Build.Ability`, so a new game starts with an empty slot (the Warden grants Bio-Shield); the prefab's Bio-Shield only shows when playing a scene without a save. F5 Abilities: Equip Bio-Shield, Unequip.
- Debug: Abilities tab (equipped, state, "Free abilities (no charge)", "Activate").

## Bio-Shield (`BioShield/`, `BioShield.asset`)

- Task TV-11 baseline (Balance Sheet charge-meter section): 50 charge, absorbs up to 40 damage for 3 s, then bursts for 25 damage in 4 m, early when the absorb runs out. "Heavy stagger" has no number in the design: `_burstStagger` is 45 (breaks every prototype enemy bar but the TestBrute's). The Balance Sheet's prototype table lists 40 charge and 2 s instead; the asset follows the task until the developer settles it.
- `ShieldAbsorb` (unit tested) is an `IDamageModifier` at order 900 in `Health.Incoming`: it eats the hit after earlier scaling and passes the rest through.
- The burst is a `HitFlags.Heavy` overlap on `_hitLayers` (Enemy) from the creature, so hit feedback and knockback treat it as a heavy hit.
- Tiers: `AbilityDefinition._tierCosts` (biomass to reach tier 2, 3...; Bio-Shield has 800, so tier 2 is its maximum in the slice) and `MaxTier`; the tier is `Core.Progression.Upgrades.AbilityTier(id)`, bought in the nest menu's Upgrades tab. Bio-Shield reads it on activation: tier 2 absorbs 60 and bursts for 35 (`_tier2Absorb`, `_tier2BurstDamage`). Tier 3 (perfect timing) isn't built. F5 Abilities: Bio-Shield tier, Toggle Bio-Shield tier 1/2.
- Placeholder presentation: an amber transparent sphere (`BioShieldShell.prefab`, URP Unlit) that swells as hits land and expands to the burst radius, the amber status glow on the creature, and FMOD placeholder events `event:/Abilities/BioShield/Activate`, `Absorb` and `Burst`.

## Tentacle Grapple (`Grapple/`, `TentacleGrapple.asset`; design: Tentacle Grapple page, Changelog Grilling sessions 3 and 5)

- A traversal ability on its own input, the `Traverse` action (LT / F). It isn't in the ability slot: `GrappleDefinition.SlotAbility` is false, so `AbilityController` never equips it. It's unlocked like a boss reward, by the id `TentacleGrapple` in `Build.Abilities` (the Surgical Automaton's `RewardAbility`), and it's in the `ContentCatalog`.
- `TentacleGrapple` on the creature prefab:
  - **Targeting:** it picks the `GrappleAnchor` or enemy (a `Hurtbox` on `_enemyLayers`) within 10 m whose direction is closest to the aim, or to the facing with no aim input, up to 70° off (`GrappleRules`, unit tested). Bosses count.
  - **Shot and zip:** it shoots a placeholder `LineRenderer` tentacle (0.12 s), then zips along an arc in 0.2–0.4 s (28 m/s). Control is locked, `CreatureMotor.Suspended` is set, the `CharacterController` is off, and i-frames come from `GrantInvulnerability`.
  - **Landing:** on an anchor's `Landing` point, or 1.6 m short of the enemy, which is tracked while it moves. 2 s cooldown, no charge cost. FMOD `event:/Abilities/Grapple/Shoot`, `Zip`, `Latch`.
- **Chasms:** the `Chasm.prefab` has invisible `ChasmEdge` walls that the creature can't walk through and the zip ignores. An enemy zip whose path crosses a `ChasmEdge` (a ray at knee height on `_blockerLayers`) is refused, so only anchors cross chasms. Put `ChasmEdge` on hazard volumes too. `GrappleAnchor.prefab` is a hazard-striped post (stripes are reserved for anchors).
- **Debug:** `Grapple/Debug/Resources/DebugGrappleRig.prefab` (floor, 6 m chasm, anchor, far floor). F5 Abilities tab: Grapple state, Grapple unlocked (debug), Spawn grapple rig. `TestGamepad` can't press triggers, so drive it with `TryGrapple()` in evals.

## Breach (`Breach/`, `Breach.asset`; design: Breach page, which has no numbers: all values are placeholders)

- A traversal ability on the `Traverse` input (LT / F) like the grapple: `BreachDefinition.SlotAbility` is false and it unlocks by the id `Breach` in `Build.Abilities` (the Cryo Loader Mech's future reward; in the `ContentCatalog`).
- `WeakWall` (`Breach/WeakWall.prefab`: a 3 × 3 × 0.6 m cracked slab on Default with a collider, an `Intact` and a `Broken` (floor rubble) child, a `Debris` VFX (`VFX/Abilities/BreachDebris` placeholder burst), `event:/Abilities/Breach/WallBreak`, and a `World.Map.MapGate` child for the map's blocked mark). Its id defaults to `<scene>/<name>`; a breached wall is saved through `Core.Progression.BreachedWalls` and loads broken. Place it in a doorway gap (a `LayoutOpening` of kind Prefab works: the wall runs along its local X, 1.5 m either side) and rebake the room NavMesh with it intact.
- `CreatureBreach` (creature prefab, `[DefaultExecutionOrder(-50)]`): on LT, when unlocked, it picks the nearest unbroken wall whose closest point is within `Range` 2.5 m and within 75° of the facing (or 0.9 m in any direction; `BreachRules`, unit tested), lunges to 0.9 m short of it in 0.18 s with control locked, smashes it (hit-stop, shake, rumble), and recovers for 0.15 s. A successful press calls `Core.Interaction.Traversals.Claim()`, which `CreatureTraversal` and the grapple check, so one LT press does one thing (order: Breach, spot traversals, grapple).
- **Debug:** `Breach/Debug/Resources/DebugWeakWall.prefab`. F5 Abilities tab: Breach (state and target), Breach unlocked (debug), Spawn weak wall ahead, Restore breached walls. `TestGamepad` can't press triggers: call `TryBreach()` from evals.

## LT spot traversals (Burrow; later Vent Squeeze and Overload)

- `Core.Interaction.Traversals` is a registry of `ITraversal` spots (ability id, position, radius, `CanTraverse`, `Traverse()`), like `Interactions` but on LT. `CreatureTraversal` (creature prefab, `[DefaultExecutionOrder(-40)]`) uses the nearest spot in reach whose ability is unlocked (`Build.Abilities` through `BossProgress.IsAbilityUnlocked`, or `CreatureTraversal.SetForced(id)` for debug) and claims the press. `TraversalAbilityDefinition` is the plain non-slot definition for such abilities (`Burrow.asset`, id `Burrow`, in the `ContentCatalog`); every ability asset needs `ability.<id>.name` in the UI table (`loc-add.py`), or content validation fails.
- Burrow (TV-150; the Excavator Rig's future reward; no design numbers): `World/Burrow/BurrowGate.prefab` is a 3 m barrier (collider) with a soft-soil `BurrowSpot` 1.6 m either side (radius 1.4) and a `MapGate`. LT on a spot tunnels to the other one with `FastTravel.Tunnel` (the crawlspace transit: cover, 3 m under the floor), arriving facing away from the barrier, with `VFX/Abilities/BurrowDirt` at both spots and `event:/Abilities/Burrow/Dig`. For a barrier across rooms, pair two `BurrowSpot`s through `_exit` in one scene.
- **Debug:** F5 Abilities tab: LT traversal (available spot), Burrow unlocked (debug), Spawn burrow gate ahead (`World/Burrow/Debug/Resources/DebugBurrowGate.prefab`). Evals call `CreatureTraversal.TryTraverse()`.

## Mutations (`Mutations/`; design: Progression & Economy, Mutations, and TV-31's slice choices)

- `MutationDefinition` (ScriptableObject): `TextKey` (UI table `<key>.name` / `<key>.effect`), `Notches` 1–3, and `CreateEffect()`, which returns a plain `MutationEffect` (`Enable`/`Disable`/`Tick` against a `MutationHost` holding the creature's motor, consume and grapple). Each mutation class needs its own file named after it, or Unity can't save its assets (four classes in one file left three assets missing).
- **Slice mutations** (`Mutations/Data/`, in the `ContentCatalog`; values proposed in TV-31, pending review):
  - `WideDodge` (1 notch): `CreatureMotor.MinDashInvulnerability` 0.3 s.
  - `Gluttony` (1): after a consume starts, the next consume within 6 s gets `CreatureConsume.NextConsumeSpeed` ×1.3 (via `CreatureMaw.SpeedMultiplier`) and `NextHealScale` ×1.5.
  - `RapidDigestion` (1): `HealDurationScale` 0.5.
  - `LungingMaw` (2): `TentacleGrapple.GrabOnArrival`, so an enemy zip ends in `CreatureConsume.GrabTarget` when the target is grabbable.
- **Runtime:** `MutationController` (creature prefab) rebuilds the effects from `Core.Progression.MutationLoadout.Equipped` on `Changed` and on load. `Equip(id)` checks the notch fit, and `UsedNotches()` sums the equipped costs. Equipping happens in the nest menu's Mutations tab.
- **Debug:** F5 Abilities tab: Mutations readout, Grant mutation, Equip mutation (ignores nests), Clear mutations.
