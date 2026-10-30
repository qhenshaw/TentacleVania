using NUnit.Framework;
using TentacleVania.Creature.Rig;

namespace TentacleVania.Creature.Tests
{
    public sealed class DashTrailTests
    {
        private const float Dt = 1f / 60f;
        private const float Rise = 0.04f;
        private const float Frequency = 2.5f;
        private const float Damping = 0.5f;

        private static void Run(DashTrail trail, bool dashing, float seconds)
        {
            for (var t = 0f; t < seconds; t += Dt)
                trail.Update(dashing, Dt, Rise, Frequency, Damping);
        }

        [Test]
        public void RisesTowardFullStretchWhileDashing()
        {
            var trail = new DashTrail();
            Run(trail, true, 0.15f);
            Assert.That(trail.Amount, Is.GreaterThan(0.95f).And.LessThanOrEqualTo(1f));
            Assert.That(trail.LengthScale(2f), Is.GreaterThan(1.95f));
            Assert.That(trail.WidthScale(0.5f), Is.LessThan(0.53f));
        }

        [Test]
        public void SnapsBackWithOvershootThenSettles()
        {
            var trail = new DashTrail();
            Run(trail, true, 0.15f);
            var lowest = 1f;
            for (var t = 0f; t < 0.5f; t += Dt)
            {
                trail.Update(false, Dt, Rise, Frequency, Damping);
                lowest = UnityEngine.Mathf.Min(lowest, trail.Amount);
            }
            Assert.That(lowest, Is.LessThan(-0.05f), "should overshoot past rest");
            Run(trail, false, 1.5f);
            Assert.That(UnityEngine.Mathf.Abs(trail.Amount), Is.LessThan(0.02f));
            Assert.That(trail.WidthScale(0.5f), Is.EqualTo(1f).Within(0.02f));
        }

        [Test]
        public void ChainedDashKeepsTheStretch()
        {
            var trail = new DashTrail();
            Run(trail, true, 0.15f);
            Run(trail, false, 0.05f);
            var between = trail.Amount;
            Run(trail, true, 0.05f);
            Assert.That(between, Is.GreaterThan(0.5f));
            Assert.That(trail.Amount, Is.GreaterThan(between));
        }

        [Test]
        public void LengthNeverCollapses()
        {
            var trail = new DashTrail();
            Run(trail, true, 0.15f);
            for (var t = 0f; t < 1f; t += Dt)
            {
                trail.Update(false, Dt, Rise, Frequency, 0.05f);
                Assert.That(trail.LengthScale(3f), Is.GreaterThanOrEqualTo(0.5f));
            }
        }
    }
}
