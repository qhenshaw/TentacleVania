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
            DebugMenu.Value(tab, "Feats", () =>
            {
                var defs = Feats.FeatTracker.Definitions;
                var text = new System.Text.StringBuilder();
                for (var i = 0; i < defs.Count; i++)
                    text.Append(defs[i].Id).Append(' ').Append(Feats.FeatTracker.Progress(i)).Append('/').Append(defs[i].Count).Append(i < defs.Count - 1 ? ", " : "");
                return defs.Count == 0 ? "-" : text.ToString();
            });
            DebugMenu.Toggle(tab, "Feat tracking", () => Feats.FeatTracker.Enabled, v => Feats.FeatTracker.Enabled = v);
            DebugMenu.Button(tab, "Reset feat progress", Feats.FeatTracker.ResetProgress);
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
            DebugMenu.Value(tab, "Breach", () =>
            {
                var breach = CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<Breach.CreatureBreach>() : null;
                if (breach == null)
                    return "-";
                var wall = breach.Definition != null ? breach.FindTarget() : null;
                return !breach.IsUnlocked ? "locked" : breach.IsBusy ? "smashing" : wall != null ? $"ready: {wall.Id}" : $"no weak wall in reach ({Breach.CreatureBreach.Smashed} smashed)";
            });
            DebugMenu.Toggle(tab, "Breach unlocked (debug)", () => Breach.CreatureBreach.ForceUnlocked, v => Breach.CreatureBreach.ForceUnlocked = v);
            DebugMenu.Button(tab, "Spawn weak wall ahead", () =>
            {
                var prefab = Resources.Load<GameObject>("DebugWeakWall");
                var creature = CreatureSpawner.Spawned;
                if (prefab == null || creature == null)
                    return;
                var forward = creature.transform.forward;
                forward.y = 0f;
                var rotation = Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward);
                Object.Instantiate(prefab, creature.transform.position + rotation * new Vector3(0f, 0f, 2.5f), rotation);
            });
            DebugMenu.Button(tab, "Restore breached walls", () =>
            {
                Core.Progression.BreachedWalls.RestoreAll();
                foreach (var wall in Breach.WeakWall.All)
                    wall.Restore();
            });
            DebugMenu.Value(tab, "LT traversal", () =>
            {
                var traversal = CreatureSpawner.Spawned != null ? CreatureSpawner.Spawned.GetComponent<CreatureTraversal>() : null;
                var spot = traversal != null ? traversal.Available() : null;
                return spot != null ? $"{spot.AbilityId} ready" : $"none in reach ({CreatureTraversal.Used} used)";
            });
            DebugMenu.Toggle(tab, "Burrow unlocked (debug)", () => CreatureTraversal.IsForced("Burrow"), v => CreatureTraversal.SetForced("Burrow", v));
            DebugMenu.Toggle(tab, "Vent Squeeze unlocked (debug)", () => CreatureTraversal.IsForced("VentSqueeze"), v => CreatureTraversal.SetForced("VentSqueeze", v));
            DebugMenu.Toggle(tab, "Overload unlocked (debug)", () => CreatureTraversal.IsForced("Overload"), v => CreatureTraversal.SetForced("Overload", v));
            DebugMenu.Button(tab, "Reset overloaded doors (save)", Core.Progression.OverloadedNodes.ResetAll);
            var gates = new[] { "DebugBurrowGate", "DebugVentGate", "DebugOverloadDoor", "DebugOverloadJunction" };
            DebugMenu.Choice(tab, "Spawn LT gate ahead", new[] { "Burrow gate", "Vent gate", "Overload door", "Overload junction" }, i =>
            {
                var prefab = Resources.Load<GameObject>(gates[i]);
                var creature = CreatureSpawner.Spawned;
                if (prefab == null || creature == null)
                    return;
                var forward = creature.transform.forward;
                forward.y = 0f;
                var rotation = Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward);
                Object.Instantiate(prefab, creature.transform.position + rotation * new Vector3(0f, 0f, 2.2f), rotation);
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
