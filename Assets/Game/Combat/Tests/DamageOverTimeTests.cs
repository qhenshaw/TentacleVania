using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Feedback;
using TentacleVania.Combat.Status;

namespace TentacleVania.Combat.Tests
{
    public sealed class DamageOverTimeTests
    {
        private const float Tolerance = 1e-3f;
        private const float Interval = 0.5f;

        private static float Run(DamageOverTime dot, float from, float to)
        {
            var total = 0f;
            for (var t = from; t <= to + 1e-4f; t += 0.1f)
                total += dot.Tick(t, Interval);
            return total;
        }

        [Test]
        public void OneBleedStackDealsItsFullDuration()
        {
            var dot = new DamageOverTime();
            Assert.That(dot.Apply(3f, 5f, 5, 0f, Interval), Is.True);
            Assert.That(Run(dot, 0f, 6.5f), Is.EqualTo(15f).Within(Tolerance));
            Assert.That(dot.IsActive, Is.False);
        }

        [Test]
        public void BleedStacksUpToTheCapAndRefreshes()
        {
            var dot = new DamageOverTime();
            for (var i = 0; i < 7; i++)
                Assert.That(dot.Apply(3f, 5f, 5, 0f, Interval), Is.EqualTo(i == 0));
            Assert.That(dot.Stacks, Is.EqualTo(5));
            Assert.That(dot.DamagePerSecond, Is.EqualTo(15f).Within(Tolerance));
            Run(dot, 0f, 4f);
            dot.Apply(3f, 5f, 5, 4f, Interval);
            Assert.That(dot.Remaining(4f), Is.EqualTo(5f).Within(Tolerance));
            Assert.That(dot.Stacks, Is.EqualTo(5));
        }

        [Test]
        public void BurnRefreshesWithoutStacking()
        {
            var dot = new DamageOverTime();
            dot.Apply(5f, 4f, 1, 0f, Interval);
            dot.Apply(5f, 4f, 1, 2f, Interval);
            Assert.That(dot.Stacks, Is.EqualTo(1));
            Assert.That(Run(dot, 0f, 7f), Is.EqualTo(30f).Within(Tolerance));
        }

        [Test]
        public void TicksCatchUpAfterALongFrame()
        {
            var dot = new DamageOverTime();
            dot.Apply(4f, 5f, 1, 0f, Interval);
            Assert.That(dot.Tick(0.4f, Interval), Is.EqualTo(0f));
            Assert.That(dot.Tick(2f, Interval), Is.EqualTo(8f).Within(Tolerance));
        }

        [Test]
        public void BurstDealsTheRemainingDamageAndClears()
        {
            var dot = new DamageOverTime();
            dot.Apply(3f, 5f, 5, 0f, Interval);
            dot.Apply(3f, 5f, 5, 0f, Interval);
            Assert.That(dot.Burst(1f), Is.EqualTo(24f).Within(Tolerance));
            Assert.That(dot.IsActive, Is.False);
            Assert.That(dot.Burst(1f), Is.EqualTo(0f));
        }

        [Test]
        public void StrongerApplicationRaisesPerStackDamage()
        {
            var dot = new DamageOverTime();
            dot.Apply(3f, 5f, 5, 0f, Interval);
            dot.Apply(4f, 5f, 5, 0f, Interval);
            Assert.That(dot.PerStack, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(dot.Apply(0f, 5f, 5, 0f, Interval), Is.False);
            Assert.That(dot.Stacks, Is.EqualTo(2));
        }

        [Test]
        public void DamageOverTimeTicksGiveNoHitFeedback()
        {
            Assert.That(HitFeedbackRules.Classify(HitFlags.DamageOverTime, true, false), Is.EqualTo(HitFeedbackKind.None));
            Assert.That(HitFeedbackRules.Classify(HitFlags.DamageOverTime, false, true), Is.EqualTo(HitFeedbackKind.None));
        }
    }
}
