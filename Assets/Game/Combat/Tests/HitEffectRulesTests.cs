using NUnit.Framework;
using TentacleVania.Combat.Feedback;

namespace TentacleVania.Combat.Tests
{
    public sealed class HitEffectRulesTests
    {
        private const float BigHit = 0.1f;

        [Test]
        public void ImpactAlwaysPlaysEvenWithoutDamage()
        {
            var effects = HitEffectRules.Select(false, HitFeedbackKind.Light, 0f, 100f, false, true, BigHit);
            Assert.That(effects, Is.EqualTo(HitEffect.Impact));
        }

        [Test]
        public void LightEnemyHitSpraysWithoutDecal()
        {
            var effects = HitEffectRules.Select(false, HitFeedbackKind.Light, 10f, 100f, false, true, BigHit);
            Assert.That(effects, Is.EqualTo(HitEffect.Impact | HitEffect.Spray));
        }

        [TestCase(HitFeedbackKind.Heavy)]
        [TestCase(HitFeedbackKind.Finisher)]
        [TestCase(HitFeedbackKind.DashAttack)]
        public void BigEnemyHitsLeaveDecals(HitFeedbackKind kind)
        {
            var effects = HitEffectRules.Select(false, kind, 10f, 100f, false, true, BigHit);
            Assert.That(effects & HitEffect.Decal, Is.EqualTo(HitEffect.Decal));
        }

        [Test]
        public void KillsLeaveDecals()
        {
            var effects = HitEffectRules.Select(false, HitFeedbackKind.Light, 5f, 100f, true, true, BigHit);
            Assert.That(effects & HitEffect.Decal, Is.EqualTo(HitEffect.Decal));
        }

        [Test]
        public void CreatureDecalNeedsTheThreshold()
        {
            var small = HitEffectRules.Select(true, HitFeedbackKind.CreatureHurt, 9f, 100f, false, true, BigHit);
            var big = HitEffectRules.Select(true, HitFeedbackKind.CreatureHurt, 10f, 100f, false, true, BigHit);
            Assert.That(small & HitEffect.Decal, Is.EqualTo(HitEffect.None));
            Assert.That(big & HitEffect.Decal, Is.EqualTo(HitEffect.Decal));
        }

        [Test]
        public void GoreOffKeepsOnlyTheImpact()
        {
            var effects = HitEffectRules.Select(false, HitFeedbackKind.Heavy, 50f, 100f, true, false, BigHit);
            Assert.That(effects, Is.EqualTo(HitEffect.Impact));
        }
    }
}
