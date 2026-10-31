using NUnit.Framework;
using TentacleVania.Bosses.Warden;

namespace TentacleVania.Bosses.Tests
{
    public sealed class WardenRulesTests
    {
        [Test]
        public void OneBeamAlwaysCoversTheCreature()
        {
            var offsets = new float[3];
            WardenRules.PickBeams(24f, 1.6f, 2, 3.3f, 0.5f, offsets);
            Assert.That(WardenRules.InBeam(3.3f, offsets, 2, 1.6f), Is.True);
        }

        [Test]
        public void BeamsLeaveSafeLanes()
        {
            var offsets = new float[3];
            WardenRules.PickBeams(24f, 1.6f, 3, 0f, 0.5f, offsets);
            var safe = 0;
            for (var x = -11f; x <= 11f; x += 0.5f)
                if (!WardenRules.InBeam(x, offsets, 3, 1.6f))
                    safe++;
            Assert.That(safe, Is.GreaterThan(30));
        }

        [Test]
        public void BeamsStayInsideTheHall()
        {
            var offsets = new float[3];
            WardenRules.PickBeams(24f, 1.6f, 2, 40f, 1f, offsets);
            foreach (var offset in offsets)
                Assert.That(System.Math.Abs(offset), Is.LessThanOrEqualTo(12f - 1.6f + 1e-4f));
        }
    }
}
