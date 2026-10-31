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
            DebugMenu.Toggle(DebugMenu.Camera, "Wall fade", () => Core.Rendering.OcclusionFader.Enabled, v => Core.Rendering.OcclusionFader.Enabled = v);
            DebugMenu.Value(DebugMenu.Camera, "Faded walls", () =>
            {
                var fader = Object.FindAnyObjectByType<Core.Rendering.OcclusionFader>();
                return fader != null ? fader.FadedCount.ToString() : "no fader";
            });
            DebugMenu.Value(DebugMenu.Camera, "Room bounds", () =>
            {
                var bounds = CameraBounds.Active;
                return bounds != null ? $"{bounds.Size.x:F0} x {bounds.Size.y:F0} m" : "none";
            });
        }
    }
}
