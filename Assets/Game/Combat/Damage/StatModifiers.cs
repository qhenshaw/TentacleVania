using TentacleVania.Core.Progression;

namespace TentacleVania.Combat.Damage
{
    public sealed class OutgoingStatModifier : IDamageModifier
    {
        public int Order => 0;

        public void Modify(ref DamageContext context)
        {
            if ((context.Info.Flags & HitFlags.Ability) != 0)
            {
                var ability = Stats.AbilityDamage;
                context.HealthScale *= ability;
                context.ArmorScale *= ability;
                return;
            }
            var damage = Stats.WeaponDamage;
            context.HealthScale *= damage;
            context.ArmorScale *= damage;
            context.StaggerScale *= Stats.WeaponStagger;
        }
    }

    public sealed class IncomingStatModifier : IDamageModifier
    {
        public int Order => 0;

        public void Modify(ref DamageContext context) => context.HealthScale *= Stats.DamageTaken;
    }
}
