using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class PylonGuardEnemy : GuardEnemy
    {
        private static readonly List<PylonGuardEnemy> Guards = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Guards.Clear();

        public static bool AnyAliveNear(Vector3 position, float range)
        {
            var range2 = range * range;
            foreach (var guard in Guards)
                if (guard != null && guard.Health.IsAlive && guard.State != EnemyState.Consumed && (guard.Position - position).sqrMagnitude <= range2)
                    return true;
            return false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Guards.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Guards.Remove(this);
        }
    }
}
