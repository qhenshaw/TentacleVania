using System;
using UnityEngine;

namespace TentacleVania.Core.Characters
{
    public static class CreatureSignals
    {
        public static event Action HeavyAttack;
        public static event Action Lockdown;

        public static void RaiseHeavyAttack() => HeavyAttack?.Invoke();

        public static void RaiseLockdown() => Lockdown?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HeavyAttack = null;
            Lockdown = null;
        }
    }
}
