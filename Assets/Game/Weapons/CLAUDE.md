# Weapons

The creature's weapons (`TentacleVania.Weapons`), following Core Mechanics & Controls › Attacks.

## Data

- `Data/WeaponDefinition` (one asset per weapon in `Data/Weapons/`): base damage and stagger (Balance Sheet tier 1; stagger defaults to the damage, Crusher Mass ×3), armor damage scale, damage type, three combo `Steps`, a `Heavy` and a `DashAttack`.
- `Data/WeaponMove` is one move: wind-up / strike / recovery, multipliers on the weapon's base values, extra `HitFlags`, and a parametric shape (developer decision) that drives both the hit casts and the tip path the tentacles follow:
  - `Sweep`: an arc from `StartAngle` to `EndAngle` (0 = forward, positive = right) at `Reach`, as sphere casts of `Radius` subdivided to ≤10° (sweeps, thrusts with equal angles, the 360° Whirlwind).
  - `Area`: a circle or sector (`SectorAngle`) of `Radius` at `AreaOffset` in front, checked once at strike start (slams, Quake, Tentacle storm).
  - `Body`: an overlap of `Radius` around the creature every strike frame, for dash attacks that `LungeDistance` along the attack direction.
- Heavies have no wind-up of their own: the charge is the wind-up. While charging, `WeaponTentacleView` raises that slot's weapon tentacle into `MoveShapes.ChargeTip` (sweeps: pulled 45° behind the start angle, raised 0.9 m; areas: raised overhead; body: coiled behind) over `HeavyWindUp` (0.5 s), holds it with a slight tremble, and flashes the creature and rumbles once when ready (developer decisions).

## Weapons

