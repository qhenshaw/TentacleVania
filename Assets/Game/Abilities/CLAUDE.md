# Abilities

Active abilities (`TentacleVania.Abilities`), on the ability input (RT / Q), paid from the charge meter (`Creature/CreatureCharge`).

## Structure

- Create new abilities and mutations with `Tools/content-new.sh ability|mutation <Name>`: stub classes, asset, `ContentCatalog` entry and UI strings.
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

## Shock (`Shock/`, `Shock.asset`; TV-154; placeholder numbers)

- A slot ability on the charge meter (40 charge): an instant bio-electric discharge around the creature, an overlap on the Enemy layer that hits each enemy once with Electric damage flagged `HitFlags.Ability` (so the Galvanic Overload feat counts it), plus `StatusKind.Static` (a full Static bar chains lightning, `Combat/CLAUDE.md`) and 40 stagger.
- Tiers (bought at nests: 600 and 1,200 biomass): radius 4 / 5 / 6 m, damage 25 / 35 / 45, Static 35 / 50 / 70 per hit (`ShockDefinition.At`).
- Placeholder presentation: a cyan creature flash and a pooled `VFX/Abilities/ShockBurst` burst (scaled to the radius); FMOD `event:/Abilities/Shock/Activate`. Reward of the optional Sentry Titan (TV-199).

## Spine Volley (`SpineVolley/`, `SpineVolley.asset`; TV-155; placeholder numbers)

- A slot ability (30 charge) and Major Ross's reward: a fan of spines over 30° from the creature, auto-aimed at the nearest enemy within 12 m and 35° of its facing (`SpineVolleyDefinition.FanDirection`, unit tested). Spines are pooled (`MaxSpines × 2` instances of `Spine.prefab`, a thin cylinder, made at equip) and simulated in `Tick`: 24 m/s, a 0.2 m sphere cast each frame on the Enemy layer, stopped by Default walls, gone after 12 m. Each hit is Physical `HitFlags.Ability` damage with 6 stagger (FMOD `event:/Abilities/SpineVolley/Hit`).
- Tiers (700 / 1,400 biomass): 5 / 7 / 9 spines, 10 / 13 / 16 damage.
- **Mutation hooks:** `PiercingSpines` equipped lets a spine pass through up to 4 enemies; `EmbeddedBarbs` makes hits apply Bleed; `SpineVolleyRuntime.SpineHit` (static event: target, point) is for Spine Cascade and the like. Ability-flagged hits also count for the Embedded Barbs feat.

## Tendril Snare (`TendrilSnare/`, `TendrilSnare.asset`; TV-156; placeholder numbers)

- A slot ability (35 charge): tentacles erupt 0.3 s later on the nearest enemy within 7 m and 45° of the facing (else 4 m ahead), hitting everything in the radius once for 8 Physical `HitFlags.Ability` damage plus `StatusKind.Root`. Tiers (700 / 1,400): radius 3 / 3.5 / 4 m, root 2.5 / 3 / 3.5 s. Placeholder `VFX/Abilities/TendrilSnare` burst, FMOD `event:/Abilities/TendrilSnare/Activate`, `Erupt`.
- Enemies now honour Root and Slow: `Enemy.Tick` scales the agent speed by `StatusEffects.MoveScale` (0 while rooted).
- Hidden in Cold Storage R3 as `World/Pickups/AbilityPickup.prefab` (a pickup of the new `PickupKind.Ability`, which unlocks the id through `BossProgress.Unlock`, toast `pickup.ability`).

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

