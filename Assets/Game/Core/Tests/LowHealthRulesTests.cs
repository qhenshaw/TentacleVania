using NUnit.Framework;
using TentacleVania.Core.Rendering;

namespace TentacleVania.Core.Tests
{
    public sealed class LowHealthRulesTests
    {
        [Test]
        public void StepsAtFortyAndFifteenPercent()
        {
            Assert.That(LowHealthRules.Target(0.41f, true), Is.EqualTo(0f));
            Assert.That(LowHealthRules.Target(0.4f, true), Is.EqualTo(LowHealthRules.LightAmount));
            Assert.That(LowHealthRules.Target(0.16f, true), Is.EqualTo(LowHealthRules.LightAmount));
            Assert.That(LowHealthRules.Target(0.15f, true), Is.EqualTo(LowHealthRules.HeavyAmount));
            Assert.That(LowHealthRules.Target(0.05f, false), Is.EqualTo(0f));
        }

        [Test]
        public void HeartbeatPeaksOnTheBeatAndRestsBetween()
        {
            Assert.That(LowHealthRules.Heartbeat(0.05f, 60f), Is.GreaterThan(0.9f));
            Assert.That(LowHealthRules.Heartbeat(0.6f, 60f), Is.LessThan(0.05f));
        }

        [Test]
        public void LurchStartsFullAndFades()
        {
            Assert.That(LowHealthRules.Lurch(0f, 1f), Is.EqualTo(1f));
            Assert.That(LowHealthRules.Lurch(0.5f, 1f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(LowHealthRules.Lurch(1.2f, 1f), Is.EqualTo(0f));
            Assert.That(LowHealthRules.Lurch(-1f, 1f), Is.EqualTo(0f));
        }
    }
}
