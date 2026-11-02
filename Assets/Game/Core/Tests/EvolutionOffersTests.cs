using NUnit.Framework;
using TentacleVania.Core.Runs;

namespace TentacleVania.Core.Tests
{
    public sealed class EvolutionOffersTests
    {
        [Test]
        public void ClosedWeaponsAreNeverOffered()
        {
            var rng = new System.Random(1);
            for (var i = 0; i < 100; i++)
                foreach (var offer in EvolutionOffers.Pick(rng, new[] { 0, 2 }))
                    Assert.That(offer.Slot, Is.EqualTo(1));
            Assert.That(EvolutionOffers.Pick(rng, new[] { 0, 0 }), Is.Empty);
        }

        [Test]
        public void OneOpenWeaponOffersBothOfItsEvolutions()
        {
            var offers = EvolutionOffers.Pick(new System.Random(2), new[] { 2, 0 });
            Assert.That(offers.Count, Is.EqualTo(2));
            Assert.That(offers[0].Slot, Is.EqualTo(0));
            Assert.That(offers[1].Slot, Is.EqualTo(0));
            Assert.That(offers[0].Option, Is.Not.EqualTo(offers[1].Option));
        }

        [Test]
        public void TwoOpenWeaponsOfferOneWeaponsPairOrOneFromEach()
        {
            var rng = new System.Random(3);
            var pairs = 0;
            var mixed = 0;
            for (var i = 0; i < 200; i++)
            {
                var offers = EvolutionOffers.Pick(rng, new[] { 2, 2 });
                Assert.That(offers.Count, Is.EqualTo(2));
                if (offers[0].Slot == offers[1].Slot)
                {
                    Assert.That(offers[0].Option, Is.Not.EqualTo(offers[1].Option));
                    pairs++;
                }
                else
                {
                    mixed++;
                }
            }
            Assert.That(pairs, Is.GreaterThan(0));
            Assert.That(mixed, Is.GreaterThan(0));
        }

        [Test]
        public void OpenSlotsCountsWeaponsWithOptions() => Assert.That(EvolutionOffers.OpenSlots(new[] { 2, 0, 1 }), Is.EqualTo(2));
    }
}
