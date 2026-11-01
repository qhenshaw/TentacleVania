using NUnit.Framework;
using TentacleVania.UI.Credits;

namespace TentacleVania.UI.Tests
{
    public class CreditsScrollTests
    {
        [Test]
        public void StartsBelowTheScreenAndEndsAboveIt()
        {
            Assert.AreEqual(1080f, CreditsView.Scroll(0f, 24f, 1080f, 2000f), 1e-3f);
            Assert.AreEqual(-2000f, CreditsView.Scroll(24f, 24f, 1080f, 2000f), 1e-3f);
            Assert.AreEqual(-2000f, CreditsView.Scroll(40f, 24f, 1080f, 2000f), 1e-3f);
        }
    }
}
