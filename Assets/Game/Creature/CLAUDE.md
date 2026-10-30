# Creature

The player creature (`TentacleVania.Creature`). The prefab `Prefabs/Creature.prefab` is spawned at runtime by a `CreatureSpawner` in the scene (`CombatArena` has one at the origin); `CreatureSpawner.Spawned` returns the instance for evals.

## Movement (`Movement/`)

- `CreatureMotor` drives a `CharacterController` from `GameplayInput`; all tuning lives in a `CreatureMotorSettings` asset (`Data/HatchlingMotor.asset`, Balance Sheet baselines: 7 m/s, 4 m dash, 0.3 s cooldown chained twice then 0.8 s lockout, 0.2 s i-frames).
- Snappy acceleration (120 m/s²). Movement is camera-relative. The maw faces the aim (mouse pointer on the creature's plane, or the right stick past the deadzone, by `GameplayInput.LastDevice`), otherwise the move direction.
- A dash covers exactly `DashDistance` over `DashDuration` (0.15 s, not in the design), in the move direction or the facing when there is no move input, then drops straight to walking speed. Presses up to 0.1 s early are buffered. The cooldown counts from the end of the dash; `DashTimer` holds the chain and lockout rules and is unit tested.
- Hooks for later systems: `TryDash(direction)` for attack dash-cancels, `IsInvulnerable` for the damage pipeline, `InputEnabled` to lock control (consume, stuns), and the `Dashed` event.
- The body is a placeholder knot (dark sphere plus a red maw marking the front).

## Procedural legs (`Rig/`)

- `CreatureLegs` (runs in `LateUpdate` after the motor) builds one chain per leg from a `CreatureRigSettings` asset per size stage (`Data/HatchlingRig.asset`: 6 legs of 5 bones, 0.9 m long, feet resting 0.62 m out, about 1.2 m across). `Build(settings)` rebuilds it for another stage; stages must stay under `CreatureRigSettings.MaxChains` (20) counting free and weapon tentacles.
- Each frame a chain is seeded as an arch (`ChainSolver.SeedArch`, knee raised above the root) and solved with FABRIK (`ChainSolver.Solve`), so legs bow up and out instead of flipping. Feet are ground-probed with `RaycastNonAlloc`, skipping the creature's own collider.
- Gait (`LegGait.SelectSteps`, unit tested): a leg steps when its foot drifts past `StepThreshold` from its rest point, largest drift first, never while a neighbor is stepping, and with at most half the legs lifted. A foot past `EmergencyDistance` steps regardless. Steps land ahead by `PredictTime` × velocity, clamped to leg reach, so feet keep up at 7 m/s; during a dash the legs trail and catch up.
- Legs render as tapered `LineRenderer`s as a placeholder until the VFX Graph strips and tube shader replace them. The gameplay hurtbox stays the `CharacterController`, never the legs.
- The solver is plain managed code over preallocated arrays (no per-frame allocations). Move it to a Burst job if chain counts at Apex make it show up in the profiler.

## Control locks and stun

`CreatureMotor.InputEnabled` is true while no system holds a lock: call `LockControl()` / `UnlockControl()` in pairs (death, the consume root and stuns each hold one), never set it directly. A hit whose `DamageInfo.StunDuration` > 0 stuns the creature through `CreatureVitals.Stun` (control locked, electric-blue status pulse, the charge glow restored afterwards); repeated stuns extend it.

## Grab & consume

`CreatureConsume` (developer decisions): Grab (B / E) takes the nearest enemy with a `Combat.Consumable` under 25% HP within 2.2 m, preferring ones in front; with nothing grabbable the press does nothing. The creature roots for 0.4 s while two placeholder tentacles drag the victim into the maw (it shrinks, then `FinishConsume` kills it), then is free again while the heal flows: `Consumable.HealFraction` of the creature's max HP over 1.5 s, with a ×0.5 damage-taken modifier for the whole 1.5 s. Each consume adds `Consumable.Biomass` to `Biomass` and +20 charge. The creature publishes its threshold as `Consumable.GrabThreshold` (read by `Consumable.IsGrabbable` for the HUD and enemy glow). Grabbed enemies enter `EnemyState.Consumed` (no AI, colliders and agent off) so nothing else can hit them. Heal and biomass per enemy live in `EnemyDefinition` (Balance Sheet values).

## Charge meter

`CreatureCharge` (on the prefab) wraps a `Combat.ChargeMeter`: max 100, +3 for every hit the creature lands (`CombatEvents.HitLanded` with the creature as attacker, hazards excluded), +20 from `AddConsume()` (for grab & consume). Abilities call `TrySpend(cost)` (35–60). `ChargeMeter.GainScale` is the hook for Cunning's +2% charge gain. When full, the creature pulses teal through `CharacterMaterials.SetStatus` (Art Direction's charge-full bioluminescence), body and tentacles alike.

## Testing in play

`unity-play-capture.sh` with `TestGamepad.Set(move, aim, buttons...)` drives it; read `CreatureSpawner.Spawned` and its `CreatureMotor` (`Velocity`, `IsDashing`, `DashChainCount`) from `--at` files. Keep values across `--at` files with `UnityEditor.SessionState`. Virtual gamepad input only survives while Player Settings' Run In Background is on.
