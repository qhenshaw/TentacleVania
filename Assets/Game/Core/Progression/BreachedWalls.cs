using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class BreachedWalls
    {
        public static event Action<string> Breached;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Breached = null;

        public static bool IsBreached(string id) => SaveSystem.Data.World.Has(WorldFlag.BreachedWall, id);

        public static bool Breach(string id)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.BreachedWall, id))
                return false;
            Breached?.Invoke(id);
            Autosave.Request(AutosaveReason.WallBreached);
            return true;
        }

        public static void RestoreAll() => SaveSystem.Data.World.BreachedWalls.Clear();
    }
}
