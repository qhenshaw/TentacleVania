using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class CollectiblesTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void PickupIsCollectedOnce()
        {
            Assert.That(Collectibles.Collect("SH/Cache1", PickupKind.Biomass, "", 150), Is.True);
            Assert.That(Collectibles.Collect("SH/Cache1", PickupKind.Biomass, "", 150), Is.False);
            Assert.That(Biomass.Carried, Is.EqualTo(150));
            Assert.That(Collectibles.IsCollected("SH/Cache1"), Is.True);
        }

        [Test]
        public void PickupsGrantTheirItems()
        {
            Collectibles.Collect("a", PickupKind.Mutagen, "", 1);
            Collectibles.Collect("b", PickupKind.Trinket, "SpecimenTag", 1);
            Collectibles.Collect("c", PickupKind.Weapon, "CrusherMass", 1);
            Collectibles.Collect("d", PickupKind.Mutation, "WideDodge", 1);
            Collectibles.Collect("e", PickupKind.Notch, "", 1);
            Assert.That(Upgrades.Mutagen, Is.EqualTo(1));
            Assert.That(Collectibles.Trinkets, Is.EquivalentTo(new[] { "SpecimenTag" }));
            Assert.That(Collectibles.OwnedWeapons, Is.EquivalentTo(new[] { "CrusherMass" }));
            Assert.That(MutationLoadout.Owns("WideDodge"), Is.True);
            Assert.That(SaveSystem.Data.Build.BonusNotches, Is.EqualTo(1));
        }

        [Test]
        public void LoreLogsAreCollectedOnceInOrder()
        {
            Assert.That(LoreLogs.Collect("First"), Is.True);
            Assert.That(LoreLogs.Collect("Second"), Is.True);
            Assert.That(LoreLogs.Collect("First"), Is.False);
            Assert.That(LoreLogs.All, Is.EqualTo(new[] { "First", "Second" }));
        }
    }
}
