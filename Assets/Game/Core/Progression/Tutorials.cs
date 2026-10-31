using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class Tutorials
    {
        public const string Attack = "attack";
        public const string Dash = "dash";
        public const string Grab = "grab";
        public const string Consume = "consume";
        public const string Nest = "nest";
        public const string Weapon2 = "weapon2";
        public const string Ability = "ability";

        public static event Action<string> Completed;

        public static bool Enabled { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Completed = null;
            Enabled = true;
        }

        public static bool IsDone(string id) => SaveSystem.Data.World.Has(WorldFlag.SeenTutorial, id);

        public static bool Complete(string id)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.SeenTutorial, id))
                return false;
            Completed?.Invoke(id);
            return true;
        }

        public static void ResetAll()
        {
            SaveSystem.Data.World.SeenTutorials.Clear();
            Completed?.Invoke(null);
        }
    }
}
