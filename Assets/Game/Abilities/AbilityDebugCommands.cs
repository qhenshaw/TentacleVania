using TentacleVania.Abilities.BioShield;
using TentacleVania.Core.Debugging;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities
{
    public static class AbilityDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            const string tab = DebugMenu.Abilities;
            DebugMenu.Value(tab, "Equipped", () =>
            {
                var controller = Controller;
                return controller != null && controller.Equipped != null ? $"{controller.Equipped.DisplayName} ({controller.Equipped.Cost:F0} charge)" : "-";
            });
            DebugMenu.Value(tab, "State", () =>
            {
                var controller = Controller;
                if (controller == null || controller.Runtime == null)
                    return "-";
                if (controller.Runtime is BioShieldRuntime shield)
                    return shield.IsActive ? $"shield {shield.Remaining:F0} left, {shield.TimeLeft:F1} s" : $"ready (last burst hit {shield.LastBurstHits})";
                return controller.IsActive ? "active" : "ready";
            });
            DebugMenu.Toggle(tab, "Free abilities (no charge)", () => AbilityController.FreeAbilities, v => AbilityController.FreeAbilities = v);
            DebugMenu.Button(tab, "Activate", () =>
            {
                var controller = Controller;
                if (controller != null)
                    controller.TryActivate();
            });
        }

        private static AbilityController Controller =>
            CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<AbilityController>() : null;
    }
}
