# Creature

The player creature (`TentacleVania.Creature`). The prefab `Prefabs/Creature.prefab` is spawned at runtime by a `CreatureSpawner` in the scene (`CombatArena` has one at the origin); `CreatureSpawner.Spawned` returns the instance for evals.

## Movement (`Movement/`)

- `CreatureMotor` drives a `CharacterController` from `GameplayInput`; all tuning lives in a `CreatureMotorSettings` asset (`Data/HatchlingMotor.asset`, Balance Sheet baselines: 7 m/s, 4 m dash, 0.3 s cooldown chained twice then 0.8 s lockout, 0.2 s i-frames).
- Snappy acceleration (120 m/s²). Movement is camera-relative. The maw faces the aim (mouse pointer on the creature's plane, or the right stick past the deadzone, by `GameplayInput.LastDevice`), otherwise the move direction.
- A dash covers exactly `DashDistance` over `DashDuration` (0.15 s, not in the design), in the move direction or the facing when there is no move input, then drops straight to walking speed. Presses up to 0.1 s early are buffered. The cooldown counts from the end of the dash; `DashTimer` holds the chain and lockout rules and is unit tested.
- Hooks for later systems: `TryDash(direction)` for attack dash-cancels, `IsInvulnerable` for the damage pipeline, `InputEnabled` to lock control (consume, stuns), and the `Dashed` event.
- The creature is a placeholder knot (dark sphere plus a red maw) until the procedural rig replaces the visuals.

## Testing in play

`unity-play-capture.sh` with `TestGamepad.Set(move, aim, buttons...)` drives it; read `CreatureSpawner.Spawned` and its `CreatureMotor` (`Velocity`, `IsDashing`, `DashChainCount`) from `--at` files. Keep values across `--at` files with `UnityEditor.SessionState`. Virtual gamepad input only survives while Player Settings' Run In Background is on.
