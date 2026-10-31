using TentacleVania.Combat;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.World.Arenas;
using TentacleVania.World.BioNests;
using UnityEngine;

namespace TentacleVania.World
{
    public static class WorldDebugCommands
    {
        public const string Tab = "World";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Arena", () =>
            {
                var arena = ArenaEncounter.Active;
                if (arena == null)
                    return "none";
                var elapsed = arena.State == ArenaState.Cleared ? arena.ClearedAt - arena.StartedAt : Time.time - arena.StartedAt;
                return arena.State == ArenaState.Waiting
                    ? "waiting for the creature"
                    : $"{arena.State}, wave {arena.WaveNumber}/{arena.WaveCount}, {arena.AliveCount} alive, {elapsed:F0} s";
            });
            DebugMenu.Value(Tab, "Last nest", () => string.IsNullOrEmpty(Nests.LastNest) ? "none" : $"{Nests.LastNest} ({Nests.RespawnScene})");
            DebugMenu.Button(Tab, "Grow all nests", () =>
            {
                foreach (var nest in Object.FindObjectsByType<BioNest>())
                    nest.Grow();
            });
            DebugMenu.Button(Tab, "Rest at nearest nest", () =>
            {
                var nest = NearestNest();
                if (nest == null)
                    return;
                nest.Grow();
                nest.Rest();
            });
            DebugMenu.Button(Tab, "Teleport to nearest nest", () =>
            {
                var nest = NearestNest();
                var creature = CombatTargets.Creature;
                if (nest == null || creature == null)
                    return;
                var controller = creature.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = false;
                creature.transform.SetPositionAndRotation(nest.RespawnPoint.position, nest.RespawnPoint.rotation);
                if (controller != null)
                    controller.enabled = true;
            });
            DebugMenu.Button(Tab, "Start arena now", () =>
            {
                var arena = ArenaEncounter.Active;
                if (arena != null && arena.State == ArenaState.Waiting)
                    arena.Begin(Time.time);
            });
            DebugMenu.Button(Tab, "Skip wave (kill all)", () =>
            {
                var arena = ArenaEncounter.Active;
                if (arena != null)
                    arena.SkipWave();
            });
        }

        private static BioNest NearestNest()
        {
            var creature = CombatTargets.Creature;
            var origin = creature != null ? creature.transform.position : Vector3.zero;
            BioNest best = null;
            var bestDistance = float.MaxValue;
            foreach (var nest in Object.FindObjectsByType<BioNest>())
            {
                var distance = (nest.transform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = nest;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
