using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;

namespace TentacleVania.Combat.Tests
{
    public sealed class StatusTimersTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void SlowLastsItsDurationAndScalesMovement()
        {
            var timers = new StatusTimers();
            Assert.That(timers.Apply(StatusKind.Slow, 0.2f, 2f, 10f), Is.True);
            Assert.That(timers.MoveScale(10.5f), Is.EqualTo(0.8f).Within(Tolerance));
            Assert.That(timers.Remaining(StatusKind.Slow, 11f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(timers.IsActive(StatusKind.Slow, 12.01f), Is.False);
            Assert.That(timers.MoveScale(12.01f), Is.EqualTo(1f));
        }

        [Test]
        public void ReapplyingRefreshesWithoutStacking()
        {
            var timers = new StatusTimers();
            timers.Apply(StatusKind.Slow, 0.2f, 2f, 0f);
            for (var t = 0.5f; t <= 3f; t += 0.5f)
                Assert.That(timers.Apply(StatusKind.Slow, 0.2f, 2f, t), Is.False);
            Assert.That(timers.Remaining(StatusKind.Slow, 3f), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(timers.MoveScale(3f), Is.EqualTo(0.8f).Within(Tolerance));
        }

        [Test]
        public void StrongerSlowWinsWhileActive()
        {
            var timers = new StatusTimers();
            timers.Apply(StatusKind.Slow, 0.2f, 2f, 0f);
            timers.Apply(StatusKind.Slow, 0.5f, 1f, 0.5f);
            Assert.That(timers.MoveScale(1f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(timers.Remaining(StatusKind.Slow, 1f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void RootStopsMovementAndClears()
        {
            var timers = new StatusTimers();
            timers.Apply(StatusKind.Slow, 0.2f, 5f, 0f);
            timers.Apply(StatusKind.Root, 1f, 1.5f, 0f);
            Assert.That(timers.MoveScale(1f), Is.EqualTo(0f));
            Assert.That(timers.MoveScale(1.6f), Is.EqualTo(0.8f).Within(Tolerance));
            timers.Clear(StatusKind.Slow);
            Assert.That(timers.MoveScale(1.6f), Is.EqualTo(1f));
            Assert.That(timers.Apply(StatusKind.None, 1f, 1f, 0f), Is.False);
        }
    }
}
