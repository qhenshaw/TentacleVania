using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Core.Runs;

namespace TentacleVania.Core.Tests
{
    public sealed class RunGeneratorTests
    {
        [Test]
        public void SameSeedPlansTheSameFloor()
        {
            var a = RunGenerator.PlanFloor(1234, 0);
            var b = RunGenerator.PlanFloor(1234, 0);
            Assert.That(a.Steps, Is.EqualTo(b.Steps));
        }

        [Test]
        public void FloorsKeepTheRoomMix()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var plan = RunGenerator.PlanFloor(seed, seed % 4);
                Assert.That(plan.Count, Is.EqualTo(RunGenerator.DefaultRooms));
                Assert.That(plan.Steps[0], Is.EqualTo(RoomRole.Entry));
                Assert.That(plan.Steps[plan.Count - 1], Is.EqualTo(RoomRole.Boss));
                Assert.That(plan.CountOf(RoomRole.Shop), Is.EqualTo(1));
                Assert.That(plan.CountOf(RoomRole.Elite), Is.EqualTo(2));
                for (var i = 1; i < plan.Count; i++)
                    Assert.That(plan.Steps[i] == RoomRole.Elite && plan.Steps[i - 1] == RoomRole.Elite, Is.False, "elites back to back");
            }
        }

        [Test]
        public void CombatDoorsOfferDistinctRewardsAndRooms()
        {
            var rng = new System.Random(7);
            var context = new RewardContext { EvolutionsOpen = 2 };
            for (var i = 0; i < 200; i++)
            {
                var offers = RunGenerator.Offers(rng, RoomRole.Combat, context, 6, new HashSet<int>());
                Assert.That(offers.Count, Is.InRange(2, RunGenerator.MaxDoors));
                var rewards = new HashSet<RunReward>();
                var rooms = new HashSet<int>();
                foreach (var offer in offers)
                {
                    Assert.That(rewards.Add(offer.Reward), Is.True, "duplicate reward");
                    Assert.That(rooms.Add(offer.Room), Is.True, "duplicate room");
                }
            }
        }

        [Test]
        public void NoEvolutionDoorsOnceBothWeaponsEvolved()
        {
            var rng = new System.Random(3);
            var context = new RewardContext { EvolutionsOpen = 0 };
            for (var i = 0; i < 200; i++)
                foreach (var offer in RunGenerator.Offers(rng, RoomRole.Combat, context, 6, null))
                    Assert.That(offer.Reward, Is.Not.EqualTo(RunReward.Evolution));
        }

        [Test]
        public void MutationPityForcesAMutationDoor()
        {
            var rng = new System.Random(11);
            var context = new RewardContext { EvolutionsOpen = 2, StepsSinceMutation = RunGenerator.MutationPity };
            var offers = RunGenerator.Offers(rng, RoomRole.Combat, context, 6, null);
            Assert.That(offers[0].Reward, Is.EqualTo(RunReward.Mutation));
        }

        [Test]
        public void ShopsElitesAndBossesOfferTheirOwnRewards()
        {
            var rng = new System.Random(5);
            var context = new RewardContext();
            Assert.That(RunGenerator.Offers(rng, RoomRole.Shop, context, 1, null)[0].Reward, Is.EqualTo(RunReward.Shop));
            Assert.That(RunGenerator.Offers(rng, RoomRole.Boss, context, 1, null)[0].Reward, Is.EqualTo(RunReward.None));
            foreach (var offer in RunGenerator.Offers(rng, RoomRole.Elite, context, 6, null))
                Assert.That(offer.Reward, Is.EqualTo(RunReward.Challenge));
        }

        [Test]
        public void RoomWeightsMakeRareRoomsRare()
        {
            var rng = new System.Random(21);
            var weights = new[] { 10, 10, 2 };
            var rare = 0;
            for (var i = 0; i < 3000; i++)
                if (RunGenerator.Offers(rng, RoomRole.Shop, new RewardContext(), 3, null, null, weights)[0].Room == 2)
                    rare++;
            Assert.That(rare / 3000f, Is.EqualTo(2f / 22f).Within(0.025f));
        }

        [Test]
        public void ZeroWeightRoomsAreNeverPicked()
        {
            var rng = new System.Random(22);
            for (var i = 0; i < 300; i++)
                Assert.That(RunGenerator.Offers(rng, RoomRole.Shop, new RewardContext(), 3, null, null, new[] { 0, 5, 5 })[0].Room, Is.Not.EqualTo(0));
        }

        [Test]
        public void UsedRoomsAreAvoidedWhileOthersRemain()
        {
            var rng = new System.Random(9);
            var used = new HashSet<int> { 0, 1, 2, 3 };
            for (var i = 0; i < 50; i++)
                foreach (var offer in RunGenerator.Offers(rng, RoomRole.Combat, new RewardContext { EvolutionsOpen = 2 }, 8, used))
                    Assert.That(offer.Room, Is.GreaterThanOrEqualTo(4));
        }
    }
}
