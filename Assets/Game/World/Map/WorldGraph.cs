using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.World.Map
{
    public enum ConnectionRoute
    {
        Corridor,
        Elevator,
        Lift,
        Shaft,
        FreightCrate,
        Vent,
        Stairwell,
        FreightElevator,
    }

    [Serializable]
    public sealed class WorldSector
    {
        [Tooltip("Sector id (SH, BL...): matches the room registry and the Dialogue table's sector.<id>.")]
        public string Id;
        [Tooltip("Floor 0 (L0, top) to 3 (L3, deepest).")]
        [Range(0, 3)] public int Floor;
        [Tooltip("Column on its floor (0-2), for the Site map.")]
        [Range(0, 2)] public int Column;
    }

    [Serializable]
    public sealed class WorldConnection
    {
        public string From;
        public string To;
        public ConnectionRoute Route;
        [Tooltip("All must be met: an ability id (TentacleGrapple, Breach...) or boss:<boss id>. Empty means always open.")]
        public string[] Requires = Array.Empty<string>();
        [Tooltip("UI table key describing the gate on the Site map.")]
        public string GateKey;
    }

    [CreateAssetMenu(menuName = "TentacleVania/World/World Graph")]
    public sealed class WorldGraph : ScriptableObject
    {
        public const string ResourcePath = "WorldGraph";
        public const string BossPrefix = "boss:";

        [SerializeField] private List<WorldSector> _sectors = new();
        [SerializeField] private List<WorldConnection> _connections = new();

        private static WorldGraph _instance;

        public static WorldGraph Instance => _instance != null ? _instance : _instance = Resources.Load<WorldGraph>(ResourcePath);

        public IReadOnlyList<WorldSector> Sectors => _sectors;
        public IReadOnlyList<WorldConnection> Connections => _connections;

        public WorldSector Find(string id)
        {
            foreach (var sector in _sectors)
                if (sector.Id == id)
                    return sector;
            return null;
        }

        public static bool IsOpen(WorldConnection connection, Func<string, bool> abilityUnlocked, Func<string, bool> bossDefeated)
        {
            if (connection.Requires == null)
                return true;
            foreach (var requirement in connection.Requires)
            {
                if (string.IsNullOrEmpty(requirement))
                    continue;
                var met = requirement.StartsWith(BossPrefix, StringComparison.Ordinal)
                    ? bossDefeated(requirement.Substring(BossPrefix.Length))
                    : abilityUnlocked(requirement);
                if (!met)
                    return false;
            }
            return true;
        }
    }
}
