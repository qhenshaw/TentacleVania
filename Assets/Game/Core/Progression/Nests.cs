using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class Nests
    {
        public static event Action<string> Grown;
        public static event Action<string> Rested;
        public static event Action<string> MenuRequested;

        public static string LastNest => SaveSystem.Data.Progress.LastNest;
        public static string RespawnScene => SaveSystem.Data.Progress.LastNestScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Grown = null;
            Rested = null;
            MenuRequested = null;
        }

        public static bool IsGrown(string id) => SaveSystem.Data.World.Has(WorldFlag.GrownNest, id);

        public static bool Grow(string id)
        {
            if (!SaveSystem.Data.World.Set(WorldFlag.GrownNest, id))
                return false;
            Grown?.Invoke(id);
            return true;
        }

        public static void Rest(string id, string scene, Vector3 respawnPosition, float respawnYaw)
        {
            if (!SetRespawn(id, scene, respawnPosition, respawnYaw))
                return;
            Rested?.Invoke(id);
            MenuRequested?.Invoke(id);
        }

        public static bool SetRespawn(string id, string scene, Vector3 respawnPosition, float respawnYaw)
        {
            if (!IsGrown(id))
                return false;
            var progress = SaveSystem.Data.Progress;
            progress.LastNest = id;
            progress.LastNestScene = scene;
            progress.RespawnPosition = respawnPosition;
            progress.RespawnYaw = respawnYaw;
            return true;
        }

        public static bool TryGetRespawn(string scene, out Vector3 position, out float yaw)
        {
            var progress = SaveSystem.Data.Progress;
            position = progress.RespawnPosition;
            yaw = progress.RespawnYaw;
            return !string.IsNullOrEmpty(progress.LastNest) && progress.LastNestScene == scene;
        }

        public static bool StaysCleared(bool bossArena) => bossArena && !Runs.RunSession.Active;
    }
}
