using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class SecurityRelays
    {
        public const int PerSystem = 3;

        public static event Action<string, int> RelayDestroyed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => RelayDestroyed = null;

        public static string Key(string system, string relay) => system + "/" + relay;

        public static bool IsDestroyed(string system, string relay) => SaveSystem.Data.World.Has(WorldFlag.DestroyedRelay, Key(system, relay));

        public static int Count(string system)
        {
            var list = SaveSystem.Data.World.DestroyedRelays;
            var count = 0;
            for (var i = 0; i < list.Count; i++)
                if (list[i].Length > system.Length && list[i][system.Length] == '/' && list[i].StartsWith(system, StringComparison.Ordinal))
                    count++;
            return count;
        }

        public static bool Destroy(string system, string relay)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.DestroyedRelay, Key(system, relay)))
                return false;
            RelayDestroyed?.Invoke(system, Count(system));
            Autosave.Request(AutosaveReason.RelayDestroyed);
            return true;
        }

        public static void Reset(string system)
        {
            var list = SaveSystem.Data.World.DestroyedRelays;
            for (var i = list.Count - 1; i >= 0; i--)
                if (list[i].Length > system.Length && list[i][system.Length] == '/' && list[i].StartsWith(system, StringComparison.Ordinal))
                    list.RemoveAt(i);
        }

        public static DoorPower Power(int destroyed, int required) =>
            destroyed >= required ? DoorPower.Open : destroyed == required - 1 ? DoorPower.PoweringDown : DoorPower.Armed;
    }

    public enum DoorPower
    {
        Armed,
        PoweringDown,
        Open,
    }
}
