using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat
{
    public static class CombatEvents
    {
        public static event DamageHandler HitLanded;

        internal static void RaiseHit(Health target, in DamageInfo info, in DamageResult result) => HitLanded?.Invoke(target, info, result);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => HitLanded = null;
    }
}
