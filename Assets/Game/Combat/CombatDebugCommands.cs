using TentacleVania.Combat.Damage;
using TentacleVania.Core;
using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.Combat
{
    public static class CombatDebugCommands
    {
        private static Health _lastTarget;
        private static DamageInfo _lastInfo;
        private static DamageResult _lastResult;
        private static int _hits;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            _hits = 0;
            _lastTarget = null;
            CombatEvents.HitLanded += OnHit;
            DebugMenu.Slider(DebugMenu.Combat, "Damage dealt (assist)", 0.1f, 3f, () => GameSettings.DamageDealtScale, v => GameSettings.DamageDealtScale = v);
            DebugMenu.Slider(DebugMenu.Combat, "Damage taken (assist)", 0f, 3f, () => GameSettings.DamageTakenScale, v => GameSettings.DamageTakenScale = v);
            DebugMenu.Button(DebugMenu.Combat, "Reset assist sliders", () =>
            {
                GameSettings.DamageDealtScale = 1f;
                GameSettings.DamageTakenScale = 1f;
            });
            DebugMenu.Value(DebugMenu.Combat, "Hits landed", () => _hits.ToString());
            DebugMenu.Value(DebugMenu.Combat, "Last hit", FormatLastHit);
            DebugMenu.Value(DebugMenu.Combat, "Last hit feedback", () => Feedback.HitFeedback.LastKind.ToString());
            DebugMenu.Toggle(DebugMenu.Combat, "Hit-stop", () => Core.Feedback.HitStop.Enabled, v => Core.Feedback.HitStop.Enabled = v);
            DebugMenu.Toggle(DebugMenu.Combat, "Rumble", () => GameSettings.Rumble, v => GameSettings.Rumble = v);
            DebugMenu.Toggle(DebugMenu.Combat, "Gore", () => GameSettings.Gore, v => GameSettings.Gore = v);
            DebugMenu.Toggle(DebugMenu.Combat, "Hit effects", () => Feedback.HitEffects.Enabled, v => Feedback.HitEffects.Enabled = v);
            DebugMenu.Value(DebugMenu.Combat, "Blood decals", () =>
                Feedback.HitEffects.Instance != null ? $"{Feedback.HitEffects.Instance.ActiveDecals} / {Feedback.HitEffects.Instance.DecalCapacity}" : "-");
            DebugMenu.Button(DebugMenu.Combat, "Clear blood decals", () =>
            {
                if (Feedback.HitEffects.Instance != null)
                    Feedback.HitEffects.Instance.ClearDecals();
            });
        }

        private static void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            _hits++;
            _lastTarget = target;
            _lastInfo = info;
            _lastResult = result;
        }

        private static string FormatLastHit()
        {
            if (_lastTarget == null)
                return "-";
            var armor = _lastResult.ArmorDamage > 0f ? $" +{_lastResult.ArmorDamage:F0} armor" : string.Empty;
            var stagger = _lastResult.StaggerDamage > 0f ? $" {_lastResult.StaggerDamage:F0} stagger" : string.Empty;
            return $"{_lastTarget.name}: {_lastResult.HealthDamage:F0} {_lastInfo.Type}{armor}{stagger}";
        }
    }
}
