# Combat

Shared damage model (`TentacleVania.Combat`) used by the creature, enemies, hazards and abilities.

## Damage pipeline

- A hit is a `DamageInfo`: amount, stagger, `ArmorDamageScale` (Corrode, Crusher Mass), `DamageType` (Physical, Electric, Fire, Acid, Radiation), the source `Team`, `HitFlags` (Heavy, Finisher, DashAttack, Hazard), the attacker's `Health` if any, point and direction. Build it with `DamageInfo.Create` and deliver it with `Health.TakeDamage`.
- `DamagePipeline.Resolve` (pure, unit tested) runs: team check, the attacker's `Outgoing` modifiers, the target's `Incoming` modifiers, the assist sliders (`GameSettings.DamageDealtScale` for creature hits, `DamageTakenScale` for hits on the creature), then armor, then low-health protection (`Health.SetProtection(below, scale)`: a hit that would leave health under `below` × max is scaled by `scale`; enemies use it under the grab threshold). Every multiplier stacks multiplicatively, as the design's bonus stacking rule says.
- Teams: a team never damages itself, which covers both "the creature's own acid and fire never hurt it" and "no infighting". `Team.Environment` hurts everyone; tag level hazards with it.
- Armor (Balance Sheet): health damage is cut by up to 30% at full armor, scaling with the armor left; armor takes `Amount × ArmorDamageScale`.
- Modifiers implement `IDamageModifier` (`Order`, `Modify(ref DamageContext)`): scale `HealthScale`, `ArmorScale`, `StaggerScale`, or set `Cancelled` (i-frames). Add them to `Health.Incoming` or `Health.Outgoing` and remove them when the effect ends; `ScaleModifier` covers plain multipliers (consume armor −50%, stagger window +50%, typed resistances via `FilterByType`).
- `TakeDamage` returns a `DamageResult` (`Applied`, `Ignored` by team or death, `Blocked` by a modifier) with the health, armor and stagger dealt. Stagger isn't stored here: the stagger system reads `StaggerDamage` from the `Damaged` event.
- Events: `Health.Damaged`, `Healed`, `Died` per target, and the global `CombatEvents.HitLanded` for every applied hit (charge meter gain, hit feedback, lifesteal).

## Hit feedback

- `Feedback/HitFeedback` (on the creature prefab, settings `Data/HitFeedback.asset`) listens to `CombatEvents.HitLanded` and `CombatEvents.StaggerBroken` (raised by `Enemy` when a bar breaks) and, once per frame for the strongest event, requests hit-stop, screen shake and rumble from `Core.Feedback`. `HitFeedbackRules.Classify` (unit tested) maps a hit to Light, Heavy, Finisher, DashAttack (the player's hits, by flag), CreatureHurt (hits on the player) or None (hazards, enemy-on-enemy); StaggerBreak outranks everything.
- Each `HitFeedbackLevel` also carries the enemy reaction: `Knockback` metres (over `KnockbackDuration`) and `Flinch` degrees (over `FlinchDuration`), read by `Enemy` through `HitFeedback.Settings`. The defaults are first-pass feel values, not from the design: tune them in the asset. Debug: Combat tab "Last hit feedback", "Hit-stop" and "Rumble" toggles.

## Hurtboxes

`Hurtbox` maps a collider to its `Health`; weapon casts resolve hits with `Hurtbox.TryGet(collider, out hurtbox)` (a dictionary, no `GetComponent`). The creature's hurtbox is its `CharacterController`: one body hurtbox, never the tentacles.

## Creature

`Creature/CreatureVitals` adds the dash i-frames as a cancelling modifier and restarts the arena (reloads the creature's scene) 1.5 s after death, the developer's choice for the prototype until nests exist.
