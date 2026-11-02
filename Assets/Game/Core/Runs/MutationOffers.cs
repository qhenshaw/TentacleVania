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

        public static List<int> Pick(Random rng, IReadOnlyList<MutationBuild> candidates, IReadOnlyList<MutationBuild> picked, int count, List<int> into = null)
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
                        total += Weight(candidates[i], picked);
                var roll = rng.Next(total);
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (into.Contains(i))
                        continue;
                    roll -= Weight(candidates[i], picked);
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
