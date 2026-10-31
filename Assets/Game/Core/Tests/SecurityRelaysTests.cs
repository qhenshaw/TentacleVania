using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class SecurityRelaysTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void CountsOnlyItsOwnSystemOnce()
        {
            Assert.That(SecurityRelays.Destroy("SH", "A"), Is.True);
            Assert.That(SecurityRelays.Destroy("SH", "A"), Is.False);
            SecurityRelays.Destroy("SH", "B");
            SecurityRelays.Destroy("SHX", "A");
            SecurityRelays.Destroy("BL", "A");
            Assert.That(SecurityRelays.Count("SH"), Is.EqualTo(2));
            Assert.That(SecurityRelays.IsDestroyed("SH", "B"), Is.True);
            Assert.That(SecurityRelays.IsDestroyed("BL", "B"), Is.False);
        }

        [Test]
        public void EventCarriesTheNewCount()
        {
            var last = 0;
            void OnDestroyed(string system, int count) => last = count;
            SecurityRelays.RelayDestroyed += OnDestroyed;
            try
            {
                SecurityRelays.Destroy("SH", "A");
                SecurityRelays.Destroy("SH", "B");
                Assert.That(last, Is.EqualTo(2));
            }
            finally
            {
                SecurityRelays.RelayDestroyed -= OnDestroyed;
            }
        }

        [Test]
        public void ResetClearsOneSystem()
        {
            SecurityRelays.Destroy("SH", "A");
            SecurityRelays.Destroy("BL", "A");
            SecurityRelays.Reset("SH");
            Assert.That(SecurityRelays.Count("SH"), Is.EqualTo(0));
            Assert.That(SecurityRelays.Count("BL"), Is.EqualTo(1));
        }

        [Test]
        public void DoorPowersDownAtTwoAndOpensAtThree()
        {
            Assert.That(SecurityRelays.Power(0, 3), Is.EqualTo(DoorPower.Armed));
            Assert.That(SecurityRelays.Power(1, 3), Is.EqualTo(DoorPower.Armed));
            Assert.That(SecurityRelays.Power(2, 3), Is.EqualTo(DoorPower.PoweringDown));
            Assert.That(SecurityRelays.Power(3, 3), Is.EqualTo(DoorPower.Open));
        }
    }
}