- `Core.Interaction.Traversals` is a registry of `ITraversal` spots (ability id, position, radius, `CanTraverse`, `Traverse()`), like `Interactions` but on LT. `CreatureTraversal` (creature prefab, `[DefaultExecutionOrder(-40)]`) uses the nearest spot in reach whose ability is unlocked (`Build.Abilities` through `BossProgress.IsAbilityUnlocked`, or `CreatureTraversal.SetForced(id)` for debug) and claims the press. `TraversalAbilityDefinition` is the plain non-slot definition for such abilities (`Burrow.asset`, `VentSqueeze.asset` and `Overload.asset`, ids `Burrow`, `VentSqueeze` and `Overload`, in the `ContentCatalog`); every ability asset needs `ability.<id>.name` in the UI table (`loc-add.py`), or content validation fails.
- `World/Passages/PassageSpot` is the spot for both (its `_abilityId`): LT on a spot tunnels to its `_exit` spot with `FastTravel.Tunnel` (the crawlspace transit: cover, 3 m under the floor, routed through the rooms between with `TunnelRoute` when a sector streams), arriving facing along the exit's forward, and plays its VFX at both spots and its sound. Pair spots across a barrier in one scene.
- Burrow (TV-150; the Excavator Rig's future reward; no design numbers): `World/Passages/BurrowGate.prefab`, a 3 m barrier (collider) with a soft-soil spot 1.6 m either side (radius 1.4), `VFX/Abilities/BurrowDirt`, `event:/Abilities/Burrow/Dig` and a `MapGate`.
- Vent Squeeze (TV-151; The Seeper's future reward; no design numbers): `World/Passages/VentGate.prefab`, a 3 × 3 m sealed grate wall (collider) with a floor vent on each face and a spot 1.2 m out on each side (radius 1.3), `VFX/Abilities/VentSqueeze`, `event:/Abilities/VentSqueeze/Squeeze` and a `MapGate`. It replaces the old primitive grate in `BL_A6` (in `BioLabsPlan` too).
- Overload, LT side (TV-152; the Overwatch Core's future reward; no design numbers): `World/Overload/OverloadDoor.prefab` (the `NestDoor` slab with a junction box beside it, a `MapGate`): LT within 1.6 m of the junction surges it (`VFX/Abilities/OverloadSurge`, `event:/Abilities/Overload/Surge`) and the slab sinks after 0.35 s; saved through `Core.Progression.OverloadedNodes` (`WorldFlag.OverloadedNode`, id `<scene>/<name>` by default), so it loads open. `OverloadJunction.prefab` (turret arrays): LT stuns every machine enemy (`EnemyDefinition.Machine`) within 12 m for 6 s, then needs 10 s (not saved). The slot-ability side (charge cost, radius, tier 3 EMP) isn't built.
- **Debug:** F5 Abilities tab: LT traversal (available spot), Burrow / Vent Squeeze / Overload unlocked (debug), Reset overloaded doors (save), Spawn LT gate ahead (`World/Passages/Debug/Resources/DebugBurrowGate` / `DebugVentGate`, `World/Overload/Debug/Resources/DebugOverloadDoor` / `DebugOverloadJunction`). Evals call `CreatureTraversal.TryTraverse()`.

## Mutations (`Mutations/`; design: Progression & Economy, Mutations, and TV-31's slice choices)

- `MutationDefinition` (ScriptableObject): `TextKey` (UI table `<key>.name` / `<key>.effect`), `Notches` 1–3, and `CreateEffect()`, which returns a plain `MutationEffect` (`Enable`/`Disable`/`Tick` against a `MutationHost` holding the creature's motor, consume and grapple). Each mutation class needs its own file named after it, or Unity can't save its assets (four classes in one file left three assets missing).
- **Slice mutations** (`Mutations/Data/`, in the `ContentCatalog`; values proposed in TV-31, pending review):
  - `WideDodge` (1 notch): `CreatureMotor.MinDashInvulnerability` 0.3 s.
  - `Gluttony` (1): after a consume starts, the next consume within 6 s gets `CreatureConsume.NextConsumeSpeed` ×1.3 (via `CreatureMaw.SpeedMultiplier`) and `NextHealScale` ×1.5.
  - `RapidDigestion` (1): `HealDurationScale` 0.5.
  - `LungingMaw` (2): `TentacleGrapple.GrabOnArrival`, so an enemy zip ends in `CreatureConsume.GrabTarget` when the target is grabbable.
- **Runtime:** `MutationController` (creature prefab) rebuilds the effects from `Core.Progression.MutationLoadout.Equipped` on `Changed` and on load. `Equip(id)` checks the notch fit, and `UsedNotches()` sums the equipped costs. Equipping happens in the nest menu's Mutations tab.
- **Debug:** F5 Abilities tab: Mutations readout, Grant mutation, Equip mutation (ignores nests), Clear mutations.

## Combat feats (`Feats/`, TV-145)

- `FeatDefinition` (ScriptableObject, `Feats/Data/<Mutation>Feat.asset`, listed in `Feats/Resources/FeatCatalog.asset`): the mutation id it grants, a UI `TitleKey` for progress toasts, a `FeatCondition` (`Kill`, `Consume`, `ConsumeUnderPressure`, `StaggerBreak`, `AbilityMultiHit`), the `Count`, a `Window` in seconds (0 = across the run, with progress saved as `id=count` in `Progress.Feats` through `Core.Progression.FeatProgress`) and filters: target status, creature health below, machines only (a `Health` that doesn't bleed), ability hits only, Electric only, `MinTargets` (attackers during a consume, or targets of one ability use within 0.25 s) and `Streak` (a kill that doesn't match resets it).
- `FeatTracker` (static, booted after the first scene load in play) listens to `CombatEvents` (`HitLanded` fires before `Died`, so statuses are still on the target; `StaggerBroken` fires before the breaking hit's `HitLanded`, which it pairs with), keeps a `FeatCounter` (fixed-size ring of timestamps, unit tested) per feat, reports `FeatProgress.Progressed` (the pickup toast shows `feat.progress` "{name}: n/m") and grants the mutation through `Collectibles.Grant` when one completes. Feats for mutations already owned never count.
- **The seven feats** (Mutations database; counts are proposed): Embedded Barbs 4 ability kills in a row within 30 s ("only spines": Spine Volley must flag its hits `HitFlags.Ability`), Galvanic Overload 5 machine stagger breaks by Electric ability hits (Shock), Retaliation Plating a consume finished while 3+ enemies hit you, Open Wounds 5 bleeding kills within 10 s, Pyre Feast 3 burning consumes within 15 s, Acid Blood 5 corroded kills below 30% HP, Feedback Loop 10 ability uses that hit 3+ enemies.
- F5 Abilities: Feats progress readout, Feat tracking on/off, Reset feat progress.
