using System;

namespace TentacleVania.Combat.Damage
{
    public enum DamageType
    {
        Physical,
        Electric,
        Fire,
        Acid,
        Radiation,
    }

    public enum Team
    {
        Creature,
        Site,
        Environment,
    }

    [Flags]
    public enum HitFlags
    {
        None = 0,
        Heavy = 1 << 0,
        Finisher = 1 << 1,
        DashAttack = 1 << 2,
        Hazard = 1 << 3,
    }

    public static class TeamRules
    {
        public static bool CanDamage(Team source, Team target) => source == Team.Environment || source != target;
    }
}
