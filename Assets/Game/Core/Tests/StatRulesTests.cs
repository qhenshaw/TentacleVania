using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class StatRulesTests
    {
        private const float Tolerance = 1e-4f;

        [TestCase(4, 144)]
        [TestCase(20, 855)]
        [TestCase(40, 2159)]
        [TestCase(59, 3677)]
        public void LevelCostFollowsTheBalanceSheet(int totalLevel, int expected) =>
            Assert.That(StatRules.LevelCost(totalLevel), Is.EqualTo(expected));

        [TestCase(1, 1f)]
        [TestCase(20, 20f)]
        [TestCase(30, 25f)]
        [TestCase(40, 30f)]
        [TestCase(50, 32.5f)]
        [TestCase(60, 35f)]
        [TestCase(99, 35f)]
        public void SoftCapsHalveThenQuarter(int value, float expected) =>
            Assert.That(StatRules.EffectivePoints(value), Is.EqualTo(expected).Within(Tolerance));

        [Test]
        public void StartingStatsGiveNoBonus()
        {
            Assert.That(StatRules.WeaponDamage(1), Is.EqualTo(1f));
            Assert.That(StatRules.MaxHealthBonus(1), Is.EqualTo(0f));
            Assert.That(StatRules.DashRecovery(1), Is.EqualTo(1f));
            Assert.That(StatRules.DamageTaken(1), Is.EqualTo(1f));
        }

        [Test]
        public void PerPointValuesMatchTheBalanceSheet()
        {
            Assert.That(StatRules.WeaponDamage(20), Is.EqualTo(1.57f).Within(Tolerance));
            Assert.That(StatRules.WeaponDamage(21), Is.EqualTo(1.585f).Within(Tolerance));
            Assert.That(StatRules.AttackSpeed(11), Is.EqualTo(1.15f).Within(Tolerance));
            Assert.That(StatRules.DashRecovery(11), Is.EqualTo(0.85f).Within(Tolerance));
            Assert.That(StatRules.MaxHealthBonus(11), Is.EqualTo(80f).Within(Tolerance));
            Assert.That(StatRules.DamageTaken(21), Is.EqualTo(0.9025f).Within(Tolerance));
            Assert.That(StatRules.ChargeGain(6), Is.EqualTo(1.1f).Within(Tolerance));
            Assert.That(StatRules.AbilityDamage(6), Is.EqualTo(1.1f).Within(Tolerance));
            Assert.That(StatRules.StatusPotency(6), Is.EqualTo(1.15f).Within(Tolerance));
        }

        [TestCase(0, 0)]
        [TestCase(2499, 0)]
        [TestCase(2500, 1)]
        [TestCase(10000, 2)]
        [TestCase(30000, 3)]
        public void SizeStageFollowsBiomassSpent(int spent, int stage) => Assert.That(StatRules.SizeStage(spent), Is.EqualTo(stage));

        [Test]
        public void LevelingSpendsBiomassAndTracksIt()
        {
            SaveSystem.NewGame(0);
            Assert.That(Stats.TotalLevel, Is.EqualTo(4));
            Assert.That(Stats.TryLevelUp(StatKind.Brutality), Is.False);
            Biomass.Add(200);
            Assert.That(Stats.TryLevelUp(StatKind.Brutality), Is.True);
            Assert.That(Stats.Brutality, Is.EqualTo(2));
            Assert.That(Stats.TotalLevel, Is.EqualTo(5));
            Assert.That(Biomass.Carried, Is.EqualTo(200 - 144));
            Assert.That(Stats.BiomassSpent, Is.EqualTo(144));
        }

        [Test]
        public void RespecKeepsTheTotalAndMinimums()
        {
            SaveSystem.NewGame(0);
            Stats.Set(StatKind.Brutality, 5);
            Assert.That(Stats.TotalLevel, Is.EqualTo(8));
            Assert.That(Stats.Respec(1, 1, 5, 1), Is.True);
            Assert.That(Stats.Tenacity, Is.EqualTo(5));
            Assert.That(Stats.Respec(2, 2, 2, 1), Is.False);
            Assert.That(Stats.Respec(0, 1, 6, 1), Is.False);
        }
    }
}
