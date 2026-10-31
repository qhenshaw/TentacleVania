using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class MapProgress
    {
        public static event Action<string> Explored;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Explored = null;

        public static bool IsExplored(string room) => SaveSystem.Data.World.Has(WorldFlag.ExploredRoom, room);

        public static bool Explore(string room)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.ExploredRoom, room))
                return false;
            Explored?.Invoke(room);
            return true;
        }

        public static void Forget(string room)
        {
            if (SaveSystem.Data.World.Clear(WorldFlag.ExploredRoom, room))
                Explored?.Invoke(room);
        }
    }
}
