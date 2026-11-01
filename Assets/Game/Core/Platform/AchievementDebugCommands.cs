using System.Text;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Platform
{
    public static class AchievementDebugCommands
    {
        private const string Tab = "Platform";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Achievements", Describe);
            DebugMenu.Button(Tab, "Check achievements now", () =>
                Achievements.Check(Resources.Load<AchievementList>(AchievementList.ResourcePath), SaveSystem.Data, SaveSystem.LivePlaytime));
            DebugMenu.Button(Tab, "Reset local achievements", () => Achievements.Backend.ResetAll());
            DebugMenu.Toggle(Tab, "Speedrun timer", () => GameSettings.SpeedrunTimer, v => GameSettings.SpeedrunTimer = v);
        }

        private static string Describe()
        {
            var list = Resources.Load<AchievementList>(AchievementList.ResourcePath);
            if (list == null)
                return "no list";
            var unlocked = new StringBuilder();
            var count = 0;
            foreach (var entry in list.Entries)
            {
                if (!Achievements.IsUnlocked(entry.Id))
                    continue;
                count++;
                unlocked.Append(entry.Id).Append(' ');
            }
            return $"{count}/{list.Entries.Count} unlocked: {unlocked}";
        }
    }
}
