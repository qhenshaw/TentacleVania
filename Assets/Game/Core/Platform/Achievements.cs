using System;
using System.IO;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Platform
{
    public static class Achievements
    {
        private static IAchievementBackend _backend;

        public static event Action<string> Unlocked;

        public static IAchievementBackend Backend
        {
            get => _backend ??= new LocalAchievementBackend(Path.Combine(Application.persistentDataPath, "achievements.json"));
            set => _backend = value;
        }

        public static bool Enabled { get; set; } = true;

        public static bool IsUnlocked(string id) => Backend.IsUnlocked(id);

        public static void Unlock(string id)
        {
            if (!Enabled || string.IsNullOrEmpty(id) || Backend.IsUnlocked(id))
                return;
            Backend.Unlock(id);
            Unlocked?.Invoke(id);
        }

        public static int Check(AchievementList list, SaveData data, double playtime)
        {
            if (list == null || data == null)
                return 0;
            var unlocked = 0;
            foreach (var entry in list.Entries)
            {
                if (Backend.IsUnlocked(entry.Id) || !AchievementRules.Met(entry, data, playtime))
                    continue;
                Unlock(entry.Id);
                unlocked++;
            }
            return unlocked;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Unlocked = null;
            Enabled = true;
        }
    }
}
