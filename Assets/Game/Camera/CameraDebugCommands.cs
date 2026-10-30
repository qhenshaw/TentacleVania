using TentacleVania.Core;
using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.Camera
{
    public static class CameraDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Toggle(DebugMenu.Camera, "Screen shake", () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
            DebugMenu.Toggle(DebugMenu.Camera, "Flashes", () => GameSettings.Flashes, v => GameSettings.Flashes = v);
            DebugMenu.Value(DebugMenu.Camera, "Room bounds", () =>
            {
                var bounds = CameraBounds.Active;
                return bounds != null ? $"{bounds.Size.x:F0} x {bounds.Size.y:F0} m" : "none";
            });
        }
    }
}
