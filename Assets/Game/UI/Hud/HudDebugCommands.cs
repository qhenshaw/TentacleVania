using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.UI.Hud
{
    public static class HudDebugCommands
    {
        private const string Tab = "HUD";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Toggle(Tab, "Show HUD", () => CombatHud.Visible, v => CombatHud.Visible = v);
            DebugMenu.Toggle(Tab, "Always show enemy bars", () => CombatHud.AlwaysShowEnemyBars, v => CombatHud.AlwaysShowEnemyBars = v);
            DebugMenu.Button(Tab, "Open settings", () =>
            {
                if (Settings.SettingsScreen.Instance != null)
                    Settings.SettingsScreen.Instance.Open();
            });
            DebugMenu.Choice(Tab, "Low health screen", new[] { "Auto", "Off", "Light", "Heavy" }, index => LowHealthScreenView.ForceStep = index - 1);
            DebugMenu.Toggle(Tab, "Low health effect", () => Core.Rendering.LowHealthTendrilsFeature.Enabled, v => Core.Rendering.LowHealthTendrilsFeature.Enabled = v);
        }
    }
}
