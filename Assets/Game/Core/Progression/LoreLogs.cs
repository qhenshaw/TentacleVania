using System;
using System.Collections.Generic;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class LoreLogs
    {
        public static event Action<string> Collected;
        public static event Action<string> ReadRequested;

        public static List<string> All => SaveSystem.Data.World.CollectedLogs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Collected = null;
            ReadRequested = null;
        }

        public static bool Has(string id) => SaveSystem.Data.World.Has(WorldFlag.CollectedLog, id);

        public static bool Collect(string id)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.CollectedLog, id))
                return false;
            Collected?.Invoke(id);
            Autosave.Request(AutosaveReason.Pickup);
            return true;
        }

        public static void Read(string id)
        {
            if (!string.IsNullOrEmpty(id))
                ReadRequested?.Invoke(id);
        }
    }
}
