using System;

namespace TentacleVania.Combat.Feedback
{
    [Flags]
    public enum HitEffect
    {
        None = 0,
        Impact = 1,
        Spray = 2,
        Decal = 4,
    }

    public static class HitEffectRules
    {
        private const float Tolerance = 1e-3f;

        public static HitEffect Select(bool targetIsCreature, HitFeedbackKind kind, float healthDamage, float maxHealth, bool killed, bool gore, float creatureBigHitFraction)
        {
            var effects = HitEffect.Impact;
            if (!gore || healthDamage <= 0f)
                return effects;
            effects |= HitEffect.Spray;
            if (killed || IsBigHit(targetIsCreature, kind, healthDamage, maxHealth, creatureBigHitFraction))
                effects |= HitEffect.Decal;
            return effects;
        }

        public static bool IsBigHit(bool targetIsCreature, HitFeedbackKind kind, float healthDamage, float maxHealth, float creatureBigHitFraction)
        {
            if (targetIsCreature)
                return maxHealth > 0f && healthDamage >= maxHealth * creatureBigHitFraction - Tolerance;
            return kind is HitFeedbackKind.Heavy or HitFeedbackKind.Finisher or HitFeedbackKind.DashAttack;
        }
    }
}
