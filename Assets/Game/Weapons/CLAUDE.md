# Weapons

The creature's weapons (`TentacleVania.Weapons`), following Core Mechanics & Controls › Attacks.

## Data

- `Data/WeaponDefinition` (one asset per weapon in `Data/Weapons/`): base damage and stagger (Balance Sheet tier 1; stagger defaults to the damage, Crusher Mass ×3), armor damage scale, damage type, three combo `Steps`, a `Heavy` and a `DashAttack`.
- `Data/WeaponMove` is one move: wind-up / strike / recovery, multipliers on the weapon's base values, extra `HitFlags`, and a parametric shape (developer decision) that drives both the hit casts and the tip path the tentacles follow:
  - `Sweep`: an arc from `StartAngle` to `EndAngle` (0 = forward, positive = right) at `Reach`, as sphere casts of `Radius` subdivided to ≤10° (sweeps, thrusts with equal angles, the 360° Whirlwind).
  - `Area`: a circle or sector (`SectorAngle`) of `Radius` at `AreaOffset` in front, checked once at strike start (slams, Quake, Tentacle storm).
  - `Body`: an overlap of `Radius` around the creature every strike frame, for dash attacks that `LungeDistance` along the attack direction.
- Heavies have no wind-up: the charge is the wind-up.

## Weapons

| Weapon | Slot on the prefab | Base | Moves (wind-up / strike / recovery s) |
| --- | --- | --- | --- |
| Lash Whips (`LashWhips.asset`) | Weapon 1 | 12 dmg, 12 stagger | Sweep and Backsweep ±70° at 2.2 m (0.08/0.10/0.20); Whirlwind 360° ×1.5 (0.12/0.20/0.35); Tentacle storm heavy, 100° cone 3.2 m, ×2.5 dmg ×3 stagger (0/0.25/0.45); Spinning lunge dash attack, 3 m lunge (0.04/0.15/0.25) |
| Crusher Mass (`CrusherMass.asset`) | Weapon 2 | 30 dmg, 90 stagger, armor ×2 (placeholder; the design's bonus is TBD) | Slam ×2, 1.2 m circle 1.4 m ahead (0.25/0.10/0.40); Quake 3 m circle ×1.5 (0.35/0.15/0.55); Armor breaker heavy ×2.5, armor ×3 (0/0.15/0.60); Battering ram dash attack, 3.5 m lunge (0.05/0.20/0.35) |

## WeaponController (on the creature)

- Both weapon buttons share one `ComboCounter` (3 steps, crossover, reset after 0.6 s without attacking, after a finisher, after a heavy; dash attacks don't touch it). `ChargeButton` resolves a tap on release; holding past 0.35 s charges the heavy, which fires on release or at 1.5 s. Presses up to 0.15 s before the current move ends are buffered.
- A press during a dash or within 0.12 s after it is a dash attack (immediate, no tap wait).
- While attacking or charging the creature steers at 40% speed (developer decision: slowed, free steering, no lunges on normal moves). Dash is locked during wind-up and strike; a dash in recovery cancels the move without resetting the combo.
- Soft-lock (developer decision): at attack start the move aims at the best enemy (distance × angle score) within 6 m and a 90° cone around the facing, or a 30° cone around active aim (mouse, or right stick past the deadzone). The creature snaps to that direction and keeps it for the move.
- Each target is hit once per move. Hits go through the Combat pipeline with `Team.Creature`, the creature's `Health` as attacker, and `Finisher`, `Heavy` or `DashAttack` flags.
- `WeaponTentacleView` draws a placeholder tentacle per weapon side following `MoveShapes.Tip` until the spring-chain tentacle task replaces it. Enemies flash white for 0.08 s when hit (`TelegraphGlow.Flash`).

## Testing in play

Script timed button presses from `UnityEditor.EditorApplication.update` with `TestGamepad.Set` / `Release` (see the root CLAUDE.md); log `WeaponController.AttackStarted` and `CombatEvents.HitLanded`. Freeze enemies with `EnemyManager.Instance.Paused = true` and spawn the dummy in front of the creature: soft-lock only looks within 45° of its facing.
