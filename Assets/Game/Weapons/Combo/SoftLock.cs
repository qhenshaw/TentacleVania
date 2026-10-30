using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Weapons.Combo
{
    public static class SoftLock
    {
        private static readonly Collider[] Candidates = new Collider[32];

        public static bool InCone(Vector3 forward, Vector3 toTarget, float coneDegrees) =>
            Vector3.Angle(Flat(forward), Flat(toTarget)) <= coneDegrees * 0.5f;

        public static float Score(float distance, float angleDegrees) => distance * (1f + angleDegrees / 90f);

        public static bool TryPick(Vector3 origin, Vector3 forward, float range, float coneDegrees, int layers, Health self, out Vector3 direction)
        {
            direction = default;
            var count = Physics.OverlapSphereNonAlloc(origin, range, Candidates, layers, QueryTriggerInteraction.Collide);
            var best = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (!Hurtbox.TryGet(Candidates[i], out var hurtbox))
                    continue;
                var health = hurtbox.Health;
                if (health == self || !health.IsAlive || health.Team == Team.Creature)
                    continue;
                var toTarget = Flat(hurtbox.Collider.bounds.center - origin);
                var distance = toTarget.magnitude;
                if (distance > range || distance < 1e-3f || !InCone(forward, toTarget, coneDegrees))
                    continue;
                var score = Score(distance, Vector3.Angle(Flat(forward), toTarget));
                if (score < best)
                {
                    best = score;
                    direction = toTarget / distance;
                }
            }
            return best < float.MaxValue;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
