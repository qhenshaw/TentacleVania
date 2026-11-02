using System;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public static class Stats
    {
        public static event Action Changed;

        public static int Brutality => InRun ? StatRules.StartValue + Meta.Brutality : Mathf.Max(StatRules.StartValue, Build.Brutality);
        public static int Ferocity => InRun ? StatRules.StartValue + Meta.Ferocity : Mathf.Max(StatRules.StartValue, Build.Ferocity);
        public static int Tenacity => InRun ? StatRules.StartValue + Meta.Tenacity : Mathf.Max(StatRules.StartValue, Build.Tenacity);
        public static int Cunning => InRun ? StatRules.StartValue + Meta.Cunning : Mathf.Max(StatRules.StartValue, Build.Cunning);
        public static int TotalLevel => Brutality + Ferocity + Tenacity + Cunning;
        public static int Level => StatRules.DisplayLevel(TotalLevel);
        public static int NextLevelCost => StatRules.LevelCost(Level);
        public static int BiomassSpent => Build.BiomassSpent;
        public static int SizeStage => Runs.RunSession.Active ? Runs.RunSession.SizeStage : StatRules.SizeStage(Build.BiomassSpent);

        public static float WeaponDamage => StatRules.WeaponDamage(Brutality);
        public static float WeaponStagger => StatRules.WeaponStagger(Brutality);
        public static float AttackSpeed => StatRules.AttackSpeed(Ferocity);
        public static float DashRecovery => StatRules.DashRecovery(Ferocity);
        public static float MaxHealthBonus => StatRules.MaxHealthBonus(Tenacity);
        public static float DamageTaken => StatRules.DamageTaken(Tenacity);
        public static float ChargeGain => StatRules.ChargeGain(Cunning);
        public static float AbilityDamage => StatRules.AbilityDamage(Cunning);
        public static float StatusPotency => StatRules.StatusPotency(Cunning);

        private static BuildState Build => SaveSystem.Data.Build;
        private static MetaState Meta => SaveSystem.Data.Meta;
        private static bool InRun => Runs.RunSession.Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook() => SaveSystem.Loaded += _ => Changed?.Invoke();

        public static int Get(StatKind kind) => kind switch
        {
            StatKind.Brutality => Brutality,
            StatKind.Ferocity => Ferocity,
            StatKind.Tenacity => Tenacity,
            _ => Cunning,
        };

        public static bool TryLevelUp(StatKind kind)
        {
            if (Get(kind) >= StatRules.MaxValue)
                return false;
            var cost = NextLevelCost;
            if (!Biomass.Spend(cost))
                return false;
            Build.BiomassSpent += cost;
            Set(kind, Get(kind) + 1);
            Autosave.Request(AutosaveReason.Purchase);
            return true;
        }

        public static bool Respec(int brutality, int ferocity, int tenacity, int cunning)
        {
            if (!StatRules.IsValidRespec(brutality, ferocity, tenacity, cunning, TotalLevel))
                return false;
            Build.Brutality = brutality;
            Build.Ferocity = ferocity;
            Build.Tenacity = tenacity;
            Build.Cunning = cunning;
            Commit();
            Autosave.Request(AutosaveReason.Purchase);
            return true;
        }

        public static void NotifyChanged() => Changed?.Invoke();

        public static void SetBiomassSpent(int value)
        {
            Build.BiomassSpent = Mathf.Max(0, value);
            Changed?.Invoke();
        }

        public static void Set(StatKind kind, int value)
        {
            value = Mathf.Clamp(value, StatRules.StartValue, StatRules.MaxValue);
            switch (kind)
            {
                case StatKind.Brutality: Build.Brutality = value; break;
                case StatKind.Ferocity: Build.Ferocity = value; break;
                case StatKind.Tenacity: Build.Tenacity = value; break;
                default: Build.Cunning = value; break;
            }
            Commit();
        }

        private static void Commit()
        {
            Build.Level = Level;
            Changed?.Invoke();
        }
    }
}
