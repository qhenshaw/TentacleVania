using TentacleVania.Combat;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.World.Lore;
using UnityEngine;

namespace TentacleVania.World.Pickups
{
    public static class PickupDebugCommands
    {
        private const string Tab = WorldDebugCommands.Tab;
        private static readonly string[] AllTrinkets =
        {
            "PodReleaseKey", "EvacuationFloorPlan", "DeconOverrideCard", "NavigationShard", "TaggedLockerKey", "EmergencyThermalBlanket",
            "UnsentLetter", "FlarePistol", "SurfaceLiftCode", "DistressBeacon", "AlphaSurvivorsBadge", "StarChartPlate",
        };

        private static int _spawned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Lore logs", () =>
            {
                var library = LoreLibrary.Instance;
                return $"{LoreLogs.All.Count}/{(library != null ? library.Logs.Length : 0)}";
            });
            DebugMenu.Value(Tab, "Weapons / trinkets", () => $"{string.Join(", ", Collectibles.OwnedWeapons)} / {string.Join(", ", Collectibles.Trinkets)}");
            DebugMenu.Button(Tab, "Spawn pickup row", SpawnRow);
            var library = LoreLibrary.Instance;
            var ids = new string[library != null ? library.Logs.Length : 0];
            for (var i = 0; i < ids.Length; i++)
                ids[i] = library.Logs[i] != null ? library.Logs[i].Id : "-";
            DebugMenu.Choice(Tab, "Spawn lore terminal", ids, i => SpawnTerminal(library.Logs[i]));
            DebugMenu.Button(Tab, "Spawn Hoarder", () =>
            {
                var set = Set;
                var creature = CombatTargets.Creature;
                if (set == null || set.Hoarder == null || creature == null)
                    return;
                var forward = creature.transform.forward;
                Object.Instantiate(set.Hoarder, creature.transform.position + forward * 2.5f, Quaternion.LookRotation(forward));
            });
            DebugMenu.Button(Tab, "Give all trinkets", () =>
            {
                foreach (var trinket in AllTrinkets)
                    Collectibles.Grant(PickupKind.Trinket, trinket, 1);
            });
            DebugMenu.Button(Tab, "Collect all lore logs", () =>
            {
                foreach (var id in ids)
                    LoreLogs.Collect(id);
            });
        }

        private static PickupDebugSet Set => Resources.Load<PickupDebugSet>("DebugPickupSet");

        private static void SpawnRow()
        {
            var set = Set;
            var creature = CombatTargets.Creature;
            if (set == null || creature == null)
                return;
            var origin = creature.transform.position + creature.transform.forward * 3f;
            var right = creature.transform.right;
            for (var i = 0; i < set.Pickups.Length; i++)
            {
                var offset = (i - (set.Pickups.Length - 1) * 0.5f) * 1.8f;
                var instance = Object.Instantiate(set.Pickups[i], origin + right * offset, Quaternion.identity);
                instance.name = set.Pickups[i].name + "_Debug" + _spawned++;
            }
        }

        private static void SpawnTerminal(LoreLog log)
        {
            var set = Set;
            var creature = CombatTargets.Creature;
            if (set == null || set.LoreTerminal == null || creature == null)
                return;
            var position = creature.transform.position + creature.transform.forward * 2f;
            var instance = Object.Instantiate(set.LoreTerminal, position, Quaternion.LookRotation(creature.transform.forward));
            instance.GetComponent<LoreTerminal>().SetLog(log);
        }
    }
}
