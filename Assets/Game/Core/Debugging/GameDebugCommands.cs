using TentacleVania.Core.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TentacleVania.Core.Debugging
{
    public static class GameDebugCommands
    {
        private static float _smoothedDelta = 1f / 60f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(DebugMenu.Game, "Version", () => GameVersion.Text);
            DebugMenu.Value(DebugMenu.Game, "FPS", () =>
            {
                _smoothedDelta = Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.2f);
                return $"{1f / _smoothedDelta:F0} ({_smoothedDelta * 1000f:F1} ms)";
            });
            DebugMenu.Value(DebugMenu.Game, "Scene", () => SceneManager.GetActiveScene().name);
            DebugMenu.Toggle(DebugMenu.Game, "Dynamic resolution", () => Rendering.DynamicResolution.Enabled, v => Rendering.DynamicResolution.Enabled = v);
            DebugMenu.Value(DebugMenu.Game, "Render scale", () => $"{Rendering.DynamicResolution.Scale:P0}, GPU {Rendering.DynamicResolution.LastGpuMs:F1} ms (players only)");
            DebugMenu.Slider(DebugMenu.Game, "Time scale", 0.05f, 2f, () => Time.timeScale, v => Time.timeScale = v);
            DebugMenu.Button(DebugMenu.Game, "Normal speed", () => Time.timeScale = 1f);
            DebugMenu.Button(DebugMenu.Game, "Load CombatArena (test)", () =>
            {
                Time.timeScale = 1f;
                SceneFlow.Load(SceneFlow.TestArena);
            });
            DebugMenu.Button(DebugMenu.Game, "Restart scene", () =>
            {
                Time.timeScale = 1f;
                SceneFlow.Reload();
            });
        }
    }
}
