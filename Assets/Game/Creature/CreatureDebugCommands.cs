using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
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

        private static CreatureMotor Motor => Spawned != null ? Spawned.GetComponent<CreatureMotor>() : null;
    }
}
