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
        }
    }
}
