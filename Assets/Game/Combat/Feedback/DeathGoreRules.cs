using System;

namespace TentacleVania.Combat.Feedback
{
    [Flags]
    public enum DeathEffect
    {
        None = 0,
        Gibs = 1,
        Spray = 2,
        Decal = 4,
        Smoke = 8,
        Machine = 16,
    }

    public static class DeathGoreRules
    {
        private const DeathEffect Burst = DeathEffect.Gibs | DeathEffect.Spray | DeathEffect.Decal;

        public static DeathEffect OnKill(bool gore, bool bleeds)
        {
            if (!gore)
                return DeathEffect.Smoke;
            return bleeds ? Burst : Burst | DeathEffect.Machine;
        }

        public static DeathEffect OnBite(bool gore, bool bleeds, bool coreBleeds)
        {
            if (!gore)
                return DeathEffect.None;
            return bleeds || coreBleeds ? Burst : Burst | DeathEffect.Machine;
        }
    }
}
