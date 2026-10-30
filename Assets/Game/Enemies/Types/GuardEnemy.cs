using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class GuardEnemy : Enemy
    {
        [SerializeField] private Vector2 _preferredRange = new(5f, 8f);

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            var away = Position - targetPosition;
            away.y = 0f;
            var distance = away.magnitude;
            var ideal = (_preferredRange.x + _preferredRange.y) * 0.5f;
            if (distance >= _preferredRange.x && distance <= _preferredRange.y)
                return Position;
            var direction = distance > 1e-3f ? away / distance : -transform.forward;
            for (var turn = 0; turn < 5; turn++)
            {
                var angle = (turn + 1) / 2 * 30f * (turn % 2 == 0 ? 1f : -1f);
                var candidate = targetPosition + Quaternion.Euler(0f, angle, 0f) * direction * ideal;
                if (TrySampleNavMesh(candidate, 1.5f, out var point))
                    return point;
            }
            return distance > _preferredRange.y ? targetPosition : Position;
        }

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => ChaseDestination(targetPosition);

        protected override bool FaceTarget(float distance) => distance < _preferredRange.y + 4f;
    }
}
