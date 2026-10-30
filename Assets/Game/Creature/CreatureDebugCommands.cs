using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Debugging;
using TentacleVania.Creature.Movement;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Creature
{
    public static class CreatureDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            const string tab = DebugMenu.Creature;
            DebugMenu.Value(tab, "Health", () =>
            {
                var health = Health;
                return health != null ? $"{health.Current:F0} / {health.Max:F0}" : "no creature";
            });
            DebugMenu.Value(tab, "Position", () => Spawned != null ? Spawned.transform.position.ToString("F1") : "-");
            DebugMenu.Value(tab, "Speed", () =>
            {
                var motor = Motor;
                return motor != null ? $"{motor.Velocity.magnitude:F1} m/s{(motor.IsDashing ? " (dashing)" : "")}" : "-";
            });
            DebugMenu.Value(tab, "Dash chain", () =>
            {
                var motor = Motor;
                return motor != null ? motor.DashChainCount.ToString() : "-";
            });
            DebugMenu.Value(tab, "Legs stepping", () =>
                Spawned != null && Spawned.TryGetComponent<CreatureLegs>(out var legs) ? $"{legs.SteppingCount} / {legs.LegCount}" : "-");
            DebugMenu.Value(tab, "Charge", () =>
            {
                var charge = Charge;
                return charge != null ? $"{charge.Current:F0} / {charge.Max:F0}{(charge.IsFull ? " FULL" : "")}" : "-";
            });
            DebugMenu.Value(tab, "Biomass carried", () =>
                Spawned != null && Spawned.TryGetComponent<CreatureConsume>(out var consume) ? consume.Biomass.ToString() : "-");
            DebugMenu.Value(tab, "Consume", () =>
            {
                if (Spawned == null || !Spawned.TryGetComponent<CreatureConsume>(out var consume))
                    return "-";
                return consume.IsRooted ? "gulping" : consume.IsHealing ? "healing (armored)" : "ready";
            });
            DebugMenu.Value(tab, "Stun", () =>
            {
                var vitals = Vitals;
                if (vitals == null)
                    return "-";
                return vitals.IsStunned ? "stunned" : vitals.StunGraceRemaining > 0f ? $"grace {vitals.StunGraceRemaining:F1} s" : "stunnable";
            });
            DebugMenu.Button(tab, "Stun 1 s", () =>
            {
                var vitals = Vitals;
                if (vitals != null)
                    vitals.Stun(1f);
            });
            DebugMenu.Toggle(tab, "Organic gait", () => CreatureLegs.Organic, v => CreatureLegs.Organic = v);
            DebugMenu.Toggle(tab, "Free tentacles", () => CreatureTentacles.Visible, v => CreatureTentacles.Visible = v);
            DebugMenu.Toggle(tab, "Placeholder tentacle lines", () => Chains.ShowPlaceholderLines, v => Chains.ShowPlaceholderLines = v);
            DebugMenu.Value(tab, "Chain texture", () =>
                Spawned != null && Spawned.TryGetComponent<ChainTexture>(out var chains) && chains.Texture != null
                    ? $"{chains.Texture.width} x {chains.Texture.height} ({chains.Count} chains)"
                    : "-");
            DebugMenu.Button(tab, "Fill charge", () =>
            {
                var charge = Charge;
                if (charge != null)
                    charge.Set(charge.Max);
            });
            DebugMenu.Button(tab, "Empty charge", () =>
            {
                var charge = Charge;
                if (charge != null)
                    charge.Set(0f);
            });
            DebugMenu.Toggle(tab, "God mode", () => CreatureVitals.GodMode, v => CreatureVitals.GodMode = v);
            DebugMenu.Button(tab, "Heal to full", () =>
            {
                var health = Health;
                if (health != null)
                    health.Heal(float.MaxValue);
            });
            DebugMenu.Button(tab, "Take 25 damage", () =>
            {
                var health = Health;
                if (health != null)
                    health.TakeDamage(DamageInfo.Create(25f, Team.Environment));
            });
            DebugMenu.Button(tab, "Kill", () =>
            {
                var health = Health;
                if (health != null)
                    health.Kill();
            });
            DebugMenu.Button(tab, "Teleport to spawn", () =>
            {
                var spawner = Object.FindAnyObjectByType<CreatureSpawner>();
                if (Spawned == null || spawner == null)
                    return;
                var controller = Spawned.GetComponent<CharacterController>();
                controller.enabled = false;
                Spawned.transform.position = spawner.transform.position;
                controller.enabled = true;
            });
        }

        private static GameObject Spawned => CreatureSpawner.Spawned;

        private static Health Health => Spawned != null ? Spawned.GetComponent<Health>() : null;

        private static CreatureCharge Charge => Spawned != null ? Spawned.GetComponent<CreatureCharge>() : null;

        private static CreatureVitals Vitals => Spawned != null ? Spawned.GetComponent<CreatureVitals>() : null;

        private static CreatureMotor Motor => Spawned != null ? Spawned.GetComponent<CreatureMotor>() : null;
    }
}