| Weapon | Slot on the prefab | Base | Moves (wind-up / strike / recovery s) |
| --- | --- | --- | --- |
| Lash Whips (`LashWhips.asset`) | Weapon 1 | 12 dmg, 12 stagger | Sweep and Backsweep ±70° at 2.2 m (0.08/0.10/0.20); Whirlwind 360° ×1.5 (0.12/0.20/0.35); Tentacle storm heavy, 100° cone 3.2 m, ×2.5 dmg ×3 stagger (0/0.25/0.45); Spinning lunge dash attack, 3 m lunge (0.04/0.15/0.25) |
| Crusher Mass (`CrusherMass.asset`) | None: found in Specimen Holding A8 (`CrusherMassPickup`); F5 Weapons "Give Crusher Mass" | 30 dmg, 90 stagger, armor ×2 (placeholder; the design's bonus is TBD) | Slam ×2, 1.2 m circle 1.4 m ahead (0.25/0.10/0.40); Quake 3 m circle ×1.5 (0.35/0.15/0.55); Armor breaker heavy ×2.5, armor ×3 (0/0.15/0.60); Battering ram dash attack, 3.5 m lunge (0.05/0.20/0.35) |

## Modifiers

- `Data/WeaponModifier` assets in `Data/Modifiers/`, listed on their weapon's `WeaponDefinition.Modifiers` (2 each; ids are asset names). Unlock costs from the Balance Sheet (1,000 biomass + 1 Mutagen); effect values are proposals awaiting review: Maelstrom (Lash Whips) runs the Whirlwind finisher at 1.5× duration and knocks enemies farther than 1.5 m inward instead of outward (reusing the finisher knockback); Charged Cyclone adds +4 charge per Whirlwind hit; Shatter (Crusher Mass) makes an enemy whose armor an Armor breaker heavy empties take ×1.25 damage for 6 s (a pooled `ScaleModifier` in its `Health.Incoming`, 8 at once); Bone Breaker raises the grab threshold of a staggered enemy it hits to 40% for 3 s (`Consumable.RaiseThreshold`, read through `Health.IsStaggered`, which `Enemy` sets during a stagger break). Developer decision: Lab Security and Hazmat have 20 armor so Shatter has slice targets; the values are kept for the slice.
- Owned and equipped modifiers live in `Core.Progression.Upgrades`; `WeaponController` caches each slot's equipped modifier on `Upgrades.Changed` and on `Equip`. F5 Weapons: Modifiers, Mutagen, +1 Mutagen, Cycle weapon 1/2 modifier (grants and equips the next, then none).
- Testing: in play, the creature aims at the editor's mouse position, so a move started from an eval can face away from a target placed in front; place the target along `ActiveForward` after starting the move.

## WeaponController (on the creature)
- Tuning is in assets, not on the prefab: `Data/HatchlingWeaponHandling` (`WeaponHandlingSettings`: heavy threshold, heavy windup, input buffer, combo reset, dash-attack window, attack move speed, soft-lock range and cones) for `WeaponController`, and `Data/HatchlingWeaponTentacles` (`WeaponTentacleSettings`: joints, length, arch, root offset, widths, emerge and retract times, spring) for `WeaponTentacleView`. The numbers below are their current values.

- Both weapon buttons share one `ComboCounter` (3 steps, crossover, reset after 0.6 s without attacking, after a finisher, after a heavy; dash attacks don't touch it). `ChargeButton` resolves a tap on release; holding past 0.35 s charges the heavy, which fires only on release (never by itself; developer decision); a release before the 0.5 s windup completes queues it until then (`ChargeButton`, unit tested). A dash cancels any charging or queued heavy and just dashes (developer decision). Presses up to 0.15 s before the current move ends are buffered.
- A press during a dash or within 0.12 s after it is a dash attack (immediate, no tap wait).
- While attacking or charging the creature steers at 40% speed (developer decision: slowed, free steering, no lunges on normal moves). Dash is locked during wind-up and strike; a dash in recovery cancels the move without resetting the combo.
- Soft-lock (developer decision): at attack start the move aims at the best enemy (distance × angle score) within 6 m and a 90° cone around the facing, or a 30° cone around active aim (mouse, or right stick past the deadzone). The creature snaps to that direction and keeps it for the move.
- `WeaponVfx` (on the creature next to `WeaponController`; developer decisions) plays a move's optional `WeaponMove.Vfx` (scaled by `VfxScale`) once when its strike starts. Sweeps play at the creature facing the move, areas at `MoveShapes.AreaCenter`, and body moves (dash attacks) follow the creature. It sets `StartAngle`, `EndAngle`, `Reach`, `Radius` and `Duration` when the graph exposes them (an Area cone passes ±`SectorAngle`/2 and its radius as reach). Instances are pooled, 3 per prefab, prewarmed at `Start` and on `Equipped`. Placeholders are in `VFX/Weapons/`: `WeaponSwipe` (Lash Whips slashes and Tentacle storm), `WeaponGroundImpact` (Crusher slams, Armor breaker, Quake ×1.5) and `WeaponDashTrail` (both dash attacks). F5 Weapons: Weapon VFX toggle, Weapon VFX played.
- Each target is hit once per move. Hits go through the Combat pipeline with `Team.Creature`, the creature's `Health` as attacker, and `Finisher`, `Heavy` or `DashAttack` flags.
- `WeaponTentacleView` runs a `SpringChain` per weapon side: the tentacle grows out of the body over the first `_emergeTime` of the wind-up, its tip is pinned to `MoveShapes.Tip` (so the hit casts, which use the same shape, always match the CPU-side tip) while the middle springs behind for whip overlap, and it pulls back into the body over `_retractTime` after the move. Enemies flash white for 0.08 s when hit (`TelegraphGlow.Flash`).
- Tip meshes (developer decisions): `WeaponDefinition` sets `_tipMesh` (a prefab modelled along +Y from its base: `Meshes/LashBlade` = `blade_008`, `Meshes/CrusherHead` = `mace-head_005`, both 1.5x, from the Low Poly Modular Medieval Weapons pack), `_tipScale` and `_tipOverlap` (how far the base sinks into the tip). The view makes one instance per slot and weapon (at `Start` for the equipped ones), scales it with the tentacle's emerge and retract and hides it otherwise, points it along the last bone and rolls it so the blade edge leads the swing (flat when slow). Visual only: the drawn tip is pulled in by the weapon's length so the weapon ends where the hit sweep is; hit reach is unchanged. The prefabs use `Meshes/WeaponTip.mat` (CharacterMaster, tentacle black, smoothness 0.92), are on the `TentacleMerge` rendering layer and register with `CharacterMaterials`. Debug toggle: Weapons tab "Weapon tip meshes".

## Testing in play

Script timed button presses from `UnityEditor.EditorApplication.update` with `TestGamepad.Set` / `Release` (see the root CLAUDE.md); log `WeaponController.AttackStarted` and `CombatEvents.HitLanded`. Freeze enemies with `EnemyManager.Instance.Paused = true` and spawn the dummy in front of the creature: soft-lock only looks within 45° of its facing.
