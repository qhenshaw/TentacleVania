using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class BossProgressTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void DefeatIsSavedAndUnlocksTheRewardOnce()
        {
            var unlocked = 0;
            void OnUnlocked(string ability) => unlocked++;
            BossProgress.AbilityUnlocked += OnUnlocked;
            try
            {
                Assert.That(BossProgress.IsDefeated("BL/Warden"), Is.False);
                BossProgress.Defeat("BL/Warden", "Shock");
                BossProgress.Defeat("BL/Warden", "Shock");
                Assert.That(BossProgress.IsDefeated("BL/Warden"), Is.True);
                Assert.That(BossProgress.IsAbilityUnlocked("Shock"), Is.True);
                Assert.That(SaveSystem.Data.Build.Abilities.Count, Is.EqualTo(1));
                Assert.That(unlocked, Is.EqualTo(1));
            }
            finally
            {
                BossProgress.AbilityUnlocked -= OnUnlocked;
            }
        }

        [Test]
        public void RevivedBossesFightAgain()
        {
            BossProgress.Defeat("BL/Warden", "");
            BossProgress.Revive("BL/Warden");
            Assert.That(BossProgress.IsDefeated("BL/Warden"), Is.False);
        }

        [Test]
        public void PhaseIsTheLastThresholdHealthIsUnder()
        {
            var thresholds = new[] { 1f, 0.6f, 0.3f };
            Assert.That(BossProgress.PhaseFor(1f, thresholds), Is.EqualTo(0));
            Assert.That(BossProgress.PhaseFor(0.61f, thresholds), Is.EqualTo(0));
            Assert.That(BossProgress.PhaseFor(0.6f, thresholds), Is.EqualTo(1));
            Assert.That(BossProgress.PhaseFor(0.31f, thresholds), Is.EqualTo(1));
            Assert.That(BossProgress.PhaseFor(0.05f, thresholds), Is.EqualTo(2));
            Assert.That(BossProgress.PhaseFor(0.5f, new[] { 1f }), Is.EqualTo(0));
        }
    }
}
