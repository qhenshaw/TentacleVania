using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core
{
    public static class Cutscene
    {
        private static readonly List<object> Owners = new();

        public static bool Active => Owners.Count > 0;
        public static event Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Owners.Clear();
            Changed = null;
        }

        public static void Begin(object owner)
        {
            if (owner == null || Owners.Contains(owner))
                return;
            Owners.Add(owner);
            if (Owners.Count == 1)
                Changed?.Invoke(true);
        }

        public static void End(object owner)
        {
            if (!Owners.Remove(owner))
                return;
            if (Owners.Count == 0)
                Changed?.Invoke(false);
        }
    }
}
