using TentacleVania.Core.Progression;
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
                    return data.World.Has(WorldFlag.DefeatedBoss, entry.Key);
                case AchievementCondition.GameCompleted:
                    return data.Progress.Completed;
                case AchievementCondition.CompletedWithin:
                    return data.Progress.Completed && playtime <= entry.Value;
                case AchievementCondition.SizeStage:
                    return StatRules.SizeStage(data.Build.BiomassSpent) >= (int)entry.Value;
                case AchievementCondition.MutationsOwned:
                    return data.Build.OwnedMutations.Count >= entry.Value;
                case AchievementCondition.BonusNotches:
                    return data.Build.BonusNotches >= entry.Value;
                case AchievementCondition.Trinkets:
                    return data.Build.Trinkets.Count >= entry.Value;
                case AchievementCondition.LoreLogs:
                    return data.World.CollectedLogs.Count >= entry.Value;
                case AchievementCondition.WeaponsOwned:
                    return data.Build.OwnedWeapons.Count >= entry.Value;
                case AchievementCondition.BiomassSpent:
                    return data.Build.BiomassSpent >= entry.Value;
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
    }
}
