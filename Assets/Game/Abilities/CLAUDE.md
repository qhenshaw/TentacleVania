# Abilities

Active abilities (`TentacleVania.Abilities`), on the ability input (RT / Q), paid from the charge meter (`Creature/CreatureCharge`).

## Structure

- `AbilityDefinition` (ScriptableObject): display name, charge `Cost`, and `CreateRuntime(AbilityContext)`, which builds the ability's plain C# `AbilityRuntime` (`Activate`, `Tick`, `Cancel`, `Dispose`) once at equip, so activations allocate nothing.
- `AbilityController` (on the creature prefab) holds the one equipped ability (`Equip(definition)` swaps it), ticks the runtime, and on `Ability.Pressed` spends the cost with `CreatureCharge.TrySpend` and activates. Nothing fires while control is locked (stun, consume) or the creature is dead; death or disabling cancels without effects. `AbilityContext.RefreshStatus` hands the status glow back to the charge meter (`CreatureCharge.RefreshGlow`) when an ability ends.
- Debug: Abilities tab (equipped, state, "Free abilities (no charge)", "Activate").

## Bio-Shield (`BioShield/`, `BioShield.asset`)

- Task TV-11 baseline (Balance Sheet charge-meter section): 50 charge, absorbs up to 40 damage for 3 s, then bursts for 25 damage in 4 m, early when the absorb runs out. "Heavy stagger" has no number in the design: `_burstStagger` is 45 (breaks every prototype enemy bar but the TestBrute's). The Balance Sheet's prototype table lists 40 charge and 2 s instead; the asset follows the task until the developer settles it.
- `ShieldAbsorb` (unit tested) is an `IDamageModifier` at order 900 in `Health.Incoming`: it eats the hit after earlier scaling and passes the rest through.
- The burst is a `HitFlags.Heavy` overlap on `_hitLayers` (Enemy) from the creature, so hit feedback and knockback treat it as a heavy hit.
- Placeholder presentation: an amber transparent sphere (`BioShieldShell.prefab`, URP Unlit) that swells as hits land and expands to the burst radius, the amber status glow on the creature, and FMOD placeholder events `event:/Abilities/BioShield/Activate`, `Absorb` and `Burst`.
- Look FMOD events up in editor evals with `FMODUnity.EventManager.EventFromPath(path)` (then `new EventReference { Guid, Path }`); `RuntimeManager.PathToEventReference` only works in play mode and logs an error otherwise.
