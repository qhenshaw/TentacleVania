using NUnit.Framework;
using TentacleVania.Core.Interaction;

namespace TentacleVania.Core.Tests
{
    public sealed class HoldRulesTests
    {
        [Test]
        public void ReleaseBeforeTheDurationIsATap()
        {
            Assert.That(HoldRules.Resolve(true, 0.3f, 0.6f), Is.EqualTo(HoldResult.Pending));
            Assert.That(HoldRules.Resolve(false, 0.3f, 0.6f), Is.EqualTo(HoldResult.Tap));
        }

        [Test]
        public void ReachingTheDurationIsAHold()
        {
            Assert.That(HoldRules.Resolve(true, 0.6f, 0.6f), Is.EqualTo(HoldResult.Hold));
            Assert.That(HoldRules.Progress(0.3f, 0.6f), Is.EqualTo(0.5f).Within(1e-4f));
        }
    }
}
