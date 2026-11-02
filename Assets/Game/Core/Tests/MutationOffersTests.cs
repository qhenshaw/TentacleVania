using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Runs;

namespace TentacleVania.Core.Tests
{
    public sealed class MutationOffersTests
    {
        private static readonly MutationBuild[] Pool =
        {
            MutationBuild.Fire, MutationBuild.Bleed, MutationBuild.Acid, MutationBuild.Healing,
            MutationBuild.Stagger, MutationBuild.Spines, MutationBuild.Fire | MutationBuild.Consuming, MutationBuild.None,
        };

        [Test]
        public void OffersAreDistinct()
        {
            var rng = new System.Random(1);
            for (var i = 0; i < 200; i++)
            {
                var offers = MutationOffers.Pick(rng, Pool, null, MutationOffers.Count);
                Assert.That(offers.Count, Is.EqualTo(MutationOffers.Count));
                Assert.That(new HashSet<int>(offers).Count, Is.EqualTo(offers.Count));
            }
        }

        [Test]
        public void SmallPoolsOfferWhatIsLeft()
        {
            var offers = MutationOffers.Pick(new System.Random(2), new[] { MutationBuild.Fire, MutationBuild.Acid }, null, MutationOffers.Count);
            Assert.That(offers, Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(MutationOffers.Pick(new System.Random(2), new MutationBuild[0], null, 3), Is.Empty);
        }

        [Test]
        public void SharedBuildsAreOfferedMoreOften()
        {
            var rng = new System.Random(3);
            var picked = new[] { MutationBuild.Fire, MutationBuild.Fire | MutationBuild.BioElectric };
            var fire = 0;
            var bleed = 0;
            for (var i = 0; i < 2000; i++)
                foreach (var index in MutationOffers.Pick(rng, Pool, picked, 1))
                {
                    if (index == 0)
                        fire++;
                    if (index == 1)
                        bleed++;
                }
            Assert.That(fire, Is.GreaterThan(bleed * 2));
        }

        [Test]
        public void ChallengeOffersFavourRareMutations()
        {
            var builds = new[] { MutationBuild.Fire, MutationBuild.Acid };
            var notches = new[] { 1, 3 };
            int RareShare(bool rare)
            {
                var rng = new System.Random(5);
                var hits = 0;
                for (var i = 0; i < 2000; i++)
                    if (MutationOffers.Pick(rng, builds, notches, rare, null, 1)[0] == 1)
                        hits++;
                return hits;
            }
            Assert.That(RareShare(true), Is.GreaterThan(RareShare(false) * 3));
        }

        [Test]
        public void WeightCountsEachSharedPick()
        {
            var picked = new[] { MutationBuild.Fire, MutationBuild.Bleed, MutationBuild.Fire };
            Assert.That(MutationOffers.Weight(MutationBuild.Acid, picked), Is.EqualTo(MutationOffers.BaseWeight));
            Assert.That(MutationOffers.Weight(MutationBuild.Fire, picked), Is.EqualTo(MutationOffers.BaseWeight + 2 * MutationOffers.SharedBuildWeight));
            Assert.That(MutationOffers.Weight(MutationBuild.None, picked), Is.EqualTo(MutationOffers.BaseWeight));
        }
    }
}
