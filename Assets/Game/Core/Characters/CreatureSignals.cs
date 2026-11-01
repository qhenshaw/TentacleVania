using System;
using UnityEngine;

namespace TentacleVania.Core.Characters
{
    public static class CreatureSignals
    {
        public static event Action HeavyAttack;
        public static event Action Lockdown;
        public static event Action<Vector3> ChargedHeavy;

        public static void RaiseHeavyAttack() => HeavyAttack?.Invoke();

        public static void RaiseLockdown() => Lockdown?.Invoke();

        public static void RaiseChargedHeavy(Vector3 forward) => ChargedHeavy?.Invoke(forward);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HeavyAttack = null;
            Lockdown = null;
            ChargedHeavy = null;
        }
    }
}
