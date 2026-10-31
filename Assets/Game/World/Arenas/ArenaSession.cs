using System.Collections.Generic;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Scenes;
using UnityEngine;

namespace TentacleVania.World.Arenas
{
    public static class ArenaSession
    {
        private static readonly HashSet<string> Cleared = new();

        public static int Count => Cleared.Count;

        public static bool IsCleared(string id) => !string.IsNullOrEmpty(id) && Cleared.Contains(id);

        public static void MarkCleared(string id)
        {
            if (!string.IsNullOrEmpty(id))
                Cleared.Add(id);
        }

        public static void Forget() => Cleared.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            Cleared.Clear();
            SceneFlow.Loading += _ => Forget();
            Nests.Rested += _ => Forget();
        }
    }
}
