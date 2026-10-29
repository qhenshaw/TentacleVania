using NUnit.Framework;
using TentacleVania.Creature.Movement;

namespace TentacleVania.Creature.Tests
{
    public class DashTimerTests
    {
        private const float Duration = 0.15f;
        private const float Cooldown = 0.3f;
        private const float Lockout = 0.8f;

        private static DashTimer Create() => new(Duration, Cooldown, 2, Lockout);

        [Test]
        public void FirstDashIsAvailable() => Assert.IsTrue(Create().TryStart(0f));

        [Test]
        public void CooldownStartsWhenTheDashEnds()
        {
            var timer = Create();
            timer.TryStart(0f);
            Assert.IsFalse(timer.CanDash(Duration + Cooldown - 0.01f));
            Assert.IsTrue(timer.CanDash(Duration + Cooldown));
        }

        [Test]
        public void SecondChainedDashTriggersLockout()
        {
            var timer = Create();
            timer.TryStart(0f);
            var second = Duration + Cooldown;
            Assert.IsTrue(timer.TryStart(second));
            var end = second + Duration;
            Assert.IsFalse(timer.CanDash(end + Cooldown));
            Assert.IsFalse(timer.CanDash(end + Lockout - 0.01f));
            Assert.IsTrue(timer.CanDash(end + Lockout));
        }

        [Test]
        public void ChainRestartsAfterLockout()
        {
            var timer = Create();
            timer.TryStart(0f);
            var second = Duration + Cooldown;
            timer.TryStart(second);
            var third = second + Duration + Lockout;
            Assert.IsTrue(timer.TryStart(third));
            Assert.AreEqual(1, timer.ChainCount);
            Assert.IsTrue(timer.CanDash(third + Duration + Cooldown));
        }

        [Test]
        public void WaitingOutTheLockoutResetsTheChain()
        {
            var timer = Create();
            timer.TryStart(0f);
            var later = Duration + Lockout;
            Assert.IsTrue(timer.TryStart(later));
            Assert.AreEqual(1, timer.ChainCount);
        }

        [Test]
        public void DashDuringCooldownIsRejected()
        {
            var timer = Create();
            timer.TryStart(0f);
            Assert.IsFalse(timer.TryStart(0.1f));
            Assert.AreEqual(1, timer.ChainCount);
        }
    }
}
