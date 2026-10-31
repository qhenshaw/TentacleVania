using NUnit.Framework;
using TentacleVania.Core.Rendering;

namespace TentacleVania.Core.Tests
{
    public sealed class DynamicResolutionRulesTests
    {
        [Test]
        public void DropsAStepWhenGpuIsOverBudget() => Assert.That(DynamicResolutionRules.Next(1f, 17f, 0f, 2f), Is.EqualTo(0.95f).Within(1e-4f));

        [Test]
        public void NeverDropsBelowSeventyPercent() => Assert.That(DynamicResolutionRules.Next(0.7f, 30f, 0f, 2f), Is.EqualTo(0.7f).Within(1e-4f));

        [Test]
        public void RaisesOnlyAfterACalmStretch()
        {
            Assert.That(DynamicResolutionRules.Next(0.8f, 10f, 1f, 2f), Is.EqualTo(0.8f).Within(1e-4f));
            Assert.That(DynamicResolutionRules.Next(0.8f, 10f, 2.5f, 2f), Is.EqualTo(0.85f).Within(1e-4f));
        }

        [Test]
        public void HoldsInTheComfortBand() => Assert.That(DynamicResolutionRules.Next(0.9f, 14f, 10f, 2f), Is.EqualTo(0.9f).Within(1e-4f));

        [Test]
        public void NeverAboveFull() => Assert.That(DynamicResolutionRules.Next(1f, 5f, 10f, 2f), Is.EqualTo(1f).Within(1e-4f));
    }
}
