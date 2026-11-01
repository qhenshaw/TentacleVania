using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class OverloadedNodes
    {
        public static event Action<string> Overloaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Overloaded = null;

        public static bool IsOverloaded(string id) => SaveSystem.Data.World.Has(WorldFlag.OverloadedNode, id);

        public static bool Overload(string id)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.OverloadedNode, id))
                return false;
            Overloaded?.Invoke(id);
            Autosave.Request(AutosaveReason.NodeOverloaded);
            return true;
        }

        public static void ResetAll() => SaveSystem.Data.World.OverloadedNodes.Clear();
    }
}
