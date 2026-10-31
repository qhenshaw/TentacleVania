using NUnit.Framework;

namespace TentacleVania.Audio.Tests
{
    public sealed class MusicRulesTests
    {
        [Test]
        public void ArenaLayersRiseWithEachWave()
        {
            Assert.That(MusicRules.ArenaIntensity(0, 4), Is.EqualTo(0f));
            Assert.That(MusicRules.ArenaIntensity(1, 4), Is.EqualTo(MusicRules.FirstWave));
            Assert.That(MusicRules.ArenaIntensity(2, 4), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(MusicRules.ArenaIntensity(4, 4), Is.EqualTo(1f));
            Assert.That(MusicRules.ArenaIntensity(1, 1), Is.EqualTo(1f));
        }

        [Test]
        public void BossPhasesRaiseIntensity()
        {
            Assert.That(MusicRules.BossIntensity(0, 2), Is.EqualTo(0.5f));
            Assert.That(MusicRules.BossIntensity(1, 2), Is.EqualTo(1f));
            Assert.That(MusicRules.BossIntensity(0, 1), Is.EqualTo(0.75f));
        }

        [Test]
        public void BossBeatsArenaBeatsRest()
        {
            Assert.That(MusicRules.Pick(true, true, true), Is.EqualTo(MusicState.Boss));
            Assert.That(MusicRules.Pick(false, true, true), Is.EqualTo(MusicState.Arena));
            Assert.That(MusicRules.Pick(false, false, true), Is.EqualTo(MusicState.NestRest));
            Assert.That(MusicRules.Pick(false, false, false), Is.EqualTo(MusicState.Silent));
        }
    }
}
