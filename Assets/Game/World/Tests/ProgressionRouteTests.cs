using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Bosses;
using TentacleVania.Core.Progression;
using TentacleVania.World.Map;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Tests
{
    public sealed class ProgressionRouteTests
    {
        private static ProgressionSolver.Result Solve()
        {
            var graph = Resources.Load<WorldGraph>(WorldGraph.ResourcePath);
            Assert.That(graph, Is.Not.Null);
            var gates = new List<ProgressionSolver.Gate>();
            foreach (var connection in graph.Connections)
                gates.Add(new(connection.From, connection.To, connection.Requires));
            var sectors = new List<string>();
            foreach (var sector in graph.Sectors)
                sectors.Add(sector.Id);
            var bosses = new List<ProgressionSolver.BossNode>();
            foreach (var guid in AssetDatabase.FindAssets("t:BossDefinition", new[] { "Assets/Game/Bosses" }))
            {
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (boss == null || string.IsNullOrEmpty(boss.Id) || !boss.Id.Contains("/") || boss.Id.StartsWith("Debug/"))
                    continue;
                var rewards = new List<string>();
                if (boss.RewardAbility != null)
                    rewards.Add(boss.RewardAbility.name);
                if (boss.SecondReward != null)
                    rewards.Add(boss.SecondReward.name);
                bosses.Add(new(boss.Id, rewards.ToArray(), boss.ArenaRequires));
            }
            return ProgressionSolver.Solve("SH", sectors, gates, bosses);
        }

        [Test]
        public void EverySectorAndBossIsReachableInOrder()
        {
            var result = Solve();
            Assert.That(result.UnreachedSectors, Is.Empty, "unreached sectors");
            Assert.That(result.UndefeatedBosses, Is.Empty, "unreachable bosses");
            Debug.Log("Progression route: " + string.Join(" > ", result.Order));
        }

        [Test]
        public void EveryGateAbilityIsGrantedSomewhere()
        {
            var result = Solve();
            var graph = Resources.Load<WorldGraph>(WorldGraph.ResourcePath);
            foreach (var connection in graph.Connections)
                foreach (var requirement in connection.Requires)
                    if (!string.IsNullOrEmpty(requirement) && !requirement.StartsWith(ProgressionSolver.BossPrefix))
                        Assert.That(result.Abilities, Does.Contain(requirement), $"{connection.From}-{connection.To} needs {requirement}");
        }
    }
}
