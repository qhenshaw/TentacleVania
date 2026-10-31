using NUnit.Framework;

namespace TentacleVania.Audio.Tests
{
    public sealed class PaRulesTests
    {
        [Test]
        public void RelayLinesAreSavedPerSector()
        {
            Assert.That(PaRules.SaveKey("relay.1", true, "SH"), Is.EqualTo("relay.1@SH"));
            Assert.That(PaRules.SaveKey("relay.1", true, null), Is.EqualTo("relay.1"));
            Assert.That(PaRules.SaveKey("sh.entry", false, "SH"), Is.EqualTo("sh.entry"));
        }

        [Test]
        public void LinesLastForTheVoiceOrTheReadingTime()
        {
            Assert.That(PaRules.LineSeconds(100, 1f, 0.05f, 2f), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(PaRules.LineSeconds(10, 6f, 0.05f, 2f), Is.EqualTo(6f));
            Assert.That(PaRules.LineSeconds(10, 0f, 0.05f, 2f), Is.EqualTo(2f));
        }
    }
}
