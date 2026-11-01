using NUnit.Framework;
using TentacleVania.UI.Fonts;

namespace TentacleVania.UI.Tests
{
    public sealed class LocaleFontRulesTests
    {
        [TestCase("en", null)]
        [TestCase("de", null)]
        [TestCase("ja", "jp")]
        [TestCase("ko-KR", "kr")]
        [TestCase("zh-Hans", "sc")]
        [TestCase("zh-CN", "sc")]
        [TestCase("zh-Hant", "tc")]
        [TestCase("zh-TW", "tc")]
        [TestCase("zh-HK", "tc")]
        public void PicksTheRegionalFont(string code, string region) => Assert.That(LocaleFontRules.RegionFor(code), Is.EqualTo(region));
    }
}
