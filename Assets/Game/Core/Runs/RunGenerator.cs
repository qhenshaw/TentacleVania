using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Runs
{
    public static class RunGenerator
    {
        public const int DefaultRooms = 12;
        public const int Elites = 2;
        public const int Shops = 1;
        public const int MaxDoors = 3;
        public const int MutationPity = 3;

        private static readonly RunReward[] CombatRewards = { RunReward.Mutation, RunReward.Evolution, RunReward.Biomass, RunReward.Essence };
        private static readonly int[] CombatWeights = { 40, 20, 25, 15 };

        public static int FloorSeed(int runSeed, int floor) => unchecked(runSeed * 397 ^ (floor + 1) * 7919);

        public static FloorPlan PlanFloor(int runSeed, int floor, int rooms = DefaultRooms)
        {
            rooms = Math.Max(rooms, Elites + Shops + 4);
            var rng = new Random(FloorSeed(runSeed, floor));
            var steps = new RoomRole[rooms];
            for (var i = 0; i < rooms; i++)
                steps[i] = RoomRole.Combat;
            steps[0] = RoomRole.Entry;
            steps[rooms - 1] = RoomRole.Boss;
            var shop = rng.Next(rooms / 2, rooms - 2);
            steps[shop] = RoomRole.Shop;
            var placed = 0;
            var guard = 0;
            while (placed < Elites && guard++ < 100)
            {
                var index = rng.Next(3, rooms - 1);
                if (steps[index] != RoomRole.Combat || steps[index - 1] == RoomRole.Elite || (index + 1 < rooms && steps[index + 1] == RoomRole.Elite))
                    continue;
                steps[index] = RoomRole.Elite;
                placed++;
            }
            return new FloorPlan(floor, steps);
        }

        public static List<DoorOffer> Offers(Random rng, RoomRole next, in RewardContext context, int roomsForRole, ICollection<int> usedRooms, List<DoorOffer> into = null, IReadOnlyList<int> roomWeights = null)
        {
            into ??= new List<DoorOffer>(MaxDoors);
            into.Clear();
            switch (next)
            {
                case RoomRole.Shop:
                    into.Add(new DoorOffer(next, RunReward.Shop, PickRoom(rng, roomsForRole, usedRooms, into, roomWeights)));
                    return into;
                case RoomRole.Boss:
                case RoomRole.Entry:
                    into.Add(new DoorOffer(next, RunReward.None, PickRoom(rng, roomsForRole, usedRooms, into, roomWeights)));
                    return into;
                case RoomRole.Elite:
                    var elites = rng.Next(1, 3);
                    for (var i = 0; i < elites; i++)
                        into.Add(new DoorOffer(next, RunReward.Challenge, PickRoom(rng, roomsForRole, usedRooms, into, roomWeights)));
                    return into;
            }
            var doors = rng.Next(2, MaxDoors + 1);
            for (var i = 0; i < doors; i++)
            {
                var reward = i == 0 && context.StepsSinceMutation >= MutationPity ? RunReward.Mutation : PickReward(rng, context, into);
                if (reward == RunReward.None)
                    break;
                into.Add(new DoorOffer(next, reward, PickRoom(rng, roomsForRole, usedRooms, into, roomWeights)));
            }
            return into;
        }

        public static RunReward PickReward(Random rng, in RewardContext context, List<DoorOffer> taken)
        {
            var total = 0;
            for (var i = 0; i < CombatRewards.Length; i++)
                if (Allowed(CombatRewards[i], context, taken))
                    total += CombatWeights[i];
            if (total <= 0)
                return RunReward.None;
            var roll = rng.Next(total);
            for (var i = 0; i < CombatRewards.Length; i++)
            {
                if (!Allowed(CombatRewards[i], context, taken))
                    continue;
                roll -= CombatWeights[i];
                if (roll < 0)
                    return CombatRewards[i];
            }
            return RunReward.None;
        }

        private static bool Allowed(RunReward reward, in RewardContext context, List<DoorOffer> taken)
        {
            if (reward == RunReward.Evolution && context.EvolutionsOpen <= 0)
                return false;
            foreach (var offer in taken)
                if (offer.Reward == reward)
                    return false;
            return true;
        }

        public static int RoomWeight(IReadOnlyList<int> weights, int room) => weights == null || room >= weights.Count ? 1 : Math.Max(0, weights[room]);

        private static int PickRoom(Random rng, int count, ICollection<int> used, List<DoorOffer> taken, IReadOnlyList<int> weights)
        {
            if (count <= 0)
                return -1;
            for (var pass = 0; pass < 2; pass++)
            {
                var total = 0;
                for (var room = 0; room < count; room++)
                    if (Available(room, pass, count, used, taken))
                        total += RoomWeight(weights, room);
                if (total <= 0)
                    continue;
                var roll = rng.Next(total);
                for (var room = 0; room < count; room++)
                {
                    if (!Available(room, pass, count, used, taken))
                        continue;
                    roll -= RoomWeight(weights, room);
                    if (roll < 0)
                        return room;
                }
            }
            return rng.Next(count);
        }

        private static bool Available(int room, int pass, int count, ICollection<int> used, List<DoorOffer> taken)
        {
            if (pass == 0 && used != null && used.Contains(room))
                return false;
            if (count <= taken.Count)
                return true;
            foreach (var offer in taken)
                if (offer.Room == room)
                    return false;
            return true;
        }
    }
}
