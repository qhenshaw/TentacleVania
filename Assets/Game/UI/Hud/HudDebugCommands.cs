using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.UI.Hud
{
    public static class HudDebugCommands
    {
        private const string Tab = "HUD";

        private static readonly object DebugCutscene = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Toggle(Tab, "Show HUD", () => CombatHud.Visible, v => CombatHud.Visible = v);
            DebugMenu.Toggle(Tab, "Always show enemy bars", () => CombatHud.AlwaysShowEnemyBars, v => CombatHud.AlwaysShowEnemyBars = v);
            DebugMenu.Toggle(Tab, "Pseudo-locale (long accented text)", () => UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale is UnityEngine.Localization.Pseudo.PseudoLocale, SetPseudo);
            DebugMenu.Toggle(Tab, "Tutorial prompts", () => Core.Progression.Tutorials.Enabled, v => Core.Progression.Tutorials.Enabled = v);
            DebugMenu.Value(Tab, "Tutorials seen", () => string.Join(", ", Core.Saves.SaveSystem.Data.World.SeenTutorials));
            DebugMenu.Button(Tab, "Reset tutorials", Core.Progression.Tutorials.ResetAll);
            DebugMenu.Toggle(Tab, "Letterbox (test cutscene)", () => Core.Cutscene.Active, v => { if (v) Core.Cutscene.Begin(DebugCutscene); else Core.Cutscene.End(DebugCutscene); });
            DebugMenu.Button(Tab, "Open settings", () =>
            {
                if (Settings.SettingsScreen.Instance != null)
                    Settings.SettingsScreen.Instance.Open();
            });
            DebugMenu.Choice(Tab, "Low health screen", new[] { "Auto", "Off", "Light", "Heavy" }, index => LowHealthScreenView.ForceStep = index - 1);
            DebugMenu.Toggle(Tab, "Low health effect", () => Core.Rendering.LowHealthTendrilsFeature.Enabled, v => Core.Rendering.LowHealthTendrilsFeature.Enabled = v);
        }

        private static void SetPseudo(bool on)
        {
            var settings = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales;
            foreach (var locale in settings.Locales)
            {
                var pseudo = locale is UnityEngine.Localization.Pseudo.PseudoLocale;
                if (pseudo == on)
                {
                    UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = locale;
                    return;
                }
            }
        }
    }
}
