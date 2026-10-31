using NUnit.Framework;

namespace TentacleVania.Benchmark.Tests
{
    public sealed class BenchmarkStatsTests
    {
        [Test]
        public void ComputesAveragePercentilesAndOverBudget()
        {
            var samples = new float[100];
            for (var i = 0; i < 100; i++)
                samples[i] = i + 1;
            var stats = BenchmarkStats.Compute(samples, 100);
            Assert.That(stats.Frames, Is.EqualTo(100));
            Assert.That(stats.AverageMs, Is.EqualTo(50.5f).Within(1e-3f));
            Assert.That(stats.P50Ms, Is.EqualTo(50f));
            Assert.That(stats.P95Ms, Is.EqualTo(95f));
            Assert.That(stats.P99Ms, Is.EqualTo(99f));
            Assert.That(stats.MaxMs, Is.EqualTo(100f));
            Assert.That(stats.OverBudget, Is.EqualTo(84));
        }

        [Test]
        public void OnlyTheRecordedCountIsUsed()
        {
            var samples = new[] { 10f, 10f, 500f };
            var stats = BenchmarkStats.Compute(samples, 2);
            Assert.That(stats.MaxMs, Is.EqualTo(10f));
            Assert.That(stats.OverBudget, Is.EqualTo(0));
        }

        [Test]
        public void EmptyIsZero() => Assert.That(BenchmarkStats.Compute(new float[4], 0).Frames, Is.EqualTo(0));
    }
}
