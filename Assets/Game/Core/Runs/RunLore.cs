using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Runs
{
    public static class RunLore
    {
        public const float Chance = 0.25f;

        public static int Pick(Random rng, IReadOnlyList<bool> collected, float chance = Chance)
        {
            if (collected == null || rng.NextDouble() >= chance)
                return -1;
            var open = 0;
            foreach (var has in collected)
                if (!has)
                    open++;
            if (open == 0)
                return -1;
            var choice = rng.Next(open);
            for (var i = 0; i < collected.Count; i++)
            {
                if (collected[i])
                    continue;
                if (choice-- == 0)
                    return i;
            }
            return -1;
        }
    }
}
