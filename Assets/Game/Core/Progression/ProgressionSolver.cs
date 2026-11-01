using System;
using System.Collections.Generic;

namespace TentacleVania.Core.Progression
{
    public static class ProgressionSolver
    {
        public const string BossPrefix = "boss:";

        public readonly struct Gate
        {
            public Gate(string from, string to, string[] requires)
            {
                From = from;
                To = to;
                Requires = requires ?? Array.Empty<string>();
            }

            public string From { get; }
            public string To { get; }
            public string[] Requires { get; }
        }

        public readonly struct BossNode
        {
            public BossNode(string id, string[] rewards, string[] requires = null)
            {
                Id = id;
                Rewards = rewards ?? Array.Empty<string>();
                Requires = requires ?? Array.Empty<string>();
            }

            public string Id { get; }
            public string Sector => Id.Contains("/") ? Id.Substring(0, Id.IndexOf('/')) : Id;
            public string[] Rewards { get; }
            public string[] Requires { get; }
        }

        public sealed class Result
        {
            public readonly List<string> Order = new();
            public readonly HashSet<string> Sectors = new();
            public readonly HashSet<string> Abilities = new();
            public readonly List<string> UnreachedSectors = new();
            public readonly List<string> UndefeatedBosses = new();
            public bool Complete => UnreachedSectors.Count == 0 && UndefeatedBosses.Count == 0;
        }

        public static Result Solve(string start, IEnumerable<string> sectors, IReadOnlyList<Gate> gates, IReadOnlyList<BossNode> bosses, IEnumerable<string> startAbilities = null)
        {
            var result = new Result();
            var defeated = new HashSet<string>();
            result.Sectors.Add(start);
            if (startAbilities != null)
                foreach (var ability in startAbilities)
                    result.Abilities.Add(ability);
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var gate in gates)
                {
                    var fromIn = result.Sectors.Contains(gate.From);
                    var toIn = result.Sectors.Contains(gate.To);
                    if (fromIn == toIn || !Met(gate.Requires, result.Abilities, defeated))
                        continue;
                    result.Sectors.Add(fromIn ? gate.To : gate.From);
                    changed = true;
                }
                if (changed)
                    continue;
                foreach (var boss in bosses)
                {
                    if (defeated.Contains(boss.Id) || !result.Sectors.Contains(boss.Sector) || !Met(boss.Requires, result.Abilities, defeated))
                        continue;
                    defeated.Add(boss.Id);
                    result.Order.Add(boss.Id);
                    foreach (var reward in boss.Rewards)
                        if (!string.IsNullOrEmpty(reward))
                            result.Abilities.Add(reward);
                    changed = true;
                    break;
                }
            }
            foreach (var sector in sectors)
                if (!result.Sectors.Contains(sector))
                    result.UnreachedSectors.Add(sector);
            foreach (var boss in bosses)
                if (!defeated.Contains(boss.Id))
                    result.UndefeatedBosses.Add(boss.Id);
            return result;
        }

        private static bool Met(string[] requires, HashSet<string> abilities, HashSet<string> defeated)
        {
            foreach (var requirement in requires)
            {
                if (string.IsNullOrEmpty(requirement))
                    continue;
                var met = requirement.StartsWith(BossPrefix, StringComparison.Ordinal)
                    ? defeated.Contains(requirement.Substring(BossPrefix.Length))
                    : abilities.Contains(requirement);
                if (!met)
                    return false;
            }
            return true;
        }
    }
}
