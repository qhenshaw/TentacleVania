using UnityEngine;

namespace TentacleVania.Core.Progression
{
    public enum StatKind
    {
        Brutality,
        Ferocity,
        Tenacity,
        Cunning,
    }

    public static class StatRules
    {
        public const int StartValue = 1;
        public const int MaxValue = 60;
        public const int StartTotal = StartValue * 4;

        public const float BrutalityDamage = 0.03f;
        public const float BrutalityStagger = 0.03f;
        public const float FerocityAttackSpeed = 0.015f;
        public const float FerocityDashRecovery = 0.015f;
        public const float TenacityMaxHealth = 8f;
        public const float TenacityResistance = 0.005f;
        public const float CunningChargeGain = 0.02f;
        public const float CunningAbilityDamage = 0.02f;
        public const float CunningStatusPotency = 0.03f;

        private const float MinDashRecovery = 0.25f;
        private const float MinDamageTaken = 0.5f;

        public static int DisplayLevel(int totalLevel) => Mathf.Max(1, totalLevel - StartTotal + 1);

        public static int LevelCost(int level) => Mathf.RoundToInt(60f + 12f * Mathf.Pow(Mathf.Max(0, level), 1.4f));

        public static float EffectivePoints(int value)
        {
            value = Mathf.Clamp(value, 0, MaxValue);
            return Mathf.Min(value, 20) + 0.5f * Mathf.Clamp(value - 20, 0, 20) + 0.25f * Mathf.Clamp(value - 40, 0, 20);
        }

        public static float BonusPoints(int value) => EffectivePoints(value) - EffectivePoints(StartValue);

        public static float WeaponDamage(int brutality) => 1f + BrutalityDamage * BonusPoints(brutality);
        public static float WeaponStagger(int brutality) => 1f + BrutalityStagger * BonusPoints(brutality);
        public static float AttackSpeed(int ferocity) => 1f + FerocityAttackSpeed * BonusPoints(ferocity);
        public static float DashRecovery(int ferocity) => Mathf.Max(MinDashRecovery, 1f - FerocityDashRecovery * BonusPoints(ferocity));
        public static float MaxHealthBonus(int tenacity) => TenacityMaxHealth * BonusPoints(tenacity);
        public static float DamageTaken(int tenacity) => Mathf.Max(MinDamageTaken, 1f - TenacityResistance * BonusPoints(tenacity));
        public static float ChargeGain(int cunning) => 1f + CunningChargeGain * BonusPoints(cunning);
        public static float AbilityDamage(int cunning) => 1f + CunningAbilityDamage * BonusPoints(cunning);
        public static float StatusPotency(int cunning) => 1f + CunningStatusPotency * BonusPoints(cunning);

        public static int SizeStage(int biomassSpent) => biomassSpent >= 30000 ? 3 : biomassSpent >= 10000 ? 2 : biomassSpent >= 2500 ? 1 : 0;

        public static readonly int[] SizeStageThresholds = { 0, 2500, 10000, 30000 };

        public static float SizeHealthScale(int stage) => stage switch { 1 => 1.2f, 2 => 1.4f, >= 3 => 1.7f, _ => 1f };

        public static bool IsValidRespec(int brutality, int ferocity, int tenacity, int cunning, int total) =>
            brutality >= StartValue && ferocity >= StartValue && tenacity >= StartValue && cunning >= StartValue &&
            brutality <= MaxValue && ferocity <= MaxValue && tenacity <= MaxValue && cunning <= MaxValue &&
            brutality + ferocity + tenacity + cunning == total;
    }
}
