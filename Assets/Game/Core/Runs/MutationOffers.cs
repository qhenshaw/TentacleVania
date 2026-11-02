using System;
using System.Collections.Generic;
using TentacleVania.Core.Progression;

namespace TentacleVania.Core.Runs
{
    public static class MutationOffers
    {
        public const int Count = 3;
        public const int BaseWeight = 2;
        public const int SharedBuildWeight = 3;

        private static readonly int[] CommonRarity = { 3, 2, 1 };
        private static readonly int[] RareRarity = { 1, 2, 4 };

        public static int Weight(MutationBuild builds, IReadOnlyList<MutationBuild> picked)
        {
            var weight = BaseWeight;
            if (picked == null || builds == MutationBuild.None)
                return weight;
            foreach (var other in picked)
                if ((other & builds) != 0)
                    weight += SharedBuildWeight;
            return weight;
        }

        private static int WeightAt(IReadOnlyList<MutationBuild> candidates, IReadOnlyList<int> notches, bool rare, IReadOnlyList<MutationBuild> picked, int i) =>
            notches == null ? Weight(candidates[i], picked) : Weight(candidates[i], notches[i], rare, picked);

        public static int Rarity(int notches, bool rare) => (rare ? RareRarity : CommonRarity)[Math.Clamp(notches, 1, 3) - 1];

        public static int Weight(MutationBuild builds, int notches, bool rare, IReadOnlyList<MutationBuild> picked) => Weight(builds, picked) * Rarity(notches, rare);

        public static List<int> Pick(Random rng, IReadOnlyList<MutationBuild> candidates, IReadOnlyList<MutationBuild> picked, int count, List<int> into = null) =>
            Pick(rng, candidates, null, false, picked, count, into);

        public static List<int> Pick(Random rng, IReadOnlyList<MutationBuild> candidates, IReadOnlyList<int> notches, bool rare, IReadOnlyList<MutationBuild> picked, int count, List<int> into = null)
        {
            into ??= new List<int>(count);
            into.Clear();
            if (candidates == null)
                return into;
            count = Math.Min(count, candidates.Count);
            while (into.Count < count)
            {
                var total = 0;
                for (var i = 0; i < candidates.Count; i++)
                    if (!into.Contains(i))
                        total += WeightAt(candidates, notches, rare, picked, i);
                var roll = rng.Next(total);
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (into.Contains(i))
                        continue;
                    roll -= WeightAt(candidates, notches, rare, picked, i);
                    if (roll < 0)
                    {
                        into.Add(i);
                        break;
                    }
                }
            }
            return into;
        }
    }
}
