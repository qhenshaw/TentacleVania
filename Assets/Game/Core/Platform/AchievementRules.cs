using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Platform
{
    public static class AchievementRules
    {
        public static bool Met(AchievementList.Entry entry, SaveData data, double playtime)
        {
            switch (entry.Condition)
            {
                case AchievementCondition.BossDefeated:
                    return data.World.Has(WorldFlag.DefeatedBoss, entry.Key) || data.Meta.Bosses.Contains(entry.Key);
                case AchievementCondition.GameCompleted:
                    return data.Progress.Completed || WonRun(data, double.MaxValue);
                case AchievementCondition.CompletedWithin:
                    return (data.Progress.Completed && playtime <= entry.Value) || WonRun(data, entry.Value);
                case AchievementCondition.SizeStage:
                    return StatRules.SizeStage(data.Build.BiomassSpent) >= (int)entry.Value || data.Meta.MaxSizeStage >= (int)entry.Value;
                case AchievementCondition.MutationsOwned:
                    return data.Build.OwnedMutations.Count >= entry.Value || data.Meta.MostMutations >= entry.Value;
                case AchievementCondition.BonusNotches:
                    return data.Build.BonusNotches >= entry.Value || data.Meta.MaxBonusNotches >= entry.Value;
                case AchievementCondition.Trinkets:
                    return data.Build.Trinkets.Count >= entry.Value || data.Meta.HoarderBuys >= entry.Value;
                case AchievementCondition.LoreLogs:
                    return data.World.CollectedLogs.Count >= entry.Value;
                case AchievementCondition.WeaponsOwned:
                    return data.Build.OwnedWeapons.Count >= entry.Value || UnlockedWeapons(data) >= entry.Value;
                case AchievementCondition.BiomassSpent:
                    return data.Build.BiomassSpent >= entry.Value || data.Meta.BiomassSpent >= entry.Value;
                case AchievementCondition.MutationsAll:
                    if (string.IsNullOrEmpty(entry.Key))
                        return false;
                    foreach (var id in entry.Key.Split(','))
                        if (!data.Build.OwnedMutations.Contains(id.Trim()))
                            return false;
                    return true;
                default:
                    return false;
            }
        }

        private static bool WonRun(SaveData data, double seconds)
        {
            foreach (var run in data.Meta.Runs)
                if (run.Won && run.Seconds <= seconds)
                    return true;
            return false;
        }

        private static int UnlockedWeapons(SaveData data)
        {
            var count = MotherCall.Count(data.Meta, UnlockKind.Weapon);
            var stock = MotherCallStock.Loaded;
            if (stock != null)
                foreach (var entry in stock.Entries)
                    if (entry.Kind == UnlockKind.Weapon && entry.Cost <= 0)
                        count++;
            return count;
        }
    }
}
