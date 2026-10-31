using NUnit.Framework;
using TentacleVania.Combat.Feedback;

namespace TentacleVania.Combat.Tests
{
    public sealed class DeathGoreRulesTests
    {
        [Test]
        public void KillBurstsFleshWithGore()
        {
            var effects = DeathGoreRules.OnKill(true, true);
            Assert.That(effects, Is.EqualTo(DeathEffect.Gibs | DeathEffect.Spray | DeathEffect.Decal));
        }

        [Test]
        public void MachineKillBurstsMetal()
        {
            var effects = DeathGoreRules.OnKill(true, false);
            Assert.That(effects & DeathEffect.Machine, Is.EqualTo(DeathEffect.Machine));
            Assert.That(effects & DeathEffect.Gibs, Is.EqualTo(DeathEffect.Gibs));
        }

        [Test]
        public void GoreOffKillOnlySmokes()
        {
            Assert.That(DeathGoreRules.OnKill(false, true), Is.EqualTo(DeathEffect.Smoke));
            Assert.That(DeathGoreRules.OnKill(false, false), Is.EqualTo(DeathEffect.Smoke));
        }

        [Test]
        public void GoreOffBiteStaysInsideTheMaw()
        {
            Assert.That(DeathGoreRules.OnBite(false, true, false), Is.EqualTo(DeathEffect.None));
        }

        [Test]
        public void BossCoreBleedsOnTheFinishingBite()
        {
            var effects = DeathGoreRules.OnBite(true, false, true);
            Assert.That(effects & DeathEffect.Machine, Is.EqualTo(DeathEffect.None));
            Assert.That(effects & DeathEffect.Spray, Is.EqualTo(DeathEffect.Spray));
        }

        [Test]
        public void MachineBiteSparks()
        {
            Assert.That(DeathGoreRules.OnBite(true, false, false) & DeathEffect.Machine, Is.EqualTo(DeathEffect.Machine));
        }
    }
}
