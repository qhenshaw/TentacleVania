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
            DebugMenu.Button(tab, "Equip Bio-Shield", () =>
            {
                var controller = Controller;
                if (controller != null)
                    controller.EquipById("BioShield");
            });
            DebugMenu.Button(tab, "Unequip", () =>
            {
                var controller = Controller;
                if (controller != null)
                    controller.Equip(null);
            });
            DebugMenu.Value(tab, "Bio-Shield tier", () => Core.Progression.Upgrades.AbilityTier("BioShield").ToString());
            DebugMenu.Button(tab, "Toggle Bio-Shield tier 1/2", () =>
                Core.Progression.Upgrades.SetAbilityTier("BioShield", Core.Progression.Upgrades.AbilityTier("BioShield") >= 2 ? 1 : 2));
            DebugMenu.Value(tab, "Grapple", () =>
            {
                var grapple = CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<Grapple.TentacleGrapple>() : null;
                if (grapple == null)
                    return "-";
                return !grapple.IsUnlocked ? "locked" : grapple.IsBusy ? "grappling" : grapple.CooldownRemaining > 0f ? $"cooldown {grapple.CooldownRemaining:F1} s" : "ready (LT / F)";
            });
            DebugMenu.Toggle(tab, "Grapple unlocked (debug)", () => Grapple.TentacleGrapple.ForceUnlocked, v => Grapple.TentacleGrapple.ForceUnlocked = v);
            DebugMenu.Button(tab, "Spawn grapple rig", () =>
            {
                var prefab = Resources.Load<GameObject>("DebugGrappleRig");
                var creature = CreatureSpawner.Spawned;
                if (prefab == null || creature == null)
                    return;
                var forward = creature.transform.forward;
                forward.y = 0f;
                var rotation = Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward);
                Object.Instantiate(prefab, creature.transform.position + rotation * new Vector3(0f, 0.01f, -2f), rotation);
            });
            var mutationIds = new[] { "WideDodge", "Gluttony", "RapidDigestion", "LungingMaw" };
            DebugMenu.Value(tab, "Mutations", () =>
            {
                var controller = CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<Mutations.MutationController>() : null;
                var used = controller != null ? controller.UsedNotches() : 0;
                return $"notches {used}/{Core.Progression.MutationLoadout.NotchPool}, owned {string.Join(", ", Core.Progression.MutationLoadout.Owned)}, equipped {string.Join(", ", Core.Progression.MutationLoadout.Equipped)}";
            });
            DebugMenu.Choice(tab, "Grant mutation", mutationIds, i => Core.Progression.MutationLoadout.Grant(mutationIds[i]));
            DebugMenu.Choice(tab, "Equip mutation (ignores nests)", mutationIds, i =>
            {
                var controller = CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<Mutations.MutationController>() : null;
                if (controller != null)
                    controller.Equip(mutationIds[i]);
            });
            DebugMenu.Button(tab, "Clear mutations", Core.Progression.MutationLoadout.Clear);
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
