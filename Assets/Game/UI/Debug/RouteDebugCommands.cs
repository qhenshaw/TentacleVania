using System.Collections.Generic;
using TentacleVania.Bosses;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Enemies.Data;
using TentacleVania.World.Map;
using UnityEngine;

namespace TentacleVania.UI.Debug
{
    public static class RouteDebugCommands
    {
        private const string Tab = "World";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Progression route", () =>
            {
                var result = Solve(out _);
                var next = NextStep(result);
                return (result.Complete ? "complete" : "BROKEN: " + string.Join(", ", result.UnreachedSectors) + " " + string.Join(", ", result.UndefeatedBosses))
                    + " | next: " + (next ?? "done");
            });
            DebugMenu.Button(Tab, "Route: defeat next boss (grants its rewards)", () => Advance(1));
            DebugMenu.Button(Tab, "Route: defeat every boss but Crane", () => Advance(int.MaxValue));
        }

        private static ProgressionSolver.Result Solve(out Dictionary<string, BossDefinition> definitions)
        {
            definitions = new();
            var graph = WorldGraph.Instance;
            var list = Resources.Load<EnemySpawnList>(BossDebugCommands.BossListResource);
            if (graph == null || list == null)
                return new ProgressionSolver.Result();
            var gates = new List<ProgressionSolver.Gate>();
            foreach (var connection in graph.Connections)
                gates.Add(new(connection.From, connection.To, connection.Requires));
            var sectors = new List<string>();
            foreach (var sector in graph.Sectors)
                sectors.Add(sector.Id);
            var bosses = new List<ProgressionSolver.BossNode>();
            foreach (var prefab in list.Prefabs)
            {
                if (prefab is not BossEnemy enemy || enemy.Boss == null)
                    continue;
                var boss = enemy.Boss;
                if (string.IsNullOrEmpty(boss.Id) || !boss.Id.Contains("/") || boss.Id.StartsWith("Debug/") || definitions.ContainsKey(boss.Id))
                    continue;
                definitions[boss.Id] = boss;
                var rewards = new List<string>();
                if (boss.RewardAbility != null)
                    rewards.Add(boss.RewardAbility.name);
                if (boss.SecondReward != null)
                    rewards.Add(boss.SecondReward.name);
                bosses.Add(new(boss.Id, rewards.ToArray(), boss.ArenaRequires));
            }
            return ProgressionSolver.Solve("SH", sectors, gates, bosses);
        }

        private static string NextStep(ProgressionSolver.Result result)
        {
            foreach (var id in result.Order)
                if (!BossProgress.IsDefeated(id))
                    return id;
            return null;
        }

        private static void Advance(int steps)
        {
            var result = Solve(out var definitions);
            foreach (var id in result.Order)
            {
                if (steps <= 0 || id == "MV/Crane")
                    return;
                if (BossProgress.IsDefeated(id))
                    continue;
                var boss = definitions[id];
                if (!string.IsNullOrEmpty(boss.RewardMutation))
                    Collectibles.Grant(PickupKind.Mutation, boss.RewardMutation, 1);
                BossProgress.Defeat(id, ContentCatalog.IdOf(boss.RewardAbility), ContentCatalog.IdOf(boss.SecondReward));
                steps--;
            }
        }
    }
}
