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
