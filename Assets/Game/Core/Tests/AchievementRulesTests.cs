using NUnit.Framework;
using TentacleVania.Core.Platform;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class AchievementRulesTests
    {
        private static AchievementList.Entry Entry(AchievementCondition condition, string key = "", float value = 0f) =>
            new() { Id = "Test", Condition = condition, Key = key, Value = value };

        [Test]
        public void BossAndCompletionConditionsReadTheSave()
        {
            var data = new SaveData();
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.BossDefeated, "SH/WardenDrone"), data, 0), Is.False);
            data.World.Set(WorldFlag.DefeatedBoss, "SH/WardenDrone");
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.BossDefeated, "SH/WardenDrone"), data, 0), Is.True);
            data.Progress.Completed = true;
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.GameCompleted), data, 0), Is.True);
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.CompletedWithin, value: 3600f), data, 3000), Is.True);
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.CompletedWithin, value: 3600f), data, 4000), Is.False);
        }

        [Test]
        public void CountsAndListsUseThresholds()
        {
            var data = new SaveData();
            data.Build.OwnedMutations.AddRange(new[] { "A", "B" });
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.MutationsOwned, value: 2f), data, 0), Is.True);
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.MutationsAll, "A, B"), data, 0), Is.True);
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.MutationsAll, "A,C"), data, 0), Is.False);
            data.Build.BiomassSpent = 30000;
            Assert.That(AchievementRules.Met(Entry(AchievementCondition.SizeStage, value: 3f), data, 0), Is.True);
        }
    }
}
