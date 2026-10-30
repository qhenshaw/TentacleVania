using NUnit.Framework;
using TentacleVania.UI.Hud;

namespace TentacleVania.UI.Tests
{
    public class BarTrailTests
    {
        private static BarTrail Full()
        {
            var bar = new BarTrail(0.5f, 1f, 0.5f);
            bar.Snap(1f);
            return bar;
        }

        [Test]
        public void DamageDropsFillAtOnceAndHoldsTheTrail()
        {
            var bar = Full();
            bar.Update(0.6f, 0.1f);
            Assert.AreEqual(0.6f, bar.Shown, 1e-5f);
            Assert.AreEqual(1f, bar.Trail, 1e-5f);
            bar.Update(0.6f, 0.3f);
            Assert.AreEqual(1f, bar.Trail, 1e-5f);
        }

        [Test]
        public void TrailDrainsAfterTheDelay()
        {
            var bar = Full();
            bar.Update(0.6f, 0.1f);
            bar.Update(0.6f, 0.4f);
            Assert.AreEqual(1f, bar.Trail, 1e-5f);
            bar.Update(0.6f, 0.2f);
            Assert.AreEqual(0.8f, bar.Trail, 1e-5f);
            for (var i = 0; i < 10; i++)
                bar.Update(0.6f, 0.1f);
            Assert.AreEqual(0.6f, bar.Trail, 1e-5f);
        }

        [Test]
        public void AnotherHitRestartsTheDelay()
        {
            var bar = Full();
            bar.Update(0.6f, 0.1f);
            bar.Update(0.6f, 0.4f);
            bar.Update(0.5f, 0.1f);
            bar.Update(0.5f, 0.3f);
            Assert.AreEqual(1f, bar.Trail, 1e-5f);
        }

        [Test]
        public void HealLeadsAndTheFillCatchesUp()
        {
            var bar = new BarTrail(0.5f, 1f, 0.5f);
            bar.Snap(0.4f);
            bar.Update(0.6f, 0.2f);
            Assert.AreEqual(0.6f, bar.Value, 1e-5f);
            Assert.AreEqual(0.5f, bar.Shown, 1e-5f);
            Assert.AreEqual(0.6f, bar.Trail, 1e-5f);
            bar.Update(0.6f, 0.2f);
            Assert.AreEqual(0.6f, bar.Shown, 1e-5f);
        }

        [Test]
        public void ValuesAreClamped()
        {
            var bar = Full();
            bar.Update(-1f, 0.1f);
            Assert.AreEqual(0f, bar.Shown, 1e-5f);
            bar.Update(2f, 10f);
            Assert.AreEqual(1f, bar.Shown, 1e-5f);
        }
    }
}
