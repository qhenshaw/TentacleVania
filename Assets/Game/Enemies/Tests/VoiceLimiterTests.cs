using NUnit.Framework;

namespace TentacleVania.Enemies.Tests
{
    public sealed class VoiceLimiterTests
    {
        [Test]
        public void AllowsUpToTheLimitPerWindow()
        {
            var limiter = new VoiceLimiter(3, 0.6f);
            Assert.That(limiter.TryUse(0f), Is.True);
            Assert.That(limiter.TryUse(0.1f), Is.True);
            Assert.That(limiter.TryUse(0.2f), Is.True);
            Assert.That(limiter.TryUse(0.3f), Is.False);
            Assert.That(limiter.TryUse(0.65f), Is.True);
        }

        [Test]
        public void ImportantVoicesAlwaysPlay()
        {
            var limiter = new VoiceLimiter(1, 1f);
            Assert.That(limiter.TryUse(0f), Is.True);
            Assert.That(limiter.TryUse(0.1f), Is.False);
            Assert.That(limiter.TryUse(0.2f, true), Is.True);
        }
    }
}
