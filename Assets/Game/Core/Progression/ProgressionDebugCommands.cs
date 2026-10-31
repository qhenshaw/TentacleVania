using TentacleVania.Core.Debugging;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class ProgressionDebugCommands
    {
        public const string Tab = "Progression";

        private static readonly string[] StatNames = { "Brutality", "Ferocity", "Tenacity", "Cunning" };
        private static StatKind _stat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Stats", () => $"B{Stats.Brutality} F{Stats.Ferocity} T{Stats.Tenacity} C{Stats.Cunning}, level {Stats.Level}, next {Stats.NextLevelCost}");
            DebugMenu.Value(Tab, "Multipliers", () =>
                $"dmg x{Stats.WeaponDamage:F2}, speed x{Stats.AttackSpeed:F2}, dash rec x{Stats.DashRecovery:F2}, +{Stats.MaxHealthBonus:F0} HP, taken x{Stats.DamageTaken:F3}, charge x{Stats.ChargeGain:F2}, ability x{Stats.AbilityDamage:F2}");
            DebugMenu.Value(Tab, "Biomass spent", () => $"{Stats.BiomassSpent} (size stage {Stats.SizeStage + 1})");
            DebugMenu.Choice(Tab, "Stat", StatNames, i => _stat = (StatKind)i);
            DebugMenu.Button(Tab, "Level up (spend biomass)", () =>
            {
                if (!Stats.TryLevelUp(_stat))
                    Debug.LogWarning($"Not enough biomass: {Stats.NextLevelCost} needed, {Biomass.Carried} carried");
            });
            DebugMenu.Button(Tab, "+1 (free)", () => Stats.Set(_stat, Stats.Get(_stat) + 1));
            DebugMenu.Button(Tab, "+10 (free)", () => Stats.Set(_stat, Stats.Get(_stat) + 10));
            DebugMenu.Button(Tab, "Reset stats to 1", () =>
            {
                foreach (StatKind kind in System.Enum.GetValues(typeof(StatKind)))
                    Stats.Set(kind, StatRules.StartValue);
            });
        }
    }
}
