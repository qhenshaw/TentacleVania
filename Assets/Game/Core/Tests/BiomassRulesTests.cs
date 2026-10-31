using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class BiomassRulesTests
    {
        private static readonly Vector3 Spot = new(2f, 0f, 3f);

        [Test]
        public void DeathDropsEverythingCarried()
        {
            var state = new BiomassState { Carried = 80 };
            BiomassRules.Die(state, false, "A1", Spot);
            Assert.That(state.Carried, Is.EqualTo(0));
            Assert.That(state.Dropped, Is.True);
            Assert.That(state.DroppedAmount, Is.EqualTo(80));
            Assert.That(state.DroppedScene, Is.EqualTo("A1"));
            Assert.That(state.DroppedPosition, Is.EqualTo(Spot));
        }

        [Test]
        public void ReclaimReturnsTheDrop()
        {
            var state = new BiomassState { Carried = 80 };
            BiomassRules.Die(state, false, "A1", Spot);
            BiomassRules.Add(state, 15);
            Assert.That(BiomassRules.Reclaim(state), Is.EqualTo(80));
            Assert.That(state.Carried, Is.EqualTo(95));
            Assert.That(state.Dropped, Is.False);
            Assert.That(BiomassRules.Reclaim(state), Is.EqualTo(0));
        }

        [Test]
        public void DyingAgainLosesTheOldDrop()
        {
            var state = new BiomassState { Carried = 80 };
            BiomassRules.Die(state, false, "A1", Spot);
            BiomassRules.Add(state, 20);
            var lost = BiomassRules.Die(state, false, "A2", Vector3.one);
            Assert.That(lost, Is.EqualTo(80));
            Assert.That(state.DroppedAmount, Is.EqualTo(20));
            Assert.That(state.DroppedScene, Is.EqualTo("A2"));
        }

        [Test]
        public void DyingWithNothingCarriedLosesTheDropAndLeavesNone()
        {
            var state = new BiomassState { Carried = 50 };
            BiomassRules.Die(state, false, "A1", Spot);
            Assert.That(BiomassRules.Die(state, false, "A2", Spot), Is.EqualTo(50));
            Assert.That(state.Dropped, Is.False);
            Assert.That(state.DroppedScene, Is.EqualTo(""));
        }

        [Test]
        public void KeepOnDeathChangesNothing()
        {
            var state = new BiomassState { Carried = 80 };
            Assert.That(BiomassRules.Die(state, true, "A1", Spot), Is.EqualTo(0));
            Assert.That(state.Carried, Is.EqualTo(80));
            Assert.That(state.Dropped, Is.False);
        }

        [Test]
        public void SpendNeedsEnough()
        {
            var state = new BiomassState { Carried = 30 };
            Assert.That(BiomassRules.Spend(state, 40), Is.False);
            Assert.That(BiomassRules.Spend(state, 30), Is.True);
            Assert.That(state.Carried, Is.EqualTo(0));
            Assert.That(BiomassRules.Add(state, -5), Is.EqualTo(0));
        }
    }
}
