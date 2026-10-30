using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat
{
    public static class CombatEvents
    {
        public static event DamageHandler HitLanded;
        public static event System.Action<Health> StaggerBroken;
        public static event System.Action<Consumable> Consumed;

        internal static void RaiseHit(Health target, in DamageInfo info, in DamageResult result) => HitLanded?.Invoke(target, info, result);

        public static void RaiseStaggerBroken(Health target) => StaggerBroken?.Invoke(target);

        internal static void RaiseConsumed(Consumable consumable) => Consumed?.Invoke(consumable);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            HitLanded = null;
            StaggerBroken = null;
            Consumed = null;
        }
    }
}
