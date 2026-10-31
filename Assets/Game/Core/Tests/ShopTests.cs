using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class ShopTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void PricesFollowNotches()
        {
            Assert.That(Shop.MutationPrice(1), Is.EqualTo(300));
            Assert.That(Shop.MutationPrice(2), Is.EqualTo(700));
            Assert.That(Shop.MutationPrice(3), Is.EqualTo(1500));
        }

        [Test]
        public void TrinketUnlocksStock()
        {
            Assert.That(Shop.InStock("Gluttony", "PodReleaseKey"), Is.False);
            Collectibles.Grant(PickupKind.Trinket, "PodReleaseKey", 1);
            Assert.That(Shop.InStock("Gluttony", "PodReleaseKey"), Is.True);
        }

        [Test]
        public void BuyingSpendsBiomassGrantsAndSellsOut()
        {
            Collectibles.Grant(PickupKind.Trinket, "PodReleaseKey", 1);
            Biomass.Add(250);
            Assert.That(Shop.TryBuyMutation("Gluttony", "PodReleaseKey", 300), Is.False);
            Biomass.Add(100);
            Assert.That(Shop.TryBuyMutation("Gluttony", "PodReleaseKey", 300), Is.True);
            Assert.That(Biomass.Carried, Is.EqualTo(50));
            Assert.That(SaveSystem.Data.Build.BiomassSpent, Is.EqualTo(300));
            Assert.That(MutationLoadout.Owns("Gluttony"), Is.True);
            Assert.That(Shop.InStock("Gluttony", "PodReleaseKey"), Is.False);
        }
    }
}
