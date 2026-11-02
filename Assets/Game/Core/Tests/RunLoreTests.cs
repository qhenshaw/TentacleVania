using NUnit.Framework;
using TentacleVania.Core.Runs;

namespace TentacleVania.Core.Tests
{
    public sealed class RunLoreTests
    {
        [Test]
        public void OnlyUncollectedLogsAreOffered()
        {
            var collected = new[] { true, false, true, false };
            var rng = new System.Random(1);
            for (var i = 0; i < 200; i++)
            {
                var pick = RunLore.Pick(rng, collected, 1f);
                Assert.That(pick == 1 || pick == 3, Is.True);
            }
        }

        [Test]
        public void NothingWhenEverythingIsCollectedOrTheRollFails()
        {
            Assert.That(RunLore.Pick(new System.Random(2), new[] { true, true }, 1f), Is.EqualTo(-1));
            Assert.That(RunLore.Pick(new System.Random(2), new[] { false, false }, 0f), Is.EqualTo(-1));
        }

        [Test]
        public void TheChanceRoughlyHolds()
        {
            var rng = new System.Random(3);
            var hits = 0;
            for (var i = 0; i < 4000; i++)
                if (RunLore.Pick(rng, new[] { false }) >= 0)
                    hits++;
            Assert.That(hits / 4000f, Is.EqualTo(RunLore.Chance).Within(0.03f));
        }
    }
}
