using NUnit.Framework;
using TentacleVania.Enemies.AI;

namespace TentacleVania.Enemies.Tests
{
    public class StaggerBarTests
    {
        [Test]
        public void FillsWithoutBreakingBelowMax()
        {
            var bar = new StaggerBar(40f);
            Assert.IsFalse(bar.Add(12f));
            Assert.IsFalse(bar.Add(12f));
            Assert.AreEqual(24f, bar.Current, 1e-4f);
            Assert.AreEqual(0.6f, bar.Normalized, 1e-4f);
        }

        [Test]
        public void BreaksAndEmptiesWhenFull()
        {
            var bar = new StaggerBar(40f);
            bar.Add(30f);
            Assert.IsTrue(bar.Add(12f));
            Assert.AreEqual(0f, bar.Current);
        }

        [Test]
        public void OneBigHitBreaksAFreshBar() => Assert.IsTrue(new StaggerBar(60f).Add(90f));

        [Test]
        public void ZeroOrNegativeStaggerIsIgnored()
        {
            var bar = new StaggerBar(10f);
            Assert.IsFalse(bar.Add(0f));
            Assert.IsFalse(bar.Add(-5f));
            Assert.AreEqual(0f, bar.Current);
        }

        [Test]
        public void BarNeverDrainsOnItsOwn()
        {
            var bar = new StaggerBar(40f);
            bar.Add(20f);
            Assert.AreEqual(20f, bar.Current, "no decay (developer decision)");
        }
    }
}
