using TentacleVania.Core.Debugging;
using TentacleVania.World.Arenas;
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
    }
}
