using NUnit.Framework;
using TentacleVania.UI.Hud;

namespace TentacleVania.UI.Tests
{
    public sealed class LoadoutInitialsTests
    {
        [Test]
        public void TwoWordsGiveTwoInitials()
        {
            Assert.That(LoadoutHudView.Initials("Lash Whips"), Is.EqualTo("LW"));
            Assert.That(LoadoutHudView.Initials("Bio-Shield"), Is.EqualTo("BS"));
            Assert.That(LoadoutHudView.Initials("Tentacle Grapple Hook"), Is.EqualTo("TG"));
        }

        [Test]
        public void OneWordGivesItsFirstTwoLetters()
        {
            Assert.That(LoadoutHudView.Initials("Shock"), Is.EqualTo("SH"));
            Assert.That(LoadoutHudView.Initials(""), Is.EqualTo(""));
        }
    }
}
