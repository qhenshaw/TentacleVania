using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat
{
    public static class CombatEvents
    {
        public static event DamageHandler HitLanded;
        public static event System.Action<Health> StaggerBroken;
        public static event System.Action<Consumable> ConsumeStarted;
        public static event System.Action<Consumable> ConsumeBitten;
        public static event System.Action<Consumable> Consumed;
        public static event System.Action<Health> Died;

        internal static void RaiseHit(Health target, in DamageInfo info, in DamageResult result) => HitLanded?.Invoke(target, info, result);

        public static void RaiseStaggerBroken(Health target) => StaggerBroken?.Invoke(target);

        internal static void RaiseConsumeStarted(Consumable consumable) => ConsumeStarted?.Invoke(consumable);

        internal static void RaiseConsumeBitten(Consumable consumable) => ConsumeBitten?.Invoke(consumable);

        internal static void RaiseConsumed(Consumable consumable) => Consumed?.Invoke(consumable);

        internal static void RaiseDied(Health health) => Died?.Invoke(health);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            HitLanded = null;
            StaggerBroken = null;
            ConsumeStarted = null;
            ConsumeBitten = null;
            Consumed = null;
            Died = null;
        }
    }
}
