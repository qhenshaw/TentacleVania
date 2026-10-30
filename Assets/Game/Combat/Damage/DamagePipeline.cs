using UnityEngine;

namespace TentacleVania.Combat.Damage
{
    public struct HealthPool
    {
        public float Health;
        public float MaxHealth;
        public float Armor;
        public float MaxArmor;
        public float ProtectedBelow;
        public float ProtectedScale;

        public HealthPool(float maxHealth, float maxArmor = 0f)
        {
            Health = MaxHealth = maxHealth;
            Armor = MaxArmor = maxArmor;
            ProtectedBelow = 0f;
            ProtectedScale = 1f;
        }

        public bool IsAlive => Health > 0f;
        public float ArmorFraction => MaxArmor > 0f ? Armor / MaxArmor : 0f;
    }

    public static class DamagePipeline
    {
        public const float MaxArmorReduction = 0.3f;

        public static DamageResult Resolve(in DamageInfo info, Team targetTeam, ref HealthPool pool, ModifierStack outgoing, ModifierStack incoming,
            float dealtScale = 1f, float takenScale = 1f)
        {
            if (!pool.IsAlive || !TeamRules.CanDamage(info.SourceTeam, targetTeam))
                return new(DamageOutcome.Ignored);

            var context = new DamageContext(info, targetTeam);
            outgoing?.Apply(ref context);
            if (!context.Cancelled)
                incoming?.Apply(ref context);
            if (context.Cancelled)
                return new(DamageOutcome.Blocked);

            var global = 1f;
            if (info.SourceTeam == Team.Creature)
                global *= dealtScale;
            if (targetTeam == Team.Creature)
                global *= takenScale;

            var amount = Mathf.Max(0f, info.Amount * context.HealthScale * global);
            var armorDamage = 0f;
            if (pool.MaxArmor > 0f)
            {
                amount *= 1f - MaxArmorReduction * pool.ArmorFraction;
                armorDamage = Mathf.Min(pool.Armor, Mathf.Max(0f, info.Amount * info.ArmorDamageScale * context.ArmorScale * global));
                pool.Armor -= armorDamage;
            }

            if (pool.ProtectedBelow > 0f && pool.Health - amount < pool.MaxHealth * pool.ProtectedBelow)
                amount *= pool.ProtectedScale;

            var healthDamage = Mathf.Min(pool.Health, amount);
            pool.Health -= healthDamage;
            var stagger = Mathf.Max(0f, info.Stagger * context.StaggerScale);
            return new(DamageOutcome.Applied, healthDamage, armorDamage, stagger, !pool.IsAlive);
        }
    }
}
