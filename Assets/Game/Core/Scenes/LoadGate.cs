using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Scenes
{
    public static class LoadGate
    {
        private static readonly HashSet<object> Holders = new();

        public static bool IsOpen => Holders.Count == 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Holders.Clear();

        public static void Hold(object holder) => Holders.Add(holder);

        public static void Release(object holder) => Holders.Remove(holder);
    }
}
